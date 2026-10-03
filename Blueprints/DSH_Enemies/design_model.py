"""Offline geometry / mass / power model for the DSH enemy blueprints.

Mirrors the rules actually enforced by the runtime:

  * Block.Awake snaps every block to a 0.5 m grid, so blueprint local positions must be
    multiples of 0.5 (Block.cs:73-83).
  * A block of size N centred at c spans [c-N/2, c+N/2]; two blocks may share a face but
    must never share a unit cell.
  * Enabled connector sockets are baked into each prefab at local offsets and rotate with
    the block (Block.GetConnectorWorldPosition / GetConnectorWorldNormal).  A connection
    needs both sockets at the *same* world point with opposite normals (Block.cs:417-437).
    This produces the lattice rule used below:
        - a 2x2x2 block's Up/Down sockets sit at (+-0.5, +-1, +-0.5) relative to its centre;
        - a 1x1x1 block's vertical socket sits at (0, +-0.5, 0) relative to its centre;
        - therefore a 1x1x1 part mounted on top of a 2x2x2 sits at (+-0.5, +1.5, +-0.5).
  * BlockGroupManager.GroupBlocks walks the enabled-connector graph and EnemySpawner
    requires exactly one Cockpit, so every block must be reachable from the cockpit.
  * PowerTransmissionDevice links a PowerGeneratingUnit within maxConnectionDistance (10 m)
    and feeds every Power block within powerRange (5 m); total generator output is split
    *equally* between loads, and a load only works at >= Power.minWorkingPower (50).
  * HoverFlightController drives every HoverThruster in the unit and needs lift > weight.
"""

import itertools
import math

G = 9.81
CONNECT_RANGE = 10.0
POWER_RANGE = 5.0
MIN_WORKING_POWER = 50.0


def box_sockets(x, y, z):
    s = []
    for i in range(x):
        for j in range(z):
            s.append(("Up", (i - (x - 1) / 2, y / 2, j - (z - 1) / 2), (0, 1, 0)))
    for i in range(x):
        for j in range(z):
            s.append(("Down", (i - (x - 1) / 2, -y / 2, j - (z - 1) / 2), (0, -1, 0)))
    for i in range(x):
        for j in range(y):
            s.append(("Forward", (i - (x - 1) / 2, j - (y - 1) / 2, z / 2), (0, 0, 1)))
    for i in range(x):
        for j in range(y):
            s.append(("Back", (i - (x - 1) / 2, j - (y - 1) / 2, -z / 2), (0, 0, -1)))
    for i in range(y):
        for j in range(z):
            s.append(("Left", (-x / 2, i - (y - 1) / 2, j - (z - 1) / 2), (-1, 0, 0)))
    for i in range(y):
        for j in range(z):
            s.append(("Right", (x / 2, i - (y - 1) / 2, j - (z - 1) / 2), (1, 0, 0)))
    return s


CATALOG = {
    "1x1x1": dict(size=(1, 1, 1), mass=1, sockets=box_sockets(1, 1, 1)),
    "2x1x1": dict(size=(2, 1, 1), mass=2, sockets=box_sockets(2, 1, 1)),
    "2x2x1": dict(size=(2, 2, 1), mass=4, sockets=box_sockets(2, 2, 1)),
    "2x2x2": dict(size=(2, 2, 2), mass=8, sockets=box_sockets(2, 2, 2)),
    "Cockpit": dict(size=(2, 2, 2), mass=8, sockets=box_sockets(2, 2, 2)),
    "CargoHold": dict(size=(2, 2, 2), mass=8, std_power=20, mass_ok=True,
                      sockets=box_sockets(2, 2, 2)),
    "TechnologyHold": dict(size=(1, 1, 1), mass=1, std_power=20, sockets=box_sockets(1, 1, 1)),
    "Rack": dict(size=(1, 1, 1), mass=1, sockets=box_sockets(1, 1, 1)),
    "Stairs": dict(size=(1, 1, 1), mass=1, sockets=[
        ("Down", (0, -0.5, 0), (0, -1, 0)), ("Back", (0, 0, -0.5), (0, 0, -1)),
        ("Left", (-0.5, 0, 0), (-1, 0, 0)), ("Right", (0.5, 0, 0), (1, 0, 0))]),
    "Door": dict(size=(1, 2, 1), mass=2, sockets=box_sockets(1, 2, 1)),
    "HoverThruster": dict(size=(1, 1, 1), mass=1, thrust=100, std_power=100, min_power=50,
                          sockets=[("Up", (0, 0.5, 0), (0, 1, 0))]),
    "HoverThrusterBig": dict(size=(2, 2, 2), mass=8, thrust=800, std_power=100, min_power=50,
                             sockets=[("Up", (sx - 0.5, 1, sz - 0.5), (0, 1, 0))
                                      for sx in (0, 1) for sz in (0, 1)]),
    "UniversalThruster": dict(size=(1, 1, 1), mass=1, thrust=100, std_power=100, min_power=50,
                              sockets=[("Down", (0, -0.5, 0), (0, -1, 0))]),
    "UniversalThrusterBig": dict(size=(2, 2, 2), mass=8, thrust=800, std_power=100, min_power=50,
                                 sockets=[("Down", (sx - 0.5, -1, sz - 0.5), (0, -1, 0))
                                          for sx in (0, 1) for sz in (0, 1)]),
    "MainThruster": dict(size=(1, 1, 1), mass=1, thrust=250, std_power=100, min_power=50,
                         sockets=box_sockets(1, 1, 1)),
    "MainThrusterBig": dict(size=(2, 2, 2), mass=8, thrust=2000, std_power=100, min_power=50,
                            sockets=box_sockets(2, 2, 2)),
    "Turret": dict(size=(1, 1, 1), mass=1, std_power=100, min_power=50,
                   sockets=[("Down", (0, -0.5, 0), (0, -1, 0))]),
    "HoverFlightController": dict(size=(1, 1, 1), mass=1, std_power=100, min_power=100,
                                  sockets=[("Down", (0, -0.5, 0), (0, -1, 0))]),
    "PowerTransmissionDevice": dict(size=(1, 1, 1), mass=1,
                                    sockets=[("Down", (0, -0.5, 0), (0, -1, 0))]),
    "PowerGeneratingUnit": dict(size=(2, 2, 2), mass=8, output=4000,
                                sockets=[("Down", (sx - 0.5, -1, sz - 0.5), (0, -1, 0))
                                         for sx in (0, 1) for sz in (0, 1)]),
}


