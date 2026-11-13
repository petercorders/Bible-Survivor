#!/usr/bin/env python3
"""Run standalone core gates. Unity and device validation are deliberately not implied."""
from pathlib import Path
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from datetime import datetime, timezone

ROOT = Path(__file__).resolve().parents[1]

def main():
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
    evidence = ROOT / 'evidence' / ('r01-' + stamp)
    evidence.mkdir(parents=True)
    failures = []
    for file in (ROOT / 'src' / 'BibleSurvivor.Core').glob('*.cs'):
        if re.search(r'\bUnity(?:Engine|Editor)\b', file.read_text()):
            # Ignore explanatory comments, but reject actual using or type references.
            stripped = re.sub(r'//[^\n]*', '', file.read_text())
            if re.search(r'\bUnity(?:Engine|Editor)\b', stripped): failures.append(str(file.relative_to(ROOT)))
    tree = ET.parse(ROOT / 'src/BibleSurvivor.Core/BibleSurvivor.Core.csproj')
    if tree.findtext('.//TargetFramework') != 'netstandard2.1': failures.append('Core target changed')
    if tree.findall('.//PackageReference') or tree.findall('.//ProjectReference'): failures.append('Core acquired dependencies')
    if failures:
        print('FAIL core boundary:', failures, file=sys.stderr)
        return 1
    commands = [
        [sys.executable, 'tools/check_content.py'],
        ['bash', 'tools/dotnet.sh', 'run', '--project', 'tests/BibleSurvivor.Core.Tests', '--', '--results', str(evidence)],
    ]
    for weapon in ('bible', 'jawbone', 'sling'):
        commands.append(['bash','tools/dotnet.sh','run','--project','tools/BibleSurvivor.Sim','--',weapon])
    records = []
    for index, command in enumerate(commands):
        result = subprocess.run(command, cwd=ROOT, text=True, capture_output=True)
        (evidence / f'{index}.log').write_text(result.stdout + result.stderr)
        records.append({'command':command, 'exit_code':result.returncode})
        print(result.stdout.strip())
        if result.returncode:
            print(result.stderr, file=sys.stderr)
            break
    passed = len(records) == len(commands) and all(r['exit_code'] == 0 for r in records)
    if passed:
        tests = ET.parse(evidence / 'core-tests.xml').getroot()
        passed = int(tests.attrib['tests']) > 0 and int(tests.attrib['failures']) == 0
    summary = {'status':'PASS' if passed else 'FAIL', 'commands':records, 'unity':'NOT_RUN', 'mobile':'NOT_RUN'}
    (evidence / 'checks.json').write_text(json.dumps(summary, indent=2))
    print(f"{summary['status']}: standalone gates; evidence/{evidence.name}/checks.json")
    return 0 if passed else 1

if __name__ == '__main__':
    sys.exit(main())
