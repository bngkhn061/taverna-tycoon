"""
Mutfak (v0.7): yemek tabakları, mutfak makineleri (3 görünüm kademesi), mutfak dekoru ve Aşçı.
Stil: DESIGN_RULES.md "Oyuncak Taverna" — tek palet atlası, yumuşak low-poly, iri oranlar.
Çıktılar mevcut adlandırmaya uyar → Unity içe aktarıcı (TavernaImport) ek ayar istemez:
  PR_Drink_<Yemek>.fbx · PR_Station_<Anahtar>_T1..3.fbx · PR_Decor_<Ad>.fbx · CH_Staff_07_Asci.fbx
  assets/icons/ICO_<Yemek>.png · ICO_Staff_Asci.png · ICO_DEC_<Ad>.png · assets/renders/Props_Kitchen.png
Çalıştırma: blender -b --factory-startup --python tools/blender/build_kitchen.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import toylib as T  # noqa: E402
import bodylib as B  # noqa: E402
import build_props as P  # noqa: E402
import render_portraits as RP  # noqa: E402
from mathutils import Vector as V  # noqa: E402

at, TRIM, tier_extras = P.at, P.TRIM, P.tier_extras
MODELS = os.path.join(T.ROOT, "assets", "models")
FBX = os.path.join(T.ROOT, "assets", "export", "fbx")
RENDERS = os.path.join(T.ROOT, "assets", "renders")


# ================================================================ YEMEKLER (tabakta, ~bardak boyu)
def f_ekmek(ox, oy):
    p = at(ox, oy)
    T.box("Board", "Mese", p(0, 0, 0.012), (0.2, 0.13, 0.024), bevel=0.006)
    T.sphere("Loaf", "Bal", p(0, 0, 0.06), r=0.07, scale=(1.2, 0.75, 0.6), seg=10, rings=7)
    for i in range(3):
        T.box(f"Score{i}", "BalSarabi", p(-0.04 + i * 0.04, 0, 0.098), (0.012, 0.07, 0.01), rot=(0, 0, 25))
    T.sphere("Crumb", "Bal", p(0.08, -0.04, 0.03), r=0.012, seg=5, rings=4)


def f_guvec(ox, oy):
    p = at(ox, oy)
    T.cyl("Pot", "Bakir", p(0, 0, 0.045), r=0.075, r2=0.06, depth=0.09, verts=12)
    T.torus("Rim", "Bakir", p(0, 0, 0.09), major=0.075, minor=0.01, seg=12)
    T.cyl("Stew", "Kirmizi", p(0, 0, 0.088), r=0.068, depth=0.01, verts=12)
    for i, (x, y, c) in enumerate(((0.03, 0.02, "Bal"), (-0.03, -0.01, "Yaprak"), (0.0, -0.035, "Ten4"))):
        T.sphere(f"Chunk{i}", c, p(x, y, 0.098), r=0.015, seg=6, rings=4)
    T.limb("Spoon", "Mese", p(0.03, 0.02, 0.09), p(0.11, 0.07, 0.16), r1=0.008, verts=5)
    for s in (1, -1):
        T.torus(f"Ear{s}", "Bakir", p(s * 0.085, 0, 0.06), major=0.018, minor=0.006, rot=(90, 0, 90), seg=8)


def f_kozet(ox, oy):
    p = at(ox, oy)
    T.cyl("Plate", "Krem", p(0, 0, 0.008), r=0.1, depth=0.016, verts=14)
    T.torus("PlateRim", "Tas", p(0, 0, 0.016), major=0.095, minor=0.006, seg=14)
    T.limb("Skewer", "KoyuMese", p(-0.1, -0.02, 0.04), p(0.1, 0.02, 0.04), r1=0.006, verts=4)
    for i in range(4):
        x = -0.06 + i * 0.04
        T.sphere(f"Meat{i}", "Ten4" if i % 2 == 0 else "Ten5", p(x, x * 0.2, 0.045), r=0.024, scale=(1, 1, 0.85), seg=7, rings=5)
    T.sphere("Pepper", "Kirmizi", p(0.04, -0.06, 0.03), r=0.018, scale=(1.4, 0.7, 0.7), seg=6, rings=4)
    T.sphere("Herb", "Yaprak", p(-0.05, 0.06, 0.025), r=0.02, scale=(1, 1, 0.5), seg=6, rings=4)


def f_turta(ox, oy):
    p = at(ox, oy)
    T.cyl("Dish", "Bakir", p(0, 0, 0.015), r=0.09, r2=0.075, depth=0.03, verts=14)
    T.cyl("Crust", "Bal", p(0, 0, 0.035), r=0.085, depth=0.025, verts=14)
    for i in range(3):
        T.box(f"LatticeA{i}", "BalSarabi", p(-0.04 + i * 0.04, 0, 0.05), (0.014, 0.15, 0.01))
        T.box(f"LatticeB{i}", "BalSarabi", p(0, -0.04 + i * 0.04, 0.052), (0.15, 0.014, 0.01))
    T.sphere("Apple", "Kirmizi", p(0.05, -0.05, 0.07), r=0.022, seg=7, rings=5)
    T.box("Leaf", "Yaprak", p(0.055, -0.05, 0.095), (0.02, 0.008, 0.012), rot=(0, 30, 0))


# ================================================================ MUTFAK MAKİNELERİ (kademe 1-3)
def flame(p, name, x, y, z, s=1.0):
    T.cyl(f"{name}_A", "Ates", p(x, y, z + 0.06 * s), r=0.06 * s, r2=0.0, depth=0.16 * s, verts=6)
    T.cyl(f"{name}_B", "Altin", p(x + 0.02 * s, y - 0.01, z + 0.04 * s), r=0.035 * s, r2=0.0, depth=0.1 * s, verts=5)


def s_firin(ox, oy, tier=1):
    """Taş fırın: kubbe, ağızda ateş, üstte ekmekler."""
    p = at(ox, oy)
    T.box("Base", "Tas", p(0, 0, 0.2), (0.8, 0.62, 0.4), bevel=0.03)
    T.sphere("Dome", "Tas", p(0, 0.03, 0.42), r=0.36, scale=(1, 0.85, 0.75), seg=12, rings=7)
    T.box("Mouth", "KoyuTas", p(0, -0.27, 0.48), (0.3, 0.06, 0.2), bevel=0.04)
    flame(p, "Fire", 0, -0.26, 0.4, 0.9)
    T.box("Arch", TRIM[tier] if tier > 1 else "KoyuTas", p(0, -0.3, 0.6), (0.36, 0.04, 0.05), bevel=0.01)
    T.cyl("Chimney", "KoyuTas", p(0.18, 0.12, 0.85), r=0.07, depth=0.32, verts=8)
    for i in range(2 + tier):
        T.sphere(f"Bread{i}", "Bal", p(-0.28 + i * 0.13, -0.33, 0.07), r=0.05, scale=(1.2, 0.8, 0.6), seg=8, rings=5)
    T.box("Peel", "Mese", p(-0.36, -0.18, 0.45), (0.04, 0.04, 0.7), rot=(15, 0, 0))
    tier_extras(p, tier, 0.95, front_y=-0.32)


def s_kazan(ox, oy, tier=1):
    """Güveç kazanı: ateşin üstünde kara kazan, kepçe."""
    p = at(ox, oy)
    T.cyl("Hearth", "Tas", p(0, 0, 0.06), r=0.36, depth=0.12, verts=10)
    for i in range(3):
        a = math.radians(i * 120 + 30)
        T.box(f"Log{i}", "Mese", p(math.cos(a) * 0.12, math.sin(a) * 0.12, 0.15), (0.26, 0.06, 0.06), rot=(0, 0, i * 120 + 30), bevel=0.01)
    flame(p, "Fire", 0, 0, 0.14, 1.1)
    T.sphere("Cauldron", "KoyuTas", p(0, 0, 0.5), r=0.27, scale=(1, 1, 0.8), seg=12, rings=8)
    T.torus("Rim", TRIM[tier] if tier > 1 else "KoyuTas", p(0, 0, 0.68), major=0.24, minor=0.025, seg=12)
    T.cyl("Stew", "Kirmizi", p(0, 0, 0.67), r=0.22, depth=0.02, verts=12)
    for s in (1, -1):
        T.box(f"Leg{s}", "KoyuTas", p(s * 0.2, 0, 0.25), (0.05, 0.05, 0.3), rot=(0, s * 12, 0))
    T.limb("Ladle", "Mese", p(0.05, 0.05, 0.66), p(0.2, 0.15, 1.0), r1=0.015, verts=5)
    for i in range(2):
        T.sphere(f"Steam{i}", "Kopuk", p(-0.05 + i * 0.08, 0, 0.82 + i * 0.1), r=0.05 - i * 0.012, seg=6, rings=4)
    tier_extras(p, tier, 0.9, front_y=-0.3)


def s_mangal(ox, oy, tier=1):
    """Mangal: taş ocak, közde şişler."""
    p = at(ox, oy)
    T.box("Body", "Tas", p(0, 0, 0.3), (0.8, 0.45, 0.6), bevel=0.03)
    T.box("Pan", "KoyuTas", p(0, 0, 0.62), (0.74, 0.4, 0.06))
    for i in range(6):
        T.sphere(f"Coal{i}", "Ates" if i % 2 else "KoyuTas", p(-0.27 + i * 0.11, (i % 3 - 1) * 0.08, 0.67), r=0.045, seg=6, rings=4)
    for k in range(2 + (tier >= 2)):
        y = -0.12 + k * 0.12
        T.limb(f"Skewer{k}", TRIM[tier] if tier > 1 else "KoyuTas", p(-0.42, y, 0.74), p(0.42, y, 0.74), r1=0.008, verts=4)
        for i in range(4):
            T.sphere(f"Meat{k}{i}", "Ten4" if (i + k) % 2 else "Ten5", p(-0.18 + i * 0.12, y, 0.75), r=0.035, seg=6, rings=4)
    T.box("Hood", "KoyuTas", p(0, 0.2, 1.05), (0.8, 0.08, 0.5), bevel=0.02)
    tier_extras(p, tier, 0.95, front_y=-0.24)


def s_tatli(ox, oy, tier=1):
    """Tatlı fırını: camlı vitrin dolap, raflarda turtalar."""
    p = at(ox, oy)
    T.box("Cabinet", "Mese", p(0, 0, 0.5), (0.72, 0.5, 1.0), bevel=0.02)
    T.box("Glass", "Su", p(0, -0.24, 0.62), (0.6, 0.02, 0.6))
    for i, z in enumerate((0.42, 0.7)):
        T.box(f"Shelf{i}", "KoyuMese", p(0, -0.05, z), (0.62, 0.36, 0.02))
        for k in range(2 if i == 0 or tier >= 2 else 1):
            x = -0.15 + k * 0.3
            T.cyl(f"Pie{i}{k}", "Bal", p(x, -0.08, z + 0.04), r=0.1, depth=0.05, verts=10)
            T.sphere(f"Cherry{i}{k}", "Kirmizi", p(x, -0.08, z + 0.075), r=0.015, seg=5, rings=4)
    T.box("Top", TRIM[tier] if tier > 1 else "KoyuMese", p(0, 0, 1.02), (0.78, 0.55, 0.05), bevel=0.01)
    T.cyl("CakeStand", "Krem", p(0, 0, 1.12), r=0.12, depth=0.02, verts=10)
    T.cyl("Cake", "Bal", p(0, 0, 1.18), r=0.1, depth=0.1, verts=10)
    T.cyl("Icing", "Kopuk", p(0, 0, 1.24), r=0.1, depth=0.02, verts=10)
    tier_extras(p, tier, 1.1, front_y=-0.27)


# ================================================================ MUTFAK DEKORU
def x_tencere(ox, oy):
    """Duvara asılı tencere rafı (ön yüz −y)."""
    p = at(ox, oy)
    T.box("Rail", "KoyuMese", p(0, 0, 1.25), (0.8, 0.05, 0.05), bevel=0.01)
    for i, (x, r, c) in enumerate(((-0.25, 0.09, "Bakir"), (0.0, 0.11, "KoyuTas"), (0.25, 0.08, "Bakir"))):
        T.limb(f"Hook{i}", "KoyuTas", p(x, -0.03, 1.25), p(x, -0.03, 1.12), r1=0.006, verts=4)
        T.cyl(f"Pot{i}", c, p(x, -0.06, 1.0), r=r, depth=0.12, rot=(90, 0, 0), verts=10)


def x_cuval(ox, oy):
    """Un çuvalı + dökülmüş un."""
    p = at(ox, oy)
    T.sphere("Sack", "Bez", p(0, 0, 0.25), r=0.22, scale=(1, 0.85, 1.2), seg=10, rings=7)
    T.cyl("Neck", "Bez", p(0, 0, 0.52), r=0.07, r2=0.12, depth=0.1, verts=8)
    T.torus("Tie", "Kirmizi", p(0, 0, 0.5), major=0.075, minor=0.015, seg=8)
    T.cyl("Flour", "Krem", p(0.15, -0.18, 0.005), r=0.12, depth=0.01, verts=10)


def x_sepet(ox, oy):
    """Sebze sepeti."""
    p = at(ox, oy)
    T.cyl("Basket", "Mese", p(0, 0, 0.12), r=0.22, r2=0.18, depth=0.24, verts=12)
    T.torus("BasketRim", "KoyuMese", p(0, 0, 0.24), major=0.22, minor=0.02, seg=12)
    for i, (x, y, c, r) in enumerate(((0.06, 0.05, "Kirmizi", 0.07), (-0.08, 0.0, "Yaprak", 0.09), (0.02, -0.09, "Bal", 0.06),
                                      (-0.04, 0.1, "Kirmizi", 0.06), (0.1, -0.03, "Mor", 0.06))):
        T.sphere(f"Veg{i}", c, p(x, y, 0.27), r=r, seg=8, rings=6)


# ================================================================ AŞÇI (personel, kırmızı önlük kuralı)
def st_asci(ox):
    R = B.body(ox, "M", "Ten2", "Krem", "KoyuTas", "KoyuMese", build="fat", nose="big")
    B.apron(R, "Kirmizi", top=0.25, bottom=0.1, width=0.85)
    T.cyl("ChefBand", "Kopuk", (ox, 0, R.hz + R.hr * 0.65), r=R.hr * 0.95, depth=R.hr * 0.35, verts=12)
    for i, (x, y) in enumerate(((0, 0), (-0.4, 0.1), (0.4, 0.1), (0, 0.4))):
        T.sphere(f"ChefPuff{i}", "Kopuk", (ox + x * R.hr, y * R.hr, R.hz + R.hr * 1.15), r=R.hr * 0.55, seg=8, rings=6)
    for s in (1, -1):
        T.sphere(f"Stache_{s}", "SacKahve", (ox + s * R.hr * 0.25, -R.hr * 1.0, R.hz - R.hr * 0.36), r=1,
                 scale=(R.hr * 0.32, R.hr * 0.1, R.hr * 0.1), rot=(0, s * -25, 0), seg=6, rings=4)
    h = R.hand[1]
    T.limb("Ladle", "Mese", h + V((0, -0.02, -0.05)), h + V((0, -0.05, 0.25)), r1=0.014, verts=5)
    T.sphere("LadleCup", "Tas", h + V((0, -0.05, 0.28)), r=0.05, scale=(1, 1, 0.6), seg=6, rings=4)
    return R


FOODS = [("Ekmek", "Sıcak Ekmek", f_ekmek), ("Guvec", "Güveç", f_guvec), ("KozEt", "Köz Et", f_kozet), ("Turta", "Elmalı Turta", f_turta)]
KSTATIONS = [("Firin", "Taş Fırın", s_firin), ("Kazan", "Güveç Kazanı", s_kazan), ("Mangal", "Mangal", s_mangal),
             ("Tatli", "Tatlı Fırını", s_tatli)]
KDECOR = [("Tencere", "Tencere Rafı", x_tencere), ("Cuval", "Un Çuvalı", x_cuval), ("Sepet", "Sebze Sepeti", x_sepet)]


def export(name, sub, build):
    T.reset_scene()
    build()
    obj = T.join_all(name, smooth_angle=35)
    print(f"[{name}] üçgen: {T.tri_count(obj)}")
    T.save_blend(os.path.join(MODELS, sub, f"{name}.blend"))
    T.export_fbx(obj, os.path.join(FBX, f"{name}.fbx"))


if __name__ == "__main__":
    for key, _, fn in FOODS:
        export(f"PR_Drink_{key}", "Drink", lambda fn=fn: fn(0, 0))
    for key, _, fn in KSTATIONS:
        for t in (1, 2, 3):
            export(f"PR_Station_{key}_T{t}", "Station", lambda fn=fn, t=t: fn(0, 0, t))
    for key, _, fn in KDECOR:
        export(f"PR_Decor_{key}", "Decor", lambda fn=fn: fn(0, 0))
    T.reset_scene()
    st_asci(0.0)
    obj = T.join_all("CH_Staff_07_Asci")
    print(f"[{obj.name}] üçgen: {T.tri_count(obj)}")
    T.save_blend(os.path.join(MODELS, "characters", f"{obj.name}.blend"))
    T.export_fbx(obj, os.path.join(FBX, f"{obj.name}.fbx"))

    # İkonlar (oyun içi kartlar)
    for key, _, fn in FOODS:
        RP.shoot(lambda fn=fn: fn(0, 0), key, False)
    for key, _, fn in KDECOR:
        RP.shoot(lambda fn=fn: fn(0, 0), f"DEC_{key}", False)
    RP.shoot(lambda: st_asci(0.0), "Staff_Asci", True)

    # Önizleme sayfası: makineler (kademeler yan yana) + yemekler + dekor + aşçı
    T.reset_scene()
    for r, (key, lbl, fn) in enumerate(KSTATIONS):
        for t in (1, 2, 3):
            fn((t - 2) * 1.2 - 2.0, (1.5 - r) * 1.3, t)
        T.label(lbl, (-4.2, (1.5 - r) * 1.3 - 0.1, 0.02), size=0.13)
    for i, (key, lbl, fn) in enumerate(FOODS):
        fn(1.4 + i * 0.45, 1.8)
    for i, (key, lbl, fn) in enumerate(KDECOR):
        fn(1.6 + i * 0.9, 0.4)
    st_asci(2.6)
    T.join_all("KitchenSheet")
    T.setup_render_stage(floor_size=40, floor_color="KoyuTas")
    T.render_from(os.path.join(RENDERS, "Props_Kitchen.png"), target=(0.0, 0.2, 0.4), pitch=32, yaw=0, dist=9.0, fov=46, res=(2000, 1100))
    print("BİTTİ")