def _q(a, axis):
    r = math.radians(a) / 2.0
    s, c = math.sin(r), math.cos(r)
    return {"x": (s, 0.0, 0.0, c), "y": (0.0, s, 0.0, c), "z": (0.0, 0.0, s, c)}[axis]


IDENTITY = (0.0, 0.0, 0.0, 1.0)


def qrot(q, v):
    x, y, z, w = q
    vx, vy, vz = v
    return (
        (1 - 2 * (y * y + z * z)) * vx + 2 * (x * y - w * z) * vy + 2 * (x * z + w * y) * vz,
        2 * (x * y + w * z) * vx + (1 - 2 * (x * x + z * z)) * vy + 2 * (y * z - w * x) * vz,
        2 * (x * z - w * y) * vx + 2 * (y * z + w * x) * vy + (1 - 2 * (x * x + y * y)) * vz,
    )


def cells(px, py, pz, size):
    """Unit cells of a *uniform* build lattice, used only for the generator check.

    Kept for compatibility; Design does real interval overlap (see Design._claim).
    """
    out = []
    for ix in range(size[0]):
        for iy in range(size[1]):
            for iz in range(size[2]):
                out.append((round(px - size[0] / 2) + ix,
                            round(py - size[1] / 2) + iy,
                            round(pz - size[2] / 2) + iz))
    return out


