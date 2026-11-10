"""
v0.9 "Büyüyen Taverna": oyun salonu masaları, taverna tabelası, çatı kiremidi ve çevre dekoru
(oyuncu testi 2026-11-10: "hiçbir yerde boşluk gözükmesin, dağ taş çiçek bahçe olsun").
Stil: DESIGN_RULES.md "Oyuncak Taverna" (palet atlası, alçak poligon, yuvarlatılmış kutular).
Çıktılar:
  PR_Furniture_ZarMasasi / KartMasasi / BilekMasasi · PR_Decor_Zar · PR_Decor_Tabela
  PR_Nature_Agac / Cam / Kaya1 / Kaya2 / Cali / CicekTarhi / Tepe / Dag / Kuyu / Araba / Cati / OtTutami
  assets/icons/ICO_ZarMasasi.png · assets/renders/Props_World.png
Çalıştırma: blender -b --factory-startup --python tools/blender/build_world.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import toylib as T  # noqa: E402
import build_props as P  # noqa: E402
import render_portraits as RP  # noqa: E402

at = P.at
MODELS = os.path.join(T.ROOT, "assets", "models")
FBX = os.path.join(T.ROOT, "assets", "export", "fbx", "props")
RENDERS = os.path.join(T.ROOT, "assets", "renders")


# ------------------------------------------------------------------ oyun salonu
def zar_masasi(ox, oy):
    """Zar masası: yuvarlak, keçe kaplı yüzey, kenar pervazı, üstünde zar kupası ve 2 zar."""
    p = at(ox, oy)
    T.cyl("Leg", "KoyuMese", p(0, 0, 0.32), r=0.09, depth=0.64, verts=8)
    T.cyl("Foot", "KoyuMese", p(0, 0, 0.04), r=0.32, depth=0.08, verts=10)
    T.cyl("Top", "Mese", p(0, 0, 0.66), r=0.62, depth=0.06, verts=14)
    T.cyl("Felt", "Yesil", p(0, 0, 0.695), r=0.55, depth=0.02, verts=14)
    T.torus("Rim", "KoyuMese", p(0, 0, 0.71), major=0.58, minor=0.035, seg=16)
    T.cyl("Cup", "Bordo", p(0.28, 0.12, 0.79), r=0.07, r2=0.09, depth=0.18, verts=10)
    for i, (x, y, r) in enumerate(((-0.12, -0.1, 18), (0.05, -0.2, -25))):
        T.box(f"Die{i}", "Krem", p(x, y, 0.745), (0.08, 0.08, 0.08), rot=(0, 0, r), bevel=0.012)
    T.box("Coins", "Altin", p(-0.3, 0.2, 0.72), (0.12, 0.12, 0.03), bevel=0.01)


def kart_masasi(ox, oy):
    """Kart masası: kare, kırmızı örtü, dağıtılmış kartlar, mum."""
    p = at(ox, oy)
    for sx in (-1, 1):
        for sy in (-1, 1):
            T.box(f"Leg{sx}{sy}", "KoyuMese", p(sx * 0.42, sy * 0.42, 0.32), (0.08, 0.08, 0.64))
    T.box("Top", "Mese", p(0, 0, 0.67), (1.05, 1.05, 0.06), bevel=0.015)
    T.box("Cloth", "Kirmizi", p(0, 0, 0.705), (0.9, 0.9, 0.012))
    for i in range(5):
        a = -40 + i * 20
        T.box(f"Card{i}", "Krem", p(-0.15 + i * 0.07, -0.18 + abs(i - 2) * 0.03, 0.716 + i * 0.002), (0.1, 0.15, 0.006), rot=(0, 0, a))
    T.box("Deck", "Lacivert", p(0.25, 0.22, 0.73), (0.11, 0.16, 0.05), bevel=0.006)
    T.cyl("Candle", "Krem", p(-0.3, 0.3, 0.77), r=0.03, depth=0.12, verts=8)
    T.sphere("Flame", "Ates", p(-0.3, 0.3, 0.86), r=0.025, scale=(1, 1, 1.6), seg=6, rings=4)


def bilek_masasi(ox, oy):
    """Bilek güreşi masası: dar, sağlam, iki tutamak ve dirsek minderleri."""
    p = at(ox, oy)
    for sx in (-1, 1):
        T.box(f"Leg{sx}", "KoyuMese", p(sx * 0.3, 0, 0.34), (0.16, 0.5, 0.68), bevel=0.02)
    T.box("Top", "Mese", p(0, 0, 0.72), (0.9, 0.62, 0.08), bevel=0.02)
    for sx in (-1, 1):
        T.cyl(f"Pad{sx}", "Bordo", p(sx * 0.2, 0, 0.775), r=0.09, depth=0.03, verts=10)
        T.cyl(f"Peg{sx}", "Pirinc", p(sx * 0.36, 0.18 * sx, 0.84), r=0.025, depth=0.16, verts=8)
    T.box("Line", "Krem", p(0, 0, 0.762), (0.02, 0.55, 0.005))
    T.box("Coins", "Altin", p(0, 0.24, 0.775), (0.12, 0.08, 0.03), bevel=0.01)


def zar(ox, oy):
    """Tek zar (oyun turunda zıplar)."""
    p = at(ox, oy)
    T.box("Die", "Krem", p(0, 0, 0.05), (0.1, 0.1, 0.1), bevel=0.015)
    for i, (x, y, z) in enumerate(((0, -0.051, 0.05), (0.051, 0.02, 0.07), (0.051, -0.02, 0.03), (0, 0, 0.101))):
        T.sphere(f"Pip{i}", "Goz", p(x, y, z), r=0.012, seg=5, rings=3)


def tabela(ox, oy):
    """Taverna tabelası: direk + kol, iki zincirle asılı boş tahta pano (adı HUD üstüne yazar). Önü −y."""
    p = at(ox, oy)
    T.box("Post", "KoyuMese", p(-0.95, 0, 1.4), (0.14, 0.14, 2.8), bevel=0.02)
    T.box("Foot", "KoyuTas", p(-0.95, 0, 0.08), (0.4, 0.4, 0.16), bevel=0.02)
    T.box("Arm", "KoyuMese", p(-0.2, 0, 2.62), (1.6, 0.1, 0.1), bevel=0.015)
    T.box("Brace", "KoyuMese", p(-0.72, 0, 2.38), (0.5, 0.08, 0.08), rot=(0, 45, 0))
    for sx in (-1, 1):
        T.cyl(f"Chain{sx}", "KoyuTas", p(sx * 0.55, 0, 2.43), r=0.015, depth=0.3, verts=6)
    T.box("Board", "Mese", p(0, 0, 1.98), (1.4, 0.07, 0.62), bevel=0.03)
    T.box("Frame", "KoyuMese", p(0, 0.01, 1.98), (1.5, 0.05, 0.72), bevel=0.02)
    T.box("Mug", "Bal", p(0.82, -0.05, 2.45), (0.14, 0.1, 0.18), bevel=0.02)


# ------------------------------------------------------------------ doğa
def agac(ox, oy, s=1.0):
    """Yapraklı ağaç: gövde + üç yuvarlak taç."""
    p = at(ox, oy)
    T.cyl("Trunk", "KoyuMese", p(0, 0, 0.9 * s), r=0.16 * s, r2=0.1 * s, depth=1.8 * s, verts=7)
    for i, (x, y, z, r) in enumerate(((0, 0, 2.3, 0.95), (0.55, 0.2, 1.95, 0.7), (-0.5, -0.2, 2.0, 0.72), (0.1, -0.35, 2.85, 0.6))):
        T.sphere(f"Crown{i}", "Yaprak" if i % 2 == 0 else "Yesil", p(x * s, y * s, z * s), r=r * s, seg=7, rings=5)


def cam(ox, oy, s=1.0):
    """Çam: gövde + üç konik kat."""
    p = at(ox, oy)
    T.cyl("Trunk", "KoyuMese", p(0, 0, 0.35 * s), r=0.13 * s, depth=0.7 * s, verts=6)
    for i in range(3):
        T.cyl(f"Cone{i}", "Yesil", p(0, 0, (0.95 + i * 0.75) * s), r=(1.0 - i * 0.25) * s, r2=0.05 * s, depth=1.3 * s, verts=8)


def kaya1(ox, oy):
    p = at(ox, oy)
    T.sphere("Rock", "Tas", p(0, 0, 0.18), r=0.45, scale=(1.3, 1.0, 0.7), seg=6, rings=4)
    T.sphere("Rock2", "KoyuTas", p(0.45, 0.2, 0.1), r=0.25, scale=(1.2, 1, 0.7), seg=5, rings=3)


def kaya2(ox, oy):
    p = at(ox, oy)
    T.sphere("Big", "KoyuTas", p(0, 0, 0.45), r=0.8, scale=(1.1, 0.9, 0.85), rot=(0, 0, 20), seg=6, rings=4)
    T.sphere("Small", "Tas", p(-0.75, -0.3, 0.15), r=0.3, seg=5, rings=3)


def cali(ox, oy):
    p = at(ox, oy)
    for i, (x, y, r) in enumerate(((0, 0, 0.42), (0.38, 0.1, 0.32), (-0.35, 0.05, 0.3))):
        T.sphere(f"Bush{i}", "Yesil" if i else "Yaprak", p(x, y, r * 0.8), r=r, seg=6, rings=4)
    for i, (x, y) in enumerate(((0.1, -0.36), (-0.25, -0.25), (0.35, -0.2))):
        T.sphere(f"Berry{i}", "Kirmizi", p(x, y, 0.42), r=0.045, seg=5, rings=3)


def cicek_tarhi(ox, oy):
    """Çiçek tarhı: taş kenarlı toprak, renkli çiçekler (2 × 1 m)."""
    p = at(ox, oy)
    T.box("Soil", "KoyuMese", p(0, 0, 0.06), (1.9, 0.9, 0.12))
    for i in range(10):
        x = -0.95 + i * 0.211
        T.box(f"EdgeF{i}", "Tas", p(x + 0.1, -0.48, 0.09), (0.2, 0.1, 0.18), bevel=0.02)
        T.box(f"EdgeB{i}", "Tas", p(x + 0.1, 0.48, 0.09), (0.2, 0.1, 0.18), bevel=0.02)
    colors = ["Kirmizi", "Altin", "Mor", "Krem", "Mavi"]
    for i in range(14):
        x = -0.8 + (i % 7) * 0.27
        y = -0.2 + (i // 7) * 0.38
        T.cyl(f"Stem{i}", "Yesil", p(x, y, 0.22), r=0.015, depth=0.2, verts=4)
        T.sphere(f"Flower{i}", colors[i % len(colors)], p(x, y, 0.34), r=0.08, scale=(1, 1, 0.6), seg=6, rings=3)


def ot_tutami(ox, oy):
    p = at(ox, oy)
    for i in range(5):
        a = i * 72
        T.cyl(f"Blade{i}", "Yaprak" if i % 2 else "Yesil", p(0.06 * math.cos(math.radians(a)), 0.06 * math.sin(math.radians(a)), 0.12),
              r=0.03, r2=0.004, depth=0.26, rot=(12 * math.cos(math.radians(a)), 12 * math.sin(math.radians(a)), 0), verts=4)
    T.sphere("Flower", "Altin", p(0.05, 0, 0.27), r=0.035, seg=5, rings=3)


def tepe(ox, oy):
    """Yeşil tepe (yaklaşık 10 m çap): basık küre + üstünde birkaç ağaç kümesi."""
    p = at(ox, oy)
    T.sphere("Hill", "Yesil", p(0, 0, -0.6), r=5.0, scale=(1.2, 0.9, 0.55), seg=10, rings=6)
    T.sphere("Hill2", "Yaprak", p(3.2, 1.0, -0.5), r=3.0, scale=(1.1, 1, 0.6), seg=8, rings=5)


def dag(ox, oy):
    """Uzak dağ (yaklaşık 16 m): gri kaya koni, karlı tepe."""
    p = at(ox, oy)
    T.cyl("Mountain", "KoyuTas", p(0, 0, 4.0), r=8.0, r2=1.2, depth=8.0, verts=7)
    T.cyl("Mountain2", "Tas", p(5.5, 1.5, 2.8), r=5.5, r2=0.9, depth=5.6, verts=6)
    T.cyl("Snow", "Krem", p(0, 0, 7.6), r=1.9, r2=0.4, depth=1.4, verts=7)


def kuyu(ox, oy):
    """Taş kuyu: halka duvar, iki direk, çatı, kova."""
    p = at(ox, oy)
    T.cyl("Wall", "Tas", p(0, 0, 0.35), r=0.6, depth=0.7, verts=12)
    T.cyl("Water", "Su", p(0, 0, 0.62), r=0.48, depth=0.04, verts=12)
    for sx in (-1, 1):
        T.box(f"Post{sx}", "KoyuMese", p(sx * 0.55, 0, 1.0), (0.1, 0.1, 1.3))
    for sx in (-1, 1):
        T.box(f"Roof{sx}", "Kirmizi", p(sx * 0.32, 0, 1.78), (0.75, 0.9, 0.06), rot=(0, sx * 35, 0))
    T.cyl("Axle", "Mese", p(0, 0, 1.3), r=0.05, depth=1.1, rot=(0, 90, 0), verts=6)
    T.cyl("Bucket", "Mese", p(0.15, 0, 0.95), r=0.12, r2=0.1, depth=0.2, verts=8)


def araba(ox, oy):
    """El arabası / at arabası: kasa, iki tekerlek, oklar, üstünde fıçı ve çuval."""
    p = at(ox, oy)
    T.box("Bed", "Mese", p(0, 0, 0.7), (1.6, 1.0, 0.1), bevel=0.02)
    for sy in (-1, 1):
        T.box(f"Side{sy}", "KoyuMese", p(0, sy * 0.48, 0.88), (1.6, 0.06, 0.3))
        T.cyl(f"Wheel{sy}", "KoyuMese", p(0, sy * 0.6, 0.45), r=0.45, depth=0.08, rot=(90, 0, 0), verts=12)
        T.cyl(f"Hub{sy}", "Pirinc", p(0, sy * 0.66, 0.45), r=0.08, depth=0.06, rot=(90, 0, 0), verts=8)
        T.box(f"Shaft{sy}", "KoyuMese", p(-1.3, sy * 0.35, 0.62), (1.1, 0.07, 0.07), rot=(0, -8, 0))
    T.cyl("Barrel", "Mese", p(0.35, 0.1, 1.05), r=0.26, depth=0.6, rot=(90, 0, 0), verts=10)
    T.sphere("Sack", "Bez", p(-0.4, 0, 0.95), r=0.3, scale=(1, 0.9, 0.8), seg=7, rings=4)


def cati(ox, oy):
    """Çatı kiremidi parçası (4 × 4 m): üst katın çevresi; kiremit sıraları + mahya."""
    p = at(ox, oy)
    T.box("Base", "Bordo", p(0, 0, -0.05), (4.0, 4.0, 0.1))
    for i in range(8):
        T.cyl(f"Row{i}", "Kirmizi" if i % 2 == 0 else "Bakir", p(0, -1.75 + i * 0.5, 0.04), r=0.2, depth=4.0, rot=(0, 90, 0), scale=(1, 1, 0.45), verts=8)


# ------------------------------------------------------------------ dışa aktarım
def export(name, build):
    T.reset_scene()
    build()
    obj = T.join_all(name, smooth_angle=35)
    print(f"[{name}] üçgen: {T.tri_count(obj)}")
    T.save_blend(os.path.join(MODELS, "World", f"{name}.blend"))
    T.export_fbx(obj, os.path.join(FBX, f"{name}.fbx"))


ITEMS = [
    ("PR_Furniture_ZarMasasi", zar_masasi), ("PR_Furniture_KartMasasi", kart_masasi), ("PR_Furniture_BilekMasasi", bilek_masasi),
    ("PR_Decor_Zar", zar), ("PR_Decor_Tabela", tabela),
    ("PR_Nature_Agac", agac), ("PR_Nature_Cam", cam), ("PR_Nature_Kaya1", kaya1), ("PR_Nature_Kaya2", kaya2),
    ("PR_Nature_Cali", cali), ("PR_Nature_CicekTarhi", cicek_tarhi), ("PR_Nature_OtTutami", ot_tutami),
    ("PR_Nature_Tepe", tepe), ("PR_Nature_Dag", dag), ("PR_Nature_Kuyu", kuyu), ("PR_Nature_Araba", araba),
    ("PR_Nature_Cati", cati),
]

if __name__ == "__main__":
    only = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    for name, fn in ITEMS:
        if only and name not in only:
            continue
        export(name, lambda fn=fn: fn(0, 0))
    RP.shoot(lambda: zar_masasi(0, 0), "ZarMasasi", False)
    RP.shoot(lambda: kart_masasi(0, 0), "KartMasasi", False)
    RP.shoot(lambda: bilek_masasi(0, 0), "BilekMasasi", False)

    # Önizleme
    T.reset_scene()
    zar_masasi(-5.0, 0)
    kart_masasi(-3.4, 0)
    bilek_masasi(-1.9, 0)
    tabela(-0.2, 0)
    agac(1.8, 0.6)
    cam(3.6, 0.8)
    kaya1(5.0, -0.6)
    kaya2(6.2, 0.6)
    cali(5.0, 0.9)
    cicek_tarhi(-3.0, -1.6)
    kuyu(1.2, -1.6)
    araba(3.6, -1.6)
    for i in range(4):
        ot_tutami(-0.6 + i * 0.3, -1.6)
    T.join_all("WorldSheet")
    T.setup_render_stage(floor_size=40, floor_color="Yesil")
    T.render_from(os.path.join(RENDERS, "Props_World.png"), target=(0.6, -0.3, 0.9), pitch=30, yaw=0, dist=12.5, fov=46, res=(2000, 1000))
    print("BİTTİ")
