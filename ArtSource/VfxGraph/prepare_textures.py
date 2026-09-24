"""Losslessly convert native UNI TGA dependencies to tracked PNG assets."""
import json
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[2]
records = json.loads((root / "Assets/Art/VFX/Sources.json").read_text(encoding="utf-8-sig"))
for record in records:
    source, target = root / record["source"], root / record["destination"]
    if source.suffix.lower() != ".tga":
        continue
    target.parent.mkdir(parents=True, exist_ok=True)
    with Image.open(source) as original:
        original.save(target)
        with Image.open(target) as converted:
            assert original.size == converted.size and original.tobytes() == converted.tobytes(), source
    print(target.relative_to(root))
