#!/usr/bin/env python3
"""Isolated tests of art preparation; source art and the live Unity project are never mutated."""
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('unity_prepare_art', ROOT / 'tools/unity_prepare_art.py')
prepare = importlib.util.module_from_spec(spec)
spec.loader.exec_module(prepare)

class ArtPreparationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.source = self.root / 'art/art-concept-03'
        shutil.copytree(ROOT / 'art/art-concept-03', self.source)
        self.dest = self.root / 'UnityGame/Assets/Adam/Resources/Art03'
        self.old = prepare.ROOT, prepare.SOURCE, prepare.DEST
        prepare.ROOT, prepare.SOURCE, prepare.DEST = self.root, self.source, self.dest
    def tearDown(self):
        prepare.ROOT, prepare.SOURCE, prepare.DEST = self.old
        self.temp.cleanup()
    def test_reviewed_bytes_repeatable_and_existing_guid_meta_retained(self):
        prepare.prepare()
        png = self.dest / 'chr_player_layer-base_idle_32.png'
        meta = png.with_name(png.name + '.meta')
        meta.write_text('guid: fixed-test-guid\n')
        before = {p.name: p.read_bytes() for p in self.dest.iterdir()}
        prepare.prepare()
        self.assertEqual(before, {p.name:p.read_bytes() for p in self.dest.iterdir()})
        self.assertEqual(79, len(list(self.dest.glob('*.png'))))
        self.assertEqual([], list(self.dest.glob('*.ttf')))
        sha = hashlib.sha256((self.dest/'index.json').read_bytes()).hexdigest()
        self.assertIn(sha, (ROOT/'UnityGame/Assets/Adam/Runtime/AdamArt.cs').read_text())
    def test_corrupt_png_rejected_before_any_destination_writes(self):
        p=self.source/'export/png/chr_player_layer-base_idle_32.png';p.write_bytes(p.read_bytes()+b'corrupt')
        with self.assertRaises((ValueError,AssertionError)):prepare.prepare()
        self.assertFalse(self.dest.exists())
    def test_unreviewed_manifest_rejected_before_writes(self):
        p=self.source/'export/manifest_v3.json';data=json.loads(p.read_text());data['ppu']=16;p.write_text(json.dumps(data))
        with self.assertRaises((ValueError,AssertionError)):prepare.prepare()
        self.assertFalse(self.dest.exists())
    def test_related_contract_hash_rejected(self):
        p=self.source/'src/contract_v3.json';p.write_bytes(p.read_bytes()+b' ')
        with self.assertRaises((ValueError,AssertionError)):prepare.prepare()
        self.assertFalse(self.dest.exists())

font_spec=importlib.util.spec_from_file_location('unity_prepare_fonts',ROOT/'tools/unity_prepare_fonts.py')
fonts=importlib.util.module_from_spec(font_spec)
font_spec.loader.exec_module(fonts)

class FontPreparationTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.root=Path(self.temp.name)
        self.source=self.root/'art/art-unity-font-01';self.dest=self.root/'UnityGame/Assets/Adam/Resources/ArtFonts'
        shutil.copytree(ROOT/'art/art-unity-font-01',self.source)
        self.old=fonts.ROOT,fonts.SOURCE,fonts.DEST
        fonts.ROOT,fonts.SOURCE,fonts.DEST=self.root,self.source,self.dest
    def tearDown(self):
        fonts.ROOT,fonts.SOURCE,fonts.DEST=self.old;self.temp.cleanup()
    def test_outline_bytes_license_and_meta_are_repeatable(self):
        fonts.prepare();meta=self.dest/'Galmuri9.ttf.meta';meta.write_text('guid: retained\n')
        before={p.name:p.read_bytes() for p in self.dest.iterdir()};fonts.prepare()
        self.assertEqual(before,{p.name:p.read_bytes() for p in self.dest.iterdir()})
        self.assertEqual(4,len(list(self.dest.glob('*.ttf'))))
        self.assertEqual((self.source/'fonts/galmuri-outline/LICENSE.txt').read_bytes(),(self.dest/'LICENSE.txt').read_bytes())
    def test_corrupt_outline_is_rejected_before_writes(self):
        p=self.source/'fonts/galmuri-outline/Galmuri9.ttf';p.write_bytes(p.read_bytes()+b'corrupt')
        with self.assertRaises(ValueError):fonts.prepare()
        self.assertFalse(self.dest.exists())
    def test_corrupt_license_is_rejected_before_writes(self):
        p=self.source/'fonts/galmuri-outline/LICENSE.txt';p.write_bytes(p.read_bytes()+b'corrupt')
        with self.assertRaises(ValueError):fonts.prepare()
        self.assertFalse(self.dest.exists())

if __name__ == '__main__':unittest.main()
