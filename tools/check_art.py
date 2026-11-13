#!/usr/bin/env python3
"""Verify reviewed offline art in a disposable copy. Default requires only Python stdlib."""
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
    parser.add_argument('--full', action='store_true', help='Also regenerate and rebuild review media; requires art requirements')
    args = parser.parse_args()
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
    evidence = ROOT / 'evidence' / ('art-integration-' + stamp)
    evidence.mkdir(parents=True)
    results = []
    with tempfile.TemporaryDirectory(prefix='bible-art-check-') as temporary:
        art = Path(temporary) / 'art'
        shutil.copytree(ROOT / 'art', art, ignore=shutil.ignore_patterns('__pycache__', '*.pyc', 'results'))
        package = art / 'art-concept-03'
        catalog = ROOT / 'content/genesis/catalog.json'
        commands = [('catalog + art contract', ['scripts/check_art03.py', '--catalog', str(catalog), '--quiet'])]
        if args.full:
            # Compare the committed export before verify_all's build can overwrite it.
            commands += [('committed export regeneration', ['scripts/regen_check.py']),
                         ('full offline verification', ['scripts/verify_all.py', '--catalog', str(catalog), '--review'])]
        else:
            commands += [('41 invalid-input cases', ['tests/test_check_art03.py']),
                         ('5 checker regressions', ['tests/checker_mutations_iter02.py'])]
        for index, (name, arguments) in enumerate(commands):
            result = subprocess.run([sys.executable, '-I', '-B', *arguments], cwd=package,
                                    capture_output=True, text=True)
            (evidence / f'{index}.log').write_text(result.stdout + result.stderr)
            results.append({'name': name, 'exit_code': result.returncode})
            print(f'{name}: exit {result.returncode}', flush=True)
            if result.returncode:
                break
        generated = package / 'tests/results'
        if generated.exists():
            shutil.copytree(generated, evidence / 'results')
    passed = len(results) == len(commands) and all(result['exit_code'] == 0 for result in results)
    summary = {'status': 'PASS' if passed else 'FAIL', 'checks': results,
               'scope': 'offline art integration only', 'unity': 'NOT_RUN', 'device': 'NOT_RUN'}
    (evidence / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n')
    print(summary['status'] + ': ' + str(evidence.relative_to(ROOT)))
    return 0 if passed else 1

if __name__ == '__main__':
    raise SystemExit(main())
