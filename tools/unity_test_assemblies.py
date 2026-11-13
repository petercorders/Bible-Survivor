#!/usr/bin/env python3
"""Exercise the explicit Editor-only Sprite Editor exception without launching Unity."""
import json
from pathlib import Path
import shutil
import tempfile
from unity_assemblies import check_assemblies

SOURCE = Path(__file__).resolve().parents[1] / 'UnityGame/Assets/Adam'

def main():
    cases = [
        ('runtime Sprite Editor reference', 'Runtime/Adam.Unity.asmdef',
         lambda value: value['references'].append('Unity.2D.Sprite.Editor')),
        ('Editor assembly exposed to Player', 'Editor/Adam.Editor.asmdef',
         lambda value: value.update(includePlatforms=[])),
        ('unapproved Editor dependency', 'Editor/Adam.Editor.asmdef',
         lambda value: value['references'].append('Unapproved.Editor')),
    ]
    with tempfile.TemporaryDirectory(prefix='unity-boundary-') as temporary:
        base = Path(temporary) / 'baseline'
        for source in SOURCE.rglob('*.asmdef'):
            target = base / source.relative_to(SOURCE)
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, target)
        if check_assemblies(base):
            raise AssertionError('Current declared assemblies are not a valid baseline')
        for index, (name, relative, mutate) in enumerate(cases):
            root = Path(temporary) / str(index)
            shutil.copytree(base, root)
            path = root / relative
            value = json.loads(path.read_text()); mutate(value)
            path.write_text(json.dumps(value))
            if not check_assemblies(root):
                raise AssertionError('Accepted: ' + name)
        root = Path(temporary) / 'runtime-import'
        shutil.copytree(base, root)
        (root / 'Runtime/Bad.cs').write_text('using UnityEditor;\n')
        if not check_assemblies(root):
            raise AssertionError('Accepted Runtime editor import')
        root = Path(temporary) / 'duplicate'
        shutil.copytree(base, root)
        shutil.copy2(root / 'Editor/Adam.Editor.asmdef', root / 'Duplicate.asmdef')
        if not check_assemblies(root):
            raise AssertionError('Accepted duplicate assembly identity')
    print('PASS: valid Sprite Editor exception and 5 forbidden boundary inputs')

if __name__ == '__main__':
    main()
