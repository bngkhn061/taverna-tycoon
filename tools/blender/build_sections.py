"""
v1.0 yeni bölümler (onay 2026-11-10): Şarap Mahzeni (bahçenin arkasında taş kiler, Sv17) ve Çatı Terası (üst katta açık teras, Sv20).
İçecekler:   PR_Drink_YillanmisSarap · PR_Drink_BalLikoru · PR_Drink_GunBatimiPuncu
Makineler:   PR_Station_Yillanmis_T1..3 (dinlendirme fıçıları) · PR_Station_Likor_T1..3 (bakır imbik) · PR_Station_Punc_T1..3 (punç barı)
Mahzen:      PR_Decor_SarapRafi (duvar şişe rafı) · PR_Building_TasKemerTavan (tavan kemeri, 1 m)
Teras:       PR_Building_Korkuluk (1 m) · PR_Decor_Semsiye · PR_Decor_FenerDizisi · PR_Decor_CicekKutusu
İkonlar:     ICO_YillanmisSarap / BalLikoru / GunBatimiPuncu · önizleme assets/renders/Props_Sections.png
Çalıştırma: blender -b --factory-startup --python tools/blender/build_sections.py
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


# ------------------------------------------------------------------ içecekler (tepsi / masa ölçeği)
def d_yillanmis(ox, oy):
    p = at(ox, oy)
    T.cyl("Bottle", "Bordo", p(-0.04, 0, 0.09), r=0.035, depth=0.18, verts=10)
    T.cyl("Neck", "Bordo", p(-0.04, 0, 0.21), r=0.013, depth=0.07, verts=8)
    T.cyl("Cork", "Mese", p(-0.04, 0, 0.25), r=0.014, depth=0.02, verts=8)
    T.box("Label", "Krem", p(-0.04, -0.034, 0.1), (0.05, 0.004, 0.05))
    T.cyl("GlassStem", "Su", p(0.06, 0, 0.04), r=0.006, depth=0.08, verts=6)
    T.cyl("GlassBowl", "Bordo", p(0.06, 0, 0.11), r=0.032, r2=0.02, depth=0.06, verts=10)
    T.cyl("GlassFoot", "Su", p(0.06, 0, 0.004), r=0.025, depth=0.008, verts=10)


def d_likor(ox, oy):
    p = at(ox, oy)
    T.cyl("Glass", "Su", p(0, 0, 0.04), r=0.03, r2=0.02, depth=0.08, verts=10)
    T.cyl("Liquor", "Bal", p(0, 0, 0.05), r=0.026, r2=0.019, depth=0.05, verts=10)
    T.sphere("Honey", "BalSarabi", p(0.035, 0, 0.09), r=0.015, seg=6, rings=4)


def d_punc(ox, oy):
    p = at(ox, oy)
    T.cyl("Glass", "Su", p(0, 0, 0.06), r=0.04, r2=0.03, depth=0.12, verts=10)
    T.cyl("Punch", "EjderAtesi", p(0, 0, 0.065), r=0.036, r2=0.027, depth=0.08, verts=10)
    T.cyl("Top", "Kirmizi", p(0, 0, 0.105), r=0.036, depth=0.008, verts=10)
    T.cyl("Orange", "Bal", p(0.035, 0, 0.12), r=0.025, depth=0.008, rot=(90, 0, 0), verts=10)
    T.cyl("Straw", "Krem", p(-0.015, 0, 0.14), r=0.005, depth=0.1, rot=(15, 0, 0), verts=5)


# ------------------------------------------------------------------ makineler (3 görünüm kademesi)
def s_yillanmis(ox, oy, tier=1):
    """Dinlendirme fıçıları: iki katlı fıçı sehpası, öndeki fıçıda musluk, yanda şişe kasası."""
    p = at(ox, oy)
    T.box("Rack", "KoyuMese", p(0, 0, 0.12), (0.86, 0.5, 0.24), bevel=0.02)
    for i, (x, z, r) in enumerate(((-0.22, 0.42, 0.2), (0.22, 0.42, 0.2), (0, 0.78, 0.18))):
        T.cyl(f"Barrel{i}", "Mese", p(x, 0, z), r=r, depth=0.46, rot=(90, 0, 0), verts=12)
        for k in (-0.16, 0.16):
            T.cyl(f"Hoop{i}{k}", TRIM[tier] if tier > 1 else "KoyuTas", p(x, k, z), r=r + 0.008, depth=0.03, rot=(90, 0, 0), verts=12)
        T.cyl(f"Lid{i}", "KoyuMese", p(x, -0.235, z), r=r - 0.03, depth=0.01, rot=(90, 0, 0), verts=12)
    T.cyl("Tap", "Pirinc", p(-0.22, -0.28, 0.36), r=0.02, depth=0.08, rot=(90, 0, 0), verts=6)
    T.box("Crate", "Mese", p(0.36, -0.1, 0.33), (0.18, 0.18, 0.18), bevel=0.01)
    for i in range(2):
        T.cyl(f"Bottle{i}", "Bordo", p(0.32 + i * 0.07, -0.1, 0.48), r=0.025, depth=0.14, verts=8)
    tier_extras(p, tier, 1.0, front_y=-0.27)


def s_likor(ox, oy, tier=1):
    """Bakır imbik: kazan, kubbe, kıvrık boru, damla kabı."""
    p = at(ox, oy)
    T.cyl("Hearth", "Tas", p(0, 0, 0.1), r=0.3, depth=0.2, verts=10)
    T.sphere("Pot", "Bakir", p(0, 0.02, 0.4), r=0.26, scale=(1, 1, 0.85), seg=12, rings=7)
    T.sphere("Dome", TRIM[tier] if tier > 1 else "Bakir", p(0, 0.02, 0.66), r=0.12, seg=10, rings=6)
    T.limb("Pipe1", "Bakir", p(0, 0.02, 0.75), p(0.28, 0.02, 0.8), r1=0.03, verts=6)
    T.limb("Pipe2", "Bakir", p(0.28, 0.02, 0.8), p(0.34, -0.12, 0.4), r1=0.025, verts=6)
    T.cyl("Jar", "Su", p(0.34, -0.14, 0.32), r=0.06, depth=0.14, verts=10)
    T.cyl("JarFill", "Bal", p(0.34, -0.14, 0.29), r=0.055, depth=0.08, verts=10)
    T.box("Mouth", "KoyuTas", p(0, -0.29, 0.1), (0.18, 0.04, 0.1))
    tier_extras(p, tier, 0.95, front_y=-0.28)


def s_punc(ox, oy, tier=1):
    """Punç barı: küçük ahşap tezgâh, büyük punç kâsesi, kepçe, meyve sepeti, renkli bayrakçıklar."""
    p = at(ox, oy)
    T.box("Counter", "Mese", p(0, 0, 0.32), (0.86, 0.5, 0.64), bevel=0.02)
    T.box("Top", "KoyuMese", p(0, 0, 0.66), (0.92, 0.56, 0.05), bevel=0.01)
    T.cyl("Bowl", "Krem", p(-0.12, 0, 0.76), r=0.13, r2=0.2, depth=0.16, verts=12)
    T.cyl("Punch", "EjderAtesi", p(-0.12, 0, 0.835), r=0.185, depth=0.02, verts=12)
    for i in range(3):
        a = math.radians(i * 120)
        T.cyl(f"Slice{i}", "Bal", p(-0.12 + math.cos(a) * 0.09, math.sin(a) * 0.09, 0.865), r=0.04, depth=0.01, verts=8)
    T.limb("Ladle", "Pirinc", p(0.0, 0.06, 0.82), p(0.12, 0.1, 1.0), r1=0.01, verts=5)
    T.cyl("Basket", "Mese", p(0.28, 0.05, 0.74), r=0.11, depth=0.1, verts=8)
    for i, c in enumerate(("Bal", "Kirmizi", "Yesil")):
        T.sphere(f"Fruit{i}", c, p(0.24 + i * 0.04, 0.05 + (i % 2) * 0.03, 0.8), r=0.04, seg=6, rings=4)
    for s in (-1, 1):
        T.cyl(f"Pole{s}", "KoyuMese", p(s * 0.42, 0.22, 0.95), r=0.015, depth=0.6, verts=5)
    for i in range(5):
        T.cyl(f"Flag{i}", ("Kirmizi", "Altin", "Mavi", "Yesil", "Mor")[i], p(-0.34 + i * 0.17, 0.22, 1.18), r=0.05, r2=0.0, depth=0.08,
              rot=(180, 0, 0), verts=3)
    tier_extras(p, tier, 1.0, front_y=-0.27)


# ------------------------------------------------------------------ mahzen / teras parçaları
def sarap_rafi(ox, oy):
    """Duvara yaslı şişe rafı (1 m): petek gözler, yatık şişeler. Önü −y."""
    p = at(ox, oy)
    T.box("Back", "KoyuMese", p(0, 0.08, 0.9), (1.0, 0.06, 1.8))
    for i in range(4):
        T.box(f"Shelf{i}", "Mese", p(0, -0.06, 0.2 + i * 0.45), (1.0, 0.3, 0.04))
        for k in range(5):
            T.cyl(f"Bottle{i}{k}", "Bordo" if (i + k) % 3 else "Yesil", p(-0.38 + k * 0.19, -0.08, 0.28 + i * 0.45), r=0.04, depth=0.26,
                  rot=(90, 0, 0), verts=8)
    for s in (-1, 1):
        T.box(f"Side{s}", "KoyuMese", p(s * 0.5, -0.06, 0.9), (0.05, 0.32, 1.8))


def tas_kemer_tavan(ox, oy):
    """Mahzen tavan kemeri (1 m derinlik, 4 m açıklık): duvarlar arası taş yay, tepesinde fener askısı."""
    p = at(ox, oy)
    for i in range(9):
        a = math.radians(180 - i * 22.5)
        x, z = math.cos(a) * 2.0, 1.9 + math.sin(a) * 0.7
        T.box(f"Arch{i}", "Tas" if i % 2 else "KoyuTas", p(x, 0, z), (0.5, 0.3, 0.22), rot=(0, -math.degrees(a) + 90, 0), bevel=0.015)


def korkuluk(ox, oy):
    """Teras korkuluğu (1 m): ahşap küpeşte, dört dikme."""
    p = at(ox, oy)
    T.box("Rail", "Mese", p(0, 0, 0.9), (1.0, 0.08, 0.06), bevel=0.01)
    T.box("Bottom", "KoyuMese", p(0, 0, 0.12), (1.0, 0.06, 0.05))
    for i in range(4):
        T.box(f"Post{i}", "KoyuMese", p(-0.375 + i * 0.25, 0, 0.5), (0.05, 0.05, 0.8))


def semsiye(ox, oy):
    """Teras şemsiyesi: direk + çizgili kubbe."""
    p = at(ox, oy)
    T.cyl("Pole", "Mese", p(0, 0, 1.1), r=0.03, depth=2.2, verts=6)
    for i in range(8):
        a = i * 45
        T.cyl(f"Panel{i}", "Kirmizi" if i % 2 else "Krem", p(0, 0, 2.15), r=0.9, r2=0.0, depth=0.35, verts=3,
              rot=(0, 0, a), scale=(1, 0.42, 1))
    T.sphere("Finial", "Altin", p(0, 0, 2.38), r=0.04, seg=6, rings=4)


def fener_dizisi(ox, oy):
    """İp üzerinde renkli fenerler (4 m, iki direk arasında)."""
    p = at(ox, oy)
    for s in (-1, 1):
        T.cyl(f"Post{s}", "KoyuMese", p(s * 2.0, 0, 1.2), r=0.04, depth=2.4, verts=6)
    for i in range(9):
        x = -2.0 + i * 0.5
        sag = 0.25 * (1 - (x / 2.0) ** 2)
        T.cyl(f"Rope{i}", "KoyuMese", p(x + 0.25, 0, 2.3 - sag - 0.02), r=0.008, depth=0.5, rot=(0, 90, 0), verts=4) if i < 8 else None
        if 0 < i < 8:
            T.sphere(f"Lamp{i}", ("Altin", "Kirmizi", "Ates", "Mavi")[i % 4], p(x, 0, 2.18 - sag), r=0.07, scale=(1, 1, 1.3), seg=6, rings=4)


def cicek_kutusu(ox, oy):
    """Teras çiçek kutusu (1 × 0.3 m)."""
    p = at(ox, oy)
    T.box("Box", "Mese", p(0, 0, 0.15), (1.0, 0.3, 0.3), bevel=0.02)
    T.box("Soil", "KoyuMese", p(0, 0, 0.29), (0.94, 0.24, 0.03))
    for i in range(6):
        T.sphere(f"Leaf{i}", "Yaprak", p(-0.4 + i * 0.16, 0, 0.36), r=0.08, seg=6, rings=4)
        T.sphere(f"Flower{i}", ("Kirmizi", "Altin", "Mor", "Krem")[i % 4], p(-0.4 + i * 0.16, -0.04, 0.44), r=0.045, seg=6, rings=3)


def export(name, sub, build):
    T.reset_scene()
    build()
    obj = T.join_all(name, smooth_angle=35)
    print(f"[{name}] üçgen: {T.tri_count(obj)}")
    T.save_blend(os.path.join(MODELS, sub, f"{name}.blend"))
    T.export_fbx(obj, os.path.join(FBX, f"{name}.fbx"))


DRINKS = [("YillanmisSarap", d_yillanmis), ("BalLikoru", d_likor), ("GunBatimiPuncu", d_punc)]
STATIONS = [("Yillanmis", s_yillanmis), ("Likor", s_likor), ("Punc", s_punc)]
PARTS = [("PR_Decor_SarapRafi", sarap_rafi), ("PR_Building_TasKemerTavan", tas_kemer_tavan), ("PR_Building_Korkuluk", korkuluk),
         ("PR_Decor_Semsiye", semsiye), ("PR_Decor_FenerDizisi", fener_dizisi), ("PR_Decor_CicekKutusu", cicek_kutusu)]

if __name__ == "__main__":
    for key, fn in DRINKS:
        export(f"PR_Drink_{key}", "Drink", lambda fn=fn: fn(0, 0))
    for key, fn in STATIONS:
        for t in (1, 2, 3):
            export(f"PR_Station_{key}_T{t}", "Station", lambda fn=fn, t=t: fn(0, 0, t))
    for name, fn in PARTS:
        export(name, "Sections", lambda fn=fn: fn(0, 0))
    for key, fn in DRINKS:
        RP.shoot(lambda fn=fn: fn(0, 0), key, False)

    T.reset_scene()
    for r, (key, fn) in enumerate(STATIONS):
        for t in (1, 2, 3):
            fn((t - 2) * 1.2 - 3.0, (1.0 - r) * 1.3, t)
    for i, (key, fn) in enumerate(DRINKS):
        fn(-0.8 + i * 0.4, 1.6)
    sarap_rafi(0.6, 1.2)
    korkuluk(2.0, -1.0)
    cicek_kutusu(2.0, -1.5)
    semsiye(3.4, 0.4)
    fener_dizisi(2.0, 1.4)
    T.join_all("SectionsSheet")
    T.setup_render_stage(floor_size=40, floor_color="KoyuTas")
    T.render_from(os.path.join(RENDERS, "Props_Sections.png"), target=(0.0, 0.2, 0.7), pitch=28, yaw=0, dist=10.0, fov=46, res=(2000, 1100))
    print("BİTTİ")