class Design:
    #: blocks that are safe to embed in the deck grid; their own sockets may stay unmated
    #: because the neighbours that carry the matching face belong to the same call site.
    STRUCTURAL = ("2x2x2", "2x2x1", "2x1x1", "1x1x1", "Door", "Rack", "Stairs",
                  "TechnologyHold", "CargoHold")
    #: blocks whose enabled sockets must all be mated to a neighbour
    MOUNTED = ("HoverThruster", "HoverThrusterBig", "UniversalThruster", "UniversalThrusterBig",
               "Turret", "HoverFlightController", "PowerTransmissionDevice")

    def __init__(self, name):
        self.name = name
        self.blocks = []
        self.spans = []               # (x_lo, x_hi, y_lo, y_hi, z_lo, z_hi) per block
        self.by_pos = {}
        self.rejected = []
        self._socket_buckets = {}     # world socket point -> [[block index, normal], ...]
        self._cache_dirty = False

    # -- true volume overlap on the 0.5 m lattice ----------------------------------------
    @staticmethod
    def _span(pos, size):
        return (pos[0] - size[0] / 2, pos[0] + size[0] / 2,
                pos[1] - size[1] / 2, pos[1] + size[1] / 2,
                pos[2] - size[2] / 2, pos[2] + size[2] / 2)

    def _conflict(self, span):
        """Index of a block whose volume overlaps `span`, else None.

        Touching faces (shared boundary plane) do not overlap, which is exactly the
        adjacency the connector rule wants; half-offset mounts such as a 1x1x1 relay
        nestled under a 2x2x2 deck block are therefore legal.
        """
        for i, s in enumerate(self.spans):
            if (span[0] < s[1] - 1e-6 and s[0] < span[1] - 1e-6
                    and span[2] < s[3] - 1e-6 and s[2] < span[3] - 1e-6
                    and span[4] < s[5] - 1e-6 and s[4] < span[5] - 1e-6):
                return i
        return None

    # -- world socket index, maintained incrementally ------------------------------------
    def _index_block(self, index):
        b = self.blocks[index]
        for sname, lp, ln in b["spec"]["sockets"]:
            wp = tuple(round(b["pos"][k] + qrot(b["rot"], lp)[k], 4) for k in range(3))
            wn = qrot(b["rot"], ln)
            self._socket_buckets.setdefault(wp, []).append([index, wn])

    def _buckets(self):
        if self._cache_dirty:
            self._socket_buckets = {}
            for i in range(len(self.blocks)):
                self._index_block(i)
            self._cache_dirty = False
        return self._socket_buckets

    # -- placing a part that must physically bolt onto an existing socket -----------------
    def place(self, type_name, target, rot=IDENTITY, radius=14, note="", require_all=True,
              lattice_exempt=False):
        """Find the free lattice slot nearest `target` whose sockets mate.

        A mounted part is legal when every enabled socket of the new block coincides with
        an enabled socket of a different block at the same world point and with an opposing
        normal (Block.FindMatchingConnector).  Parts embedded *inside* the deck lattice
        (2x2x2 equipment such as the generator) only need one mating face; their free
        sockets are the ones the deck block's opposite face would have had.
        Searching instead of hand-placing removes the whole class of half-cell mistakes.
        """
        buckets = self._buckets()
        spec = CATALOG[type_name]
        size = spec["size"]
        off = (0.5 if size[0] % 2 else 0.0,
               0.5 if size[1] % 2 else 0.0,
               0.5 if size[2] % 2 else 0.0)
        best = None
        rng = range(-radius, radius + 1)
        for dx in rng:
            for dy in rng:
                for dz in rng:
                    dist = dx * dx + dy * dy + dz * dz
                    if best is not None and dist >= best[0]:
                        continue
                    key = (round((target[0] + dx + off[0]) * 2) / 2,
                           round((target[1] + dy + off[1]) * 2) / 2,
                           round((target[2] + dz + off[2]) * 2) / 2)
                    if key in self.by_pos:
                        continue
                    if self._conflict(self._span(key, size)) is not None:
                        continue
                    if not self._sockets_mate(spec, key, rot, buckets, require_all):
                        continue
                    best = (dist, key)
        if best is None:
            self.rejected.append((type_name, target, "no mated slot found " + note))
            return None
        return self.add(type_name, best[1][0], best[1][1], best[1][2], rot=rot,
                        lattice_exempt=lattice_exempt)

    def _sockets_mate(self, spec, key, rot, buckets, require_all=True):
        """True when the candidate's sockets pair up with existing sockets.

        require_all -> every socket must mate (a part bolted on top of the hull).
        otherwise  -> at least one socket must mate (a part embedded in the lattice).
        """
        mates = 0
        for sname, lp, ln in spec["sockets"]:
            wp = tuple(round(key[k] + qrot(rot, lp)[k], 4) for k in range(3))
            wn = qrot(rot, ln)
            matched = False
            for other in buckets.get(wp, ()):
                if sum(wn[k] * other[1][k] for k in range(3)) < -0.75:
                    matched = True
                    break
            if matched:
                mates += 1
            elif require_all:
                return False
        return mates >= 1

    def add(self, type_name, x, y, z, rot=IDENTITY, lattice_exempt=False):
        spec = CATALOG[type_name]
        size = spec["size"]
        key = (round(x * 2) / 2, round(y * 2) / 2, round(z * 2) / 2)
        if key in self.by_pos:
            self.rejected.append((type_name, key, "duplicate position"))
            return None
        # Lattice invariant, verified against every shipped blueprint: a block of size N
        # occupies [c-N/2, c+N/2], so two same-size blocks tile without overlap only when
        # their centres differ by a multiple of N.  On the horizontal plane the whole deck
        # therefore sits on even centres for 2x2x2 blocks (CODEX decks use x,z in -8..8
        # step 2), while 1x1x1 parts sit on half-integer centres and 2x2x2 equipment that
        # bolts onto the deck sits half-offset from a deck cell -- the one case that is
        # exempt, because it is placed by _deck_and_ring against a real socket.  Stacked
        # layers are exempt too: the hover ring at y=-2 and the deck at y=0 are a legal
        # half-cell offset.
        for axis in (0, 2):
            if size[axis] == 2 and not lattice_exempt and abs(key[axis] % 2) != 0:
                self.rejected.append((type_name, key,
                                      "axis %d centre %.1f breaks the 2 m lattice parity"
                                      % (axis, key[axis])))
                return None
        span = self._span(key, size)
        conflict = self._conflict(span)
        if conflict is not None:
            self.rejected.append((type_name, key,
                                  "overlaps %s at %s"
                                  % (self.blocks[conflict]["type"], self.blocks[conflict]["pos"])))
            return None
        index = len(self.blocks)
        self.blocks.append(dict(type=type_name, pos=key, rot=rot, spec=spec))
        self.spans.append(span)
        self.by_pos[key] = index
        self._cache_dirty = True
        return index

    def socket_map(self):
        out = []
        for i, b in enumerate(self.blocks):
            for sname, lp, ln in b["spec"]["sockets"]:
                wp = tuple(b["pos"][k] + qrot(b["rot"], lp)[k] for k in range(3))
                wn = qrot(b["rot"], ln)
                out.append((i, sname, tuple(round(c * 2) / 2 for c in wp), wn))
        return out

    def graph(self):
        buckets = {}
        for entry in self.socket_map():
            buckets.setdefault(entry[2], []).append(entry)
        adj = {i: set() for i in range(len(self.blocks))}
        pairs = []
        for pos, entries in buckets.items():
            for a, b in itertools.combinations(entries, 2):
                if a[0] == b[0]:
                    continue
                if sum(a[3][k] * b[3][k] for k in range(3)) < -0.75:
                    adj[a[0]].add(b[0])
                    adj[b[0]].add(a[0])
                    pairs.append((a[0], b[0], pos))
        return adj, pairs

    # -- analytic helpers for the two deck-mounting patterns -----------------------------

    def place_deck_cell(self, type_name, target, radius=12, note=""):
        """Embed a 2x2x2 part in the deck lattice (generator) at the nearest free deck cell.

        Embedded 2x2x2 parts sit where a deck block would sit: integer centre, own Up
        sockets therefore absent.  Only one mating face is required to stay connected.
        """
        return self.place(type_name, target, radius=radius, note=note, require_all=False)

    def place_on_deck(self, type_name, target, radius=12, note=""):
        """Bolt a part onto a deck block's top face.

        A 2x2x2 deck block centred at (dx, 0, dz) exposes Up sockets at
        (dx +- 0.5, 1, dz +- 0.5).  A 1x1x1 part therefore mates at
        (dx +- 0.5, 1.5, dz +- 0.5) and a 2x2x2 part at (dx +- 0.5, 2, dz +- 0.5).
        """
        size = CATALOG[type_name]["size"]
        dy = 1.5 if size[1] == 1 else 2.0
        # A 2x2x2 deck block exposes Up sockets at (bx +- 0.5, 1, bz +- 0.5).  A part of
        # horizontal size S centred at (bx + o, *, bz + o) has its own sockets at
        # o -+ (S-1)/2, so o = (S-1)/2 + 0.5 = S/2 is what makes the two sets coincide.
        off = (size[0] / 2.0, size[2] / 2.0)
        offsets = [(sx * off[0], sz * off[1]) for sx in (-1, 1) for sz in (-1, 1)]
        buckets = self._buckets()
        best = None
        for (bx, by, bz) in list(self.by_pos):
            if by != 0:
                continue
            for dx, dz in offsets:
                key = (round((bx + dx) * 2) / 2, dy, round((bz + dz) * 2) / 2)
                if not (abs(key[0]) <= 30 and abs(key[2]) <= 30):
                    continue
                if key in self.by_pos:
                    continue
                if self._conflict(self._span(key, size)) is not None:
                    continue
                if not self._sockets_mate(CATALOG[type_name], key, IDENTITY, buckets, True):
                    continue
                dist = (key[0] - target[0]) ** 2 + (key[1] - target[1]) ** 2 \
                    + (key[2] - target[2]) ** 2
                if best is None or dist < best[0]:
                    best = (dist, key)
        if best is None:
            self.rejected.append((type_name, target, "no deck top slot " + note))
            return None
        return self.add(type_name, best[1][0], best[1][1], best[1][2])

    # -- structural requirements for non-structural parts -----------------------------

    def free_mount_slots(self, y, limit=40):
        """Lattice slots where a 1x1x1 part at height y would mate the hull below it."""
        buckets = self._buckets()
        out = []
        for (bx, by, bz) in list(self.by_pos):
            if by != y - 1.5:
                continue
            for dx in (-0.5, 0.5):
                for dz in (-0.5, 0.5):
                    key = (round((bx + dx) * 2) / 2, y, round((bz + dz) * 2) / 2)
                    if key in self.by_pos:
                        continue
                    if self._conflict(self._span(key, (1, 1, 1))) is not None:
                        continue
                    if self._sockets_mate(CATALOG["Turret"], key, IDENTITY, buckets):
                        out.append(key)
        return out[:limit]

    def place_on_station(self, type_name, target, note=""):
        """Bolt a 1x1x1 part onto a station block's top face.

        Station blocks (cockpit, generators) sit at y = 2 and span y in [1, 3], so the deck's
        top face at y = 1 lies inside their volume and cannot carry equipment.  Their own Up
        sockets sit at y = 3, and a 1x1x1 part's Down socket reaches y = 3 from y = 3.5,
        half-offset from the station centre exactly as equipment on a deck block would be.
        """
        buckets = self._buckets()
        best = None
        for (bx, by, bz), index in list(self.by_pos.items()):
            if index < 0 or by not in (0, 2):
                continue
            if self.blocks[index]["type"] not in ("Cockpit", "PowerGeneratingUnit"):
                continue
            for dx in (-0.5, 0.5):
                for dz in (-0.5, 0.5):
                    key = (round((bx + dx) * 2) / 2, 3.5, round((bz + dz) * 2) / 2)
                    if key in self.by_pos:
                        continue
                    if self._conflict(self._span(key, (1, 1, 1))) is not None:
                        continue
                    if not self._sockets_mate(CATALOG[type_name], key, IDENTITY, buckets, True):
                        continue
                    dist = (key[0] - target[0]) ** 2 + (key[1] - target[1]) ** 2 \
                        + (key[2] - target[2]) ** 2
                    if best is None or dist < best[0]:
                        best = (dist, key)
        if best is None:
            self.rejected.append((type_name, target, "no station top slot " + note))
            return None
        return self.add(type_name, best[1][0], best[1][1], best[1][2])

    def place_under_deck(self, type_name, target, note=""):
        """Hang a 1x1x1 part under a deck block, rolled 180 degrees about X.

        The deck's Down sockets sit at y = -1.  A part mounted upside down has its own Down
        socket pointing up, reaching y = -1 from a centre at y = -1.5 and half-offset in x and
        z from the deck block's centre.  That is the hull's only free mounting surface: the
        deck's top face at y = 1 lies inside the station blocks, which span y in [1, 3].
        """
        rot = _q(180, "x")
        buckets = self._buckets()
        best = None
        for (bx, by, bz), index in list(self.by_pos.items()):
            if index < 0 or by != 0 or self.blocks[index]["type"] != "2x2x2":
                continue
            for dx in (-0.5, 0.5):
                for dz in (-0.5, 0.5):
                    key = (round((bx + dx) * 2) / 2, -1.5, round((bz + dz) * 2) / 2)
                    if key in self.by_pos:
                        continue
                    if self._conflict(self._span(key, (1, 1, 1))) is not None:
                        continue
                    if not self._sockets_mate(CATALOG[type_name], key, rot, buckets, True):
                        continue
                    dist = (key[0] - target[0]) ** 2 + (key[1] - target[1]) ** 2 \
                        + (key[2] - target[2]) ** 2
                    if best is None or dist < best[0]:
                        best = (dist, key)
        if best is None:
            self.rejected.append((type_name, target, "no under-deck slot " + note))
            return None
        return self.add(type_name, best[1][0], best[1][1], best[1][2], rot=rot)

    def under_deck_slots(self, type_name):
        """Every legal slot for a 1x1x1 part hanging under a deck block, rolled 180 deg."""
        rot = _q(180, "x")
        buckets = self._buckets()
        out = []
        for (bx, by, bz), index in list(self.by_pos.items()):
            if index < 0 or by != 0 or self.blocks[index]["type"] != "2x2x2":
                continue
            for dx in (-0.5, 0.5):
                for dz in (-0.5, 0.5):
                    key = (round((bx + dx) * 2) / 2, -1.5, round((bz + dz) * 2) / 2)
                    if key in self.by_pos:
                        continue
                    if self._conflict(self._span(key, (1, 1, 1))) is not None:
                        continue
                    if self._sockets_mate(CATALOG[type_name], key, rot, buckets, True):
                        out.append(key)
        return out

    def place_relays(self, count, seed=(-0.5, -1.5, -0.5), note=""):
        """Place `count` relays so that between them they power the most otherwise-uncovered loads.

        Relay coverage is a hard constraint, not a preference: a Power block is only fed when
        some live relay lies inside its 5 m box, and the hover ring and the outboard vector
        pods sit at the plate rim where a relay placed by eye does not reach.  Greedy set
        cover over the real candidate slots finds a placement that covers whatever is
        reachable at all, instead of guessing coordinates and failing the coverage check.
        """
        self.place_under_deck("PowerTransmissionDevice", seed, note=note)
        slots = self.under_deck_slots("PowerTransmissionDevice")

        def inside(a, b, r):
            return all(abs(a[k] - b[k]) <= r for k in range(3))

        while True:
            relay_pos = [b["pos"] for b in self.blocks
                         if b["type"] == "PowerTransmissionDevice"]
            if len(relay_pos) >= count or not slots:
                break
            loads = [b["pos"] for b in self.blocks
                     if "std_power" in b["spec"] and b["type"] != "PowerTransmissionDevice"]
            missing = [p for p in loads if not any(inside(p, q, 5.0) for q in relay_pos)]
            if not missing:
                break
            best = None
            for slot in slots:
                gained = sum(1 for p in missing if inside(p, slot, 5.0))
                if best is None or gained > best[0]:
                    best = (gained, slot)
            if best is None or best[0] == 0:
                break
            self.add("PowerTransmissionDevice", best[1][0], best[1][1], best[1][2],
                     rot=_q(180, "x"))
            slots = [s for s in slots if s != best[1]]
        return [b["pos"] for b in self.blocks if b["type"] == "PowerTransmissionDevice"]

    def place_under_deck_set(self, type_name, count, prefer=(), avoid=()):
        """Hang `count` parts of `type_name` under deck blocks, preferring the given (x, z).

        Every deck block offers four half-offset slots on its underside, so what has to be
        decided is *which* of them to use.  Trying the caller's preferences first and then
        falling back to the remaining slots keeps the machines where the design wants them
        (near the centre of mass, where lateral thrust buys the most control authority) while
        guaranteeing that each part really bolts onto a deck socket.  `avoid` protects slots
        another system has already claimed -- the relays must win, because a load outside
        every relay's 5 m box is simply dead.
        """
        avoid = set(avoid)
        for (px, pz) in prefer:
            if count <= 0:
                break
            if self.place_under_deck(type_name, (px, -1.5, pz)) is not None:
                count -= 1
        while count > 0:
            slots = [s for s in self.under_deck_slots(type_name) if s not in avoid]
            if not slots:
                break
            slots.sort(key=lambda s: s[0] * s[0] + s[2] * s[2])
            self.add(type_name, slots[0][0], slots[0][1], slots[0][2], rot=_q(180, "x"))
            count -= 1
        return [b["pos"] for b in self.blocks if b["type"] == type_name]

    def place_deck_top_set(self, type_name, count):
        """Bolt 1x1x1 parts onto the deck's top face wherever a socket is still free.

        The station blocks occupy the plate's centre band (y in [1, 3]), but each deck block
        still exposes four Up sockets at y = 1 around them, so the top face carries equipment
        too: a part of size 1 mates at (deck +- 0.5, 1.5, deck +- 0.5).  Slots nearest the
        centre of mass go first, which keeps lateral thrust balanced.
        """
        placed = 0
        while placed < count:
            slots = []
            for (bx, by, bz), index in list(self.by_pos.items()):
                if index < 0 or by != 0 or self.blocks[index]["type"] != "2x2x2":
                    continue
                for dx in (-0.5, 0.5):
                    for dz in (-0.5, 0.5):
                        key = (round((bx + dx) * 2) / 2, 1.5, round((bz + dz) * 2) / 2)
                        if key in self.by_pos:
                            continue
                        if self._conflict(self._span(key, (1, 1, 1))) is not None:
                            continue
                        if not self._sockets_mate(CATALOG[type_name], key, IDENTITY,
                                                  self._buckets(), True):
                            continue
                        slots.append(key)
            if not slots:
                break
            slots.sort(key=lambda s: s[0] * s[0] + s[2] * s[2])
            self.add(type_name, slots[0][0], slots[0][1], slots[0][2])
            placed += 1
        return placed

    def place_upright_on_deck(self, type_name, target, note=""):
        """Bolt a 1x1x1 part onto a deck block's top face *without* rolling it.

        Most under-deck equipment can tolerate the 180-degree roll that hanging upside down
        requires, but HoverFlightController cannot: it reads its own transform.up as the
        craft's up-vector (CalculateTiltAdjustment / ApplyRotationCorrection), so a flipped
        controller would fly the craft inverted.  It has to sit upright on the deck's top
        face at (deck +- 0.5, 1.5, deck +- 0.5), where its Down socket meets the deck block's
        Up socket and its local up still points at the sky.
        """
        buckets = self._buckets()
        best = None
        for (bx, by, bz), index in list(self.by_pos.items()):
            if index < 0 or by != 0 or self.blocks[index]["type"] != "2x2x2":
                continue
            for dx in (-0.5, 0.5):
                for dz in (-0.5, 0.5):
                    key = (round((bx + dx) * 2) / 2, 1.5, round((bz + dz) * 2) / 2)
                    if key in self.by_pos:
                        continue
                    if self._conflict(self._span(key, (1, 1, 1))) is not None:
                        continue
                    if not self._sockets_mate(CATALOG[type_name], key, IDENTITY, buckets, True):
                        continue
                    dist = (key[0] - target[0]) ** 2 + (key[1] - target[1]) ** 2 \
                        + (key[2] - target[2]) ** 2
                    if best is None or dist < best[0]:
                        best = (dist, key)
        if best is None:
            self.rejected.append((type_name, target, "no upright deck slot " + note))
            return None
        return self.add(type_name, best[1][0], best[1][1], best[1][2])

    def report(self, verbose=False):
        mass = sum(b["spec"]["mass"] for b in self.blocks)
        moment = [sum(b["spec"]["mass"] * b["pos"][k] for b in self.blocks) for k in range(3)]
        com = tuple(moment[k] / mass for k in range(3))

        ring = [b for b in self.blocks if b["type"] in ("HoverThruster", "HoverThrusterBig")]
        ring_thrust = sum(b["spec"]["thrust"] for b in ring)
        ring_mass = sum(b["spec"]["mass"] for b in ring)
        ring_c = tuple(sum(b["spec"]["mass"] * b["pos"][k] for b in ring) / ring_mass
                       for k in range(3)) if ring else (0.0, 0.0, 0.0)
        horiz = sum(b["spec"].get("thrust", 0.0) for b in self.blocks
                    if b["type"] in ("UniversalThruster", "UniversalThrusterBig"))

        loads = [b for b in self.blocks if "std_power" in b["spec"]]
        demand = sum(b["spec"]["std_power"] for b in loads)
        supply = sum(b["spec"].get("output", 0.0) for b in self.blocks)
        gens = [b["pos"] for b in self.blocks if b["spec"].get("output")]
        relays = [b["pos"] for b in self.blocks if b["type"] == "PowerTransmissionDevice"]
        per_load = supply / len(loads) if loads else 0.0
        min_req = max((b["spec"].get("min_power", MIN_WORKING_POWER) for b in loads), default=0)

        def inside(a, b, r):
            return all(abs(a[k] - b[k]) <= r for k in range(3))
        # PowerTransmissionDevice splits the relays into components (generator->relay and
        # relay<->relay within maxConnectionDistance = 10 m) and hands each component's total
        # generator output *equally* to the Power blocks inside powerRange = 5 m of it.  A
        # small island can therefore starve or be starved, so both the coverage and the share
        # have to be modelled rather than eyeballed.
        n = len(relays)
        adj_relay = {i: set() for i in range(n)}
        seeds = set()
        for i in range(n):
            for j in range(i + 1, n):
                if inside(relays[i], relays[j], CONNECT_RANGE):
                    adj_relay[i].add(j)
                    adj_relay[j].add(i)
            if any(inside(g, relays[i], CONNECT_RANGE) for g in gens):
                seeds.add(i)
        components = []
        seen = set()
        for start in range(n):
            if start in seen:
                continue
            stack, comp = [start], set()
            while stack:
                k = stack.pop()
                if k in comp:
                    continue
                comp.add(k)
                stack.extend(adj_relay[k] - comp)
            seen |= comp
            if comp & seeds:
                components.append(comp)

        uncovered = []
        starved = []
        for comp in components:
            members = [relays[i] for i in comp]
            output = sum(g["spec"].get("output", 0.0) for g in self.blocks
                         if g["spec"].get("output")
                         and any(inside(g["pos"], r, CONNECT_RANGE) for r in members))
            comp_loads = [b for b in loads
                          if any(inside(b["pos"], r, POWER_RANGE) for r in members)]
            if not comp_loads:
                continue
            share = output / len(comp_loads)
            for b in comp_loads:
                need = b["spec"].get("min_power", MIN_WORKING_POWER)
                if share < need:
                    starved.append("%s%s share=%.0f need=%.0f" % (b["type"], b["pos"], share, need))
        for b in loads:
            if not any(any(inside(b["pos"], relays[i], POWER_RANGE) for i in comp)
                       for comp in components):
                uncovered.append(b["type"] + str(b["pos"]))

        adj, pairs = self.graph()
        mated = set()
        for a, b, _ in pairs:
            mated.add(a)
            mated.add(b)
        seen, stack = set(), [0]
        while stack:
            n = stack.pop()
            if n in seen:
                continue
            seen.add(n)
            stack.extend(adj[n] - seen)

        cockpits = [i for i, b in enumerate(self.blocks) if b["type"] == "Cockpit"]
        unmated = [self.blocks[i]["type"] + str(self.blocks[i]["pos"])
                   for i in range(len(self.blocks))
                   if i not in mated and self.blocks[i]["type"] in self.MOUNTED]

        return dict(
            name=self.name, blocks=len(self.blocks), mass=round(mass, 1),
            com=tuple(round(c, 3) for c in com),
            ring_centroid=tuple(round(c, 3) for c in ring_c),
            ring_offset_from_com=round(math.dist(com, ring_c), 3),
            ring_thrust=ring_thrust, lift_ratio=round(ring_thrust / (mass * G), 3),
            hover_utilisation=round(mass * G / ring_thrust, 3),
            horizontal_thrust=horiz, horizontal_accel=round(horiz / mass, 2),
            demand=demand, supply=supply, loads=len(loads), generators=len(gens),
            relays=len(relays), live_relays=len(components), per_load=round(per_load, 1),
            min_required=min_req, powered=per_load >= min_req,
            uncovered=len(uncovered), uncovered_list=uncovered[:6],
            starved=len(starved), starved_list=starved[:6],
            cockpit_count=len(cockpits), connected=len(seen),
            connected_ok=len(seen) == len(self.blocks),
            socket_pairs=len(pairs), unmated=unmated,
            rejected=self.rejected,
        )

    def summary_line(self):
        r = self.report()
        return ("%-18s blocks=%-4d mass=%-6.1f  L/W=%.2f  hover=%.0f%%  hor=%4dN (%.1f m/s2)  "
                "P=%d/%d -> %d/load  conn=%d/%d  sockets=%d  unmated=%d  rejected=%d"
                % (r["name"], r["blocks"], r["mass"], r["lift_ratio"],
                   100 * r["hover_utilisation"], r["horizontal_thrust"], r["horizontal_accel"],
                   r["supply"], r["demand"], r["per_load"],
                   r["connected"], r["blocks"], r["socket_pairs"], len(r["unmated"]),
                   len(r["rejected"])))


