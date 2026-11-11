"""
v1.5 Paket C (2026-11-10): oyun sonu içerikleri.
İçecek: PR_Drink_KralSerbeti · PR_Station_Serbet_T1..3 (Ana Salon, Sv27: mermer kaideli altın şerbet çeşmesi)
Yemek:  PR_Drink_SolenTabagi · PR_Station_Solen_T1..3 (Mutfak, Sv29: şişte kuzu çeviren taş şölen ocağı)
İkonlar: ICO_KralSerbeti / ICO_SolenTabagi · önizleme assets/renders/Props_Sections3.png
Çalıştırma: blender -b --factory-startup --python tools/blender/build_sections3.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import toylib as T  # noqa: E402
import build_props as P  # noqa: E402
import render_portraits as RP  # noqa: E402

at, TRIM, tier_extras = P.at, P.TRIM, P.tier_extras
MODELS = os.path.join(T.ROOT, "assets", "models")
FBX = os.path.join(T.ROOT, "assets", "export", "fbx", "props")
RENDERS = os.path.join(T.ROOT, "assets", "renders")


# ------------------------------------------------------------------ Kral Şerbeti (tepsi / masa ölçeği)
def d_kral_serbeti(ox, oy):
    """Altın kadeh: ayak, sap, kâse; içinde mor-kırmızı şerbet, kenarında taç dişleri ve nane yaprağı."""
    p = at(ox, oy)
    T.cyl("Foot", "Altin", p(0, 0, 0.006), r=0.04, depth=0.012, verts=12)
    T.cyl("Stem", "Altin", p(0, 0, 0.045), r=0.009, depth=0.07, verts=8)
    T.sphere("Knot", "Kirmizi", p(0, 0, 0.05), r=0.015, seg=6, rings=4)
    T.cyl("Bowl", "Altin", p(0, 0, 0.115), r=0.05, r2=0.032, depth=0.075, rot=(180, 0, 0), verts=12)
    T.cyl("Sherbet", "Mor", p(0, 0, 0.148), r=0.045, depth=0.01, verts=12)
    for k in range(5):
        a = k * 2 * math.pi / 5
        T.cyl(f"Tip{k}", "Altin", p(math.cos(a) * 0.05, math.sin(a) * 0.05, 0.165), r=0.012, r2=0.0, depth=0.025, verts=4)
    T.sphere("Mint", "Yaprak", p(0.02, -0.01, 0.156), r=0.015, scale=(1.4, 0.8, 0.4), seg=6, rings=3)
    T.sphere("Gem", "ElfCiyi", p(0, -0.048, 0.12), r=0.01, seg=5, rings=3)


def s_serbet(ox, oy, tier=1):
    """Şerbet çeşmesi: mermer kaide, altın havuz, iki katlı çanak, tepede taç; yanda testi ve kadehler."""
    p = at(ox, oy)
    T.box("Plinth", "Siva", p(0, 0, 0.2), (0.86, 0.5, 0.4), bevel=0.03)
    T.box("Trim", TRIM[tier] if tier > 1 else "Pirinc", p(0, 0, 0.41), (0.9, 0.54, 0.04), bevel=0.01)
    T.cyl("Basin", "Altin", p(-0.1, 0.02, 0.48), r=0.24, r2=0.28, depth=0.1, verts=14)
    T.cyl("Pool", "Mor", p(-0.1, 0.02, 0.53), r=0.25, depth=0.012, verts=14)
    T.cyl("Column", "Siva", p(-0.1, 0.02, 0.68), r=0.04, depth=0.3, verts=8)
    T.cyl("Bowl2", "Altin", p(-0.1, 0.02, 0.84), r=0.1, r2=0.15, depth=0.06, verts=12)
    T.cyl("Pool2", "Mor", p(-0.1, 0.02, 0.87), r=0.13, depth=0.01, verts=12)
    T.cyl("Column2", "Siva", p(-0.1, 0.02, 0.95), r=0.025, depth=0.14, verts=8)
    T.cyl("CrownBase", "Altin", p(-0.1, 0.02, 1.04), r=0.07, depth=0.04, verts=8)
    for k in range(5):
        a = k * 2 * math.pi / 5
        T.cyl(f"CrownTip{k}", "Altin", p(-0.1 + math.cos(a) * 0.06, 0.02 + math.sin(a) * 0.06, 1.09), r=0.018, r2=0.0, depth=0.06, verts=4)
    T.sphere("CrownGem", "Kirmizi", p(-0.1, 0.02, 1.08), r=0.028, seg=6, rings=4)
    for i in range(3):   # akan şerbet damlaları
        a = math.radians(i * 120 + 30)
        T.sphere(f"Drop{i}", "Mor", p(-0.1 + math.cos(a) * 0.15, 0.02 + math.sin(a) * 0.15, 0.7), r=0.022, scale=(1, 1, 1.8), seg=6, rings=4)
    T.cyl("Jug", "Bakir", p(0.3, -0.05, 0.53), r=0.06, r2=0.04, depth=0.18, verts=10)
    T.limb("JugHandle", "Bakir", p(0.36, -0.05, 0.6), p(0.37, -0.05, 0.5), r1=0.012, verts=5)
    for i in range(2):
        T.cyl(f"Goblet{i}", "Altin", p(0.3 + i * 0.07 - 0.04, 0.15, 0.48), r=0.025, r2=0.018, depth=0.07, rot=(180, 0, 0), verts=8)
    T.box("Sign", TRIM[tier] if tier > 1 else "KoyuMese", p(0, -0.26, 0.24), (0.4, 0.03, 0.12), bevel=0.01)
    if tier >= 2:
        for s in (-1, 1):
            T.cyl(f"Pillar{s}", "Siva", p(s * 0.4, 0.2, 0.75), r=0.03, depth=0.7, verts=8)
            T.sphere(f"PillarTop{s}", TRIM[tier], p(s * 0.4, 0.2, 1.12), r=0.045, seg=8, rings=5)
    if tier >= 3:
        T.box("Banner", "Bordo", p(0, 0.23, 0.95), (0.7, 0.02, 0.3))
        T.box("BannerTrim", "Altin", p(0, 0.22, 0.81), (0.7, 0.02, 0.03))


# ------------------------------------------------------------------ Şölen Tabağı (yemek)
def d_solen(ox, oy):
    """Büyük şölen tabağı: kızarmış tavuk budu, üzüm salkımı, ekmek, elma dilimleri."""
    p = at(ox, oy)
    T.cyl("Plate", "Altin", p(0, 0, 0.008), r=0.13, depth=0.016, verts=14)
    T.cyl("PlateIn", "Krem", p(0, 0, 0.016), r=0.115, depth=0.004, verts=14)
    T.sphere("Roast", "Bakir", p(-0.02, 0.0, 0.05), r=0.055, scale=(1.4, 1.0, 0.75), seg=10, rings=6)
    for s in (-1, 1):
        T.limb(f"Leg{s}", "Bakir", p(0.04, s * 0.035, 0.05), p(0.085, s * 0.05, 0.07), r1=0.018, r2=0.012, verts=6)
        T.sphere(f"Bone{s}", "Krem", p(0.09, s * 0.052, 0.072), r=0.008, seg=5, rings=3)
    for i in range(5):
        T.sphere(f"Grape{i}", "Mor", p(-0.07 + (i % 2) * 0.016, -0.07 + i * 0.008, 0.03 + (i // 2) * 0.006), r=0.012, seg=6, rings=4)
    T.box("Bread", "Bal", p(0.02, 0.08, 0.03), (0.08, 0.035, 0.03), rot=(0, 0, 20), bevel=0.01)
    T.sphere("Apple", "Kirmizi", p(0.08, -0.06, 0.03), r=0.02, seg=6, rings=4)
    T.sphere("Herb", "Yaprak", p(-0.02, 0.0, 0.09), r=0.012, scale=(1.5, 0.8, 0.4), seg=6, rings=3)


def s_solen(ox, oy, tier=1):
    """Şölen ocağı: taş ocak, korlar, iki çatal üzerinde şişte dönen kuzu, yanda baharat çanakları ve büyük tabak."""
    p = at(ox, oy)
    T.box("Hearth", "Tas", p(0, 0, 0.22), (0.92, 0.5, 0.44), bevel=0.03)
    T.box("Coals", "Ates", p(0, 0, 0.45), (0.74, 0.34, 0.03))
    for i in range(5):
        T.sphere(f"Ember{i}", "EjderAtesi", p(-0.3 + i * 0.15, (i % 2) * 0.06 - 0.03, 0.47), r=0.035, seg=6, rings=3)
    for s in (-1, 1):
        T.limb(f"ForkA{s}", "KoyuTas", p(s * 0.42, -0.06, 0.44), p(s * 0.42, 0.0, 0.8), r1=0.018, verts=6)
        T.limb(f"ForkB{s}", "KoyuTas", p(s * 0.42, 0.06, 0.44), p(s * 0.42, 0.0, 0.8), r1=0.018, verts=6)
    T.cyl("Spit", "KoyuTas", p(0, 0, 0.8), r=0.012, depth=0.95, rot=(0, 90, 0), verts=6)
    T.limb("Crank", "KoyuTas", p(0.47, 0, 0.8), p(0.5, 0, 0.7), r1=0.012, verts=5)
    T.sphere("Lamb", "Bakir", p(0, 0, 0.8), r=0.14, scale=(2.0, 0.95, 0.9), seg=12, rings=7)
    for s in (-1, 1):
        T.limb(f"LambLeg{s}", "Bakir", p(0.18, s * 0.06, 0.74), p(0.3, s * 0.08, 0.7), r1=0.04, r2=0.025, verts=6)
    T.sphere("Apple", "Kirmizi", p(-0.28, 0, 0.83), r=0.04, seg=6, rings=4)
    T.cyl("Platter", "Altin", p(0.0, 0.18, 0.455), r=0.12, depth=0.015, verts=12)
    for i, c in enumerate(("Kirmizi", "Yesil", "Bal")):
        T.cyl(f"Spice{i}", "Krem", p(-0.3 + i * 0.12, -0.18, 0.47), r=0.035, r2=0.025, depth=0.04, verts=8)
        T.cyl(f"SpiceIn{i}", c, p(-0.3 + i * 0.12, -0.18, 0.49), r=0.028, depth=0.008, verts=8)
    for i in range(3):
        T.sphere(f"Smoke{i}", "Tas", p(0.05 * i - 0.05, 0.05, 1.05 + i * 0.16), r=0.07 + i * 0.02, seg=6, rings=4)
    T.box("Sign", TRIM[tier] if tier > 1 else "KoyuMese", p(0, -0.26, 0.3), (0.4, 0.03, 0.12), bevel=0.01)
    tier_extras(p, tier, 1.0, front_y=-0.27)


def export(name, sub, build):
    T.reset_scene()
    build()
    obj = T.join_all(name, smooth_angle=35)
    print(f"[{name}] üçgen: {T.tri_count(obj)}")
    T.save_blend(os.path.join(MODELS, sub, f"{name}.blend"))
    T.export_fbx(obj, os.path.join(FBX, f"{name}.fbx"))


DRINKS = [("KralSerbeti", d_kral_serbeti), ("SolenTabagi", d_solen)]
STATIONS = [("Serbet", s_serbet), ("Solen", s_solen)]

if __name__ == "__main__":
    for key, fn in DRINKS:
        export(f"PR_Drink_{key}", "Drink", lambda fn=fn: fn(0, 0))
    for key, fn in STATIONS:
        for t in (1, 2, 3):
            export(f"PR_Station_{key}_T{t}", "Station", lambda fn=fn, t=t: fn(0, 0, t))
    for key, fn in DRINKS:
        RP.shoot(lambda fn=fn: fn(0, 0), key, False)

    T.reset_scene()
    for r, (key, fn) in enumerate(STATIONS):
        for t in (1, 2, 3):
            fn((t - 2) * 1.3 - 1.0, (0.7 - r) * 1.4, t)
    for i, (key, fn) in enumerate(DRINKS):
        fn(1.8 + i * 0.5, 0.0)
    T.join_all("Sections3Sheet")
    T.setup_render_stage(floor_size=40, floor_color="KoyuTas")
    T.render_from(os.path.join(RENDERS, "Props_Sections3.png"), target=(0.0, 0.0, 0.6), pitch=28, yaw=0, dist=7.5, fov=46, res=(1800, 1000))
    print("BİTTİ")
