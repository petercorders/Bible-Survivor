#!/usr/bin/env python3
"""Run the font handoff's regeneration and invalid-input gates in an isolated copy.

Default is stdlib only and offline: archive-dependent cases are explicitly skipped.
--zip enables official-release comparisons; --full also runs checker mutants and Pillow rendering.
Neither mode launches Unity.
"""
import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--zip', type=Path, help='Existing official Galmuri archive, checked against the contract')
    parser.add_argument('--full', action='store_true', help='Include checker mutations and Pillow render comparison')
    args = parser.parse_args()
    archive = args.zip.resolve() if args.zip else None
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
    evidence = ROOT / 'evidence' / ('font-integration-' + stamp)
    evidence.mkdir(parents=True)
    with tempfile.TemporaryDirectory(prefix='bible-font-check-') as temporary:
        package = Path(temporary) / 'art-unity-font-01'
        shutil.copytree(ROOT / 'art/art-unity-font-01', package,
                        ignore=shutil.ignore_patterns('__pycache__', '*.pyc', 'results'))
        command = [sys.executable, '-I', '-B', 'scripts/verify_all.py', '--bitmap-dir',
                   str(ROOT / 'art/art-concept-03/third_party/galmuri')]
        if archive:
            command += ['--zip', str(archive)]
        if args.full:
            command += ['--mutations', '--render']
        result = subprocess.run(command, cwd=package, capture_output=True, text=True)
        (evidence / 'verify.log').write_text(result.stdout + result.stderr)
        generated = package / 'tests/results'
        if generated.exists():
            shutil.copytree(generated, evidence / 'results')
    summary = {'status': 'PASS' if result.returncode == 0 else 'FAIL', 'exit_code': result.returncode,
               'scope': 'offline font regeneration/contract/invalid inputs',
               'archive_verification': 'REQUESTED' if archive else 'NOT_RUN: no archive supplied',
               'checker_mutations_and_pillow': 'REQUESTED' if args.full else 'NOT_RUN',
               'unity': 'NOT_RUN', 'device': 'NOT_RUN'}
    (evidence / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n')
    print(json.dumps(summary, indent=2))
    print('Evidence: ' + str(evidence.relative_to(ROOT)))
    return 0 if result.returncode == 0 else 1

if __name__ == '__main__':
    raise SystemExit(main())