# ======================================================================================
# E01 "Skimmer" - light delta-wing interceptor
#   Silhouette: 11 m wide, 13 m long swept delta plate; 16-thruster hover ring straddling
#   the deck plane; four inboard vector pods on the plate edges.  Light and agile.
# ======================================================================================
def _deck_and_ring(d, xs, zs, ring_cells, station):
    """Lay a solid 2x2x2 deck plate and mount the hover ring underneath it.

    `ring_cells` are (x, z) deck-block positions carrying a HoverThrusterBig at y = -2.
    Its enabled Up sockets sit at y = -1, exactly where the deck block's Down sockets are,
    so the thruster hangs directly under the deck block -- the arrangement the shipped
    CODEX enemies use, and the only one that keeps the deck's own Up sockets free for the
    equipment layer at y = 1.5.

    `station` maps (x, z) -> block type for the cockpit and generators, keyed by the deck
    cell they stand on.  Each is seated one rung up directly over that deck cell, at
    (x, 2, z): the deck block's Up sockets at (x +- 0.5, 1, z +- 0.5) are then exactly the
    station block's own Down sockets, so it bolts on instead of floating.  The station's
    cell volume and the deck block's merely touch at y = 1, so nothing overlaps.
    """
    for z in zs:
        for x in xs:
            d.add("2x2x2", x, 0, z)
    for (x, z), type_name in sorted(station.items()):
        d.add(type_name, x, 2, z, lattice_exempt=True)
    for x, z in ring_cells:
        d.add("HoverThrusterBig", x, -2, z)


