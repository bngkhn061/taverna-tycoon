"""
v1.0 "Tavernayı Büyüt" kademe parçaları (onaylı mockup design/ui/ui_v11_buyut.html, 2026-11-10).
Kademe 2 · Taş Han:   PR_Tier_TasSutun, PR_Tier_TasKemer, PR_Tier_DemirAvize, PR_Tier_Ortu1
Kademe 3 · Saray Hanı: PR_Tier_MermerSutun, PR_Tier_AltinKemer, PR_Tier_SarayAvize, PR_Tier_Ortu2, PR_Tier_Perde, PR_Tier_Hali
Hedef alanı: PR_Tier_BuyutKaresi (2 × 2 m altın ödeme alanı)
İkonlar: ICO_Tier_1 / 2 / 3 (kademe önizlemesi) · önizleme assets/renders/Props_Tiers.png
Avizeler kökten sarkar: model 0 noktası zeminde, gövde 2.0–2.7 m yükseklikte.
Çalıştırma: blender -b --factory-startup --python tools/blender/build_tiers.py
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
H = 2.6   # duvar yüksekliği


def tas_sutun(ox, oy):
    p = at(ox, oy)
    T.box("Base", "KoyuTas", p(0, 0, 0.12), (0.46, 0.46, 0.24), bevel=0.02)
    for i in range(5):
        T.box(f"Block{i}", "Tas" if i % 2 == 0 else "Siva", p(0, 0, 0.24 + 0.22 + i * 0.44), (0.36, 0.36, 0.42), bevel=0.02)
    T.box("Cap", "KoyuTas", p(0, 0, H - 0.05), (0.48, 0.48, 0.16), bevel=0.02)


def tas_kemer(ox, oy):
    """Kapı / geçit kemeri (1.3 m açıklık), iki yanında ayak; önü −y."""
    p = at(ox, oy)
    for s in (-1, 1):
        T.box(f"Leg{s}", "Tas", p(s * 0.75, 0, 1.0), (0.24, 0.3, 2.0), bevel=0.02)
    for i in range(7):
        a = math.radians(180 - i * 30)
        x, z = math.cos(a) * 0.75, 2.0 + math.sin(a) * 0.45
        T.box(f"Voussoir{i}", "Tas" if i % 2 else "KoyuTas", p(x, 0, z), (0.3, 0.3, 0.2), rot=(0, -math.degrees(a) + 90, 0), bevel=0.015)
    T.box("Key", "KoyuTas", p(0, -0.02, 2.47), (0.18, 0.34, 0.24), bevel=0.02)


def _hang(p, color):
    T.cyl("Chain", color, p(0, 0, 2.6), r=0.012, depth=0.4, verts=6)


def demir_avize(ox, oy):
    p = at(ox, oy)
    _hang(p, "KoyuTas")
    T.torus("Ring", "KoyuTas", p(0, 0, 2.3), major=0.42, minor=0.03, seg=16)
    for i in range(3):
        a = math.radians(i * 120)
        T.limb(f"Arm{i}", "KoyuTas", p(0, 0, 2.6), p(math.cos(a) * 0.42, math.sin(a) * 0.42, 2.3), r1=0.012, verts=5)
    for i in range(6):
        a = math.radians(i * 60)
        x, y = math.cos(a) * 0.42, math.sin(a) * 0.42
        T.cyl(f"Candle{i}", "Krem", p(x, y, 2.38), r=0.022, depth=0.12, verts=6)
        T.sphere(f"Flame{i}", "Ates", p(x, y, 2.48), r=0.02, scale=(1, 1, 1.6), seg=5, rings=3)


def ortu1(ox, oy):
    """Taş Han masa örtüsü: keten yolluk + demir şamdan (masa üstü 0.66 m)."""
    p = at(ox, oy)
    T.box("Runner", "Bez", p(0, 0, 0.675), (0.32, 0.9, 0.012))
    T.cyl("Holder", "KoyuTas", p(0, 0, 0.7), r=0.05, depth=0.04, verts=8)
    T.cyl("Candle", "Krem", p(0, 0, 0.78), r=0.025, depth=0.12, verts=6)
    T.sphere("Flame", "Ates", p(0, 0, 0.87), r=0.02, scale=(1, 1, 1.6), seg=5, rings=3)


def mermer_sutun(ox, oy):
    p = at(ox, oy)
    T.box("Base", "Krem", p(0, 0, 0.12), (0.48, 0.48, 0.24), bevel=0.02)
    T.box("BaseGold", "Altin", p(0, 0, 0.26), (0.42, 0.42, 0.05))
    T.cyl("Shaft", "Krem", p(0, 0, 1.33), r=0.17, depth=2.1, verts=12)
    for i in range(6):
        a = math.radians(i * 60)
        T.box(f"Flute{i}", "Siva", p(math.cos(a) * 0.17, math.sin(a) * 0.17, 1.33), (0.03, 0.03, 2.0), rot=(0, 0, i * 60))
    T.box("CapGold", "Altin", p(0, 0, H - 0.18), (0.44, 0.44, 0.1), bevel=0.02)
    T.box("Cap", "Krem", p(0, 0, H - 0.06), (0.52, 0.52, 0.14), bevel=0.02)


def altin_kemer(ox, oy):
    p = at(ox, oy)
    for s in (-1, 1):
        T.box(f"Leg{s}", "Krem", p(s * 0.75, 0, 1.0), (0.24, 0.3, 2.0), bevel=0.02)
        T.box(f"LegGold{s}", "Altin", p(s * 0.75, 0, 2.02), (0.28, 0.34, 0.06))
    for i in range(7):
        a = math.radians(180 - i * 30)
        x, z = math.cos(a) * 0.75, 2.0 + math.sin(a) * 0.45
        T.box(f"Voussoir{i}", "Krem" if i % 2 else "Altin", p(x, 0, z), (0.3, 0.3, 0.2), rot=(0, -math.degrees(a) + 90, 0), bevel=0.015)
    T.sphere("Crest", "Kirmizi", p(0, -0.18, 2.45), r=0.08, scale=(1, 0.5, 1), seg=8, rings=5)


def saray_avize(ox, oy):
    p = at(ox, oy)
    _hang(p, "Altin")
    T.torus("RingA", "Altin", p(0, 0, 2.35), major=0.45, minor=0.028, seg=18)
    T.torus("RingB", "Altin", p(0, 0, 2.5), major=0.26, minor=0.022, seg=14)
    for i in range(8):
        a = math.radians(i * 45)
        x, y = math.cos(a) * 0.45, math.sin(a) * 0.45
        T.cyl(f"Candle{i}", "Krem", p(x, y, 2.43), r=0.02, depth=0.1, verts=6)
        T.sphere(f"Flame{i}", "Ates", p(x, y, 2.52), r=0.018, scale=(1, 1, 1.6), seg=5, rings=3)
        T.sphere(f"Drop{i}", "ElfCiyi", p(x * 0.9, y * 0.9, 2.22), r=0.03, scale=(1, 1, 1.8), seg=6, rings=4)
    T.sphere("Core", "Altin", p(0, 0, 2.2), r=0.07, scale=(1, 1, 1.4), seg=8, rings=5)


def ortu2(ox, oy):
    """Saray masa örtüsü: kadife (bordo) örtü, altın kenar, üç kollu altın şamdan."""
    p = at(ox, oy)
    T.box("Cloth", "Bordo", p(0, 0, 0.674), (0.5, 0.95, 0.012))
    T.box("Trim", "Altin", p(0, 0, 0.672), (0.54, 0.99, 0.008))
    T.cyl("Stand", "Altin", p(0, 0, 0.74), r=0.02, depth=0.14, verts=6)
    for s in (-1, 0, 1):
        T.cyl(f"Candle{s}", "Krem", p(s * 0.08, 0, 0.86), r=0.018, depth=0.1, verts=6)
        T.sphere(f"Flame{s}", "Ates", p(s * 0.08, 0, 0.94), r=0.016, scale=(1, 1, 1.6), seg=5, rings=3)
    T.box("Bar", "Altin", p(0, 0, 0.81), (0.2, 0.02, 0.02))


def perde(ox, oy):
    """Duvar perdesi (1 m): üstte kadife saçak + iki yanda bağlı perde; duvara yaslı, önü −y."""
    p = at(ox, oy)
    T.cyl("Rod", "Altin", p(0, -0.06, 2.45), r=0.02, depth=1.1, rot=(0, 90, 0), verts=6)
    for i in range(5):
        T.sphere(f"Swag{i}", "Bordo", p(-0.4 + i * 0.2, -0.08, 2.32), r=0.13, scale=(1, 0.4, 0.8), seg=8, rings=5)
    for s in (-1, 1):
        T.box(f"Drape{s}", "Bordo", p(s * 0.42, -0.08, 1.5), (0.16, 0.06, 1.8), bevel=0.02)
        T.torus(f"Tie{s}", "Altin", p(s * 0.42, -0.1, 1.2), major=0.07, minor=0.015, rot=(90, 0, 0), seg=10)


def hali(ox, oy):
    """Kırmızı yolluk halı (1 × 3 m), altın kenarlı."""
    p = at(ox, oy)
    T.box("Carpet", "Kirmizi", p(0, 0, 0.044), (1.0, 3.0, 0.008))
    for s in (-1, 1):
        T.box(f"Edge{s}", "Altin", p(s * 0.46, 0, 0.046), (0.06, 3.0, 0.008))


def buyut_karesi(ox, oy):
    """"Tavernayı Büyüt" hedef alanı: 2 × 2 m altın kare, köşe süsleri ve ortada ev simgesi."""
    p = at(ox, oy)
    T.box("Plate", "Bal", p(0, 0, 0.012), (2.0, 2.0, 0.02))
    T.box("Inner", "Altin", p(0, 0, 0.018), (1.7, 1.7, 0.02))
    for s in (-1, 1):
        for t in (-1, 1):
            T.cyl(f"Stud{s}{t}", "KoyuMese", p(s * 0.85, t * 0.85, 0.03), r=0.07, depth=0.03, verts=8)
    T.box("HouseBase", "KoyuMese", p(0, -0.1, 0.035), (0.5, 0.4, 0.02))
    T.cyl("HouseRoof", "KoyuMese", p(0, 0.18, 0.035), r=0.36, depth=0.02, verts=3, rot=(0, 0, 90), scale=(1, 0.7, 1))


ITEMS = [("PR_Tier_TasSutun", tas_sutun), ("PR_Tier_TasKemer", tas_kemer), ("PR_Tier_DemirAvize", demir_avize), ("PR_Tier_Ortu1", ortu1),
         ("PR_Tier_MermerSutun", mermer_sutun), ("PR_Tier_AltinKemer", altin_kemer), ("PR_Tier_SarayAvize", saray_avize),
         ("PR_Tier_Ortu2", ortu2), ("PR_Tier_Perde", perde), ("PR_Tier_Hali", hali), ("PR_Tier_BuyutKaresi", buyut_karesi)]


def scene(tier):
    """Kademe önizlemesi: masa + sütun + avize + (Saray'da perde)."""
    P.f_masa_kare(0, 0) if hasattr(P, "f_masa_kare") else T.box("Table", "Mese", (0, 0, 0.33), (0.9, 0.9, 0.66), bevel=0.02)
    if tier == 1:
        T.box("Lamp", "Ates", (0, 0, 0.75), (0.05, 0.05, 0.12))
        T.box("Post", "Mese", (-0.9, 0.3, 1.3), (0.2, 0.2, 2.6))
    elif tier == 2:
        ortu1(0, 0); tas_sutun(-0.9, 0.3); demir_avize(0, 0)
    else:
        ortu2(0, 0); mermer_sutun(-0.9, 0.3); saray_avize(0, 0); perde(0.6, 0.6)


if __name__ == "__main__":
    for name, fn in ITEMS:
        T.reset_scene()
        fn(0, 0)
        obj = T.join_all(name, smooth_angle=35)
        print(f"[{name}] üçgen: {T.tri_count(obj)}")
        T.save_blend(os.path.join(MODELS, "Tiers", f"{name}.blend"))
        T.export_fbx(obj, os.path.join(FBX, f"{name}.fbx"))
    for t in (1, 2, 3):
        RP.shoot(lambda t=t: scene(t), f"Tier_{t}", False)
    T.reset_scene()
    tas_sutun(-4.0, 0); tas_kemer(-2.6, 0); demir_avize(-1.0, 0); ortu1(-1.0, 0)
    mermer_sutun(0.6, 0); altin_kemer(2.0, 0); saray_avize(3.6, 0); ortu2(3.6, 0); perde(5.0, 0.3); hali(5.0, -1.6)
    T.join_all("TierSheet")
    T.setup_render_stage(floor_size=40, floor_color="KoyuTas")
    T.render_from(os.path.join(RENDERS, "Props_Tiers.png"), target=(0.5, 0, 1.3), pitch=12, yaw=0, dist=10.5, fov=46, res=(2000, 1000))
    print("BİTTİ")
