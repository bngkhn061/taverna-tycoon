"""
30 müşteri tipi. Tasarım: CHARACTERS.md §B. Kurallar: DESIGN_RULES.md.
Çalıştırma:
  blender -b --factory-startup --python tools/blender/build_customers.py            # hepsi
  blender -b --factory-startup --python tools/blender/build_customers.py -- 7 12    # sadece seçilenler
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import toylib as T  # noqa: E402
import bodylib as B  # noqa: E402
from mathutils import Vector  # noqa: E402

MODELS = os.path.join(T.ROOT, "assets", "models", "characters")
FBX = os.path.join(T.ROOT, "assets", "export", "fbx")
RENDERS = os.path.join(T.ROOT, "assets", "renders")
V = Vector


# ============================================================ Kademe 1 — Köy
def c01_ciftci(ox):
    R = B.body(ox, "M", "Ten2", "Bez", "Yesil", "Mese", sleeves="short")
    B.apron(R, "Yesil", top=0.45, bottom=R.tz - R.rz * 0.6, width=0.7)
    for s in (1, -1):
        T.box(f"OverallStrap_{s}", "Yesil", (ox + s * R.rx * 0.4, -R.ry * 0.55, R.tz + R.rz * 0.75),
              (0.035, 0.02, R.rz * 0.6), rot=(-40, 0, 0))
    B.brim_hat(R, "SacSari", brim=1.95, crown_h=0.55, z=0.5, band="Mese")
    T.limb("Straw", "SacSari", (ox + R.hr * 0.12, -R.hr * 0.92, R.hz - R.hr * 0.45),
           (ox + R.hr * 0.75, -R.hr * 1.3, R.hz - R.hr * 0.3), r1=0.008, verts=4)
    return R


def c02_balikci(ox):
    R = B.body(ox, "M", "Ten3", "Altin", "Mavi", "KoyuMese")
    T.dome("Hood", "Altin", (ox, 0.01, R.hz + R.hr * 0.05), r=R.hr * 1.1, rot=(-15, 0, 0))
    T.cyl("HatBrim", "Altin", (ox, 0.02, R.hz + R.hr * 0.12), r=R.hr * 1.65, r2=R.hr * 1.05, depth=R.hr * 0.22,
          rot=(-12, 0, 0), verts=14)
    for i in range(3):
        T.sphere(f"Btn{i}", "Krem", (ox, -R.ry * 1.0, R.tz + R.rz * (0.4 - i * 0.3)), r=0.014, seg=6, rings=4)
    h = R.hand[1]
    T.limb("Rod", "Mese", h + V((0, 0, -0.05)), V((ox + 0.45, 0.35, R.hz + R.hr * 1.8)), r1=0.014, r2=0.007, verts=6)
    B.beard(R, "SacGri", length=0.6, width=0.75)
    return R


def c03_oduncu(ox):
    R = B.body(ox, "L", "Ten2", "Kirmizi", "Lacivert", "KoyuMese")
    for z in (0.35, -0.2):
        T.torus(f"Plaid{z}", "Lacivert", (ox, 0, R.tz + R.rz * z), major=R.rx * (0.95 if z > 0 else 0.98),
                minor=0.014, scale=(1, R.ry / R.rx, 1), seg=16, mseg=4)
    B.hair(R, "SacKahve")
    B.beard(R, "SacKahve", length=1.3, width=0.9)
    a, b = V((ox + 0.18, R.ry + 0.06, R.tz - 0.25)), V((ox - 0.22, R.ry + 0.06, R.hz + 0.2))
    T.limb("AxeHandle", "Mese", a, b, r1=0.022, verts=6)
    T.box("AxeHead", "Tas", (ox - 0.28, R.ry + 0.07, R.hz + 0.16), (0.2, 0.03, 0.16), rot=(0, -40, 0), bevel=0.01)
    return R


def c04_ozan(ox):
    R = B.body(ox, "M", "Ten1", "Mor", "Krem", "KoyuMese", build="thin")
    B.hair(R, "SacSari", rot=(-25, 0, 0))
    T.sphere("Beret", "Mor", (ox + 0.03, 0, R.hz + R.hr * 0.72), r=R.hr * 1.05, scale=(1.15, 1.1, 0.38),
             rot=(0, -12, 0), seg=12, rings=8)
    T.sphere("Feather", "Altin", (ox - R.hr * 0.75, 0.04, R.hz + R.hr * 1.15), r=1, scale=(0.03, 0.02, 0.16),
             rot=(0, -40, 0), seg=8, rings=6)
    c = V((ox + 0.04, R.ry + 0.07, R.tz))
    T.sphere("LuteBody", "Mese", c, r=0.12, scale=(1, 0.45, 1.2), seg=10, rings=8)
    T.limb("LuteNeck", "KoyuMese", c + V((0, 0, 0.1)), V((ox - 0.13, R.ry + 0.07, R.hz + 0.12)), r1=0.022, verts=6)
    return R


def c05_firinci(ox):
    R = B.body(ox, "M", "Ten2", "Krem", "Bez", "KoyuMese", build="fat", sleeves="short")
    B.apron(R, "Bez", top=0.35)
    T.cyl("ChefHat", "Krem", (ox, 0, R.hz + R.hr * 0.95), r=R.hr * 0.78, depth=R.hr * 0.6, verts=12)
    T.sphere("ChefPuff", "Krem", (ox, 0, R.hz + R.hr * 1.45), r=R.hr * 0.9, scale=(1, 1, 0.65), seg=12, rings=8)
    B.mustache(R, "SacKahve", size=0.8)
    h = R.hand[-1]
    T.limb("Bread", "Bal", h + V((-0.02, -0.05, -0.12)), h + V((0.06, -0.05, 0.22)), r1=0.035, r2=0.03, verts=8)
    return R


def c06_coban_kiz(ox):
    R = B.body(ox, "M", "Ten1", "Mavi", "Mavi", "KoyuMese", sleeves="short")
    B.skirt(R, "Mavi", bottom=0.08, flare=1.3)
    T.torus("Collar", "Krem", (ox, 0, R.tz + R.rz * 0.85), major=R.rx * 0.6, minor=0.03, seg=12, mseg=5)
    T.cyl("SkirtHem", "Krem", (ox, 0, 0.11), r=R.rx * 1.32, depth=0.05, scale=(1, R.ry / R.rx, 1), verts=14)
    B.hair(R, "SacSari", rot=(-22, 0, 0))
    for s in (1, -1):
        a = V((ox + s * R.hr * 0.8, 0.03, R.hz - R.hr * 0.1))
        b = V((ox + s * R.hr * 0.95, -0.04, R.hz - R.hr * 1.15))
        T.limb(f"Braid_{s}", "SacSari", a, b, r1=R.hr * 0.16, r2=R.hr * 0.09, verts=8)
        T.sphere(f"Tie_{s}", "Yesil", b, r=R.hr * 0.11, seg=6, rings=4)
    top = B.staff(R, 1, "Mese", R.hz + R.hr * 1.3)
    T.torus("Crook", "Mese", top + V((0.06, 0, 0)), major=0.06, minor=0.015, rot=(90, 0, 0), seg=10, mseg=4)
    return R


def c07_kunduraci(ox):
    R = B.body(ox, "S", "Ten2", "Mese", "KoyuMese", "Bal", nose="big")
    for s in (1, -1):
        T.sphere(f"SideHair_{s}", "SacGri", (ox + s * R.hr * 0.85, 0.04, R.hz + R.hr * 0.15), r=R.hr * 0.35,
                 scale=(0.7, 1, 1), seg=8, rings=6)
    B.glasses(R, "Bal")
    T.torus("Laces", "Krem", (ox, -0.01, R.tz + R.rz * 0.8), major=R.rx * 0.65, minor=0.01, rot=(-20, 0, 0), seg=12, mseg=4)
    for s in (1, -1):
        T.box(f"HangShoe_{s}", "KoyuMese", (ox + s * 0.05, -R.ry * 1.05, R.tz - R.rz * 0.1), (0.05, 0.08, 0.04),
              rot=(70, 0, s * 15), bevel=0.012)
    return R


# ============================================================ Kademe 2 — Kasaba
def c08_demirci(ox):
    R = B.body(ox, "L", "Ten3", "Tas", "Tas", "KoyuMese", build="fat", sleeves="short")
    B.apron(R, "KoyuMese", top=0.6, width=0.9)
    B.beard(R, "SacKoyu", length=0.6, width=0.85)
    h = R.hand[1]
    top = V((ox + R.sx * 0.5, R.ry * 0.6, R.hz + 0.05))
    T.limb("HammerHandle", "Mese", h, top, r1=0.022, verts=6)
    T.box("HammerHead", "KoyuTas", top + V((0, 0.02, 0.04)), (0.2, 0.11, 0.11), rot=(0, 25, 0), bevel=0.012)
    T.sphere("Ember", "Ates", (ox - R.rx * 0.5, -R.ry * 0.95, R.tz - R.rz * 0.4), r=0.02, seg=6, rings=4)
    return R


def c09_tuccar(ox):
    R = B.body(ox, "M", "Ten2", "Mavi", "Krem", "KoyuMese", build="fat", nose="big")
    B.belt(R, "KoyuMese", buckle="Altin", z=-0.1)
    T.sphere("Purse", "Altin", (ox + R.rx * 1.0, -0.06, R.tz - R.rz * 0.35), r=0.1, scale=(1, 0.9, 1.1), seg=10, rings=7)
    T.cyl("PurseTie", "KoyuMese", (ox + R.rx * 1.0, -0.06, R.tz - R.rz * 0.35 + 0.1), r=0.035, r2=0.05, depth=0.05, verts=8)
    T.sphere("Beret", "Mavi", (ox - 0.02, 0, R.hz + R.hr * 0.7), r=R.hr * 1.1, scale=(1.2, 1.15, 0.42), rot=(0, 10, 0),
             seg=12, rings=8)
    T.sphere("Brooch", "Altin", (ox + R.hr * 0.3, -R.hr * 1.05, R.hz + R.hr * 0.72), r=0.025, seg=6, rings=4)
    B.mustache(R, "SacKoyu", size=0.65, tilt=-8)
    return R


def c10_muhafiz(ox):
    R = B.body(ox, "M", "Ten3", "Tas", "KoyuTas", "KoyuMese")
    T.box("Tabard", "Kirmizi", (ox, -R.ry * 0.82, R.tz - R.rz * 0.15), (R.rx * 1.05, 0.04, R.rz * 1.7), bevel=0.01)
    T.sphere("Emblem", "Altin", (ox, -R.ry * 0.86 - 0.03, R.tz + R.rz * 0.15), r=0.035, scale=(1, 0.4, 1), seg=8, rings=6)
    T.dome("Helmet", "Tas", (ox, 0, R.hz + R.hr * 0.12), r=R.hr * 1.1, seg=14, rings=10)
    T.torus("HelmRim", "Tas", (ox, 0, R.hz + R.hr * 0.12), major=R.hr * 1.15, minor=0.018, seg=16, mseg=4)
    top = B.staff(R, 1, "Mese", R.hz + R.hr * 1.9, r=0.016)
    T.cyl("SpearTip", "Tas", top + V((0, 0, 0.06)), r=0.035, r2=0.0, depth=0.12, verts=6)
    return R


def c11_kesis(ox):
    R = B.body(ox, "M", "Ten2", "Mese", "Mese", "KoyuMese", build="fat")
    B.skirt(R, "Mese", bottom=0.02, flare=1.25)
    B.belt(R, "Bez", z=-0.2, minor=0.018)
    T.sphere("HoodBack", "Mese", (ox, R.hr * 0.6, R.hz - R.hr * 0.45), r=1, scale=(R.hr * 1.05, R.hr * 0.55, R.hr * 0.75),
             seg=12, rings=8)
    T.torus("Beads", "Yesil", (ox, -0.02, R.tz + R.rz * 0.5), major=R.rx * 0.75, minor=0.016, rot=(-25, 0, 0), seg=14, mseg=4)
    return R


def c12_cuce(ox):
    R = B.body(ox, "S", "Ten2", "Lacivert", "Mese", "KoyuMese", build="fat", nose="big")
    T.dome("Helmet", "Bal", (ox, 0, R.hz + R.hr * 0.1), r=R.hr * 1.12)
    T.torus("HelmRim", "Bal", (ox, 0, R.hz + R.hr * 0.1), major=R.hr * 1.15, minor=0.015, seg=14, mseg=4)
    T.cyl("Candle", "Krem", (ox, -R.hr * 0.3, R.top + 0.06), r=0.025, depth=0.08, verts=8)
    T.sphere("Flame", "Ates", (ox, -R.hr * 0.3, R.top + 0.13), r=0.02, scale=(1, 1, 1.7), seg=6, rings=4)
    T.sphere("Beard", "SacKahve", (ox, -R.hr * 0.6, R.hz - R.hr * 1.1), r=1,
             scale=(R.hr * 0.85, R.hr * 0.5, R.hr * 1.15), seg=10, rings=8)
    a, b = V((ox + 0.15, R.ry + 0.05, R.tz - 0.15)), V((ox - 0.12, R.ry + 0.05, R.hz + 0.12))
    T.limb("PickHandle", "Mese", a, b, r1=0.016, verts=6)
    T.limb("PickHead", "Tas", b + V((-0.12, 0, -0.09)), b + V((0.12, 0, 0.09)), r1=0.022, r2=0.008, verts=6)
    return R


def c13_bucukluk(ox):
    R = B.body(ox, "S", "Ten2", "Bez", "Bal", "Ten2", ears="pointy")
    B.vest(R, "Yesil", buttons="Bal")
    for s in (1, -1):
        for j in range(2):
            T.sphere(f"FootHair_{s}{j}", "SacKahve", (ox + s * R.lx + (j - 0.5) * 0.04, -0.05, R.foot[2] + 0.005),
                     r=0.025, seg=6, rings=4)
    for i, (dx, dz) in enumerate(((0, 1.0), (0.5, 0.85), (-0.5, 0.85), (0.8, 0.45), (-0.8, 0.45), (0.3, 0.95), (-0.3, 0.95))):
        T.sphere(f"Curl{i}", "SacKahve", (ox + dx * R.hr, 0.03, R.hz + dz * R.hr * 0.9), r=R.hr * 0.38, seg=7, rings=5)
    T.box("Pack", "KoyuMese", (ox, R.ry + 0.08, R.tz + 0.02), (0.22, 0.12, 0.24), bevel=0.02)
    T.cyl("Bedroll", "Yesil", (ox, R.ry + 0.08, R.tz + 0.17), r=0.05, depth=0.28, rot=(0, 90, 0), verts=8)
    return R


def c14_vergi(ox):
    R = B.body(ox, "M", "Ten1", "Lacivert", "Tas", "KoyuMese", build="thin", nose="long")
    B.skirt(R, "Lacivert", bottom=0.14, flare=1.15)
    T.torus("Ruff", "Krem", (ox, 0, R.tz + R.rz * 0.95), major=R.rx * 0.55, minor=0.03, seg=12, mseg=6)
    B.glasses(R, "Tas")
    T.cyl("TopHatBrim", "Lacivert", (ox, 0, R.hz + R.hr * 0.55), r=R.hr * 1.15, depth=0.018, verts=14)
    T.cyl("TopHat", "Lacivert", (ox, 0, R.hz + R.hr * 1.2), r=R.hr * 0.72, depth=R.hr * 1.3, verts=12)
    T.cyl("TopHatBand", "Tas", (ox, 0, R.hz + R.hr * 0.66), r=R.hr * 0.74, depth=R.hr * 0.18, verts=12)
    h = R.hand[-1]
    T.cyl("Scroll", "Krem", h + V((0.02, -0.04, 0.0)), r=0.035, depth=0.2, rot=(0, 90, 20), verts=8)
    return R


def c15_yaver(ox):
    R = B.body(ox, "S", "Ten1", "Tas", "Krem", "Mese")
    T.box("Tabard", "Mavi", (ox, -R.ry * 0.8, R.tz - R.rz * 0.1), (R.rx * 1.1, 0.03, R.rz * 1.7), bevel=0.008)
    T.dome("Helmet", "Tas", (ox, 0, R.hz + R.hr * 0.38), r=R.hr * 1.25, rot=(10, 0, 0), seg=12, rings=8)
    T.torus("HelmRim", "Tas", (ox, -0.01, R.hz + R.hr * 0.38), major=R.hr * 1.27, minor=0.014, rot=(10, 0, 0), seg=14, mseg=4)
    h = R.hand[1]
    T.limb("Sword", "Mese", h, h + V((0.02, -0.06, 0.3)), r1=0.02, r2=0.014, verts=4)
    T.box("Guard", "Mese", h + V((0, -0.01, 0.04)), (0.1, 0.02, 0.02), bevel=0.005)
    return R


# ============================================================ Kademe 3 — Maceracılar / Fantastik
def c16_sovalye(ox):
    R = B.body(ox, "L", "Ten2", "Tas", "Tas", "KoyuTas", eyes=False, nose="none", ears="none")
    T.cyl("HelmCyl", "Tas", (ox, 0, R.hz - R.hr * 0.05), r=R.hr * 1.05, depth=R.hr * 1.6, verts=14)
    T.dome("HelmTop", "Tas", (ox, 0, R.hz + R.hr * 0.75), r=R.hr * 1.05)
    T.box("Visor", "KoyuTas", (ox, -R.hr * 1.03, R.hz + R.hr * 0.1), (R.hr * 1.3, 0.02, R.hr * 0.12))
    for i in range(5):
        T.sphere(f"Plume{i}", "Kirmizi", (ox, 0.04 + i * 0.07, R.hz + R.hr * (1.85 - i * 0.18)), r=0.065 - i * 0.004,
                 scale=(0.8, 1.3, 1), seg=8, rings=6)
    for s in (1, -1):
        T.dome(f"Pauldron_{s}", "Tas", (ox + s * R.sx, 0, R.sz + 0.02), r=0.13, rot=(0, s * 25, 0))
    c = R.hand[-1] + V((-0.06, -0.06, 0.12))
    T.cyl("Shield", "Kirmizi", c, r=0.22, depth=0.04, rot=(90, 0, -20), verts=14)
    T.torus("ShieldRim", "Altin", c + V((0, -0.01, 0)), major=0.22, minor=0.018, rot=(90, 0, -20), seg=14, mseg=4)
    T.box("ShieldCross", "Altin", c + V((0.0, -0.03, 0)), (0.05, 0.02, 0.3), rot=(0, 0, -20))
    return R


def c17_goblin(ox):
    R = B.body(ox, "S", "TenGoblin", "Bez", "Mese", "TenGoblin", ears="long", nose="long", head_scale=1.05)
    T.dome("Hood", "Bez", (ox, 0.03, R.hz + R.hr * 0.05), r=R.hr * 1.1, rot=(-30, 0, 0))
    T.box("Patch", "Mese", (ox + R.hr * 0.35, -R.hr * 0.4, R.hz + R.hr * 0.8), (0.06, 0.06, 0.02), rot=(45, 0, 20))
    T.box("Patch2", "Mese", (ox - R.rx * 0.5, -R.ry * 0.95, R.tz - 0.03), (0.06, 0.02, 0.05), rot=(0, 10, 0))
    T.box("Grin", "Krem", (ox, -R.hr * 0.9, R.hz - R.hr * 0.5), (R.hr * 0.6, 0.02, R.hr * 0.12), bevel=0.004)
    return R


def c18_ork(ox):
    R = B.body(ox, "L", "TenOrk", "TenOrk", "KoyuMese", "KoyuMese", build="huge", ears="pointy", nose="big")
    B.belt(R, "KoyuMese", buckle="Tas", z=-0.3, minor=0.03)
    T.dome("Pauldron", "Kirmizi", (ox - R.sx, 0, R.sz + 0.03), r=0.15, rot=(0, -25, 0))
    T.box("Strap", "Kirmizi", (ox, -R.ry * 0.9, R.tz + R.rz * 0.2), (0.06, 0.03, R.rz * 2.0), rot=(0, 38, 0))
    for s in (1, -1):
        T.cyl(f"Tusk_{s}", "Krem", (ox + s * R.hr * 0.32, -R.hr * 0.88, R.hz - R.hr * 0.42), r=0.022, r2=0.0, depth=0.08,
              rot=(-10, 0, s * -10), verts=6)
    T.torus("Necklace", "Krem", (ox, -0.02, R.tz + R.rz * 0.75), major=R.rx * 0.55, minor=0.012, rot=(-25, 0, 0), seg=12, mseg=4)
    for i in (-1, 0, 1):
        T.cyl(f"Bone{i}", "Krem", (ox + i * 0.07, -R.ry * 0.8, R.tz + R.rz * 0.5), r=0.015, r2=0.0, depth=0.07,
              rot=(180, 0, 0), verts=5)
    T.sphere("TopKnot", "SacKoyu", (ox, 0.04, R.top + 0.03), r=0.06, seg=8, rings=6)
    T.limb("TopTail", "SacKoyu", (ox, 0.06, R.top + 0.05), (ox, R.hr * 0.9, R.hz + R.hr * 0.3), r1=0.035, r2=0.015, verts=6)
    return R


def c19_elf(ox):
    R = B.body(ox, "M", "TenElf", "Yesil", "KoyuMese", "Mese", build="thin", ears="pointy")
    B.hair(R, "SacSari", rot=(-25, 0, 0))
    T.sphere("HairBack", "SacSari", (ox, R.hr * 0.45, R.hz - R.hr * 0.55), r=1, scale=(R.hr * 0.85, R.hr * 0.4, R.hr * 1.15),
             seg=10, rings=8)
    B.cape(R, "Yesil")
    T.sphere("Brooch", "Altin", (ox, -R.ry * 0.95, R.tz + R.rz * 0.75), r=0.022, seg=6, rings=4)
    c = V((ox + 0.04, R.ry + 0.1, R.tz + 0.2))
    T.torus("Bow", "Mese", c, major=0.4, minor=0.014, scale=(0.3, 1, 1), rot=(90, 0, 15), seg=16, mseg=4)
    T.limb("BowString", "Krem", c + V((-0.1, 0, -0.38)), c + V((0.1, 0, 0.38)), r1=0.004, verts=4)
    return R


def c20_buyucu(ox):
    R = B.body(ox, "M", "Ten1", "Mor", "Mor", "KoyuMese")
    B.skirt(R, "Mor", bottom=0.02, flare=1.3)
    T.cyl("HatBrim", "Mor", (ox, 0, R.hz + R.hr * 0.55), r=R.hr * 1.5, depth=0.02, verts=14)
    T.limb("HatCone", "Mor", (ox, 0, R.hz + R.hr * 0.55), (ox, 0.06, R.hz + R.hr * 2.3), r1=R.hr * 0.95, r2=R.hr * 0.25, verts=12)
    T.limb("HatTip", "Mor", (ox, 0.06, R.hz + R.hr * 2.3), (ox - 0.14, 0.12, R.hz + R.hr * 2.6), r1=R.hr * 0.25, r2=0.0, verts=8)
    for i, (dx, dz) in enumerate(((0.35, 1.0), (-0.25, 1.5), (0.1, 1.9))):
        T.sphere(f"Star{i}", "Altin", (ox + dx * R.hr, -R.hr * (0.88 - dz * 0.25), R.hz + R.hr * dz), r=0.02, seg=6, rings=4)
    T.sphere("Beard", "SacGri", (ox, -R.hr * 0.6, R.hz - R.hr * 1.2), r=1, scale=(R.hr * 0.7, R.hr * 0.45, R.hr * 1.3),
             seg=8, rings=6)
    B.mustache(R, "SacGri", size=0.8)
    top = B.staff(R, 1, "Mese", R.hz + R.hr * 1.5, r=0.018)
    T.sphere("Orb", "Altin", top + V((0, 0, 0.05)), r=0.06, seg=10, rings=7)
    return R


def c21_cadi(ox):
    R = B.body(ox, "M", "Ten1", "Lacivert", "Lacivert", "KoyuMese", build="thin", nose="long")
    B.skirt(R, "Lacivert", bottom=0.04, flare=1.35)
    for s in (1, -1):
        T.limb(f"Hair_{s}", "SacKoyu", (ox + s * R.hr * 0.8, 0.05, R.hz + R.hr * 0.3),
               (ox + s * R.hr * 1.0, 0.08, R.hz - R.hr * 1.0), r1=R.hr * 0.25, r2=R.hr * 0.12, verts=8)
    T.cyl("HatBrim", "Lacivert", (ox, 0, R.hz + R.hr * 0.5), r=R.hr * 1.7, depth=0.02, verts=14)
    p1 = V((ox, 0.03, R.hz + R.hr * 1.75))
    T.limb("HatCone", "Lacivert", (ox, 0, R.hz + R.hr * 0.5), p1, r1=R.hr * 0.9, r2=R.hr * 0.3, verts=12)
    T.limb("HatTip", "Lacivert", p1, (ox + 0.16, 0.12, R.hz + R.hr * 2.15), r1=R.hr * 0.3, r2=0.0, verts=8)
    T.cyl("HatBand", "Mor", (ox, 0, R.hz + R.hr * 0.62), r=R.hr * 0.88, depth=R.hr * 0.18, verts=12)
    h = R.hand[-1]
    a, b = h + V((0.05, 0.1, -0.35)), h + V((-0.05, -0.05, 0.5))
    T.limb("Broom", "Mese", a, b, r1=0.015, verts=6)
    T.limb("Bristles", "Bal", a + V((0, 0, 0.12)), a + V((0.02, 0.03, -0.1)), r1=0.03, r2=0.08, verts=8)
    return R


def c22_simyaci(ox):
    R = B.body(ox, "M", "Ten2", "Mavi", "KoyuMese", "Mese")
    B.apron(R, "Krem", top=0.45)
    B.belt(R, "KoyuMese", z=-0.2)
    for i, (dx, c) in enumerate(((0.75, "ElfCiyi"), (-0.75, "Grog"), (-0.95, "EjderAtesi"))):
        p = (ox + dx * R.rx, -R.ry * 0.5, R.tz - R.rz * 0.35)
        T.sphere(f"Flask{i}", c, p, r=0.045, seg=6, rings=5)
        T.cyl(f"FlaskNeck{i}", "Krem", (p[0], p[1], p[2] + 0.055), r=0.014, depth=0.04, verts=6)
    for i, (dx, dz) in enumerate(((0.6, 0.6), (-0.6, 0.6), (0.85, 0.2), (-0.85, 0.2), (0, 0.95))):
        T.sphere(f"WildHair{i}", "SacGri", (ox + dx * R.hr, 0.06, R.hz + dz * R.hr), r=R.hr * 0.4, seg=6, rings=5)
    T.torus("GoggleStrap", "KoyuMese", (ox, 0, R.hz + R.hr * 0.5), major=R.hr * 0.9, minor=0.012, seg=14, mseg=4)
    for s in (1, -1):
        p = (ox + s * R.hr * 0.33, -R.hr * 0.85, R.hz + R.hr * 0.55)
        T.cyl(f"Goggle_{s}", "Bal", p, r=R.hr * 0.24, depth=R.hr * 0.18, rot=(80, 0, 0), verts=8)
        T.cyl(f"Lens_{s}", "ElfCiyi", (p[0], p[1] - 0.02, p[2]), r=R.hr * 0.18, depth=0.01, rot=(80, 0, 0), verts=8)
    return R


def c23_avci(ox):
    R = B.body(ox, "M", "Ten3", "Yesil", "KoyuMese", "KoyuMese")
    T.dome("FurCape", "Bez", (ox, 0.01, R.tz + R.rz * 0.45), r=1, scale=(R.rx * 1.25, R.ry * 1.25, R.rz * 0.7), seg=14, rings=8)
    T.cyl("FurHat", "Bez", (ox, 0, R.hz + R.hr * 0.75), r=R.hr * 1.0, r2=R.hr * 0.9, depth=R.hr * 0.6, verts=12)
    T.limb("HatTail", "Bez", (ox + R.hr * 0.5, R.hr * 0.6, R.hz + R.hr * 0.6), (ox + R.hr * 0.9, R.hr * 1.1, R.hz - R.hr * 0.5),
           r1=0.04, r2=0.03, verts=8)
    T.sphere("TailTip", "KoyuMese", (ox + R.hr * 0.95, R.hr * 1.15, R.hz - R.hr * 0.6), r=0.035, seg=6, rings=4)
    base = V((ox, R.ry + 0.06, R.tz + 0.1))
    for s in (1, -1):
        tip = base + V((s * 0.22, 0.02, 0.35))
        T.limb(f"Antler_{s}", "Krem", base, tip, r1=0.018, r2=0.01, verts=5)
        T.limb(f"AntlerB_{s}", "Krem", base + V((s * 0.11, 0.01, 0.17)), base + V((s * 0.05, 0.0, 0.33)), r1=0.012, r2=0.006, verts=5)
    B.belt(R, "KoyuMese", z=-0.2)
    T.box("Knife", "Tas", (ox + R.rx * 0.85, -R.ry * 0.5, R.tz - R.rz * 0.45), (0.025, 0.02, 0.12), bevel=0.004)
    return R


def c24_kervanci(ox):
    R = B.body(ox, "M", "Ten4", "Mavi", "Bez", "KoyuMese")
    B.vest(R, "Kirmizi")
    for i in range(3):
        T.torus(f"Turban{i}", "Altin", (ox, 0.01, R.hz + R.hr * (0.55 + i * 0.22)), major=R.hr * (0.9 - i * 0.12),
                minor=R.hr * 0.18, rot=(-8, 0, (i - 1) * 6), seg=12, mseg=5)
    T.sphere("TurbanTop", "Altin", (ox, 0.01, R.hz + R.hr * 1.05), r=R.hr * 0.55, seg=10, rings=6)
    T.sphere("Jewel", "Kirmizi", (ox, -R.hr * 1.05, R.hz + R.hr * 0.65), r=0.022, seg=6, rings=4)
    T.cyl("Goatee", "SacKoyu", (ox, -R.hr * 0.82, R.hz - R.hr * 0.75), r=R.hr * 0.18, r2=0.0, depth=R.hr * 0.45,
          rot=(180, 0, 0), verts=6)
    p = (ox + R.sx * 0.95, 0.02, R.sz + 0.07)
    T.cyl("Carpet", "Kirmizi", p, r=0.065, depth=0.6, rot=(90, 0, 15), verts=10)
    for d in (-0.2, 0.2):
        T.cyl(f"CarpetBand{d}", "Altin", (p[0] - d * 0.26, p[1] + d, p[2]), r=0.068, depth=0.03, rot=(90, 0, 15), verts=10)
    return R


def c25_korsan(ox):
    R = B.body(ox, "L", "Ten3", "Kirmizi", "Lacivert", "KoyuMese")
    B.skirt(R, "Kirmizi", bottom=R.lh * 0.55, flare=1.15)
    B.belt(R, "KoyuMese", buckle="Altin", z=-0.3, minor=0.03)
    B.beard(R, "SacKoyu", length=0.9, width=0.8)
    zb = R.hz + R.hr * 0.55
    T.cyl("TricornBase", "Lacivert", (ox, 0, zb), r=R.hr * 1.3, depth=0.03, verts=3, rot=(0, 0, 90))
    T.dome("TricornCrown", "Lacivert", (ox, 0, zb), r=R.hr * 0.95, scale=(1, 1, 0.8), seg=10, rings=6)
    for k in range(3):
        import math
        ang = math.radians(90 + k * 120)
        T.box(f"Flap{k}", "Lacivert", (ox + math.cos(ang) * R.hr * 1.0, math.sin(ang) * R.hr * 1.0, zb + 0.07),
              (R.hr * 1.5, 0.025, 0.14), rot=(-22, 0, math.degrees(ang) + 90), bevel=0.006)
    T.torus("TricornTrim", "Altin", (ox, 0, zb + 0.01), major=R.hr * 1.0, minor=0.01, seg=12, mseg=4)
    T.sphere("EyePatch", "Goz", (ox - R.hr * 0.4, -R.hr * 0.93, R.hz + R.hr * 0.14), r=R.hr * 0.22, scale=(1, 0.4, 1), seg=8, rings=6)
    T.torus("PatchStrap", "Goz", (ox, 0, R.hz + R.hr * 0.25), major=R.hr * 0.98, minor=0.008, rot=(0, -20, 0), seg=14, mseg=4)
    sp = V((ox - R.sx, 0.0, R.sz + 0.13))
    T.sphere("Parrot", "Yesil", sp, r=1, scale=(0.055, 0.055, 0.085), seg=6, rings=5)
    T.sphere("ParrotHead", "Kirmizi", sp + V((0, -0.01, 0.1)), r=0.045, seg=6, rings=5)
    T.cyl("Beak", "Altin", sp + V((0, -0.06, 0.09)), r=0.015, r2=0.0, depth=0.04, rot=(90, 0, 0), verts=5)
    T.limb("ParrotTail", "Mavi", sp + V((0, 0.03, -0.05)), sp + V((0, 0.08, -0.2)), r1=0.025, r2=0.005, verts=5)
    return R


def c26_trol(ox):
    R = B.body(ox, "L", "TenTrol", "TenTrol", "TenTrol", "TenTrol", build="huge", head_scale=0.7, nose="big")
    B.skirt(R, "KoyuMese", bottom=R.lh * 0.7, flare=1.1, top_z=R.tz - R.rz * 0.2)
    for i, (dx, dz) in enumerate(((0.8, 0.6), (-0.7, 0.75), (0.2, 0.9))):
        T.sphere(f"Rock{i}", "Tas", (ox + dx * R.rx, 0.02, R.tz + dz * R.rz), r=0.05, seg=6, rings=4)
    top = B.staff(R, 1, "KoyuMese", R.hz + 0.25, r=0.055)
    T.cyl("PoleBand", "Tas", top + V((0, 0, -0.12)), r=0.062, depth=0.05, verts=8)
    return R


# ============================================================ Kademe 4 — Gece / Efsanevi
def c27_ejder_avcisi(ox):
    R = B.body(ox, "L", "Ten3", "KoyuTas", "KoyuTas", "KoyuMese")
    B.cape(R, "Ates")
    B.belt(R, "KoyuMese", buckle="Altin", z=-0.25, minor=0.03)
    for i, (dx, dz, tilt) in enumerate(((0, 1.0, 0), (0.45, 0.85, -35), (-0.45, 0.85, 35), (0.2, 0.75, -15), (-0.2, 0.75, 15))):
        T.cyl(f"Spike{i}", "SacKizil", (ox + dx * R.hr, 0.03, R.hz + dz * R.hr), r=R.hr * 0.32, r2=0.0, depth=R.hr * 0.75,
              rot=(-15, tilt, 0), verts=6)
    T.box("Scar", "Ten4", (ox + R.hr * 0.42, -R.hr * 0.9, R.hz + R.hr * 0.05), (0.012, 0.01, R.hr * 0.45), rot=(0, 20, 0))
    for s in (1, -1):
        T.dome(f"Pauldron_{s}", "Ates", (ox + s * R.sx, 0, R.sz + 0.02), r=0.14, rot=(0, s * 25, 0))
        for k in range(2):
            T.cyl(f"Spike_{s}{k}", "Ates", (ox + s * (R.sx + 0.04), -0.04 + k * 0.08, R.sz + 0.15), r=0.025, r2=0.0,
                  depth=0.08, rot=(0, s * 30, 0), verts=5)
    a, b = V((ox + 0.2, R.ry + 0.1, R.tz - 0.3)), V((ox - 0.3, R.ry + 0.1, R.hz + 0.45))
    T.limb("Blade", "Tas", a, b, r1=0.085, r2=0.03, verts=4)
    g = a + (b - a) * 0.0
    T.box("Guard", "Altin", g, (0.22, 0.04, 0.04), rot=(0, -45, 0), bevel=0.006)
    T.limb("Grip", "KoyuMese", g, g + V((0.07, 0, -0.07)), r1=0.02, verts=6)
    return R


def c28_vampir(ox):
    R = B.body(ox, "M", "TenElf", "Lacivert", "Lacivert", "KoyuMese", build="thin")
    B.cape(R, "Lacivert", inner="Bordo", collar=True)
    T.dome("Hair", "SacKoyu", (ox, 0.02, R.hz + R.hr * 0.12), r=R.hr * 1.05, rot=(-12, 0, 0))
    T.cyl("WidowPeak", "SacKoyu", (ox, -R.hr * 0.9, R.hz + R.hr * 0.42), r=0.03, r2=0.0, depth=0.06, rot=(180, 0, 0), verts=4)
    T.sphere("Ascot", "Krem", (ox, -R.ry * 0.9, R.tz + R.rz * 0.75), r=0.04, seg=8, rings=6)
    T.sphere("Medal", "Bordo", (ox, -R.ry * 1.0, R.tz + R.rz * 0.3), r=0.03, scale=(1, 0.4, 1), seg=8, rings=6)
    return R


def c29_hayalet(ox):
    R = B.body(ox, "M", "ElfCiyi", "Mavi", "Mavi", "Mavi", legs=False)
    T.cyl("GhostTail", "Mavi", (ox, 0.04, (R.tz + 0.1) / 2), r=R.rx * 0.95, r2=R.rx * 0.2, depth=R.tz - 0.1,
          rot=(180, 0, 0), scale=(1, R.ry / R.rx, 1), verts=12)
    T.dome("Helmet", "Mavi", (ox, 0, R.hz + R.hr * 0.12), r=R.hr * 1.1)
    T.torus("HelmRim", "Krem", (ox, 0, R.hz + R.hr * 0.12), major=R.hr * 1.13, minor=0.015, seg=14, mseg=4)
    for s in (1, -1):
        T.torus(f"Shackle_{s}", "KoyuTas", R.hand[s] + V((0, 0, 0.07)), major=0.05, minor=0.012, seg=10, mseg=4)
        for k in range(3):
            T.torus(f"Chain_{s}{k}", "KoyuTas", R.hand[s] + V((s * 0.02, 0.01, -0.06 - k * 0.05)), major=0.022, minor=0.007,
                    rot=(0, 90 * (k % 2), 0), seg=8, mseg=4)
    return R


def c30_kral(ox):
    R = B.body(ox, "M", "Ten2", "Mor", "Mor", "KoyuMese", nose="big")
    B.skirt(R, "Bez", bottom=0.02, flare=1.3)
    T.dome("Cloak", "Bez", (ox, 0.01, R.tz - R.rz * 0.1), r=1, scale=(R.rx * 1.2, R.ry * 1.2, R.rz * 1.15), seg=14, rings=10)
    T.box("TunicPeek", "Mor", (ox, -R.ry * 1.12, R.tz - R.rz * 0.2), (0.07, 0.02, R.rz * 1.2), bevel=0.005)
    T.dome("Hood", "Bez", (ox, 0.04, R.hz + R.hr * 0.02), r=R.hr * 1.18, rot=(-35, 0, 0))
    for i, dx in enumerate((-0.05, 0.0, 0.05)):
        T.cyl(f"CrownTip{i}", "Altin", (ox + dx, 0.05, R.top + R.hr * 0.18), r=0.022, r2=0.0, depth=0.07, verts=4)
    B.mustache(R, "SacKoyu", size=1.25, tilt=12)
    return R


CUSTOMERS = [
    (1, "Ciftci", "Çiftçi", c01_ciftci), (2, "Balikci", "Balıkçı", c02_balikci), (3, "Oduncu", "Oduncu", c03_oduncu),
    (4, "Ozan", "Ozan", c04_ozan), (5, "Firinci", "Fırıncı", c05_firinci), (6, "CobanKiz", "Çoban Kız", c06_coban_kiz),
    (7, "Kunduraci", "Kunduracı", c07_kunduraci),
    (8, "Demirci", "Demirci", c08_demirci), (9, "Tuccar", "Tüccar", c09_tuccar), (10, "Muhafiz", "Muhafız", c10_muhafiz),
    (11, "Kesis", "Keşiş", c11_kesis), (12, "CuceMadenci", "Cüce Madenci", c12_cuce),
    (13, "Bucukluk", "Buçukluk", c13_bucukluk), (14, "VergiMemuru", "Vergi Memuru", c14_vergi),
    (15, "Yaver", "Yaver", c15_yaver),
    (16, "Sovalye", "Şövalye", c16_sovalye), (17, "Goblin", "Goblin", c17_goblin), (18, "Ork", "Ork Savaşçı", c18_ork),
    (19, "ElfOkcu", "Elf Okçu", c19_elf), (20, "Buyucu", "Büyücü", c20_buyucu), (21, "Cadi", "Cadı", c21_cadi),
    (22, "Simyaci", "Simyacı", c22_simyaci), (23, "Avci", "Avcı", c23_avci), (24, "Kervanci", "Kervancı", c24_kervanci),
    (25, "KorsanKaptan", "Korsan Kaptan", c25_korsan), (26, "Trol", "Trol", c26_trol),
    (27, "EjderhaAvcisi", "Ejderha Avcısı", c27_ejder_avcisi), (28, "VampirKont", "Vampir Kont", c28_vampir),
    (29, "HayaletSovalye", "Hayalet Şövalye", c29_hayalet), (30, "KilikKral", "Gizli Kral", c30_kral),
]
TIERS = [("Kademe1_Koy", range(1, 8)), ("Kademe2_Kasaba", range(8, 16)),
         ("Kademe3a_Fantastik", range(16, 22)), ("Kademe3b_Fantastik", range(22, 27)),
         ("Kademe4_Efsanevi", range(27, 31))]


def build_one(entry, ox=0.0):
    num, key, _, fn = entry
    fn(ox)
    return T.join_all(f"CH_Cust_{num:02d}_{key}")


def produce_files(entries):
    for e in entries:
        T.reset_scene()
        obj = build_one(e)
        tris = T.tri_count(obj)
        flag = "" if tris <= 2000 else "  <-- BÜTÇE AŞILDI"
        print(f"[{obj.name}] üçgen: {tris}{flag}")
        T.save_blend(os.path.join(MODELS, f"{obj.name}.blend"))
        T.export_fbx(obj, os.path.join(FBX, f"{obj.name}.fbx"))


def render_tier(name, entries, spacing=1.0):
    T.reset_scene()
    n = len(entries)
    for i, e in enumerate(entries):
        x = (i - (n - 1) / 2) * spacing
        build_one(e, ox=x)
        T.label(f"{e[0]}. {e[2]}", (x, -0.6, 0.02), size=0.1)
    T.setup_render_stage(floor_size=40)
    width = max(n, 4) * spacing + 0.4
    import math
    dist = (width / 2) / math.tan(math.radians(17.5)) + 0.6
    T.render_from(os.path.join(RENDERS, f"Customers_{name}.png"), target=(0, 0, 0.75), pitch=9, yaw=0, dist=dist,
                  res=(2000, int(2000 * 2.3 / width)))


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    pick = {int(a) for a in argv}
    entries = [e for e in CUSTOMERS if not pick or e[0] in pick]
    produce_files(entries)
    for name, rng in TIERS:
        sub = [e for e in CUSTOMERS if e[0] in rng and (not pick or e[0] in pick)]
        if sub:
            render_tier(name, sub)
    print("BİTTİ")
