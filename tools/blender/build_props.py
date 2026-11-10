"""
Tüm eşyalar: içecekler, üretim istasyonları (3 kademe), mobilya, dekor, yapı, oyun içi objeler.
Tasarım: GAME_DESIGN.md §4, §6, §11. Kurallar: DESIGN_RULES.md.
Çalıştırma:
  blender -b --factory-startup --python tools/blender/build_props.py               # hepsi
  blender -b --factory-startup --python tools/blender/build_props.py -- Station    # sadece bir kategori
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import toylib as T  # noqa: E402
from mathutils import Vector as V  # noqa: E402

MODELS = os.path.join(T.ROOT, "assets", "models", "props")
FBX = os.path.join(T.ROOT, "assets", "export", "fbx", "props")
RENDERS = os.path.join(T.ROOT, "assets", "renders")
TRIM = {1: "KoyuMese", 2: "Pirinc", 3: "Altin"}


def at(ox, oy):
    return lambda x, y, z: (ox + x, oy + y, z)


def barrel(p, name, x, y, z, r=0.25, length=0.55, body="Mese", band="KoyuMese", lying=True, verts=12):
    rot = (0, 90, 0) if lying else (0, 0, 0)
    T.cyl(f"{name}_Body", body, p(x, y, z), r=r, depth=length * 0.9, rot=rot, verts=verts)
    T.cyl(f"{name}_Bulge", body, p(x, y, z), r=r * 1.1, depth=length * 0.5, rot=rot, verts=verts)
    for d in (-0.36, 0.36):
        loc = p(x + d * length, y, z) if lying else p(x, y, z + d * length)
        T.torus(f"{name}_Band{d}", band, loc, major=r * 1.0, minor=0.018, rot=rot, seg=verts)
    cap = p(x + length * 0.45, y, z) if lying else p(x, y, z + length * 0.45)
    T.cyl(f"{name}_Cap", "KoyuMese", cap, r=r * 0.85, depth=0.01, rot=rot, verts=verts)


def tier_extras(p, tier, top, front_y=-0.32, width=0.4):
    """Kademe 2: pirinç isim plakası. Kademe 3: altın plaka + taç + mücevher."""
    if tier >= 2:
        T.box("TierPlate", TRIM[tier], p(0, front_y, top * 0.35), (width, 0.02, 0.09), bevel=0.008)
    if tier >= 3:
        T.cyl("CrownBase", "Altin", p(0, 0, top + 0.03), r=0.09, depth=0.05, verts=8)
        for k in range(5):
            a = k * 2 * math.pi / 5
            T.cyl(f"CrownTip{k}", "Altin", p(math.cos(a) * 0.08, math.sin(a) * 0.08, top + 0.09), r=0.022, r2=0.0,
                  depth=0.07, verts=4)
        T.sphere("CrownGem", "ElfCiyi", p(0, 0, top + 0.08), r=0.035, seg=6, rings=4)


# ================================================================ İÇECEKLER (DESIGN_RULES §8)
def d_bira(ox, oy):
    p = at(ox, oy)
    T.cyl("Mug", "Mese", p(0, 0, 0.08), r=0.06, depth=0.16, verts=10)
    for z in (0.03, 0.13):
        T.torus(f"MugBand{z}", "KoyuMese", p(0, 0, z), major=0.061, minor=0.007, seg=10)
    T.torus("Handle", "KoyuMese", p(0.07, 0, 0.08), major=0.035, minor=0.011, rot=(90, 0, 0), seg=8)
    T.sphere("Foam", "Kopuk", p(0, 0, 0.165), r=0.062, scale=(1, 1, 0.45), seg=10, rings=6)
    T.sphere("FoamDrip", "Kopuk", p(-0.05, -0.03, 0.14), r=0.018, scale=(1, 1, 1.6), seg=6, rings=4)


def d_sira(ox, oy):
    p = at(ox, oy)
    T.cyl("Cup", "Sira", p(0, 0, 0.055), r=0.06, r2=0.068, depth=0.11, verts=10)
    T.torus("Rim", "Kopuk", p(0, 0, 0.11), major=0.066, minor=0.006, seg=10)
    T.sphere("Apple", "Kirmizi", p(0.045, -0.03, 0.13), r=0.025, seg=6, rings=5)
    T.torus("Handle", "Sira", p(0.075, 0, 0.055), major=0.028, minor=0.009, rot=(90, 0, 0), seg=8)


def d_bal_sarabi(ox, oy):
    p = at(ox, oy)
    T.limb("HornA", "Krem", p(0.0, 0, 0.17), p(0.02, 0, 0.08), r1=0.055, r2=0.04, verts=10)
    T.limb("HornB", "Krem", p(0.02, 0, 0.08), p(0.08, 0, 0.02), r1=0.04, r2=0.02, verts=8)
    T.limb("HornTip", "SacKahve", p(0.08, 0, 0.02), p(0.13, 0, 0.02), r1=0.02, r2=0.0, verts=6)
    T.cyl("Mead", "BalSarabi", p(0, 0, 0.17), r=0.05, depth=0.01, verts=10)
    T.torus("HornRim", "Pirinc", p(0, 0, 0.17), major=0.055, minor=0.007, seg=10)


def d_sarap(ox, oy):
    p = at(ox, oy)
    T.cyl("Foot", "Tas", p(0, 0, 0.006), r=0.05, depth=0.012, verts=10)
    T.cyl("Stem", "Tas", p(0, 0, 0.05), r=0.01, depth=0.08, verts=6)
    T.dome("Bowl", "Tas", p(0, 0, 0.16), r=0.06, rot=(180, 0, 0), seg=10, rings=6)
    T.cyl("Wine", "Bordo", p(0, 0, 0.155), r=0.056, depth=0.01, verts=10)


def d_stout(ox, oy):
    p = at(ox, oy)
    T.cyl("Glass", "Stout", p(0, 0, 0.075), r=0.058, r2=0.066, depth=0.15, verts=10)
    T.cyl("GlassBase", "KoyuTas", p(0, 0, 0.012), r=0.06, depth=0.024, verts=10)
    T.sphere("Foam", "Kopuk", p(0, 0, 0.155), r=0.068, scale=(1, 1, 0.35), seg=10, rings=6)


def d_cay(ox, oy):
    p = at(ox, oy)
    T.cyl("Saucer", "Krem", p(0, 0, 0.008), r=0.08, r2=0.07, depth=0.016, verts=12)
    T.cyl("Cup", "Krem", p(0, 0, 0.05), r=0.045, r2=0.055, depth=0.07, verts=10)
    T.cyl("Tea", "Cay", p(0, 0, 0.083), r=0.05, depth=0.006, verts=10)
    T.torus("Handle", "Krem", p(0.06, 0, 0.05), major=0.022, minor=0.007, rot=(90, 0, 0), seg=8)
    T.sphere("Leaf", "Yaprak", p(-0.02, 0.01, 0.09), r=0.015, scale=(1.6, 0.8, 0.3), seg=6, rings=4)


def d_grog(ox, oy):
    p = at(ox, oy)
    T.cyl("Mug", "KoyuMese", p(0, 0, 0.07), r=0.065, r2=0.05, depth=0.14, rot=(0, 8, 0), verts=7)
    T.cyl("Grog", "Grog", p(0.008, 0, 0.14), r=0.05, depth=0.01, rot=(0, 8, 0), verts=7)
    for i, (x, y, r) in enumerate(((0.0, 0.0, 0.022), (0.025, -0.02, 0.015), (-0.02, 0.015, 0.012))):
        T.sphere(f"Bubble{i}", "Grog", p(x, y, 0.16 + i * 0.01), r=r, seg=6, rings=4)
    T.torus("Handle", "KoyuMese", p(-0.075, 0, 0.07), major=0.03, minor=0.01, rot=(90, 0, 0), seg=6)


def d_elf(ox, oy):
    p = at(ox, oy)
    T.cyl("Foot", "Krem", p(0, 0, 0.006), r=0.04, depth=0.012, verts=10)
    T.cyl("Stem", "Krem", p(0, 0, 0.04), r=0.008, depth=0.06, verts=6)
    T.cyl("Flute", "ElfCiyi", p(0, 0, 0.14), r=0.02, r2=0.035, depth=0.14, verts=10)
    for i in range(3):
        T.sphere(f"Sparkle{i}", "Kopuk", p(0.01 * (i - 1), -0.03, 0.12 + i * 0.03), r=0.006, seg=4, rings=3)


def d_ejder(ox, oy):
    p = at(ox, oy)
    T.cyl("Foot", "KoyuTas", p(0, 0, 0.01), r=0.05, depth=0.02, verts=8)
    T.cyl("Stem", "KoyuTas", p(0, 0, 0.045), r=0.015, depth=0.05, verts=6)
    T.cyl("Cup", "KoyuTas", p(0, 0, 0.11), r=0.04, r2=0.06, depth=0.08, verts=8)
    for k in range(3):
        a = k * 2 * math.pi / 3
        T.cyl(f"Spike{k}", "Altin", p(math.cos(a) * 0.058, math.sin(a) * 0.058, 0.16), r=0.01, r2=0.0, depth=0.03, verts=4)
    T.cyl("Flame", "EjderAtesi", p(0, 0, 0.19), r=0.045, r2=0.0, depth=0.12, verts=6)
    T.cyl("FlameIn", "Ates", p(0.01, -0.01, 0.18), r=0.025, r2=0.0, depth=0.08, verts=5)


def d_sut(ox, oy):
    p = at(ox, oy)
    T.sphere("Jug", "Bakir", p(0, 0, 0.065), r=0.065, scale=(1, 1, 1.05), seg=10, rings=7)
    T.cyl("Neck", "Bakir", p(0, 0, 0.14), r=0.035, r2=0.045, depth=0.05, verts=10)
    T.cyl("Milk", "Krem", p(0, 0, 0.162), r=0.04, depth=0.006, verts=10)
    T.torus("Handle", "Bakir", p(0.06, 0, 0.1), major=0.03, minor=0.01, rot=(90, 0, 0), seg=8)


def o_para(ox, oy):
    p = at(ox, oy)
    T.cyl("Coin", "Altin", p(0, 0, 0.012), r=0.06, depth=0.024, verts=12)
    T.torus("CoinRim", "Pirinc", p(0, 0, 0.024), major=0.05, minor=0.006, seg=12)


def o_para_yigini(ox, oy):
    p = at(ox, oy)
    for i, (x, y, n) in enumerate(((0, 0, 5), (0.1, 0.03, 3), (-0.08, 0.06, 4), (0.03, -0.09, 2))):
        for k in range(n):
            T.cyl(f"Coin{i}_{k}", "Altin" if k % 2 == 0 else "Pirinc", p(x + (k % 2) * 0.005, y, 0.012 + k * 0.024),
                  r=0.055, depth=0.022, verts=10)


def o_elmas(ox, oy):
    p = at(ox, oy)
    T.cyl("GemTop", "ElfCiyi", p(0, 0, 0.13), r=0.07, r2=0.045, depth=0.04, verts=6)
    T.cyl("GemBottom", "Su", p(0, 0, 0.06), r=0.0, r2=0.07, depth=0.1, verts=6)


# ================================================================ ÜRETİM İSTASYONLARI (tier 1-3)
def s_bira(ox, oy, tier=1):
    p = at(ox, oy)
    for x in (-0.22, 0.22):
        T.box(f"Cradle{x}", "KoyuMese", p(x, 0, 0.12), (0.08, 0.5, 0.24), bevel=0.015)
    barrel(p, "Keg", 0, 0, 0.4, r=0.27, length=0.6, band=TRIM[tier])
    T.cyl("Tap", TRIM[tier] if tier > 1 else "Tas", p(0, -0.3, 0.32), r=0.025, depth=0.1, rot=(90, 0, 0), verts=6)
    T.box("TapHandle", "Kirmizi", p(0, -0.34, 0.4), (0.03, 0.03, 0.1), bevel=0.008)
    T.cyl("DripTray", "KoyuMese", p(0, -0.3, 0.03), r=0.09, depth=0.06, verts=8)
    if tier >= 2:
        barrel(p, "Keg2", 0, 0.05, 0.85, r=0.17, length=0.42, band=TRIM[tier])
    tier_extras(p, tier, 1.05 if tier >= 2 else 0.7, front_y=-0.29)


def s_pres(ox, oy, tier=1):
    p = at(ox, oy)
    T.box("Base", "KoyuMese", p(0, 0, 0.06), (0.7, 0.55, 0.12), bevel=0.02)
    T.cyl("Tub", "Mese", p(0, 0, 0.3), r=0.24, depth=0.36, verts=12)
    T.torus("TubBand", TRIM[tier], p(0, 0, 0.42), major=0.245, minor=0.016, seg=12)
    for i, (x, y) in enumerate(((0.1, -0.05), (-0.08, 0.06), (0.0, 0.1), (-0.1, -0.08), (0.08, 0.1))):
        T.sphere(f"Apple{i}", "Kirmizi" if i % 2 == 0 else "Yaprak", p(x, y, 0.5), r=0.06, seg=8, rings=6)
    for x in (-0.3, 0.3):
        T.box(f"Post{x}", "KoyuMese", p(x, 0, 0.55), (0.07, 0.07, 0.9), bevel=0.012)
    T.box("Beam", "KoyuMese", p(0, 0, 1.0), (0.72, 0.1, 0.1), bevel=0.015)
    T.cyl("Screw", TRIM[tier] if tier > 1 else "Tas", p(0, 0, 0.78), r=0.035, depth=0.45, verts=8)
    T.cyl("Handle", "Mese", p(0, 0, 0.64), r=0.02, depth=0.5, rot=(0, 90, 0), verts=6)
    T.box("Spout", "Mese", p(0, -0.3, 0.16), (0.08, 0.14, 0.04))
    tier_extras(p, tier, 1.06, front_y=-0.29)


def s_bal(ox, oy, tier=1):
    p = at(ox, oy)
    T.cyl("Hearth", "Tas", p(0, 0, 0.12), r=0.32, r2=0.28, depth=0.24, verts=10)
    T.sphere("Fire", "Ates", p(0, -0.24, 0.12), r=0.07, scale=(1, 0.5, 1), seg=6, rings=4)
    T.sphere("Pot", "Bakir", p(0, 0, 0.42), r=0.3, scale=(1, 1, 0.8), seg=14, rings=9)
    T.torus("PotRim", TRIM[tier] if tier > 1 else "Bakir", p(0, 0, 0.62), major=0.24, minor=0.025, seg=14)
    T.cyl("Honey", "BalSarabi", p(0, 0, 0.61), r=0.22, depth=0.02, verts=12)
    for i in range(3):
        T.dome(f"Hive{i}", "SacSari", p(0.38, 0.12, 0.05 + i * 0.12), r=0.15 - i * 0.03, scale=(1, 1, 0.9))
    T.sphere("Bee", "Altin", p(0.25, -0.15, 0.85), r=0.025, scale=(1.3, 1, 1), seg=6, rings=4)
    tier_extras(p, tier, 0.75, front_y=-0.31)


def s_sarap(ox, oy, tier=1):
    p = at(ox, oy)
    T.box("RackSideL", "KoyuMese", p(-0.4, 0, 0.45), (0.06, 0.4, 0.9), bevel=0.01)
    T.box("RackSideR", "KoyuMese", p(0.4, 0, 0.45), (0.06, 0.4, 0.9), bevel=0.01)
    T.box("RackShelf", "KoyuMese", p(0, 0, 0.45), (0.8, 0.4, 0.04))
    T.box("RackTop", "KoyuMese", p(0, 0, 0.9), (0.86, 0.42, 0.05), bevel=0.01)
    for i, (x, z) in enumerate(((-0.18, 0.22), (0.18, 0.22), (0, 0.66))):
        T.cyl(f"WineKeg{i}", "Bordo", p(x, 0, z), r=0.16, depth=0.36, rot=(90, 0, 0), verts=10)
        for d in (-0.12, 0.12):
            T.torus(f"WineBand{i}{d}", TRIM[tier] if tier > 1 else "KoyuMese", p(x, d, z), major=0.162, minor=0.012,
                    rot=(90, 0, 0), seg=10)
        T.cyl(f"WineTap{i}", "Tas", p(x, -0.2, z - 0.06), r=0.015, depth=0.06, rot=(90, 0, 0), verts=5)
    for i in range(2):
        T.cyl(f"Bottle{i}", "Bordo", p(-0.25 + i * 0.12, 0.05, 1.02), r=0.03, depth=0.18, verts=6)
    tier_extras(p, tier, 1.12, front_y=-0.23)


def s_sut(ox, oy, tier=1):
    p = at(ox, oy)
    T.box("Pallet", "Mese", p(0, 0, 0.04), (0.75, 0.5, 0.08), bevel=0.01)
    for i, x in enumerate((-0.18, 0.18)):
        T.cyl(f"Churn{i}", "Tas", p(x, 0, 0.32), r=0.15, r2=0.12, depth=0.46, verts=10)
        T.cyl(f"ChurnNeck{i}", "Tas", p(x, 0, 0.6), r=0.08, r2=0.1, depth=0.1, verts=10)
        T.cyl(f"ChurnLid{i}", TRIM[tier] if tier > 1 else "KoyuTas", p(x, 0, 0.66), r=0.105, depth=0.03, verts=10)
        T.torus(f"ChurnBand{i}", TRIM[tier] if tier > 1 else "KoyuTas", p(x, 0, 0.2), major=0.145, minor=0.012, seg=10)
    T.cyl("Bucket", "Mese", p(0.0, -0.18, 0.15), r=0.08, r2=0.1, depth=0.14, verts=8)
    T.cyl("BucketMilk", "Krem", p(0.0, -0.18, 0.215), r=0.095, depth=0.01, verts=8)
    tier_extras(p, tier, 0.7, front_y=-0.27)


def s_stout(ox, oy, tier=1):
    p = at(ox, oy)
    T.cyl("Base", "Tas", p(0, 0, 0.06), r=0.36, depth=0.12, verts=10)
    barrel(p, "Vat", 0, 0, 0.55, r=0.3, length=0.8, body="KoyuMese", band=TRIM[tier] if tier > 1 else "KoyuTas", lying=False)
    T.limb("Pipe", "Bakir", p(0.15, 0.1, 0.95), p(0.15, 0.1, 1.35), r1=0.04, verts=8)
    T.limb("PipeBend", "Bakir", p(0.15, 0.1, 1.35), p(0.35, 0.1, 1.45), r1=0.04, verts=8)
    T.cyl("Tap", "Tas", p(0, -0.33, 0.3), r=0.025, depth=0.1, rot=(90, 0, 0), verts=6)
    T.sphere("Steam", "Kopuk", p(0.4, 0.1, 1.55), r=0.06, seg=6, rings=4)
    tier_extras(p, tier, 1.0, front_y=-0.32)


def s_cay(ox, oy, tier=1):
    p = at(ox, oy)
    T.box("Stove", "Tas", p(0, 0, 0.3), (0.6, 0.5, 0.6), bevel=0.04)
    T.box("StoveMouth", "KoyuTas", p(0, -0.24, 0.22), (0.25, 0.04, 0.18), bevel=0.02)
    T.sphere("Fire", "Ates", p(0, -0.25, 0.2), r=0.06, scale=(1.2, 0.4, 1), seg=6, rings=4)
    T.sphere("Kettle", "Bakir", p(-0.1, 0, 0.72), r=0.15, scale=(1, 1, 0.85), seg=12, rings=8)
    T.limb("KettleSpout", "Bakir", p(-0.2, -0.05, 0.7), p(-0.32, -0.1, 0.8), r1=0.03, r2=0.015, verts=6)
    T.torus("KettleHandle", TRIM[tier] if tier > 1 else "KoyuTas", p(-0.1, 0, 0.86), major=0.09, minor=0.012, rot=(90, 0, 0), seg=10)
    for i in range(3):
        T.cyl(f"Cup{i}", "Krem", p(0.14 + (i % 2) * 0.1, -0.1 + i * 0.1, 0.64), r=0.035, depth=0.06, verts=8)
    T.sphere("Steam", "Kopuk", p(-0.34, -0.12, 0.9), r=0.04, seg=6, rings=4)
    tier_extras(p, tier, 0.95, front_y=-0.26)


def s_grog(ox, oy, tier=1):
    p = at(ox, oy)
    for i, a in enumerate((0, 60, 120)):
        T.cyl(f"Log{i}", "KoyuMese", p(0, 0, 0.05), r=0.05, depth=0.6, rot=(90, 0, a), verts=6)
    T.sphere("Fire", "Ates", p(0, 0, 0.12), r=0.12, scale=(1, 1, 0.6), seg=6, rings=4)
    T.sphere("Cauldron", "KoyuTas", p(0, 0, 0.42), r=0.32, scale=(1, 1, 0.85), rot=(0, 8, 0), seg=12, rings=8)
    T.torus("CauldronRim", TRIM[tier] if tier > 1 else "KoyuTas", p(0.02, 0, 0.66), major=0.25, minor=0.03, rot=(0, 8, 0), seg=12)
    T.cyl("Brew", "Grog", p(0.02, 0, 0.65), r=0.23, depth=0.02, rot=(0, 8, 0), verts=12)
    for i, (x, y, r) in enumerate(((0.05, 0.0, 0.07), (-0.1, 0.06, 0.05), (0.1, -0.1, 0.04), (-0.05, -0.08, 0.035))):
        T.sphere(f"Bubble{i}", "Grog", p(x, y, 0.7 + r * 0.5), r=r, seg=6, rings=4)
    T.limb("Ladle", "Mese", p(-0.1, 0.05, 0.6), p(-0.3, 0.15, 1.0), r1=0.02, verts=5)
    tier_extras(p, tier, 0.82, front_y=-0.33)


def s_kristal(ox, oy, tier=1):
    p = at(ox, oy)
    T.cyl("Basin", "Tas", p(0, 0, 0.18), r=0.4, r2=0.42, depth=0.36, verts=12)
    T.torus("BasinRim", TRIM[tier] if tier > 1 else "Tas", p(0, 0, 0.36), major=0.4, minor=0.035, seg=12)
    T.cyl("Water", "Su", p(0, 0, 0.33), r=0.37, depth=0.02, verts=12)
    T.cyl("Pillar", "Tas", p(0, 0, 0.5), r=0.08, depth=0.35, verts=8)
    for i, (x, y, h, r) in enumerate(((0, 0, 0.55, 0.11), (0.1, 0.05, 0.35, 0.07), (-0.09, 0.06, 0.4, 0.07), (0.02, -0.1, 0.3, 0.06))):
        T.cyl(f"Crystal{i}", "ElfCiyi", p(x, y, 0.65 + h / 2), r=r, r2=0.0, depth=h, rot=(x * 80, -y * 80, 0), verts=6)
    for i in range(4):
        a = i * math.pi / 2 + 0.4
        T.sphere(f"Drop{i}", "Su", p(math.cos(a) * 0.2, math.sin(a) * 0.2, 0.55), r=0.025, scale=(1, 1, 1.6), seg=6, rings=4)
    tier_extras(p, tier, 1.25, front_y=-0.41)


def s_ejder(ox, oy, tier=1):
    p = at(ox, oy)
    T.box("Plinth", "KoyuTas", p(0, 0, 0.15), (0.7, 0.6, 0.3), bevel=0.04)
    T.sphere("Head", "KoyuTas", p(0, 0.02, 0.62), r=0.27, scale=(1, 1.1, 0.95), seg=12, rings=8)
    T.box("Snout", "KoyuTas", p(0, -0.25, 0.55), (0.3, 0.3, 0.2), bevel=0.05)
    T.box("Jaw", "KoyuTas", p(0, -0.24, 0.4), (0.26, 0.28, 0.08), rot=(-12, 0, 0), bevel=0.03)
    for s in (1, -1):
        T.limb(f"Horn_{s}", TRIM[tier] if tier > 1 else "Krem", p(s * 0.15, 0.1, 0.8), p(s * 0.3, 0.3, 1.05), r1=0.05, r2=0.0, verts=6)
        T.sphere(f"Eye_{s}", "Ates", p(s * 0.13, -0.17, 0.72), r=0.04, seg=6, rings=4)
        T.sphere(f"Nostril_{s}", "Goz", p(s * 0.07, -0.4, 0.6), r=0.022, seg=6, rings=4)
    T.cyl("Flame", "EjderAtesi", p(0, -0.43, 0.47), r=0.09, r2=0.0, depth=0.22, rot=(-80, 0, 0), verts=6)
    T.cyl("FlameIn", "Ates", p(0, -0.42, 0.47), r=0.05, r2=0.0, depth=0.18, rot=(-80, 0, 0), verts=5)
    tier_extras(p, tier, 1.0, front_y=-0.31)


# ================================================================ MOBİLYA
def f_tezgah(ox, oy):
    p = at(ox, oy)
    T.box("Body", "Mese", p(0, 0, 0.42), (1.5, 0.5, 0.84), bevel=0.02)
    T.box("Top", "KoyuMese", p(0, -0.03, 0.87), (1.6, 0.6, 0.07), bevel=0.02)
    for x in (-0.5, 0.0, 0.5):
        T.box(f"Panel{x}", "KoyuMese", p(x, -0.255, 0.42), (0.4, 0.02, 0.6), bevel=0.01)
    T.box("Kick", "KoyuMese", p(0, -0.24, 0.04), (1.5, 0.04, 0.08))
    for i in range(3):
        T.cyl(f"Mug{i}", "Mese", p(-0.5 + i * 0.12, 0.05, 0.96), r=0.045, depth=0.11, verts=8)


def f_servis(ox, oy):
    p = at(ox, oy)
    T.box("Body", "Mese", p(0, 0, 0.36), (1.0, 0.45, 0.72), bevel=0.02)
    T.box("Top", "Pirinc", p(0, 0, 0.75), (1.06, 0.5, 0.05), bevel=0.01)
    for i in range(4):
        T.cyl(f"Mat{i}", "KoyuMese", p(-0.36 + i * 0.24, 0, 0.78), r=0.08, depth=0.01, verts=10)
    T.box("Bell", "Altin", p(0.45, 0.15, 0.8), (0.06, 0.06, 0.04), bevel=0.01)


def f_masa_yuvarlak(ox, oy):
    p = at(ox, oy)
    T.cyl("Top", "Mese", p(0, 0, 0.6), r=0.4, depth=0.06, verts=14)
    T.torus("Edge", "KoyuMese", p(0, 0, 0.6), major=0.4, minor=0.03, seg=14)
    T.cyl("Pedestal", "KoyuMese", p(0, 0, 0.32), r=0.06, r2=0.05, depth=0.55, verts=8)
    for a in (0, 120, 240):
        r = math.radians(a)
        T.box(f"Foot{a}", "KoyuMese", p(math.cos(r) * 0.15, math.sin(r) * 0.15, 0.03), (0.3, 0.07, 0.06), rot=(0, 0, a), bevel=0.01)
    T.cyl("Candle", "Krem", p(0.1, 0.1, 0.68), r=0.025, depth=0.1, verts=6)
    T.sphere("CandleFlame", "Ates", p(0.1, 0.1, 0.75), r=0.015, scale=(1, 1, 1.6), seg=5, rings=4)


def f_masa_kare(ox, oy):
    p = at(ox, oy)
    T.box("Top", "Mese", p(0, 0, 0.6), (0.95, 0.95, 0.07), bevel=0.02)
    for x in (-0.38, 0.38):
        for y in (-0.38, 0.38):
            T.box(f"Leg{x}{y}", "KoyuMese", p(x, y, 0.29), (0.08, 0.08, 0.58), bevel=0.01)
    T.box("Runner", "Kirmizi", p(0, 0, 0.64), (0.25, 0.97, 0.01))


def f_masa_uzun(ox, oy):
    p = at(ox, oy)
    T.box("Top", "Mese", p(0, 0, 0.6), (1.9, 0.8, 0.07), bevel=0.02)
    for x in (-0.75, 0.75):
        T.box(f"Trestle{x}", "KoyuMese", p(x, 0, 0.29), (0.1, 0.6, 0.58), bevel=0.015)
    T.box("Stretcher", "KoyuMese", p(0, 0, 0.18), (1.5, 0.07, 0.07))
    for x in (-0.5, 0.5):
        T.cyl(f"Candle{x}", "Krem", p(x, 0, 0.68), r=0.025, depth=0.1, verts=6)


def f_tabure(ox, oy):
    p = at(ox, oy)
    T.cyl("Seat", "Mese", p(0, 0, 0.4), r=0.18, depth=0.06, verts=10)
    for a in (0, 120, 240):
        r = math.radians(a)
        T.limb(f"Leg{a}", "KoyuMese", p(math.cos(r) * 0.1, math.sin(r) * 0.1, 0.38), p(math.cos(r) * 0.16, math.sin(r) * 0.16, 0.0),
               r1=0.025, verts=5)


def f_sandalye(ox, oy):
    p = at(ox, oy)
    T.box("Seat", "Mese", p(0, 0, 0.4), (0.4, 0.4, 0.06), bevel=0.01)
    for x in (-0.16, 0.16):
        for y in (-0.16, 0.16):
            T.box(f"Leg{x}{y}", "KoyuMese", p(x, y, 0.19), (0.05, 0.05, 0.38))
    for x in (-0.16, 0.16):
        T.box(f"BackPost{x}", "KoyuMese", p(x, 0.18, 0.65), (0.05, 0.05, 0.5))
    T.box("BackRail", "Mese", p(0, 0.18, 0.82), (0.4, 0.05, 0.12), bevel=0.01)
    T.box("BackRail2", "Mese", p(0, 0.18, 0.62), (0.36, 0.04, 0.06))


def f_koltuk(ox, oy):
    p = at(ox, oy)
    T.box("Base", "KoyuMese", p(0, 0, 0.22), (0.56, 0.5, 0.36), bevel=0.03)
    T.box("Cushion", "Kirmizi", p(0, -0.02, 0.44), (0.46, 0.44, 0.1), bevel=0.04)
    T.box("Back", "KoyuMese", p(0, 0.22, 0.7), (0.56, 0.08, 0.6), bevel=0.03)
    T.box("BackCushion", "Kirmizi", p(0, 0.17, 0.7), (0.44, 0.06, 0.42), bevel=0.03)
    for x in (-0.26, 0.26):
        T.box(f"Arm{x}", "KoyuMese", p(x, 0, 0.55), (0.08, 0.5, 0.12), bevel=0.025)
    T.sphere("Stud", "Altin", p(0, 0.27, 0.98), r=0.03, seg=6, rings=4)


def f_legen(ox, oy):
    p = at(ox, oy)
    T.box("Stand", "KoyuMese", p(0, 0, 0.25), (0.6, 0.45, 0.5), bevel=0.02)
    T.cyl("Tub", "Mese", p(0, 0, 0.6), r=0.26, r2=0.3, depth=0.2, verts=12)
    T.torus("TubBand", "KoyuMese", p(0, 0, 0.66), major=0.29, minor=0.015, seg=12)
    T.cyl("Water", "Su", p(0, 0, 0.68), r=0.27, depth=0.02, verts=12)
    for i in range(3):
        T.sphere(f"Suds{i}", "Kopuk", p(-0.1 + i * 0.1, 0.05 * (i - 1), 0.7), r=0.05, seg=6, rings=4)
    T.cyl("DirtyMug", "Mese", p(0.15, -0.05, 0.72), r=0.045, depth=0.1, rot=(30, 0, 0), verts=8)


def f_sandik(ox, oy):
    p = at(ox, oy)
    T.box("Chest", "Mese", p(0, 0, 0.2), (0.7, 0.45, 0.4), bevel=0.02)
    T.cyl("Lid", "Mese", p(0, 0.05, 0.42), r=0.22, depth=0.7, rot=(0, 90, 0), scale=(1, 1, 0.6), verts=10)
    for x in (-0.25, 0.25):
        T.box(f"Strap{x}", "Altin", p(x, 0, 0.3), (0.05, 0.47, 0.6), bevel=0.005)
    T.box("Lock", "Altin", p(0, -0.24, 0.35), (0.08, 0.03, 0.1), bevel=0.01)
    for i in range(5):
        T.cyl(f"Coin{i}", "Altin", p(-0.15 + i * 0.07, -0.12 + (i % 2) * 0.05, 0.53 + (i % 3) * 0.02), r=0.04, depth=0.015,
              rot=(10 * i, 0, 0), verts=8)


# ================================================================ DEKOR
def x_somine(ox, oy):
    p = at(ox, oy)
    T.box("Stone", "Tas", p(0, 0, 0.55), (1.2, 0.5, 1.1), bevel=0.04)
    T.box("Opening", "Goz", p(0, -0.21, 0.35), (0.65, 0.1, 0.5), bevel=0.04)
    T.box("Mantle", "KoyuMese", p(0, -0.05, 0.95), (1.3, 0.6, 0.09), bevel=0.02)
    T.box("Chimney", "Tas", p(0, 0.05, 1.45), (0.7, 0.4, 0.9), bevel=0.03)
    for i, a in enumerate((-20, 20)):
        T.cyl(f"Log{i}", "KoyuMese", p(0, -0.25, 0.16), r=0.06, depth=0.5, rot=(0, 90, a), verts=6)
    T.cyl("Fire", "Ates", p(0, -0.26, 0.32), r=0.16, r2=0.0, depth=0.32, verts=6)
    T.cyl("FireIn", "Altin", p(0.02, -0.28, 0.28), r=0.09, r2=0.0, depth=0.22, verts=5)
    for i in range(2):
        T.cyl(f"MantleCandle{i}", "Krem", p(-0.45 + i * 0.9, -0.1, 1.05), r=0.03, depth=0.12, verts=6)
    T.cyl("MantleMug", "Mese", p(0.15, -0.05, 1.04), r=0.05, depth=0.1, verts=8)


def x_avize(ox, oy):
    p = at(ox, oy)
    T.torus("Ring", "KoyuMese", p(0, 0, 0.8), major=0.45, minor=0.04, seg=16)
    for k in range(6):
        a = k * math.pi / 3
        x, y = math.cos(a) * 0.45, math.sin(a) * 0.45
        T.cyl(f"Candle{k}", "Krem", p(x, y, 0.89), r=0.025, depth=0.12, verts=6)
        T.sphere(f"Flame{k}", "Ates", p(x, y, 0.97), r=0.017, scale=(1, 1, 1.6), seg=5, rings=4)
        T.limb(f"Chain{k}", "KoyuTas", p(x * 0.9, y * 0.9, 0.82), p(0, 0, 1.4), r1=0.008, verts=4)
    T.cyl("Hook", "KoyuTas", p(0, 0, 1.45), r=0.03, depth=0.1, verts=6)


def x_mesale(ox, oy):
    p = at(ox, oy)
    T.box("WallPlate", "KoyuTas", p(0, 0.05, 0.9), (0.14, 0.03, 0.2), bevel=0.01)
    T.limb("Arm", "KoyuTas", p(0, 0.04, 0.85), p(0, -0.12, 0.95), r1=0.015, verts=5)
    T.limb("Torch", "KoyuMese", p(0, -0.12, 0.85), p(0, -0.16, 1.2), r1=0.025, r2=0.035, verts=6)
    T.cyl("Flame", "Ates", p(0, -0.16, 1.28), r=0.06, r2=0.0, depth=0.16, verts=6)
    T.cyl("FlameIn", "Altin", p(0, -0.165, 1.26), r=0.035, r2=0.0, depth=0.1, verts=5)


def x_mum(ox, oy):
    p = at(ox, oy)
    T.cyl("Plate", "Pirinc", p(0, 0, 0.01), r=0.1, depth=0.02, verts=10)
    for i, (x, h) in enumerate(((0, 0.22), (0.06, 0.15), (-0.05, 0.12))):
        T.cyl(f"Candle{i}", "Krem", p(x, (i - 1) * 0.03, h / 2 + 0.02), r=0.03, depth=h, verts=6)
        T.sphere(f"Flame{i}", "Ates", p(x, (i - 1) * 0.03, h + 0.04), r=0.016, scale=(1, 1, 1.6), seg=5, rings=4)
        T.sphere(f"Drip{i}", "Krem", p(x + 0.02, (i - 1) * 0.03 - 0.02, h - 0.02), r=0.012, scale=(1, 1, 2), seg=4, rings=3)


def x_saksi(ox, oy):
    p = at(ox, oy)
    T.cyl("Pot", "Bakir", p(0, 0, 0.17), r=0.15, r2=0.2, depth=0.34, verts=10)
    T.torus("PotRim", "Bakir", p(0, 0, 0.34), major=0.2, minor=0.025, seg=10)
    T.cyl("Soil", "KoyuMese", p(0, 0, 0.33), r=0.18, depth=0.02, verts=10)
    for i, (x, y, z, r) in enumerate(((0, 0, 0.6, 0.17), (0.12, 0.05, 0.48, 0.12), (-0.11, -0.04, 0.5, 0.13),
                                      (0.02, -0.1, 0.75, 0.1), (-0.04, 0.1, 0.7, 0.11))):
        T.sphere(f"Leaf{i}", "Yaprak" if i % 2 == 0 else "Yesil", p(x, y, z), r=r, seg=8, rings=6)
    T.sphere("Flower", "Kirmizi", p(0.08, -0.12, 0.66), r=0.04, seg=6, rings=4)


def x_hali(ox, oy):
    p = at(ox, oy)
    T.box("Rug", "Kirmizi", p(0, 0, 0.008), (1.6, 1.0, 0.016), bevel=0.004)
    T.box("Inner", "Bordo", p(0, 0, 0.012), (1.3, 0.7, 0.016))
    T.box("Center", "Altin", p(0, 0, 0.016), (0.3, 0.3, 0.016), rot=(0, 0, 45))
    for x in (-0.82, 0.82):
        for k in range(5):
            T.box(f"Fringe{x}{k}", "Krem", p(x, -0.4 + k * 0.2, 0.006), (0.05, 0.03, 0.012))


def x_flama(ox, oy):
    p = at(ox, oy)
    T.cyl("Base", "KoyuTas", p(0, 0, 0.04), r=0.15, depth=0.08, verts=8)
    T.cyl("Pole", "KoyuMese", p(0, 0, 0.85), r=0.025, depth=1.6, verts=6)
    T.sphere("Finial", "Altin", p(0, 0, 1.68), r=0.045, seg=6, rings=4)
    T.box("Bar", "KoyuMese", p(0, 0, 1.55), (0.6, 0.03, 0.03))
    T.box("Banner", "Mavi", p(0, -0.02, 1.2), (0.55, 0.02, 0.65), bevel=0.005)
    T.cyl("BannerTail", "Mavi", p(0, -0.02, 0.78), r=0.28, r2=0.0, depth=0.2, rot=(180, 0, 0), scale=(1, 0.07, 1), verts=3)
    T.sphere("Emblem", "Altin", p(0, -0.04, 1.25), r=0.08, scale=(1, 0.3, 1), seg=8, rings=6)


def x_kalkan(ox, oy):
    p = at(ox, oy)
    T.box("Plaque", "KoyuMese", p(0, 0.04, 1.1), (0.7, 0.04, 0.5), bevel=0.02)
    for s in (1, -1):
        T.limb(f"Sword_{s}", "Tas", p(-s * 0.32, -0.02, 0.85), p(s * 0.32, -0.02, 1.38), r1=0.03, r2=0.008, verts=4)
        T.box(f"Guard_{s}", "Pirinc", p(-s * 0.24, -0.03, 0.92), (0.14, 0.03, 0.03), rot=(0, s * -40, 0))
    T.cyl("Shield", "Kirmizi", p(0, -0.05, 1.1), r=0.25, depth=0.05, rot=(90, 0, 0), verts=12)
    T.torus("ShieldRim", "Pirinc", p(0, -0.08, 1.1), major=0.25, minor=0.02, rot=(90, 0, 0), seg=12)
    T.sphere("Boss", "Pirinc", p(0, -0.09, 1.1), r=0.07, scale=(1, 0.5, 1), seg=8, rings=6)


def x_boynuz(ox, oy):
    p = at(ox, oy)
    T.cyl("Plaque", "KoyuMese", p(0, 0.04, 1.1), r=0.2, depth=0.04, rot=(90, 0, 0), scale=(1, 1.3, 1), verts=8)
    T.sphere("Skull", "Krem", p(0, -0.04, 1.1), r=0.1, scale=(0.9, 0.7, 1.2), seg=8, rings=6)
    for s in (1, -1):
        b = V(p(s * 0.06, -0.04, 1.2))
        tip = b + V((s * 0.35, 0, 0.35))
        T.limb(f"Antler_{s}", "Krem", b, tip, r1=0.025, r2=0.012, verts=5)
        for k, f in enumerate((0.35, 0.65)):
            m = b + (tip - b) * f
            T.limb(f"Tine_{s}{k}", "Krem", m, m + V((s * 0.02, 0, 0.15)), r1=0.014, r2=0.005, verts=4)


def x_fici_yigini(ox, oy):
    p = at(ox, oy)
    for i, (x, z) in enumerate(((-0.3, 0.22), (0.3, 0.22), (0, 0.62))):
        barrel(p, f"Stack{i}", x, 0, z, r=0.21, length=0.5, lying=True)
    T.box("Sack", "Bez", p(0.65, 0.05, 0.2), (0.3, 0.25, 0.4), bevel=0.08)


def x_sahne(ox, oy):
    p = at(ox, oy)
    T.cyl("Stage", "Mese", p(0, 0.2, 0.15), r=1.0, depth=0.3, verts=14, scale=(1, 0.7, 1))
    T.torus("StageEdge", "KoyuMese", p(0, 0.2, 0.3), major=1.0, minor=0.03, scale=(1, 0.7, 1), seg=16)
    for s in (1, -1):
        T.cyl(f"Post_{s}", "KoyuMese", p(s * 0.85, 0.55, 1.1), r=0.05, depth=1.6, verts=6)
        T.box(f"Curtain_{s}", "Kirmizi", p(s * 0.7, 0.55, 1.2), (0.3, 0.06, 1.3), bevel=0.03)
    T.box("Valance", "Kirmizi", p(0, 0.55, 1.9), (1.8, 0.08, 0.2), bevel=0.02)
    T.box("ValanceTrim", "Altin", p(0, 0.5, 1.8), (1.8, 0.02, 0.03))
    T.cyl("Stool", "Mese", p(0, 0.3, 0.5), r=0.15, depth=0.05, verts=8)
    T.cyl("StoolLeg", "KoyuMese", p(0, 0.3, 0.4), r=0.03, depth=0.2, verts=5)


def x_muhur(ox, oy):
    p = at(ox, oy)
    T.box("Frame", "Altin", p(0, 0.04, 1.1), (0.7, 0.05, 0.9), bevel=0.02)
    T.box("Velvet", "Mor", p(0, 0.01, 1.1), (0.6, 0.03, 0.8), bevel=0.01)
    T.cyl("Seal", "Bordo", p(0, -0.02, 1.05), r=0.18, depth=0.03, rot=(90, 0, 0), verts=12)
    T.cyl("CrownBand", "Altin", p(0, -0.04, 1.08), r=0.09, depth=0.02, rot=(90, 0, 0), scale=(1.2, 1, 0.5), verts=8)
    for k in range(3):
        T.cyl(f"CrownTip{k}", "Altin", p(-0.07 + k * 0.07, -0.04, 1.15), r=0.025, r2=0.0, depth=0.08, verts=4)
    T.box("Ribbon", "Bordo", p(0, -0.02, 0.8), (0.12, 0.02, 0.25), bevel=0.005)


# ================================================================ YAPI
def floor_bed(p, mat="KoyuMese"):
    """Tahta aralarından boşluk (siyah) görünmesin: karonun altına tam kaplayan koyu taban (v0.9, kayan çizgi düzeltmesi)."""
    T.box("Bed", mat, p(0, 0, 0.015), (1.0, 1.0, 0.03))


def b_zemin(ox, oy):
    p = at(ox, oy)
    floor_bed(p)
    for i in range(4):
        # Uçlarda pah yok: karo sınırında yatay oluk (kamera kaydıkça titreyen çizgi) oluşmasın
        T.box(f"Plank{i}", "Mese" if i % 2 == 0 else "KoyuMese", p(-0.375 + i * 0.25, 0, 0.02), (0.245, 1.0, 0.04))
        T.sphere(f"Nail{i}", "KoyuTas", p(-0.375 + i * 0.25, -0.44, 0.04), r=0.01, seg=4, rings=3)


# ---- Zemin temaları (v0.6, Dükkân > Dekor). Üst yüzey her temada 0.04 m (titreme olmasın).
def b_zemin_parke(ox, oy):
    """Koyu parke: dar, kaydırmalı tahtalar."""
    p = at(ox, oy)
    floor_bed(p)
    for i in range(6):
        x = -0.4167 + i * 0.1667
        off = 0.25 if i % 2 else 0.0
        for k, (y0, ln) in enumerate(((-0.5 + off, 0.5), (off, 0.5))):
            ln2 = min(ln, 0.5 - y0) if y0 + ln > 0.5 else ln
            if ln2 <= 0.02:
                continue
            T.box(f"P{i}{k}", "KoyuMese" if (i + k) % 2 else "Ten5", p(x, y0 + ln2 / 2, 0.02), (0.163, ln2 - 0.004, 0.04))
        if off:
            T.box(f"PEnd{i}", "KoyuMese", p(x, -0.375, 0.02), (0.163, 0.24, 0.04))


def b_zemin_tas(ox, oy):
    """Taş döşeme: derzli dört büyük taş."""
    p = at(ox, oy)
    T.box("Bed", "KoyuTas", p(0, 0, 0.015), (1.0, 1.0, 0.03))
    for i, (x, y) in enumerate(((-0.25, -0.25), (0.25, -0.25), (-0.25, 0.25), (0.25, 0.25))):
        T.box(f"Slab{i}", "Tas" if i % 3 else "Siva", p(x, y, 0.025), (0.47, 0.47, 0.03), rot=(0, 0, (i * 5) % 7 - 3), bevel=0.01)


def b_zemin_dama(ox, oy):
    """Dama karo: krem ve koyu taş 2x2."""
    p = at(ox, oy)
    floor_bed(p, "KoyuTas")
    for i, (x, y) in enumerate(((-0.25, -0.25), (0.25, -0.25), (-0.25, 0.25), (0.25, 0.25))):
        T.box(f"Tile{i}", "Krem" if i in (0, 3) else "KoyuTas", p(x, y, 0.02), (0.5, 0.5, 0.04), bevel=0.003)


def b_zemin_altin(ox, oy):
    """Altın tema zemini (v1.0 Hazine): krem mermer karolar, altın derzler ve köşe taşları."""
    p = at(ox, oy)
    floor_bed(p, "Altin")
    for i, (x, y) in enumerate(((-0.25, -0.25), (0.25, -0.25), (-0.25, 0.25), (0.25, 0.25))):
        T.box(f"Tile{i}", "Krem" if i in (0, 3) else "Siva", p(x, y, 0.02), (0.49, 0.49, 0.04))
    T.box("Center", "Altin", p(0, 0, 0.041), (0.12, 0.12, 0.002), rot=(0, 0, 45))


def wall_base(p, door=False, window=False, theme="Badana"):
    """Duvar parçası (1 m). Tema (v0.6, Dükkân > Dekor): Badana (varsayılan), Lambri, Tas, Siva (kırmızı sıva).
    Profil ve yükseklik her temada aynı → oyunda birbirinin yerine geçer."""
    plaster = {"Badana": "Siva", "Siva": "Kirmizi", "Lambri": "Mese", "Tas": "Tas", "Altin": "Krem"}[theme]
    base = {"Badana": "Tas", "Siva": "Tas", "Lambri": "KoyuMese", "Tas": "KoyuTas", "Altin": "Altin"}[theme]
    if door:
        for x in (-0.38, 0.38):
            T.box(f"StoneSide{x}", base, p(x, 0, 0.3), (0.24, 0.2, 0.6), bevel=0.015)
            T.box(f"PlasterSide{x}", plaster, p(x, 0, 1.0), (0.24, 0.16, 0.8))
        T.box("Lintel", "KoyuMese", p(0, 0, 1.38), (1.0, 0.22, 0.12), bevel=0.01)
    else:
        T.box("Stone", base, p(0, 0, 0.3), (1.0, 0.2, 0.6), bevel=0.015)
        for i in range(3):
            T.box(f"StoneJoint{i}", "KoyuTas" if theme != "Tas" else "Tas", p(-0.33 + i * 0.33, -0.101, 0.3), (0.02, 0.005, 0.6))
        if window:
            for x in (-0.36, 0.36):
                T.box(f"PlasterSide{x}", plaster, p(x, 0, 1.0), (0.28, 0.16, 0.8))
            T.box("Sill", "KoyuMese", p(0, 0, 0.64), (0.5, 0.24, 0.06))
            T.box("Glass", "Su", p(0, 0, 1.0), (0.44, 0.04, 0.66))
            T.box("MullionV", "KoyuMese", p(0, -0.02, 1.0), (0.04, 0.05, 0.68))
            T.box("MullionH", "KoyuMese", p(0, -0.02, 1.0), (0.44, 0.05, 0.04))
        else:
            T.box("Plaster", plaster, p(0, 0, 1.0), (1.0, 0.16, 0.8))
        # Tema ayrıntıları (ön yüzde, 1 cm dışarıda)
        if theme == "Lambri":
            for i in range(5):
                x = -0.4 + i * 0.2
                if window and abs(x) < 0.3:
                    continue
                T.box(f"Board{i}", "KoyuMese", p(x, -0.085, 1.0), (0.015, 0.01, 0.8))
            T.box("Rail", "KoyuMese", p(0, -0.09, 0.62), (1.0, 0.02, 0.05))
        elif theme == "Tas":
            for row, z in enumerate((0.72, 0.98, 1.24)):
                T.box(f"Course{row}", "KoyuTas", p(0, -0.085, z), (1.0, 0.01, 0.015))
                for k in range(3):
                    x = -0.33 + k * 0.33 + (0.16 if row % 2 else 0.0)
                    if window and abs(x) < 0.3 or abs(x) > 0.49:
                        continue
                    T.box(f"Joint{row}{k}", "KoyuTas", p(x, -0.085, z + 0.13), (0.015, 0.01, 0.25))
    T.box("TopBeam", "KoyuMese", p(0, 0, 1.45), (1.0, 0.22, 0.1), bevel=0.01)
    if not door and theme in ("Badana", "Siva"):
        T.box("CrossBeam", "KoyuMese", p(-0.25, -0.085, 1.0), (0.06, 0.02, 0.85), rot=(0, 35, 0))


def b_duvar(ox, oy):
    wall_base(at(ox, oy))


def b_pencere(ox, oy):
    wall_base(at(ox, oy), window=True)


def _themed(theme, window):
    return lambda ox, oy: wall_base(at(ox, oy), window=window, theme=theme)


def b_kapi(ox, oy):
    p = at(ox, oy)
    wall_base(p, door=True)
    for i in range(3):
        T.box(f"DoorPlank{i}", "Mese", p(-0.17 + i * 0.17, -0.02, 0.66), (0.16, 0.06, 1.3), bevel=0.008)
    T.box("DoorBrace", "KoyuMese", p(0, -0.06, 0.66), (0.5, 0.02, 0.06), rot=(0, 30, 0))
    T.torus("DoorRing", "Pirinc", p(0.17, -0.07, 0.7), major=0.04, minor=0.008, rot=(90, 0, 0), seg=8)
    T.box("Sign", "KoyuMese", p(0.62, -0.25, 1.25), (0.3, 0.03, 0.2), rot=(0, 0, 90), bevel=0.01)
    T.cyl("SignMug", "Altin", p(0.62, -0.27, 1.25), r=0.05, depth=0.02, rot=(0, 90, 90), verts=8)


def b_kose(ox, oy):
    p = at(ox, oy)
    T.box("Post", "KoyuMese", p(0, 0, 0.78), (0.26, 0.26, 1.56), bevel=0.02)
    T.box("PostBase", "Tas", p(0, 0, 0.15), (0.32, 0.32, 0.3), bevel=0.02)


def b_kilitli(ox, oy):
    p = at(ox, oy)
    for x in (-0.5, 0.5):
        T.cyl(f"FencePost{x}", "KoyuMese", p(x, 0, 0.35), r=0.04, depth=0.7, verts=6)
        T.sphere(f"PostTop{x}", "KoyuMese", p(x, 0, 0.71), r=0.045, seg=6, rings=4)
    for z in (0.45, 0.62):
        T.limb(f"Rope{z}", "Bez", p(-0.5, 0, z), p(0.5, 0, z - 0.05), r1=0.012, verts=4)
    T.box("SignPost", "KoyuMese", p(0, 0.05, 0.55), (0.06, 0.06, 1.1))
    T.box("Sign", "Mese", p(0, 0.0, 1.05), (0.6, 0.04, 0.35), bevel=0.015)
    T.cyl("SignCoin", "Altin", p(-0.15, -0.03, 1.05), r=0.09, depth=0.02, rot=(90, 0, 0), verts=10)
    T.box("SignText", "KoyuMese", p(0.1, -0.025, 1.05), (0.25, 0.01, 0.06))


def b_odeme(ox, oy):
    p = at(ox, oy)
    T.box("Pad", "Altin", p(0, 0, 0.005), (1.0, 1.0, 0.01))
    T.box("PadIn", "Mese", p(0, 0, 0.009), (0.86, 0.86, 0.01))
    T.cyl("CoinIcon", "Altin", p(0, 0, 0.02), r=0.22, depth=0.02, verts=14)
    T.torus("CoinRing", "Pirinc", p(0, 0, 0.03), major=0.18, minor=0.012, seg=14)
    for a in (0, 90, 180, 270):
        r = math.radians(a)
        T.cyl(f"Arrow{a}", "Altin", p(math.cos(r) * 0.38, math.sin(r) * 0.38, 0.015), r=0.06, r2=0.0, depth=0.12,
              rot=(90, 0, a + 90), scale=(1, 1, 1), verts=3)


# ================================================================ katalog
DRINKS = [("Bira", "Bira", d_bira), ("Sira", "Şıra", d_sira), ("BalSarabi", "Bal Şarabı", d_bal_sarabi),
          ("Sarap", "Şarap", d_sarap), ("KaraBira", "Kara Bira", d_stout), ("Cay", "Bitki Çayı", d_cay),
          ("Grog", "Goblin Grogu", d_grog), ("ElfCiyi", "Elf Çiyi", d_elf), ("EjderAtesi", "Ejder Ateşi", d_ejder),
          ("Sut", "Süt", d_sut), ("Para", "Altın", o_para), ("ParaYigini", "Para Yığını", o_para_yigini),
          ("Elmas", "Elmas", o_elmas)]
STATIONS = [("Bira", "Bira Fıçısı", s_bira), ("Pres", "Elma Presi", s_pres), ("Bal", "Bal Kazanı", s_bal),
            ("Sarap", "Şarap Rafı", s_sarap), ("Sut", "Süt Güğümü", s_sut), ("KaraBira", "Koyu Bira Kazanı", s_stout),
            ("Cay", "Çaydanlık Ocağı", s_cay), ("Grog", "Goblin Kazanı", s_grog), ("Kristal", "Kristal Çeşme", s_kristal),
            ("Ejder", "Ejder Ocağı", s_ejder)]
FURNITURE = [("Tezgah", "Tezgâh", f_tezgah), ("Servis", "Servis Tezgâhı", f_servis), ("MasaYuvarlak", "Yuvarlak Masa", f_masa_yuvarlak),
             ("MasaKare", "Kare Masa", f_masa_kare), ("MasaUzun", "Uzun Masa", f_masa_uzun), ("Tabure", "Tabure", f_tabure),
             ("Sandalye", "Sandalye", f_sandalye), ("Koltuk", "Minderli Koltuk", f_koltuk), ("Legen", "Bardak Leğeni", f_legen),
             ("Sandik", "Para Sandığı", f_sandik)]
DECOR = [("Somine", "Şömine", x_somine), ("Avize", "Avize", x_avize), ("Mesale", "Meşale", x_mesale), ("Mum", "Mumluk", x_mum),
         ("Saksi", "Saksı", x_saksi), ("Hali", "Halı", x_hali), ("Flama", "Flama", x_flama), ("Kalkan", "Kalkan & Kılıç", x_kalkan),
         ("Boynuz", "Geyik Boynuzu", x_boynuz), ("FiciYigini", "Fıçı Yığını", x_fici_yigini), ("Sahne", "Ozan Sahnesi", x_sahne),
         ("Muhur", "Kraliyet Mührü", x_muhur)]
def b_cim(ox, oy):
    """Bahçe zemini (1x1): çim karo + birkaç ot tutamı ve çiçek."""
    p = at(ox, oy)
    T.box("Grass", "Yaprak", p(0, 0, 0.02), (1.0, 1.0, 0.04), bevel=0.006)
    for i, (x, y) in enumerate(((-0.3, 0.25), (0.28, -0.2), (0.05, 0.35))):
        T.cyl(f"Tuft{i}", "Yesil", p(x, y, 0.07), r=0.04, r2=0.0, depth=0.08, verts=4)
    T.sphere("Flower", "Altin" if (ox + oy) % 2 else "Kirmizi", p(-0.15, -0.3, 0.06), r=0.025, seg=6, rings=4)


def b_kaldirim(ox, oy):
    """Sokak taşı (1x1): dört yamuk taş."""
    p = at(ox, oy)
    T.box("Base", "KoyuTas", p(0, 0, 0.01), (1.0, 1.0, 0.02))
    for i, (x, y) in enumerate(((-0.25, -0.25), (0.25, -0.25), (-0.25, 0.25), (0.25, 0.25))):
        T.box(f"Stone{i}", "Tas", p(x, y, 0.035), (0.44, 0.44, 0.04), rot=(0, 0, (i * 7) % 10 - 5), bevel=0.04)


def b_cit(ox, oy):
    """Bahçe çiti (1 m): iki direk + iki yatay tahta."""
    p = at(ox, oy)
    for x in (-0.45, 0.45):
        T.box(f"Post{x}", "KoyuMese", p(x, 0, 0.32), (0.09, 0.09, 0.64), bevel=0.015)
        T.cyl(f"PostCap{x}", "KoyuMese", p(x, 0, 0.68), r=0.065, r2=0.0, depth=0.08, verts=4, rot=(0, 0, 45))
    for z in (0.25, 0.48):
        T.box(f"Rail{z}", "Mese", p(0, 0, z), (1.0, 0.04, 0.08), bevel=0.008)


BUILDING = [("Zemin", "Zemin", b_zemin), ("Duvar", "Duvar", b_duvar), ("Pencere", "Pencere", b_pencere), ("Kapi", "Kapı", b_kapi),
            ("Kose", "Köşe Direği", b_kose), ("Kilitli", "Kilitli Alan", b_kilitli), ("Odeme", "Ödeme Karesi", b_odeme),
            ("Cim", "Çim", b_cim), ("Kaldirim", "Kaldırım", b_kaldirim), ("Cit", "Çit", b_cit)]

# Duvar/zemin temaları (v0.6): ayrı kategori, sadece bunları yeniden üretmek için  -- Theme
THEMES = [("Zemin_Parke", "Koyu Parke", b_zemin_parke), ("Zemin_Tas", "Taş Döşeme", b_zemin_tas), ("Zemin_Dama", "Dama Karo", b_zemin_dama),
          ("Zemin_Altin", "Altın Mermer", b_zemin_altin)]
for _t, _lbl in (("Lambri", "Lambri"), ("Tas", "Taş"), ("Siva", "Kırmızı Sıva"), ("Altin", "Altın")):
    THEMES.append((f"Duvar_{_t}", f"Duvar · {_lbl}", _themed(_t, False)))
    THEMES.append((f"Pencere_{_t}", f"Pencere · {_lbl}", _themed(_t, True)))


def build(fn, ox=0.0, oy=0.0, tier=None):
    if tier is None:
        fn(ox, oy)
    else:
        fn(ox, oy, tier)


def export_set(cat, items, tiers=False):
    for key, _, fn in items:
        for t in ((1, 2, 3) if tiers else (None,)):
            T.reset_scene()
            build(fn, tier=t)
            name = f"PR_{cat}_{key}" + (f"_T{t}" if t else "")
            obj = T.join_all(name, smooth_angle=35)
            print(f"[{name}] üçgen: {T.tri_count(obj)}")
            T.save_blend(os.path.join(MODELS, cat, f"{name}.blend"))
            T.export_fbx(obj, os.path.join(FBX, f"{name}.fbx"))


def sheet(cat, items, spacing, label_size=0.12, pitch=28, scale_cam=1.0, rows_tiers=False, zoom=1.0, target_z=0.4, cols=None,
          obj_h=1.8, gap=1.55):
    T.reset_scene()
    cols = cols or len(items)
    nrows_items = math.ceil(len(items) / cols)
    tiers = (1, 2, 3) if rows_tiers else (None,)
    row_gap = spacing * (1.15 if rows_tiers or nrows_items == 1 else gap)
    total_rows = nrows_items * len(tiers)
    for ti, t in enumerate(tiers):
        for i, (key, label, fn) in enumerate(items):
            r, c = i // cols, i % cols
            row = ti + r * len(tiers)
            x = (c - (cols - 1) / 2) * spacing
            y = ((total_rows - 1) / 2 - row) * row_gap if not rows_tiers else (row - (total_rows - 1) / 2) * row_gap
            build(fn, x, y, t)
            T.join_all(f"PR_{cat}_{key}_{ti}")
            if ti == 0:
                T.label(label, (x, y - spacing * 0.48, 0.02), size=label_size)
        if rows_tiers:
            y = (ti - (total_rows - 1) / 2) * row_gap
            T.label(["Ahşap", "Pirinç", "Altın"][ti], (-(cols / 2 + 0.05) * spacing, y - spacing * 0.2, 0.3), size=label_size * 1.3,
                    color=["Mese", "Pirinc", "Altin"][ti])
    T.setup_render_stage(floor_size=80, floor_color="KoyuTas")
    width = (cols + (1.0 if rows_tiers else 0.2)) * spacing
    dist = (width / 2) / math.tan(math.radians(17.5)) * zoom + 0.5
    depth_rows = total_rows * row_gap
    h_px = int(2000 * min(1.0, (depth_rows * math.sin(math.radians(pitch)) + obj_h * math.cos(math.radians(pitch)) + obj_h * 0.4) / width))
    T.render_from(os.path.join(RENDERS, f"Props_{cat}.png"), target=(0, 0, target_z), pitch=pitch, yaw=0, dist=dist,
                  res=(2000, max(h_px, 500)))


CATEGORIES = {
    "Drink": dict(items=DRINKS, spacing=0.42, label_size=0.045, pitch=25, target_z=0.06, cols=7, obj_h=0.35),
    "Station": dict(items=STATIONS, spacing=1.35, label_size=0.11, pitch=32, rows_tiers=True, target_z=0.4),
    "Furniture": dict(items=FURNITURE, spacing=1.9, label_size=0.13, pitch=30, cols=5, target_z=0.3),
    "Decor": dict(items=DECOR, spacing=2.0, label_size=0.14, pitch=38, cols=6, target_z=0.6, gap=2.3, obj_h=2.2),
    "Building": dict(items=BUILDING, spacing=1.45, label_size=0.11, pitch=25, target_z=0.6),
}
# Temalar "Building" adıyla dışa aktarılır (PR_Building_Zemin_Tas ...) ama ayrı çalıştırılır: -- Theme
THEME_CFG = dict(items=THEMES, spacing=1.45, label_size=0.09, pitch=25, target_z=0.6, cols=9)

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    only_sheets = "sheet" in argv  # ör: -- Drink sheet  → sadece render sayfası
    cats = [a for a in argv if a != "sheet"] or list(CATEGORIES)
    for cat in cats:
        if cat == "Theme":
            if not only_sheets:
                export_set("Building", THEMES)
            sheet("Theme", **THEME_CFG)
            continue
        cfg = CATEGORIES[cat]
        if not only_sheets:
            export_set(cat, cfg["items"], tiers=cfg.get("rows_tiers", False))
        sheet(cat, **cfg)
    print("BİTTİ")
