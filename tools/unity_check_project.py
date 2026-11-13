#!/usr/bin/env python3
"""MSBuild input/provenance + declared assembly checks; no Unity launch or build targets."""
import argparse
import json
from pathlib import Path
from unity_build_inputs import digest, verify

ROOT = Path(__file__).resolve().parents[1]

def check(root):
    assets = root / 'UnityGame/Assets/Adam'
    errors = []
    try:
        provenance = json.loads((assets / 'Resources/core-provenance.json').read_text())
        errors.extend(verify(root, provenance))
        for path, key in [(assets / 'Plugins/BibleSurvivor.Core.dll', 'dll_sha256'),
                          (assets / 'Resources/genesis-catalog.json', 'catalog_sha256'),
                          (root / 'content/genesis/catalog.json', 'catalog_sha256')]:
            if digest(path) != provenance[key]:
                errors.append('Provenance mismatch: ' + str(path.relative_to(root)))
        expected = {'Adam.Unity': ['Unity.InputSystem'], 'Adam.Editor': ['Adam.Unity'],
                    'Adam.EditMode': ['Adam.Unity'], 'Adam.PlayMode': ['Adam.Unity', 'Unity.InputSystem']}
        seen = set()
        for path in assets.rglob('*.asmdef'):
            asm = json.loads(path.read_text()); name = asm['name']; seen.add(name)
            if name not in expected or asm.get('references') != expected[name]:
                errors.append('Unexpected declared assembly references: ' + name)
            if name in ('Adam.Editor', 'Adam.EditMode') and asm.get('includePlatforms') != ['Editor']:
                errors.append('Editor assembly platform mismatch: ' + name)
        if seen != set(expected):
            errors.append('Assembly allowlist incomplete')
        for path in (assets / 'Runtime').rglob('*.cs'):
            if 'using UnityEditor' in path.read_text():
                errors.append('Runtime imports editor: ' + str(path.relative_to(assets)))
    except (OSError, ValueError, KeyError, TypeError) as error:
        errors.append('Cannot verify build inputs: ' + str(error))
    return {'status': 'FAIL' if errors else 'PASS',
            'scope': 'fresh MSBuild evaluation set/import/config/property + recorded compiler input hashes; content/DLL hashes; declared asmdef allowlist',
            'errors': errors}

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', type=Path, default=ROOT)
    args = parser.parse_args()
    result = check(args.root.resolve())
    print(json.dumps(result, indent=2))
    raise SystemExit(1 if result['errors'] else 0)

if __name__ == '__main__':
    main()