def _ring_cells(xs, zs, station, upper=False):
    """Rim ring cells: the +-x columns (skipping corner rows) plus the +-z rows.

    `upper` adds a second ring row above the deck.  It is only safe when the cell directly
    above is not needed by the equipment layer, because a 180-degree-rolled upper thruster
    competes for the same deck Up socket that a 1x1x1 part on the deck would use.
    """
    x_lo, x_hi = min(xs), max(xs)
    z_lo, z_hi = min(zs), max(zs)
    out = []
    for x in (x_lo, x_hi):
        for z in zs:
            if z not in (z_lo, z_hi) and (x, z) not in station:
                out.append((x, z))
    for z in (z_lo, z_hi):
        for x in xs:
            if (x, z) not in station:
                out.append((x, z))
    return out


# ======================================================================================
# Final hulls
#
# All three share the verified construction: a 2 m deck plate on the even lattice, a hover
# ring of HoverThrusterBig hanging directly under deck blocks (their Up sockets meet the
# deck's Down sockets at y = -1), station blocks (cockpit and generators) seated one rung
# above a deck cell so their Down sockets meet that cell's Up sockets, 1x1x1 equipment on
# free deck sockets at y = 1.5, and 2x2x2 vector pods rolled 90 degrees so the pod's Down
# socket faces inboard onto a deck block's side socket.  They differ in silhouette, mass,
# lift, armament and control authority.
# ======================================================================================

