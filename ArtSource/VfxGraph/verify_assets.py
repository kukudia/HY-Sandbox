"""Static delivery checks; GPU behavior is covered by the saved Unity Play Mode probes."""
import hashlib
import json
from pathlib import Path
import subprocess
from PIL import Image, ImageChops

root = Path(__file__).resolve().parents[2]
paths = subprocess.check_output(["git", "ls-files", "-z"], cwd=root).decode().split("\0")
checked = 0
for name in filter(None, paths):
    path = root / name
    if not path.exists() or path.suffix not in (".prefab", ".unity"):
        continue
    text = path.read_text(encoding="utf-8-sig")
    assert "\nParticleSystem:" not in text and "\nTrailRenderer:" not in text, name
    checked += 1
for path in (root / "Assets/Scripts").rglob("*.cs"):
    data = path.read_bytes()
    assert b"ParticleSystem" not in data and b"TrailRenderer" not in data, path
for record in json.loads((root / "Assets/Art/VFX/Sources.json").read_text(encoding="utf-8-sig")):
    path = root / record["destination"]
    assert hashlib.sha256(path.read_bytes()).hexdigest() == record["destinationSha256"], path
    assert (root / (record["destination"] + ".meta")).exists(), path
    ignored = subprocess.run(["git", "check-ignore", "--quiet", str(path)], cwd=root)
    assert ignored.returncode == 1, path
print(f"Passed: {checked} tracked Prefabs/scenes, runtime API audit, dependency hashes and Git visibility.")

preview = root / "Assets/Art/VFX/Preview"
results = []
for name in ("Explosion", "BreakBurst", "ImpactBurst", "SmokeBurst", "DetachedSmoke", "ThrusterJet",
             "RepairContact", "EnergyBeam", "EnergyTrail", "BuildBurst"):
    folder = preview / name
    frames = json.loads((folder / "Frames.json").read_text(encoding="utf-8-sig"))
    assert len(frames) == 7, name
    with Image.open(folder / "5.png") as cleared:
        assert all(low == high for low, high in cleared.getextrema()), f"Visible residual after Clear: {name}"
        visible = []
        for index in (1, 2, 3, 6):
            with Image.open(folder / f"{index}.png") as frame:
                visible.append(ImageChops.difference(frame, cleared).getbbox() is not None)
        assert any(visible[:3]) and visible[3], f"Missing render or replay: {name}"
    results.append({"effect": name, "rendered": True, "clearedToBackground": True, "replayed": True, "frames": 7})
(preview / "Validation.json").write_text(json.dumps({"method": "Actual Play Mode GPU frames; pixel comparison against cleared frame", "effects": results, "errors": []}, indent=2) + "\n", encoding="utf-8")
print("Passed: 10 effects / 70 GPU frames, visible playback and replay, clear leaves pure background.")

for name in ("ThrusterJet", "EnergyTrail", "DetachedSmoke", "Explosion"):
    folder = preview / f"Position_{name}"
    frames = json.loads((folder / "Frames.json").read_text(encoding="utf-8-sig"))
    assert len(frames) == 7, name
    assert all("1000." in frame["position"] or "999." in frame["position"] for frame in frames), name
    with Image.open(folder / "5.png") as cleared:
        assert all(low == high for low, high in cleared.getextrema()), name
        visible = []
        for index in (0, 1, 2, 3):
            with Image.open(folder / f"{index}.png") as frame:
                visible.append(ImageChops.difference(frame, cleared).getbbox() is not None)
        assert any(visible), f"Missing large-coordinate render: {name}"
print("Passed: 4 moving effects at world position (1000, 0, 1000), visible GPU frames and clear.")

for name, count in (("MainThrusterBig", 4), ("HoverThrusterBig", 1)):
    folder = preview / f"Block_{name}"
    frames = json.loads((folder / "Frames.json").read_text(encoding="utf-8-sig"))
    assert len(frames) == 7 and all(len(frame["effects"]) == count for frame in frames), name
    with Image.open(folder / "2.png") as emitting, Image.open(folder / "5.png") as cleared:
        assert ImageChops.difference(emitting, cleared).getbbox() is not None, name
print("Passed: large main and hover thruster Play Mode frames show the expected outlet counts and visible flames.")
