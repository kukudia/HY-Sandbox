"""Summarize a Siege Editor capture and archive its original JSON losslessly.

Entry: python Tools/Validation/summarize_siege_capture.py INPUT OUTPUT_PREFIX
Raw observations remain available as OUTPUT_PREFIX.frames.json.gz. Counters are
nested cumulative CPU times; their totals must never be added together.
"""

import argparse
import gzip
import hashlib
import json
import math
from pathlib import Path


def percentile(values, fraction):
    ordered = sorted(values)
    return ordered[max(0, math.ceil(len(ordered) * fraction) - 1)] if ordered else None


def summarize(source, prefix):
    raw = source.read_bytes()
    report = json.loads(raw)
    samples = report.pop("samples")
    census = report["census"]
    report["rawSha256"] = hashlib.sha256(raw).hexdigest()
    report["rawArchive"] = prefix.name + ".frames.json.gz"
    report["observations"] = len(samples)
    report["peakCensus"] = {
        key: max((row[key] for row in census), default=0)
        for key in ("blocks", "units", "fragments", "colliders", "rigidbodies", "awakeDynamicBodies")
    }
    report["stages"] = {
        stage: samples[-1].get(stage) if samples else None
        for stage in (
            "connectivity", "rebuild", "refresh", "buildingSupport", "buildingDamage",
            "buildingGraph", "buildingGround", "buildingTraversal", "buildingDebris",
        )
    }
    report["observedGcBytes"] = {
        "note": "Last profiler counter per Editor observation; may repeat or lag. Do not sum as total GC. Census and capture overhead included.",
        "median": percentile([row["gcBytes"] for row in samples if row["gcBytes"] >= 0], 0.5),
        "p95": percentile([row["gcBytes"] for row in samples if row["gcBytes"] >= 0], 0.95),
        "maximum": max((row["gcBytes"] for row in samples), default=-1),
    }
    prefix.parent.mkdir(parents=True, exist_ok=True)
    Path(str(prefix) + ".frames.json.gz").write_bytes(gzip.compress(raw, mtime=0))
    summary_path = Path(str(prefix) + ".json")
    summary_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(summary_path)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input", type=Path)
    parser.add_argument("output_prefix", type=Path)
    args = parser.parse_args()
    summarize(args.input, args.output_prefix)


if __name__ == "__main__":
    main()