def _plate(d, xs, zs, chamfer, station, ring):
    for z in zs:
        for x in xs:
            if (x, z) in chamfer or (x, z) in station:
                continue
            d.add("2x2x2", x, 0, z)
    for (x, y, z), type_name in sorted(station.items()):
        d.add(type_name, x, y, z, lattice_exempt=True)
    for x, z in sorted(ring):
        d.add("HoverThrusterBig", x, -2, z)


def _pods(d, pods):
    for sx, z in pods:
        d.add("UniversalThrusterBig", sx, 0, z, rot=_q(-90 * (1 if sx > 0 else -1), "z"))


def _fit(d, slots):
    """Seat 1x1x1 equipment under the deck.

    Call sites give only (x, z).  The deck's top face at y = 1 lies inside the station blocks
    (they span y in [1, 3]), so the only free mounting surface is the deck's underside: a
    1x1x1 part rolled 180 degrees at y = -1.5 has its Down socket pointing up at y = -1,
    exactly where the deck block above it has its Down socket.  place_under_deck finds the
    nearest legal slot to the requested (x, z).
    """
    for type_name, target in slots:
        d.place_under_deck(type_name, (target[0], -1.5, target[1]))


# ======================================================================================
# E01 "Skimmer" - light wide interceptor.  Broad 12 m beam, thin plate, 17-thruster ring,
# three generators, two turrets and six short vector pods.  Fastest to change direction.
# ======================================================================================
def build_e01():
    d = Design("DSH_E01_Skimmer")
    xs = (-4, -2, 0, 2, 4)
    zs = (-4, -2, 0, 2, 4)
    chamfer = {(4, 4), (4, -4), (-4, 4), (-4, -4)}
    station = {(0, 2, 0): "Cockpit",
               (0, 2, -4): "PowerGeneratingUnit",
               (2, 2, 4): "PowerGeneratingUnit",
               (-2, 2, 4): "PowerGeneratingUnit"}
    ring = [(-2, -4), (2, -4),
            (-4, -2), (-2, -2), (0, -2), (2, -2), (4, -2),
            (-4, 0), (-2, 0), (2, 0), (4, 0),
            (-4, 2), (-2, 2), (0, 2), (2, 2), (4, 2),
            (0, 4)]
    _plate(d, xs, zs, chamfer, station, ring)
    # The flight controller must NOT be flipped: it uses transform.up as the craft's up.
    d.place_upright_on_deck("HoverFlightController", (2.5, 1.5, -0.5))
    for target in ((1.5, 1.5), (-1.5, 1.5)):
        d.place_under_deck("Turret", (target[0], -1.5, target[1]))
    # Relay coverage has to be settled before anything else claims the under-deck sockets: a
    # Power block outside every relay's 5 m box simply never runs, and the hover ring reaches
    # to the plate corners, which is well outside one central relay's reach.
    relays = d.place_relays(9, note="interceptor")
    # Lateral thrust then comes from omni thrusters hung in the sockets that are left: a
    # UniversalThruster re-aims its model in world space every FixedUpdate, so it needs no
    # authored facing and can hang upside down exactly like a relay.  The 2x2x2 outboard
    # vector pods are not used: a pod does bolt onto the plate edge, but every side deck cell
    # of a ring column is already a hover-ring anchor, so no relay could then be placed within
    # 5 m of a pod mounted 2 m outside the plate.
    d.place_under_deck_set("UniversalThruster", 10,
                           prefer=((2.5, 2.5), (-2.5, -2.5), (2.5, 0.5), (-2.5, -0.5),
                                   (0.5, 2.5), (-0.5, -2.5)),
                           avoid=relays)
    d.place_deck_top_set("UniversalThruster", 10)
    return d


