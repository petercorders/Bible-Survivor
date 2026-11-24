"""MSBuild input manifests shared by sync/check; evaluation never runs build targets.

Evaluation records the current item set, imported project text and compiler properties.
Sync additionally records post-Rebuild compiler/reference/generated inputs. Check compares
the fresh evaluated set AND hashes every recorded execution input, failing closed on v1.
This describes standard SDK/MSBuild declared inputs, not arbitrary custom-task file IO.
"""
import hashlib
import json
import os
import re
from pathlib import Path
import subprocess
import tempfile

SCHEMA = 3
PROJECT = 'src/BibleSurvivor.Core/BibleSurvivor.Core.csproj'
ITEMS = ('Compile', 'AdditionalFiles', 'Analyzer', 'EmbeddedResource', 'Content', 'None',
         'ProjectReference', 'Reference', 'ReferencePath', 'ReferencePathWithRefAssemblies',
         'EditorConfigFiles', 'GlobalAnalyzerConfigFiles', 'CustomAdditionalCompileInputs')
PROPERTIES = ('TargetFramework', 'Configuration', 'Platform', 'PlatformTarget', 'DefineConstants',
              'LangVersion', 'Nullable', 'TreatWarningsAsErrors', 'AllowUnsafeBlocks', 'Optimize',
              'CheckForOverflowUnderflow', 'Deterministic', 'MSBuildVersion', 'NETCoreSdkVersion',
              'MSBuildSDKsPath', 'NuGetPackageRoot', 'TargetPath')

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def run_msbuild(root, *arguments):
    command = ['bash', 'tools/dotnet.sh', 'msbuild', PROJECT, '-p:Configuration=Release', *arguments]
    result = subprocess.run(command, cwd=root, capture_output=True, text=True)
    if result.returncode:
        raise ValueError(f'MSBuild failed (exit {result.returncode}); cannot verify inputs')
    return result.stdout

def query(root, build=False):
    arguments = ['-getItem:' + ','.join(ITEMS), '-getProperty:' + ','.join(PROPERTIES)]
    if build:
        arguments[0] += ',CoreCompileCache'
        arguments[1] += ',CoreCompileCacheIgnoreCase'
        arguments.append('-target:Rebuild')
    raw = run_msbuild(root, *arguments)
    # Build diagnostics may precede MSBuild's final structured query output.
    for index, character in enumerate(raw):
        if character == '{':
            try:
                data = json.loads(raw[index:])
                if 'Properties' in data and 'Items' in data:
                    return data
            except json.JSONDecodeError:
                pass
    raise ValueError('MSBuild returned no valid input manifest')

class Paths:
    def __init__(self, root, properties):
        self.roots = {'repo': root.resolve()}
        self.ancestors = {f'ancestor{index}': path for index, path in enumerate(root.resolve().parents, 1)}
        sdk = properties.get('MSBuildSDKsPath')
        if sdk:
            self.roots['dotnet'] = Path(sdk).resolve().parents[2]
        packages = properties.get('NuGetPackageRoot')
        if packages:
            self.roots['nuget'] = Path(packages).resolve()
        # Prefer the most specific root, including repo-local SDK/package installations.
        self.ordered = sorted(self.roots.items(), key=lambda pair: len(str(pair[1])), reverse=True)

    def key(self, path):
        path = Path(path).resolve()
        for name, base in self.ordered:
            if path.is_relative_to(base):
                return name + '/' + path.relative_to(base).as_posix()
        for name, base in self.ancestors.items():
            if path.parent == base:
                return name + '/' + path.name
        raise ValueError('Input outside repository/SDK/package roots: ' + path.name)

    def resolve(self, key):
        name, relative = key.split('/', 1)
        roots = {**self.roots, **self.ancestors}
        if name not in roots:
            raise ValueError('Unknown input root: ' + name)
        path = (roots[name] / relative).resolve()
        if not path.is_relative_to(roots[name]):
            raise ValueError('Input path escapes its root')
        return path

    def normalize(self, text):
        for name, base in self.ordered:
            text = text.replace(str(base), '${' + name + '}')
        return text

CORE_OBJ = Path(PROJECT).parent / 'obj'
RELEASE_OBJ = CORE_OBJ / 'Release/netstandard2.1'
CACHE_KEY = 'repo/' + str(RELEASE_OBJ / 'BibleSurvivor.Core.csproj.CoreCompileInputs.cache')
GENERATED_TEXT = {CORE_OBJ / 'BibleSurvivor.Core.csproj.nuget.g.props',
                  RELEASE_OBJ / 'BibleSurvivor.Core.GeneratedMSBuildEditorConfig.editorconfig'}


def input_digest(path, paths):
    root = paths.roots['repo']
    if path.is_relative_to(root) and path.relative_to(root) in GENERATED_TEXT:
        # Escape literal token prefixes before replacing checkout-specific directories.
        data = path.read_bytes().replace(b'%', b'%25')
        for name, base in paths.ordered:
            prefix = str(base).encode().replace(b'%', b'%25') + b'/'
            data = data.replace(prefix, b'%' + name.encode() + b'%/')
        return hashlib.sha256(data).hexdigest()
    return digest(path)


def cache_recipe(executed, paths):
    identities = [paths.normalize(item['Identity']) for item in executed['Items']['CoreCompileCache']]
    if not identities:
        raise ValueError('SDK returned no compiler cache inputs')
    return {'items': identities, 'ignore_case': executed['Properties']['CoreCompileCacheIgnoreCase'] or 'true'}


def recipe_digest(recipe):
    return hashlib.sha256(json.dumps(recipe, sort_keys=True).encode()).hexdigest()


