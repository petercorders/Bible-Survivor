#!/usr/bin/env python3
"""Fresh-checkout artifact gate; does not launch Unity or certify SDK-generated inputs.

The strict local checker additionally verifies SDK imports and post-build inputs.
This gate compares evaluated repository input sets and the committed artifact hashes
without regenerating the shipped DLL/provenance and thereby hiding stale artifacts.
"""
import argparse
import json
from pathlib import Path
from unity_build_inputs import SCHEMA, digest, evaluate

ROOT = Path(__file__).resolve().parents[1]
PROPERTIES = ('TargetFramework', 'Configuration', 'Platform', 'PlatformTarget', 'DefineConstants',
              'LangVersion', 'Nullable', 'TreatWarningsAsErrors', 'AllowUnsafeBlocks', 'Optimize',
              'CheckForOverflowUnderflow', 'Deterministic', 'NETCoreSdkVersion')

def repository_input(key):
    return key.startswith('repo/') and not {'obj', 'bin'}.intersection(Path(key).parts)

def projected(evaluation):
    return {
        'files': {field: {key: value for key, value in evaluation[field].items() if repository_input(key)}
                  for field in ('files_sha256', 'imports_sha256', 'config_sha256')},
        'items': {kind: [entry for entry in entries if repository_input(entry.get('path', ''))]
                  for kind, entries in evaluation['items'].items()},
        'properties': {key: evaluation['properties'].get(key) for key in PROPERTIES},
    }

def check(root):
    errors = []
    assets = root / 'UnityGame/Assets/Adam'
    try:
        record = json.loads((assets / 'Resources/core-provenance.json').read_text())
        if record.get('schema_version') != SCHEMA:
            raise ValueError('Unsupported provenance schema; sync and validate locally')
        actual, _ = evaluate(root)
        for name, evaluation in [('recorded', record['evaluation']), ('current', actual)]:
            external = [key for field in ('files_sha256', 'imports_sha256', 'config_sha256')
                        for key, value in evaluation[field].items()
                        if key.startswith('ancestor') and value is not None]
            if external:
                errors.append(name + ' inputs depend on configuration outside the repository')
        expected, current = projected(record['evaluation']), projected(actual)
        for field in expected:
            if expected[field] != current[field]:
                errors.append('Repository build input mismatch: ' + field)
        for path, key in [(assets / 'Plugins/BibleSurvivor.Core.dll', 'dll_sha256'),
                          (assets / 'Resources/genesis-catalog.json', 'catalog_sha256'),
                          (root / 'content/genesis/catalog.json', 'catalog_sha256')]:
            if digest(path) != record[key]:
                errors.append('Artifact mismatch: ' + str(path.relative_to(root)))
    except (OSError, ValueError, KeyError, TypeError) as error:
        errors.append('Cannot verify portable inputs: ' + str(error))
    return {'status': 'FAIL' if errors else 'PASS', 'errors': errors,
            'scope': 'evaluated repository input sets/properties and committed artifact hashes',
            'unity_execution': 'NOT_RUN', 'sdk_generated_input_equivalence': 'NOT_RUN'}

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=ROOT)
    args = parser.parse_args()
    result = check(args.root.resolve())
    print(json.dumps(result, indent=2))
    raise SystemExit(1 if result['errors'] else 0)