# ======================================================================================
# E02 "Halberd" - heavy long catamaran gunship.  Twin keels with a raised spine, chamfered
# bow and stern, 30-thruster ring along both keels plus bow and stern rows, two generators,
# four turrets and ten vector pods.  Half the acceleration of the others, twice the mass.
# ======================================================================================
def build_e02():
    d = Design("DSH_E02_Halberd")
    xs = (-4, -2, 0, 2, 4)
    zs = (-10, -8, -6, -4, -2, 0, 2, 4, 6, 8, 10)
    chamfer = {(4, 10), (4, -10), (-4, 10), (-4, -10)}
    station = {(0, 2, 0): "Cockpit",
               (4, 2, -8): "PowerGeneratingUnit",
               (-4, 2, 8): "PowerGeneratingUnit",
               (0, 2, -10): "PowerGeneratingUnit"}
    ring = [(x, z) for x in (-4, 4) for z in zs
            if (x, z) not in chamfer
            and (x, z) not in {(sx, sz) for sx, _, sz in station}]
    ring += [(x, z) for x in (-2, 0, 2) for z in (-10, 10) if (x, z) not in chamfer]
    ring += [(x, z) for x in (-2, 2) for z in (-8, 8) if (x, z) not in chamfer]
    _plate(d, xs, zs, chamfer, station, ring)
    # Relays first: a Power block outside every relay's 5 m box simply never runs, and this
    # hull's ring reaches to both bow and stern.
    relays = d.place_relays(14, note="gunship")
    # The flight controller must NOT be flipped: it uses transform.up as the craft's up.
    d.place_upright_on_deck("HoverFlightController", (2.5, 1.5, -0.5))
    for target in ((2.5, 3.5), (-2.5, 3.5), (2.5, -3.5), (-2.5, -3.5)):
        d.place_under_deck("Turret", (target[0], -1.5, target[1]))
    # Lateral thrust from omni thrusters in the remaining under-deck and deck-top sockets.
    # The 2x2x2 outboard pods are deliberately not used: they bolt onto the plate edge, but
    # every side deck cell of a ring column is already a hover-ring anchor, so no relay could
    # be placed within the 5 m needed to reach a pod mounted outside the plate.
    d.place_under_deck_set("UniversalThruster", 24,
                           prefer=((0.5, 0.5), (-0.5, -0.5), (2.5, 0.5), (-2.5, -0.5),
                                   (2.5, 4.5), (-2.5, -4.5), (2.5, -4.5), (-2.5, 4.5)),
                           avoid=relays)
    d.place_deck_top_set("UniversalThruster", 16)
    return d


