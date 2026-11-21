"""
v2.0 Paket 2 (oyuncu testi 2026-11-17, maddeler 14, 19, 21) varlıkları:
  Avizeler (madde 14): PR_Decor_Avize (build_props.x_avize) ve PR_Decor_KristalAvize (build_premium.kristal_avize)
    küçültülüp yükseltildi; burada yeniden dışa aktarılır. Kademe avizelerinin yerine sütun aplikleri:
    PR_Tier_DemirAplik (Taş Han) · PR_Tier_AltinAplik (Saray Hanı) — sütunun önüne asılır, önü −y, ışık ≈ 1.75 m.
  Bahçe (madde 21): PR_Decor_BahceFeneri (direkli bahçe feneri) · PR_Decor_TasYol (1 × 2 m taş patika)
  Han konforu (madde 19): PR_Inn_Kilim · PR_Inn_Lamba · PR_Inn_Dolap · PR_Inn_Somine · PR_Inn_Tablo
  İkonlar: ICO_DEC_Avize, ICO_DEC_KristalAvize, ICO_DEC_BahceFeneri, ICO_DEC_TasYol · önizleme assets/renders/Props_V20.png
Çalıştırma: blender -b --factory-startup --python tools/blender/build_v20.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import toylib as T  # noqa: E402
import build_props as P  # noqa: E402
import build_premium as PR  # noqa: E402
import render_portraits as RP  # noqa: E402

at = P.at
MODELS = os.path.join(T.ROOT, "assets", "models")
FBX = os.path.join(T.ROOT, "assets", "export", "fbx", "props")
RENDERS = os.path.join(T.ROOT, "assets", "renders")


# ------------------------------------------------------------------ kademe aplikleri (sütun önü)
def demir_aplik(ox, oy):
    """Taş Han aplik: demir plaka, kıvrık kol, kafesli fener (mum)."""
    p = at(ox, oy)
    T.box("Plate", "KoyuTas", p(0, 0.02, 1.72), (0.14, 0.03, 0.26), bevel=0.01)
    T.limb("Arm", "KoyuTas", p(0, 0.01, 1.66), p(0, -0.17, 1.74), r1=0.012, verts=5)
    T.cyl("Base", "KoyuTas", p(0, -0.19, 1.67), r=0.06, depth=0.025, verts=8)
    T.cyl("Candle", "Krem", p(0, -0.19, 1.72), r=0.022, depth=0.08, verts=6)
    T.sphere("Flame", "Ates", p(0, -0.19, 1.78), r=0.018, scale=(1, 1, 1.6), seg=5, rings=3)
    for i in range(4):
        a = math.radians(45 + i * 90)
        T.box(f"Bar{i}", "KoyuTas", p(math.cos(a) * 0.055, -0.19 + math.sin(a) * 0.055, 1.75), (0.012, 0.012, 0.15))
    T.cyl("Roof", "KoyuTas", p(0, -0.19, 1.85), r=0.075, r2=0.0, depth=0.07, verts=4)
    T.sphere("Ring", "KoyuTas", p(0, -0.19, 1.9), r=0.014, seg=5, rings=3)


def altin_aplik(ox, oy):
    """Saray Hanı aplik: altın plaka, iki kollu şamdan, cam fanuslar, kırmızı mücevher."""
    p = at(ox, oy)
    T.box("Plate", "Altin", p(0, 0.02, 1.72), (0.16, 0.03, 0.32), bevel=0.012)
    T.sphere("Gem", "Kirmizi", p(0, -0.005, 1.78), r=0.025, scale=(1, 0.6, 1), seg=6, rings=4)
    for s in (-1, 1):
        T.limb(f"Arm{s}", "Altin", p(0, 0.0, 1.66), p(s * 0.14, -0.15, 1.68), r1=0.01, verts=5)
        T.cyl(f"Cup{s}", "Altin", p(s * 0.14, -0.15, 1.69), r=0.035, depth=0.02, verts=8)
        T.cyl(f"Candle{s}", "Krem", p(s * 0.14, -0.15, 1.74), r=0.018, depth=0.08, verts=6)
        T.sphere(f"Flame{s}", "Ates", p(s * 0.14, -0.15, 1.8), r=0.016, scale=(1, 1, 1.6), seg=5, rings=3)
        T.cyl(f"Shade{s}", "Krem", p(s * 0.14, -0.15, 1.78), r=0.04, r2=0.03, depth=0.12, verts=8)
    T.sphere("Drop", "Altin", p(0, 0.0, 1.55), r=0.025, scale=(1, 1, 1.5), seg=6, rings=4)


# ------------------------------------------------------------------ bahçe
def bahce_feneri(ox, oy):
    """Bahçe feneri: taş kaide, demir direk, cam fener (mum), üstte çatı ve halka. Işık ≈ 1.45 m."""
    p = at(ox, oy)
    T.box("Base", "KoyuTas", p(0, 0, 0.08), (0.26, 0.26, 0.16), bevel=0.02)
    T.box("BaseTop", "Tas", p(0, 0, 0.17), (0.2, 0.2, 0.03), bevel=0.01)
    T.cyl("Post", "KoyuTas", p(0, 0, 0.75), r=0.035, depth=1.15, verts=8)
    T.torus("Collar", "KoyuTas", p(0, 0, 1.3), major=0.045, minor=0.012, seg=8)
    T.box("Floor", "KoyuTas", p(0, 0, 1.34), (0.2, 0.2, 0.025))
    for sx in (-1, 1):
        for sy in (-1, 1):
            T.box(f"Corner{sx}{sy}", "KoyuTas", p(sx * 0.09, sy * 0.09, 1.46), (0.02, 0.02, 0.24))
    T.box("Glass", "Bal", p(0, 0, 1.46), (0.16, 0.16, 0.22))
    T.cyl("Candle", "Krem", p(0, 0, 1.42), r=0.025, depth=0.1, verts=6)
    T.sphere("Flame", "Ates", p(0, 0, 1.5), r=0.02, scale=(1, 1, 1.6), seg=5, rings=3)
    T.cyl("Roof", "KoyuTas", p(0, 0, 1.63), r=0.17, r2=0.02, depth=0.12, verts=4, rot=(0, 0, 45))
    T.torus("Hook", "KoyuTas", p(0, 0, 1.72), major=0.03, minor=0.008, rot=(90, 0, 0), seg=8)


def tas_yol(ox, oy):
    """Taş patika (1 × 2 m, boyu y yönünde): çimde düzensiz yassı taşlar, kenarda çakıl."""
    p = at(ox, oy)
    stones = ((-0.22, -0.8, 0.34, 0.28, 8), (0.2, -0.68, 0.3, 0.26, -12), (-0.05, -0.35, 0.4, 0.3, 4),
              (-0.28, 0.0, 0.3, 0.32, -6), (0.18, 0.05, 0.36, 0.28, 10), (-0.02, 0.4, 0.42, 0.3, -3),
              (-0.25, 0.78, 0.3, 0.26, 14), (0.2, 0.75, 0.32, 0.3, -9))
    for i, (x, y, w, d, r) in enumerate(stones):
        T.box(f"Stone{i}", "Tas" if i % 3 else "KoyuTas", p(x, y, 0.012), (w, d, 0.024), rot=(0, 0, r), bevel=0.01)
    for i, (x, y) in enumerate(((0.4, -0.3), (-0.42, 0.45), (0.38, 0.45), (-0.4, -0.45), (0.05, -0.95), (0.0, 0.95))):
        T.sphere(f"Pebble{i}", "Siva", p(x, y, 0.01), r=0.035, scale=(1, 0.8, 0.4), seg=5, rings=3)


# ------------------------------------------------------------------ han konforu (oda eşyaları)
def kilim(ox, oy):
    """Konfor 2: yatak ayakucunda kilim (1.1 × 0.7 m), desenli."""
    p = at(ox, oy)
    T.box("Rug", "Kirmizi", p(0, 0, 0.006), (1.1, 0.7, 0.012))
    T.box("Field", "Bal", p(0, 0, 0.009), (0.86, 0.48, 0.012))
    for i in range(3):
        T.box(f"Motif{i}", "Lacivert", p(-0.28 + i * 0.28, 0, 0.012), (0.12, 0.12, 0.012), rot=(0, 0, 45))
    for s in (-1, 1):
        for k in range(5):
            T.box(f"Fringe{s}{k}", "Krem", p(s * 0.58, -0.28 + k * 0.14, 0.006), (0.06, 0.03, 0.008))


def lamba(ox, oy):
    """Konfor 3: ayaklı yağ lambası (1.15 m), kumaş abajur."""
    p = at(ox, oy)
    T.cyl("Foot", "KoyuMese", p(0, 0, 0.03), r=0.14, depth=0.06, verts=10)
    T.cyl("Pole", "Pirinc", p(0, 0, 0.55), r=0.02, depth=1.0, verts=6)
    T.cyl("Oil", "Pirinc", p(0, 0, 1.05), r=0.05, depth=0.06, verts=8)
    T.sphere("Flame", "Ates", p(0, 0, 1.11), r=0.02, scale=(1, 1, 1.6), seg=5, rings=3)
    T.cyl("Shade", "Krem", p(0, 0, 1.12), r=0.15, r2=0.08, depth=0.16, verts=10)
    T.torus("ShadeTrim", "Kirmizi", p(0, 0, 1.045), major=0.15, minor=0.01, seg=10)


def dolap(ox, oy):
    """Konfor 3: dar elbise dolabı (0.38 × 0.45 × 1.25 m), önü −y."""
    p = at(ox, oy)
    T.box("Body", "Mese", p(0, 0, 0.66), (0.38, 0.45, 1.2), bevel=0.02)
    T.box("Top", "KoyuMese", p(0, 0, 1.28), (0.44, 0.5, 0.06), bevel=0.01)
    T.box("Plinth", "KoyuMese", p(0, 0, 0.04), (0.4, 0.47, 0.08))
    for s in (-1, 1):
        T.box(f"Door{s}", "KoyuMese", p(s * 0.09, -0.228, 0.68), (0.16, 0.01, 1.0))
        T.sphere(f"Knob{s}", "Pirinc", p(s * 0.025, -0.24, 0.7), r=0.014, seg=5, rings=3)


def somine(ox, oy):
    """Konfor 4: küçük taş ocak (0.5 × 0.36 m), önü −y, ateşli; arkada kısa baca."""
    p = at(ox, oy)
    T.box("Hearth", "KoyuTas", p(0, 0, 0.03), (0.56, 0.42, 0.06), bevel=0.01)
    for s in (-1, 1):
        T.box(f"Jamb{s}", "Tas", p(s * 0.2, 0.02, 0.3), (0.1, 0.32, 0.5), bevel=0.015)
    T.box("Back", "KoyuTas", p(0, 0.15, 0.3), (0.3, 0.06, 0.5))
    T.box("Mantel", "Mese", p(0, 0.0, 0.58), (0.56, 0.38, 0.06), bevel=0.01)
    T.box("Chimney", "Tas", p(0, 0.08, 0.86), (0.26, 0.2, 0.5), bevel=0.015)
    for i, (x, r) in enumerate(((-0.05, 20), (0.06, -25))):
        T.box(f"Log{i}", "KoyuMese", p(x, -0.02, 0.1), (0.04, 0.24, 0.04), rot=(0, 0, r))
    T.cyl("Fire", "Ates", p(0, -0.02, 0.2), r=0.07, r2=0.0, depth=0.18, verts=6)
    T.cyl("FireIn", "Altin", p(0, -0.04, 0.18), r=0.04, r2=0.0, depth=0.12, verts=5)
    T.box("Candle", "Krem", p(0.18, 0.0, 0.65), (0.03, 0.03, 0.08))


def tablo(ox, oy):
    """Konfor 4: duvar tablosu (0.44 × 0.34 m) — tepe ve gün batımı; merkez 1.05 m, duvara yaslı (önü −y)."""
    p = at(ox, oy)
    T.box("Frame", "Altin", p(0, 0, 1.05), (0.44, 0.03, 0.34), bevel=0.01)
    T.box("Sky", "Bal", p(0, -0.017, 1.1), (0.36, 0.01, 0.16))
    T.box("Hill", "Yesil", p(0, -0.018, 0.98), (0.36, 0.01, 0.1))
    T.sphere("Sun", "Ates", p(0.08, -0.02, 1.1), r=0.035, scale=(1, 0.3, 1), seg=6, rings=4)


def export(name, sub, build):
    T.reset_scene()
    build()
    obj = T.join_all(name, smooth_angle=35)
    print(f"[{name}] üçgen: {T.tri_count(obj)}")
    T.save_blend(os.path.join(MODELS, sub, f"{name}.blend"))
    T.export_fbx(obj, os.path.join(FBX, f"{name}.fbx"))


ITEMS = [
    ("PR_Decor_Avize", "Decor", P.x_avize), ("PR_Decor_KristalAvize", "Decor", PR.kristal_avize),
    ("PR_Tier_DemirAplik", "Tiers", demir_aplik), ("PR_Tier_AltinAplik", "Tiers", altin_aplik),
    ("PR_Decor_BahceFeneri", "Decor", bahce_feneri), ("PR_Decor_TasYol", "Decor", tas_yol),
    ("PR_Inn_Kilim", "Furniture", kilim), ("PR_Inn_Lamba", "Furniture", lamba), ("PR_Inn_Dolap", "Furniture", dolap),
    ("PR_Inn_Somine", "Furniture", somine), ("PR_Inn_Tablo", "Furniture", tablo),
]

if __name__ == "__main__":
    for name, sub, fn in ITEMS:
        export(name, sub, lambda fn=fn: fn(0, 0))
    RP.shoot(lambda: P.x_avize(0, 0), "DEC_Avize", False)
    RP.shoot(lambda: PR.kristal_avize(0, 0), "DEC_KristalAvize", False)
    RP.shoot(lambda: bahce_feneri(0, 0), "DEC_BahceFeneri", False)
    RP.shoot(lambda: tas_yol(0, 0), "DEC_TasYol", False)

    T.reset_scene()
    for i, (_, _, fn) in enumerate(ITEMS):
        fn(-4.5 + i * 0.9, 0)
    T.join_all("V20Sheet")
    T.setup_render_stage(floor_size=40, floor_color="KoyuTas")
    T.render_from(os.path.join(RENDERS, "Props_V20.png"), target=(0.0, 0, 0.9), pitch=22, yaw=0, dist=11.5, fov=46, res=(2000, 1000))
    print("BİTTİ")
