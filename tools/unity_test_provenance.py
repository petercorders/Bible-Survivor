#!/usr/bin/env python3
"""Behavioral provenance regressions in a disposable repo fixture; never touch live Unity assets."""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
from unity_sync_core import sync
from unity_build_inputs import digest

ROOT = Path(__file__).resolve().parents[1]

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--out', type=Path, required=True)
    args = parser.parse_args()
    args.out.mkdir(parents=True, exist_ok=False)
    results = []
    live_paths = list((ROOT / 'src/BibleSurvivor.Core').glob('*.cs')) + [
        ROOT / 'UnityGame/Assets/Adam/Plugins/BibleSurvivor.Core.dll',
        ROOT / 'UnityGame/Assets/Adam/Resources/core-provenance.json',
        ROOT / 'UnityGame/Assets/Adam/Resources/genesis-catalog.json',
        ROOT / 'UnityGame/Builds/Adam.app/Contents/MacOS/Adam Prototype',
        ROOT / 'UnityGame/Builds/Adam.app/Contents/Resources/Data/Managed/BibleSurvivor.Core.dll']
    live_before = {str(path.relative_to(ROOT)): digest(path) for path in live_paths if path.is_file()}
    (ROOT / '.local').mkdir(exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='unity-provenance-', dir=ROOT / '.local') as temp:
        root = Path(temp)
        core = root / 'src/BibleSurvivor.Core'
        shutil.copytree(ROOT / 'src/BibleSurvivor.Core', core, ignore=shutil.ignore_patterns('bin', 'obj'))
        shutil.copytree(ROOT / 'UnityGame/Assets/Adam', root / 'UnityGame/Assets/Adam')
        (root / 'tools').mkdir()
        shutil.copy2(ROOT / 'tools/dotnet.sh', root / 'tools/dotnet.sh')
        (root / 'content/genesis').mkdir(parents=True)
        shutil.copy2(ROOT / 'content/genesis/catalog.json', root / 'content/genesis/catalog.json')
        nested = core / 'Nested/ExistingRule.cs'; nested.parent.mkdir()
        nested.write_text('namespace ProvenanceFixture { public class ExistingRule {} }\n')
        props = core / 'RuleInputs.props'
        props.write_text('<Project><ItemGroup><AdditionalFiles Include="rule-config.txt" /></ItemGroup></Project>\n')
        config = core / 'rule-config.txt'; config.write_text('baseline\n')
        targets = core / 'RuleTargets.targets'
        targets.write_text('<Project><Target Name="FixtureCompileInput" BeforeTargets="CoreCompile"><ItemGroup><Compile Include="TargetOnlyRule.source" /></ItemGroup></Target></Project>\n')
        target_source = core / 'TargetOnlyRule.source'
        target_source.write_text('namespace ProvenanceFixture { public class TargetOnlyRule {} }\n')
        project = core / 'BibleSurvivor.Core.csproj'
        original = project.read_text()
        assert original.count('</Project>') == 1
        project.write_text(original.replace('</Project>', '<Import Project="RuleInputs.props" /><Import Project="RuleTargets.targets" /></Project>'))
        record = sync(root)
        assert any('Nested/ExistingRule.cs' in key for key in record['evaluation']['files_sha256'])
        assert any('TargetOnlyRule.source' in item.get('path', '') for item in record['build']['items']['Compile'])
        assert any('/obj/' in key for key in record['build']['files_sha256']), 'Generated compiler inputs must be recorded'
        (args.out / 'fixture-provenance.json').write_text(json.dumps(record, indent=2) + '\n')

        def check(name, should_pass, expected_text=None):
            command = ['python3', str(ROOT / 'tools/unity_check_project.py'), '--root', str(root)]
            run = subprocess.run(command, cwd=ROOT, capture_output=True, text=True)
            (args.out / (name + '.log')).write_text(run.stdout + run.stderr)
            data = json.loads(run.stdout)
            passed = (run.returncode == 0) == should_pass
            if expected_text:
                passed = passed and any(expected_text in error for error in data['errors'])
            results.append({'name': name, 'pass': passed, 'checker_exit_code': run.returncode,
                            'expected_checker_pass': should_pass, 'errors': data['errors']})
            if not passed:
                raise AssertionError(name + ': ' + json.dumps(data))

        def change(name, path, contents, expected_text):
            before = path.read_bytes() if path.exists() else None
            try:
                if contents is None: path.unlink()
                else:
                    path.parent.mkdir(parents=True, exist_ok=True); path.write_text(contents)
                check(name, False, expected_text)
            finally:
                if before is None: path.unlink(missing_ok=True)
                else: path.write_bytes(before)
            check(name + '-restored', True)

        check('baseline', True)
        change('added-root-source', core / 'NewUnsyncedRule.cs', 'namespace ProvenanceFixture { class NewUnsyncedRule {} }', 'Added build input:')
        change('deleted-source', core / 'Combat.cs', None, 'Removed build input:')
        change('added-nested-source', core / 'Nested/NewNestedRule.cs', 'namespace ProvenanceFixture { class NewNestedRule {} }', 'Added build input:')
        change('changed-nested-source', nested, 'namespace ProvenanceFixture { public class ExistingRule { public int Value; } }', 'Changed build input:')
        change('deleted-nested-source', nested, None, 'Removed build input:')
        change('changed-imported-props', props, props.read_text().replace('<Project>', '<Project><PropertyGroup><DefineConstants>REVIEW_CHANGED</DefineConstants></PropertyGroup>'), 'Changed build input:')
        change('changed-imported-targets', targets, targets.read_text().replace('BeforeTargets="CoreCompile"', 'BeforeTargets="CoreCompile" Condition="true"'), 'Changed build input:')
        change('missing-import', props, None, 'MSBuild failed')
        change('changed-additional-file', config, 'changed\n', 'Changed build input:')
        change('changed-target-only-compile-input', target_source, 'namespace ProvenanceFixture { public class TargetOnlyRule { public int Changed; } }', 'Changed recorded compiler input:')
        change('added-directory-props', root / 'Directory.Build.props', '<Project><PropertyGroup><DefineConstants>EXTRA</DefineConstants></PropertyGroup></Project>', 'Added build input:')
        change('added-directory-targets', root / 'Directory.Build.targets', '<Project><PropertyGroup><DefineConstants>EXTRA_TARGETS</DefineConstants></PropertyGroup></Project>', 'Added build input:')
        change('added-editorconfig', core / '.editorconfig', 'root = true\n[*.cs]\ndotnet_diagnostic.CS0168.severity = error\n', 'Added build input:')
        change('added-optional-globalconfig', root / '.globalconfig', 'is_global = true\ndotnet_diagnostic.CS0168.severity = error\n', 'Changed build input:')
        change('added-global-json', root / 'global.json', json.dumps({'sdk': {'version': record['evaluation']['properties']['NETCoreSdkVersion'], 'rollForward': 'disable'}}), 'Added build input:')
        change('changed-project-settings', project, project.read_text().replace('<Nullable>enable</Nullable>', '<Nullable>disable</Nullable>'), 'Changed build input:')
        provenance = root / 'UnityGame/Assets/Adam/Resources/core-provenance.json'
        legacy = {'sources_sha256': {}, 'dll_sha256': record['dll_sha256'], 'catalog_sha256': record['catalog_sha256']}
        change('legacy-provenance-rejected', provenance, json.dumps(legacy), 'Legacy provenance')
    live_after = {relative: digest(ROOT / relative) for relative in live_before}
    assert live_before == live_after, 'Live Core/Unity inputs or existing Player build changed during isolated tool tests'
    summary = {'status': 'PASS' if all(item['pass'] for item in results) else 'FAIL',
               'checks': len(results), 'results': results,
               'live_files_unchanged': live_before,
               'scope': 'isolated actual .NET restore/Rebuild + checker CLI; live Core/Unity files and Player/Editor untouched'}
    (args.out / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n')
    print(json.dumps({'status': summary['status'], 'checks': len(results)}, indent=2))

if __name__ == '__main__':
    main()
