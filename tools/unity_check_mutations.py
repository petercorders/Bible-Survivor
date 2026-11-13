#!/usr/bin/env python3
"""Run behavioral pause/checkpoint mutants in an isolated Unity project; preserve original."""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--editor', required=True)
parser.add_argument('--baseline', type=Path, required=True)
parser.add_argument('--out', type=Path, required=True)
args = parser.parse_args()
args.out.mkdir(parents=True, exist_ok=False)
baseline = ET.parse(args.baseline).getroot()
assert baseline.get('failed') == '0' and int(baseline.get('passed', '0')) > 0, 'Passing baseline required'
copy = ROOT / '.local' / ('unity-mutation-' + uuid.uuid4().hex)
copy.mkdir(parents=True)
for folder in ('Assets', 'Packages', 'ProjectSettings'):
    shutil.copytree(ROOT / 'UnityGame' / folder, copy / folder)
# APFS copy-on-write import cache; never hardlink mutable Unity cache files.
subprocess.run(['cp', '-cR', str(ROOT / 'UnityGame/Library'), str(copy / 'Library')], check=True)
path = copy / 'Assets/Adam/Runtime/AdamHost.cs'
original = path.read_text()
mutants = [
    ('pause-gate-removed', '!Paused && ', '', 'AdamHostTests.PauseFocusAndLongFrameCannotAccumulateDowntime'),
    ('periodic-checkpoint-removed', ' || sinceSave >= 5', '', 'AdamHostTests.AutosaveRollsBackLessThanFiveSimulationSeconds'),
]
results = []
for name, before, after, test in mutants:
    assert any(t.get('fullname') == test and t.get('result') == 'Passed' for t in baseline.iter('test-case')), test
    assert original.count(before) == 1, before
    path.write_text(original.replace(before, after))
    output = args.out / name; output.mkdir()
    xml = output / 'results.xml'; log = output / 'unity.log'
    cmd = [args.editor, '-batchmode', '-projectPath', str(copy), '-logFile', str(log), '-runTests', '-testPlatform', 'EditMode', '-testFilter', test, '-testResults', str(xml)]
    with (output / 'stdout.log').open('w') as stream:
        run = subprocess.run(cmd, stdout=stream, stderr=subprocess.STDOUT, timeout=600)
    tree = ET.parse(xml).getroot() if xml.exists() else None
    failed = [] if tree is None else [t.get('fullname') for t in tree.iter('test-case') if t.get('result') == 'Failed']
    compile_error = 'error CS' in log.read_text(errors='replace')
    killed = run.returncode != 0 and test in failed and not compile_error
    results.append({'mutation': name, 'detected': killed, 'exit_code': run.returncode, 'command': cmd, 'xml': str(xml), 'log': str(log), 'failed_tests': failed, 'compile_error': compile_error})
    path.write_text(original)
report = {'status': 'PASS' if all(x['detected'] for x in results) else 'FAIL', 'copy': str(copy), 'baseline': str(args.baseline), 'results': results}
(args.out / 'summary.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps(report, indent=2))
raise SystemExit(0 if report['status'] == 'PASS' else 1)
