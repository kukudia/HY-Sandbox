"""Reproduce the runtime power split on the *emitted* JSON, not on the model."""
import json
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import design_model as M

DIR = r"D:\git_projects\Unity\HY-Sandbox\Blueprints\DSH_Enemies"


def inside(a, b, r):
    return all(abs(a[k] - b[k]) <= r for k in range(3))


for name in ("DSH_E01_Skimmer", "DSH_E02_Halberd", "DSH_E03_Talos"):
    data = json.load(open(os.path.join(DIR, name + ".json"), encoding="utf-8"))
    blocks = []
    for b in data["blocks"]:
        t = b["resourcePath"].rsplit("/", 1)[-1].replace(".prefab", "")
        blocks.append((t, (b["posX"], b["posY"], b["posZ"])))
    gens = [p for t, p in blocks if t == "PowerGeneratingUnit"]
    relays = [p for t, p in blocks if t == "PowerTransmissionDevice"]
    loads = [(t, p, M.CATALOG[t].get("min_power", 50)) for t, p in blocks
             if "std_power" in M.CATALOG[t] and t != "PowerTransmissionDevice"]

    n = len(relays)
    adj = {i: set() for i in range(n)}
    seeds = set()
    for i in range(n):
        for j in range(i + 1, n):
            if inside(relays[i], relays[j], 10):
                adj[i].add(j)
                adj[j].add(i)
        if any(inside(g, relays[i], 10) for g in gens):
            seeds.add(i)
    comps, seen = [], set()
    for s in range(n):
        if s in seen:
            continue
        stack, comp = [s], set()
        while stack:
            k = stack.pop()
            if k in comp:
                continue
            comp.add(k)
            stack.extend(adj[k] - comp)
        seen |= comp
        if comp & seeds:
            comps.append(comp)

    print("=== %s: %d generators, %d relays, %d live components, %d loads"
          % (name, len(gens), n, len(comps), len(loads)))
    starved = []
    for comp in comps:
        members = [relays[i] for i in comp]
        output = sum(4000 for g in gens if any(inside(g, r, 10) for r in members))
        inrange = [l for l in loads if any(inside(l[1], r, 5) for r in members)]
        if not inrange:
            continue
        share = output / len(inrange)
        print("   component of %d relays: output %d, loads %d, share %.0f"
              % (len(comp), output, len(inrange), share))
        for t, p, minimum in inrange:
            if share < minimum:
                starved.append((t, p, share, minimum))
    unreached = [l for l in loads
                 if not any(any(inside(l[1], relays[i], 5) for i in c) for c in comps)]
    print("   starved:", len(starved), starved[:6])
    print("   unreached:", len(unreached), unreached[:4])
