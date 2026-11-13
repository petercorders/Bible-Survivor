"""MSBuild input manifests shared by sync/check; evaluation never runs build targets.

Evaluation records the current item set, imported project text and compiler properties.
Sync additionally records post-Rebuild compiler/reference/generated inputs. Check compares
the fresh evaluated set AND hashes every recorded execution input, failing closed on v1.
This describes standard SDK/MSBuild declared inputs, not arbitrary custom-task file IO.
"""
import hashlib
import json
from pathlib import Path
import subprocess

SCHEMA = 2
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
                    files[key] = None  # SDK optional config probe: adding the file must invalidate.
                    entries.append({'path': key, 'metadata': {}})
                    continue
                raise ValueError('Missing declared input: ' + source.name)
            key = paths.key(source)
            files[key] = digest(source)
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
            imports[paths.key(candidate)] = digest(Path(candidate))
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
    for key, expected in provenance['build']['files_sha256'].items():
        path = paths.resolve(key)
        if expected is None:
            if path.is_file():
                errors.append('Added optional compiler input: ' + key)
        elif not path.is_file():
            errors.append('Missing recorded compiler input: ' + key)
        elif digest(path) != expected:
            errors.append('Changed recorded compiler input: ' + key)
    return errors
