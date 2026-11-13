#!/usr/bin/env python3
"""Build original core, record evaluated/executed MSBuild inputs, sync binary + catalog."""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
from unity_build_inputs import SCHEMA, digest, evaluate, query, item_manifest, compare_inputs

ROOT = Path(__file__).resolve().parents[1]

def sync(root):
    # Resolve SDK-generated imports before taking the pre-build evaluation snapshot.
    subprocess.run(['bash', 'tools/dotnet.sh', 'restore', 'src/BibleSurvivor.Core'], cwd=root, check=True,
                   stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    before, paths = evaluate(root)
    executed = query(root, build=True)
    build = item_manifest(executed, paths)
    after, _ = evaluate(root)
    changes = compare_inputs(before, after)
    if changes:
        raise ValueError('Core inputs changed during build; retry: ' + '; '.join(changes))
    target = root / 'UnityGame/Assets/Adam'
    dll = Path(executed['Properties']['TargetPath'])
    catalog = root / 'content/genesis/catalog.json'
    record = {'schema_version': SCHEMA, 'evaluation': after, 'build': build,
              'dll_sha256': digest(dll), 'catalog_sha256': digest(catalog),
              'method': 'Release Rebuild; evaluated item/import/property set + post-build compiler/reference/generated inputs'}
    (target / 'Plugins').mkdir(parents=True, exist_ok=True)
    (target / 'Resources').mkdir(parents=True, exist_ok=True)
    shutil.copy2(dll, target / 'Plugins/BibleSurvivor.Core.dll')
    shutil.copy2(catalog, target / 'Resources/genesis-catalog.json')
    (target / 'Resources/core-provenance.json').write_text(json.dumps(record, indent=2) + '\n')
    return record

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', type=Path, default=ROOT, help='Explicit repository fixture root for isolated verification')
    args = parser.parse_args()
    record = sync(args.root.resolve())
    print(json.dumps({'status': 'PASS', 'schema_version': SCHEMA,
                      'evaluated_files': len(record['evaluation']['files_sha256']),
                      'compiler_files': len(record['build']['files_sha256']),
                      'dll_sha256': record['dll_sha256']}, indent=2))

if __name__ == '__main__':
    main()
