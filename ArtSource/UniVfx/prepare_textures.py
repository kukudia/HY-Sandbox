"""Lossless extraction of the installed UNI VFX flipbooks; requires Pillow.
Run from the Unity project root. Original package assets are never modified.
"""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw

vendor = Path('Assets/UNI VFX')
output = Path('Assets/Art/BlockVisuals/UNI/Textures')
output.mkdir(parents=True, exist_ok=True)
sources = [vendor / 'Realistic Explosions, Fire & Smoke/Textures' / name for name in
           ['uni_aerial_explosion.tga', 'uni_fire.tga', 'uni_smoke_misty.tga', 'uni_smoke_spiky.tga']]
sources.append(vendor / 'Common/Textures/uni_glow.png')
records = []
for source in sources:
    target = output / (source.stem + '.png')
    original = Image.open(source).convert('RGBA')
    original.save(target, optimize=True)
    assert original.tobytes() == Image.open(target).convert('RGBA').tobytes()
    records.append(dict(source=source.as_posix(), destination=target.as_posix(),
                        source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                        output_sha256=hashlib.sha256(target.read_bytes()).hexdigest(),
                        size=original.size, pixel_identical=True))

# Soft-edged shockwave authored here, independently of the vendor flipbooks.
ring = Image.new('RGBA', (256, 256))
for y in range(256):
    for x in range(256):
        radius = ((x - 127.5) ** 2 + (y - 127.5) ** 2) ** 0.5
        alpha = max(0, 1 - abs(radius - 103) / 10) ** 1.6
        ring.putpixel((x, y), (255, 255, 255, round(alpha * 255)))
ring.save(output / 'Shockwave.png')
(output.parent / 'Sources.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
print(f'{len(sources)} lossless textures plus shockwave; {sum(p.stat().st_size for p in output.glob("*.png"))/1048576:.2f} MiB')