# ======================================================================================
# E03 "Talos" - disc gunship.  Same 5x5 plate as the interceptor but chamfered tight and
# fitted as a gun platform: 17-thruster ring, three generators, six turrets, four pods and
# an inboard omni pair.  Shortest and most heavily armed of the three.
# ======================================================================================
def build_e03():
    d = Design("DSH_E03_Talos")
    xs = (-4, -2, 0, 2, 4)
    zs = (-4, -2, 0, 2, 4)
    chamfer = {(4, 4), (4, -4), (-4, 4), (-4, -4)}
    station = {(0, 2, 0): "Cockpit",
               (0, 2, -4): "PowerGeneratingUnit",
               (4, 2, 2): "PowerGeneratingUnit",
               (-4, 2, -2): "PowerGeneratingUnit"}
    ring = [(-2, -4), (2, -4),
            (-4, -2), (-2, -2), (0, -2), (2, -2), (4, -2),
            (-4, 0), (-2, 0), (2, 0), (4, 0),
            (-4, 2), (-2, 2), (0, 2), (2, 2), (4, 2),
            (0, 4)]
    _plate(d, xs, zs, chamfer, station, ring)
    # The flight controller must NOT be flipped: it uses transform.up as the craft's up.
    d.place_upright_on_deck("HoverFlightController", (2.5, 1.5, -0.5))
    for target in ((1.5, 3.5), (-1.5, 3.5), (2.5, 1.5), (-2.5, 1.5), (2.5, -1.5), (-2.5, -1.5)):
        d.place_under_deck("Turret", (target[0], -1.5, target[1]))
    # Relays first (see the E01 note), then omni thrusters in the leftover sockets, plus two
    # that sit on free top-face sockets near the bow.
    relays = d.place_relays(12, note="disc")
    d.place_under_deck_set("UniversalThruster", 12,
                           prefer=((0.5, 0.5), (-0.5, -0.5), (2.5, -0.5), (-2.5, 0.5),
                                   (0.5, -2.5), (-0.5, 2.5)),
                           avoid=relays)
    d.place_on_deck("UniversalThruster", (0.5, 1.5, 3.5))
    d.place_on_deck("UniversalThruster", (-0.5, 1.5, 3.5))
    d.place_deck_top_set("UniversalThruster", 12)
    return d


BUILDERS = (build_e01, build_e02, build_e03)

if __name__ == "__main__":
    ok = True
    for builder in BUILDERS:
        d = builder()
        r = d.report()
        print(d.summary_line())
        problems = []
        if not r["connected_ok"]:
            problems.append("connector graph is not one component (%d/%d)"
                            % (r["connected"], r["blocks"]))
        if r["cockpit_count"] != 1:
            problems.append("cockpit count %d" % r["cockpit_count"])
        if r["unmated"]:
            problems.append("unmated mounted parts: %s" % r["unmated"])
        if r["rejected"]:
            problems.append("rejected parts: %s" % r["rejected"])
        if r["uncovered"]:
            problems.append("unpowered loads: %s" % r["uncovered_list"])
        if not r["powered"]:
            problems.append("power per load %.1f < min %d" % (r["per_load"], r["min_required"]))
        if r["lift_ratio"] < 1.6:
            problems.append("lift ratio %.2f below 1.6" % r["lift_ratio"])
        if abs(r["com"][0]) > 0.35:
            problems.append("lateral CoM offset %.3f" % r["com"][0])
        if r["ring_offset_from_com"] > 2.0:
            problems.append("hover ring centroid %.2f m from CoM" % r["ring_offset_from_com"])
        if problems:
            ok = False
            for p in problems:
                print("     !! " + p)
    print()
    print("ALL DESIGNS PASS" if ok else "PROBLEMS REMAIN")
