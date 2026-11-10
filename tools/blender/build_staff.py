"""
Personel: Garson (2 varyant), Barmen, Temizlikçi, Fedai, Ozan (sahne). Tasarım: GAME_DESIGN.md §5.
Üniforma kuralı: Her personelde tavernanın KIRMIZI rengi (önlük ya da fular) vardır → müşterilerden ayrılır.
Çalıştırma: blender -b --factory-startup --python tools/blender/build_staff.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import toylib as T  # noqa: E402
import bodylib as B  # noqa: E402
import build_main_characters as M  # noqa: E402
from mathutils import Vector as V  # noqa: E402

MODELS = os.path.join(T.ROOT, "assets", "models", "characters")
FBX = os.path.join(T.ROOT, "assets", "export", "fbx")
RENDERS = os.path.join(T.ROOT, "assets", "renders")


def neckerchief(R, color="Kirmizi"):
    T.torus("Kerchief", color, (R.ox, 0, R.tz + R.rz * 0.88), major=R.rx * 0.55, minor=0.025, seg=12)
    T.cyl("KerchiefTip", color, (R.ox, -R.ry * 0.75, R.tz + R.rz * 0.68), r=0.05, r2=0.0, depth=0.09,
          rot=(160, 0, 0), scale=(1, 0.4, 1), verts=3)


def tray(R, side=1, mugs=2):
    h = R.hand[side]
    c = h + V((0, -0.06, 0.06))
    T.cyl("Tray", "Tas", c, r=0.15, depth=0.015, verts=12)
    for i in range(mugs):
        T.cyl(f"TrayMug{i}", "Mese", c + V((-0.05 + i * 0.1, 0, 0.055)), r=0.035, depth=0.09, verts=8)
        T.sphere(f"TrayFoam{i}", "Kopuk", c + V((-0.05 + i * 0.1, 0, 0.1)), r=0.036, scale=(1, 1, 0.5), seg=6, rings=4)


def st_garson_mina(ox):
    R = B.body(ox, "M", "Ten1", "Krem", "Yesil", "KoyuMese", sleeves="short")
    B.skirt(R, "Yesil", bottom=0.12, flare=1.25)
    B.apron(R, "Kirmizi", top=0.2, bottom=0.15, width=0.75)
    B.hair(R, "SacKahve", rot=(-22, 0, 0))
    T.sphere("Bun", "SacKahve", (ox, R.hr * 0.55, R.hz + R.hr * 0.65), r=R.hr * 0.38, seg=8, rings=6)
    T.box("Headband", "Kirmizi", (ox, -R.hr * 0.2, R.hz + R.hr * 0.8), (R.hr * 1.4, 0.04, 0.035), rot=(-35, 0, 0), bevel=0.008)
    tray(R, 1, 2)
    return R


def st_garson_tobi(ox):
    R = B.body(ox, "M", "Ten4", "Krem", "KoyuMese", "KoyuMese", build="thin")
    B.vest(R, "Yesil", buttons="Altin")
    B.apron(R, "Kirmizi", top=-0.35, bottom=0.12, width=0.8)
    for i, (dx, dz) in enumerate(((0, 1.0), (0.45, 0.85), (-0.45, 0.85), (0.75, 0.5), (-0.75, 0.5))):
        T.sphere(f"Curl{i}", "SacKoyu", (ox + dx * R.hr, 0.02, R.hz + dz * R.hr * 0.9), r=R.hr * 0.36, seg=7, rings=5)
    T.limb("Towel", "Krem", (ox - R.sx - 0.02, -0.02, R.sz + 0.04), (ox - R.sx - 0.06, -0.04, R.sz - 0.2), r1=0.035, verts=4)
    tray(R, 1, 3)
    return R


def st_barmen(ox):
    R = B.body(ox, "M", "Ten2", "Krem", "Lacivert", "KoyuMese", build="thin", nose="long")
    B.vest(R, "Lacivert", buttons="Pirinc")
    T.box("BowTieL", "Kirmizi", (ox - 0.035, -R.ry * 0.85, R.tz + R.rz * 0.85), (0.06, 0.02, 0.045), rot=(0, 0, 20), bevel=0.008)
    T.box("BowTieR", "Kirmizi", (ox + 0.035, -R.ry * 0.85, R.tz + R.rz * 0.85), (0.06, 0.02, 0.045), rot=(0, 0, -20), bevel=0.008)
    T.dome("SlickHair", "SacKoyu", (ox, 0.02, R.hz + R.hr * 0.1), r=R.hr * 1.05, scale=(1, 1, 0.85), rot=(-15, 0, 0))
    T.box("Part", "SacKoyu", (ox + R.hr * 0.3, -R.hr * 0.55, R.hz + R.hr * 0.75), (R.hr * 0.6, 0.06, 0.05), rot=(-40, 0, 20))
    for s in (1, -1):
        T.sphere(f"Stache_{s}", "SacKoyu", (ox + s * R.hr * 0.22, -R.hr * 1.0, R.hz - R.hr * 0.36), r=1,
                 scale=(R.hr * 0.3, R.hr * 0.1, R.hr * 0.08), rot=(0, s * -20, 0), seg=6, rings=4)
    T.cyl("ArmBand", "Kirmizi", tuple(R.hand[1] + V((-0.04, 0.03, 0.17))), r=0.04, depth=0.03, verts=8)
    h = R.hand[-1]
    T.cyl("Shaker", "Pirinc", h + V((0, -0.04, 0.08)), r=0.035, r2=0.028, depth=0.14, verts=8)
    T.sphere("ShakerCap", "Pirinc", h + V((0, -0.04, 0.16)), r=0.03, seg=6, rings=4)
    return R


def st_temizlikci(ox):
    R = B.body(ox, "M", "Ten2", "Mor", "Mor", "KoyuMese", build="fat", nose="big")
    B.skirt(R, "Mor", bottom=0.06, flare=1.3)
    B.apron(R, "Kirmizi", top=0.25, bottom=0.1, width=0.85)
    T.dome("Scarf", "Kirmizi", (ox, 0.02, R.hz + R.hr * 0.05), r=R.hr * 1.1, rot=(-25, 0, 0))
    T.sphere("ScarfKnot", "Kirmizi", (ox, -R.hr * 0.25, R.hz - R.hr * 0.75), r=R.hr * 0.22, seg=6, rings=4)
    B.glasses(R, "Pirinc")
    for s in (1, -1):
        T.sphere(f"Curl_{s}", "SacGri", (ox + s * R.hr * 0.75, -R.hr * 0.3, R.hz + R.hr * 0.35), r=R.hr * 0.22, seg=6, rings=4)
    top = B.staff(R, 1, "Mese", R.hz + R.hr * 0.6, r=0.016, base=0.1)
    T.cyl("MopHead", "Bez", (top.x, top.y, 0.06), r=0.09, r2=0.05, depth=0.12, verts=8)
    T.cyl("Bucket", "Tas", (ox - R.rx - 0.15, -0.12, 0.1), r=0.1, r2=0.12, depth=0.2, verts=10)
    T.cyl("BucketWater", "Su", (ox - R.rx - 0.15, -0.12, 0.19), r=0.11, depth=0.01, verts=10)
    T.sphere("Suds", "Kopuk", (ox - R.rx - 0.13, -0.12, 0.21), r=0.05, seg=6, rings=4)
    return R


def st_fedai(ox):
    R = B.body(ox, "L", "Ten3", "Lacivert", "KoyuTas", "KoyuMese", build="huge", sleeves="short", nose="big")
    neckerchief(R, "Kirmizi")
    B.belt(R, "KoyuMese", buckle="Pirinc", z=-0.3, minor=0.03)
    for s in (1, -1):
        T.box(f"Brow_{s}", "SacKoyu", (ox + s * R.hr * 0.38, -R.hr * 0.86, R.hz + R.hr * 0.38), (R.hr * 0.45, 0.04, 0.04),
              rot=(0, s * 20, 0), bevel=0.008)
        T.cyl(f"Wristband_{s}", "KoyuMese", tuple(R.hand[s] + V((0, 0.01, 0.1))), r=0.075, depth=0.06, verts=8)
    T.box("Stubble", "SacKoyu", (ox, -R.hr * 0.75, R.hz - R.hr * 0.55), (R.hr * 0.9, 0.05, R.hr * 0.35), bevel=0.03)
    T.box("Badge", "Pirinc", (ox + R.rx * 0.45, -R.ry * 0.95, R.tz + R.rz * 0.35), (0.08, 0.02, 0.09), bevel=0.01)
    return R


def st_ozan_sahne(ox):
    R = B.body(ox, "M", "Ten3", "Kirmizi", "Krem", "KoyuMese", build="thin")
    B.vest(R, "Altin")
    for i in range(4):
        T.limb(f"Hair{i}", "SacKahve", (ox + (i - 1.5) * R.hr * 0.4, 0.04, R.hz + R.hr * 0.85),
               (ox + (i - 1.5) * R.hr * 0.6, R.hr * 0.6, R.hz - R.hr * 0.3), r1=R.hr * 0.22, r2=R.hr * 0.12, verts=6)
    B.hair(R, "SacKahve", rot=(-30, 0, 0))
    T.cyl("HatBrim", "Mor", (ox, 0, R.hz + R.hr * 0.6), r=R.hr * 1.4, depth=0.02, rot=(-8, 0, 10), verts=12)
    T.dome("HatCrown", "Mor", (ox, 0, R.hz + R.hr * 0.6), r=R.hr * 0.85, rot=(-8, 0, 10))
    T.sphere("HatFeather", "Kirmizi", (ox - R.hr * 0.9, 0.05, R.hz + R.hr * 1.1), r=1, scale=(0.03, 0.02, 0.18), rot=(0, -45, 0), seg=6, rings=4)
    c = (R.hand[1] + R.hand[-1]) / 2 + V((0, -0.12, 0.08))
    T.sphere("Lute", "Mese", c + V((0.06, 0, 0)), r=0.13, scale=(1.2, 0.45, 1), seg=10, rings=7)
    T.limb("LuteNeck", "KoyuMese", c + V((-0.05, 0, 0.03)), c + V((-0.35, 0, 0.22)), r1=0.022, verts=6)
    T.cyl("LuteHole", "Goz", c + V((0.06, -0.06, 0)), r=0.04, depth=0.01, rot=(90, 0, 0), verts=8)
    return R


STAFF = [(1, "GarsonMina", "Garson Mina", st_garson_mina), (2, "GarsonTobi", "Garson Tobi", st_garson_tobi),
         (3, "Barmen", "Barmen Gus", st_barmen), (4, "Temizlikci", "Temizlikçi Nana", st_temizlikci),
         (5, "Fedai", "Fedai Brak", st_fedai), (6, "OzanSahne", "Sahne Ozanı Lir", st_ozan_sahne)]

if __name__ == "__main__":
    for num, key, _, fn in STAFF:
        T.reset_scene()
        fn(0.0)
        obj = T.join_all(f"CH_Staff_{num:02d}_{key}")
        print(f"[{obj.name}] üçgen: {T.tri_count(obj)}")
        T.save_blend(os.path.join(MODELS, f"{obj.name}.blend"))
        T.export_fbx(obj, os.path.join(FBX, f"{obj.name}.fbx"))
    # Kadro sayfası: Barlo + Pip + personel
    T.reset_scene()
    import math
    entries = [("Barlo (Usta)", lambda x: M.build_barlo(ox=x)), ("Pip (Çırak)", lambda x: M.build_pip(ox=x))]
    entries += [(lbl, (lambda f: (lambda x: (f(x), T.join_all(f"CH_S_{f.__name__}"))))(fn)) for _, _, lbl, fn in STAFF]
    n = len(entries)
    for i, (lbl, f) in enumerate(entries):
        x = (i - (n - 1) / 2) * 1.05
        f(x)
        T.label(lbl, (x, -0.6, 0.02), size=0.1)
    T.setup_render_stage(floor_size=40)
    width = n * 1.05 + 0.4
    dist = (width / 2) / math.tan(math.radians(17.5)) + 0.6
    T.render_from(os.path.join(RENDERS, "Staff_Lineup.png"), target=(0, 0, 0.75), pitch=9, yaw=0, dist=dist,
                  res=(2000, int(2000 * 2.3 / width)))
    print("BİTTİ")
