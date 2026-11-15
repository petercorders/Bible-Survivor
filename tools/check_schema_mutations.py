#!/usr/bin/env python3
"""Reproduce the former typed-before-version read and concrete schema/archive regressions.

Each mutation runs only in a disposable source copy. A compile error or missing test XML
never counts as a detected behavior regression.
"""
from pathlib import Path
from datetime import datetime, timezone
import json
import os
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
CASES = [
    ('old_typed_run_before_header', [
        ('SaveState.cs', '\n            sourceVersion = SaveFormat.Probe(bytes);', '\n            sourceVersion = SaveFormat.CurrentVersion;'),
        ('SaveState.cs', 'var sourceVersion = SaveFormat.Probe(bytes); // Header before typed Run: future bodies must never trigger backup recovery.', 'var sourceVersion = SaveFormat.CurrentVersion; // Original typed read occurs before version validation.'),
    ], 'Future schema header precedes incompatible typed Run'),
    ('old_schema1_writer', [
        ('SaveState.cs', 'candidate.Revision = checked(revision + 1);', 'candidate.SchemaVersion = 1; candidate.Revision = checked(revision + 1);'),
    ], 'Schema2 writer and memory migration'),
    ('upgrade_replaces_without_archive', [
        ('SaveState.cs', 'if (sourceVersion == 1) SaveMigrationArchive.Ensure(Path, beforeMigrationArchivePublish);', '/* original writer replaced primary without migration archive */'),
    ], 'Schema2 writer and memory migration'),
    ('archive_publication_failure_ignored', [
        ('SaveMigrationArchive.cs', 'beforePublish?.Invoke();', '/* archive write/publication fault ignored */'),
    ], 'Schema archive publication failure'),
    ('existing_archive_trusted_without_verification', [
        ('SaveMigrationArchive.cs', 'if (Directory.Exists(destination)) { VerifyPublishedArchive(destination); return; }', 'if (Directory.Exists(destination)) return;'),
    ], 'Schema archive path failure and incomplete'),
    ('unsupported_schema2_content_recovers_older_backup', [
        ('SaveFormat.cs', 'if (version == CurrentVersion) throw new UnsupportedSaveVersionException();', 'if (version == CurrentVersion) throw new InvalidDataException("unsupported treated as corrupt");'),
        ('SaveFormat.cs', 'if (version == CurrentVersion)\n                {', 'if (version < 0)\n                {'),
    ], 'Schema2 preparatory reader preserves unsupported boss content'),
    ('old_shallow_schema2_content_guard', [
        ('SaveFormat.cs', 'if (version == CurrentVersion) CheckSupportedContent(root, SupportedContent);', '/* Original preparatory reader only checked envelope and direct Run fields. */'),
    ], ['Schema2 nested Profile unknown content', 'Schema2 nested World unknown content']),
    ('future_world_version_recovers_legacy_backup', [
        ('SaveFormat.cs', 'CheckWorldVersion(members.SingleOrDefault(x => x.Name == "World"));', '/* Original reader delegated future World version to typed corruption recovery. */'),
    ], ['Future World version preserves Gold19 primary', 'Future World version and overflow precede incompatible typed bodies']),
    ('archive_compared_to_later_corrupt_primary', [
        ('SaveMigrationArchive.cs', 'VerifyPublishedArchive(destination); return;', 'Verify(destination, files); return;'),
    ], 'Schema2 corrupt primary can recover its schema1 backup'),
    ('future_integer_overflow_recovers_backup', [
        ('SaveFormat.cs', 'if (!int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var version) || version > CurrentVersion)', 'if (!int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var version)) throw new InvalidDataException("version overflow");\n            if (version > CurrentVersion)'),
    ], 'Future schema integer overflow'),
]


def main():
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
    output = ROOT / 'evidence' / ('r03-schema-mutations-' + stamp); output.mkdir(parents=True)
    sdk = os.environ.get('BIBLE_SURVIVOR_DOTNET') or shutil.which('dotnet') or str(Path.home() / '.local/share/bible-survivor/dotnet/dotnet')
    env = dict(os.environ, DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1', DOTNET_GENERATE_ASPNET_CERTIFICATE='false', DOTNET_CLI_HOME=str(ROOT / '.local/dotnet-home'))
    results = []
    with tempfile.TemporaryDirectory(prefix='bible-schema-mutants-') as directory:
        temp = Path(directory)
        for name in ('src', 'tests', 'content', 'tools/BibleSurvivor.Sim'):
            shutil.copytree(ROOT / name, temp / name, ignore=shutil.ignore_patterns('bin', 'obj'))
        shutil.copy2(ROOT / 'global.json', temp / 'global.json')

        def run(name):
            target = output / name
            command = [sdk, 'run', '--project', str(temp / 'tests/BibleSurvivor.Core.Tests'), '--', '--results', str(target)]
            result = subprocess.run(command, cwd=temp, text=True, capture_output=True, env=env)
            (output / (name + '.log')).write_text(result.stdout + result.stderr)
            xml = target / 'core-tests.xml'
            failed = []
            if xml.exists(): failed = [n.attrib['name'] for n in ET.parse(xml).getroot().findall('testcase') if n.find('failure') is not None]
            return {'command': command, 'exit_code': result.returncode, 'failed_tests': failed, 'xml_present': xml.exists()}

        baseline = run('baseline')
        if baseline['exit_code'] or not baseline['xml_present'] or baseline['failed_tests']:
            print('FAIL: schema mutation baseline', file=sys.stderr); return 1
        for name, edits, required in CASES:
            originals = {}
            try:
                for file, before, after in edits:
                    path = temp / 'src/BibleSurvivor.Core' / file
                    original = path.read_text()
                    if original.count(before) != 1: raise RuntimeError(f'{name}: source mutation anchor changed')
                    originals.setdefault(path, original)
                    path.write_text(original.replace(before, after))
                result = run(name)
                result['name'] = name
                required_names = [required] if isinstance(required, str) else required
                result['detected'] = result['exit_code'] == 1 and result['xml_present'] and all(any(needed in t for t in result['failed_tests']) for needed in required_names)
                results.append(result)
                print(('PASS detected ' if result['detected'] else 'FAIL undetected ') + name)
            finally:
                for path, original in originals.items(): path.write_text(original)
        restored = run('restored')
    passed = all(r['detected'] for r in results) and len(results) == len(CASES) and restored['exit_code'] == 0 and restored['xml_present'] and not restored['failed_tests']
    summary = {'status': 'PASS' if passed else 'FAIL', 'baseline': baseline, 'regressions': results, 'restored': restored, 'unity': 'NOT_RUN', 'mobile': 'NOT_RUN'}
    (output / 'summary.json').write_text(json.dumps(summary, indent=2))
    print(f"{summary['status']}: evidence/{output.name}/summary.json")
    return 0 if passed else 1


if __name__ == '__main__': sys.exit(main())
