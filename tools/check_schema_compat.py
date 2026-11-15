#!/usr/bin/env python3
"""Execute historical schema-1 and current Core binaries in isolated processes.

Requires the pinned historical commit locally; never fetches or modifies the user's saves.
This gate is standalone Core compatibility, not Unity/IL2CPP verification.
"""
from pathlib import Path
import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
BASELINE = '2edd86eda18d6552e5a47408d0962ba238deea80'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--baseline-ref', default=BASELINE)
    args = parser.parse_args()
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
    output = ROOT / 'evidence' / ('r03-schema-compat-' + stamp)
    output.mkdir(parents=True)
    sdk = os.environ.get('BIBLE_SURVIVOR_DOTNET') or shutil.which('dotnet') or str(Path.home() / '.local/share/bible-survivor/dotnet/dotnet')
    env = dict(os.environ, DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1', DOTNET_GENERATE_ASPNET_CERTIFICATE='false', DOTNET_CLI_HOME=str(ROOT / '.local/dotnet-home'))
    records = []

    def command(parts, cwd):
        result = subprocess.run(parts, cwd=cwd, text=True, capture_output=True, env=env)
        (output / f'{len(records):02d}.log').write_text(result.stdout + result.stderr)
        records.append({'command': parts, 'exit_code': result.returncode})
        if result.returncode:
            raise RuntimeError(f'Command failed ({result.returncode}): {parts}; see evidence log')
        return result.stdout

    try:
        resolved = command(['git', 'rev-parse', '--verify', args.baseline_ref + '^{commit}'], ROOT).strip()
        with tempfile.TemporaryDirectory(prefix='bible-schema-compat-') as temporary:
            temp = Path(temporary)
            old = temp / 'old'; new = temp / 'new'
            for target in (old, new):
                (target / 'src/BibleSurvivor.Core').mkdir(parents=True)
                shutil.copy2(ROOT / 'global.json', target / 'global.json')
            paths = command(['git', 'ls-tree', '-r', '--name-only', resolved, '--', 'src/BibleSurvivor.Core'], ROOT).splitlines()
            for name in paths:
                if name.endswith(('.cs', '.csproj')):
                    content = command(['git', 'show', resolved + ':' + name], ROOT)
                    (old / name).write_text(content)
            for source in (ROOT / 'src/BibleSurvivor.Core').glob('*'):
                if source.suffix in ('.cs', '.csproj'): shutil.copy2(source, new / 'src/BibleSurvivor.Core' / source.name)
            fixture_hashes = {}
            for target in (old, new):
                probe = target / 'Probe'; probe.mkdir()
                shutil.copy2(ROOT / 'tools/fixtures/schema_compat_probe.cs.txt', probe / 'Program.cs')
                shutil.copy2(ROOT / 'tools/BibleSurvivor.Sim/CatalogReader.cs', probe / 'CatalogReader.cs')
                (probe / 'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup><ItemGroup><ProjectReference Include="../src/BibleSurvivor.Core/BibleSurvivor.Core.csproj" /></ItemGroup></Project>')
                command([sdk, 'build', str(probe)], target)
                fixtures = output / (target.name + '-fixtures')
                command([sdk, str(probe / 'bin/Debug/net10.0/Probe.dll'), 'generate', str(ROOT / 'content/genesis/catalog.json'), str(fixtures)], ROOT)
                fixture_hashes[target.name] = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in fixtures.glob('*.json')}
            names = sorted(fixture_hashes['old'])
            if len(names) != 30 or names != sorted(fixture_hashes['new']): raise RuntimeError('Fixture set mismatch')
            for name in names:
                before = json.loads((output / 'old-fixtures' / name).read_text()); after = json.loads((output / 'new-fixtures' / name).read_text())
                if before['SchemaVersion'] != 1 or after['SchemaVersion'] != 2: raise RuntimeError('Writer version mismatch')
                before['SchemaVersion'] = 2
                if before != after: raise RuntimeError('R1/R2 state/fingerprint drift: ' + name)
            command([sdk, str(new / 'Probe/bin/Debug/net10.0/Probe.dll'), 'migrate', str(output / 'old-fixtures')], ROOT)
            command([sdk, str(old / 'Probe/bin/Debug/net10.0/Probe.dll'), 'old-reject', str(output / 'new-fixtures'), str(output / 'old-fixtures')], ROOT)
            summary = {'status': 'PASS', 'baseline_commit': resolved, 'records_equal_except_schema': len(names), 'legacy_migrations': 30, 'old_reader_refusals': 30, 'fixture_sha256': fixture_hashes, 'commands': records, 'unity': 'NOT_RUN', 'mobile': 'NOT_RUN'}
    except (OSError, RuntimeError, ValueError) as error:
        summary = {'status': 'FAIL', 'error': str(error), 'commands': records}
    (output / 'summary.json').write_text(json.dumps(summary, indent=2))
    print(f"{summary['status']}: schema compatibility; evidence/{output.name}/summary.json")
    return 0 if summary['status'] == 'PASS' else 1


if __name__ == '__main__': sys.exit(main())
