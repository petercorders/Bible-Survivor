"""Narrow declared asmdef boundary shared by local and fresh-checkout gates."""
import json

EXPECTED = {
    'Adam.Unity': ['Unity.InputSystem'],
    'Adam.Editor': ['Adam.Unity', 'Unity.2D.Sprite.Editor'],
    'Adam.EditMode': ['Adam.Unity'],
    'Adam.PlayMode': ['Adam.Unity', 'Unity.InputSystem'],
}

def check_assemblies(assets):
    errors, seen = [], set()
    for path in assets.rglob('*.asmdef'):
        asm = json.loads(path.read_text())
        name = asm['name']
        if name in seen:
            errors.append('Duplicate declared assembly: ' + name)
        seen.add(name)
        if name not in EXPECTED or asm.get('references') != EXPECTED[name]:
            errors.append('Unexpected declared assembly references: ' + name)
        if name in ('Adam.Editor', 'Adam.EditMode') and asm.get('includePlatforms') != ['Editor']:
            errors.append('Editor assembly platform mismatch: ' + name)
    if seen != set(EXPECTED):
        errors.append('Assembly allowlist incomplete')
    for path in (assets / 'Runtime').rglob('*.cs'):
        if 'using UnityEditor' in path.read_text():
            errors.append('Runtime imports editor: ' + str(path.relative_to(assets)))
    return errors