def expected_cache_digest(root, recipe, paths):
    # Use the SDK implementation (including its Unicode casing and item separator).
    with tempfile.TemporaryDirectory() as scratch:
        path = Path(scratch) / 'items.txt'
        items = []
        for item in recipe['items']:
            for name, base in paths.ordered:
                item = item.replace('${' + name + '}', str(base))
            items.append(item)
        path.write_text('\n'.join(items) + '\n')
        project = Path(__file__).with_name('unity_cache_hash.proj')
        command = ['bash', 'tools/dotnet.sh', 'msbuild', str(project), '-target:Compute',
                   '-getProperty:CacheDigest', '-p:CacheItemsFile=' + str(path),
                   '-p:CacheIgnoreCase=' + recipe['ignore_case']]
        result = subprocess.run(command, cwd=root, capture_output=True, text=True)
        if result.returncode:
            raise ValueError(f'SDK cache verification failed (exit {result.returncode})')
        value = result.stdout.strip()
        if not re.fullmatch('[0-9a-f]{64}', value):
            raise ValueError('SDK Hash task returned an invalid digest')
        # WriteLinesToFile uses the build host's native newline.
        return hashlib.sha256((value + os.linesep).encode()).hexdigest()


def item_manifest(data, paths):
    items = {}
    files = {}
    for kind in ITEMS:
        entries = []
        for item in data['Items'].get(kind, []):
            path = item.get('FullPath')
            if not path:
                raise ValueError('Missing MSBuild item FullPath: ' + kind)
            source = Path(path)
            # Bare framework Reference identities are not file inputs until resolution.
            if not source.is_file():
                if kind == 'Reference':
                    entries.append({'identity': item['Identity']})
                    continue
                if kind in ('GlobalAnalyzerConfigFiles', 'EditorConfigFiles'):
                    key = paths.key(source)
                    if key.startswith('ancestor'):
                        continue  # Absent outer probes vary with checkout depth. Present files are recorded below.
                    files[key] = None  # SDK optional config probe: adding the file must invalidate.
                    entries.append({'path': key, 'metadata': {}})
                    continue
                raise ValueError('Missing declared input: ' + source.name)
            key = paths.key(source)
            files[key] = input_digest(source, paths)
            metadata = {name: paths.normalize(item[name]) for name in
                        ('Link', 'LogicalName', 'TargetPath', 'Aliases', 'EmbedInteropTypes') if item.get(name)}
            entries.append({'path': key, 'metadata': metadata})
        items[kind] = sorted(entries, key=lambda entry: json.dumps(entry, sort_keys=True))
    return {'items': items, 'files_sha256': dict(sorted(files.items()))}

def evaluate(root):
    root = root.resolve()
    data = query(root)
    paths = Paths(root, data['Properties'])
    preprocessed = run_msbuild(root, '-preprocess')
    imports = {paths.key(root / PROJECT): digest(root / PROJECT)}
    # /preprocess includes an absolute file marker for each resolved import, including
    # SDK imports which are not reliably all present in MSBuildAllProjects.
    for line in preprocessed.splitlines():
        candidate = line.strip()
        if candidate.startswith('/') and Path(candidate).is_file():
            imports[paths.key(candidate)] = input_digest(Path(candidate), paths)
    configs = {}
    for directory in [root, *(root / PROJECT).parent.parents]:
        if not directory.is_relative_to(root):
            continue
        for name in ('global.json', 'NuGet.Config', 'nuget.config', 'Directory.Packages.props', '.editorconfig'):
            path = directory / name
            if path.is_file():
                configs[paths.key(path)] = digest(path)
    for path in (root / PROJECT).parent.rglob('.editorconfig'):
        if not {'bin', 'obj'}.intersection(path.relative_to(root).parts):
            configs[paths.key(path)] = digest(path)
    result = item_manifest(data, paths)
    result.update({
        'properties': {key: paths.normalize(value) for key, value in data['Properties'].items()},
        'imports_sha256': dict(sorted(imports.items())),
        'config_sha256': dict(sorted(configs.items())),
        'preprocessed_sha256': hashlib.sha256(paths.normalize(preprocessed).encode()).hexdigest(),
    })
    return result, paths

def compare_inputs(expected, actual):
    errors = []
    for field in ('files_sha256', 'imports_sha256', 'config_sha256'):
        before, after = expected.get(field, {}), actual.get(field, {})
        for key in sorted(after.keys() - before.keys()):
            errors.append('Added build input: ' + key)
        for key in sorted(before.keys() - after.keys()):
            errors.append('Removed build input: ' + key)
        for key in sorted(before.keys() & after.keys()):
            if before[key] != after[key]:
                errors.append('Changed build input: ' + key)
    for field in ('items', 'properties', 'preprocessed_sha256'):
        if expected.get(field) != actual.get(field):
            errors.append('Changed MSBuild evaluation: ' + field)
    return errors

def verify(root, provenance):
    if provenance.get('schema_version') != SCHEMA:
        return ['Legacy provenance has no evaluated input set; re-sync after Unity/UI handoff']
    actual, paths = evaluate(root)
    errors = compare_inputs(provenance['evaluation'], actual)
    recipe = provenance['build']['compiler_cache']
    if CACHE_KEY not in provenance['build']['files_sha256']:
        raise ValueError('Missing compiler cache entry')
    for key, expected in provenance['build']['files_sha256'].items():
        path = paths.resolve(key)
        if key == CACHE_KEY:
            if expected != recipe_digest(recipe):
                raise ValueError('Invalid compiler cache record')
            expected = expected_cache_digest(root, recipe, paths)
        if expected is None:
            if path.is_file():
                errors.append('Added optional compiler input: ' + key)
        elif not path.is_file():
            errors.append('Missing recorded compiler input: ' + key)
        elif input_digest(path, paths) != expected:
            errors.append('Changed recorded compiler input: ' + key)
    return errors
