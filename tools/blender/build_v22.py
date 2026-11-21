"""
v2.2 Paket 4 — sezonluk etkinlikler ve kostümler (GAME_DESIGN.md §16).
  Sezon içecekleri (bardak + ikon): PR_Drink_ElmaSarabi · PR_Drink_SicakSarap · PR_Drink_CicekSerbeti
  Sezon tezgâhları (kademe 1–3):   PR_Station_SezonHasat_T* · PR_Station_SezonKis_T* · PR_Station_SezonBahar_T*
  Sezon süslemesi (kendiliğinden):  PR_Season_<Hasat|Kis|Bahar>_Yigin · PR_Season_<…>_Kucuk
  Kalıcı sezon dekoru (jetonla):    PR_Decor_KabakFener · BasakDemeti · KardanAdam · SusluCam · CicekAraba · KusEvi
  Sezon müşterileri:                CH_Cust_31_HasatKoylusu · CH_Cust_32_KisGezgini · CH_Cust_33_BaharCicekcisi
  Kostümler (aynı gövde, kıyafet / şapka varyantı): CH_Costume_<Kimlik>
  İkonlar: ICO_<İçecek>, ICO_DEC_<Ad>, ICO_Cust_31..33, ICO_Cos_<Kimlik>, ICO_Season_<Ad>, ICO_SeasonToken
  Önizleme: assets/renders/Season_Sheet.png, assets/renders/Costumes_Sheet.png
Çalıştırma: blender -b --factory-startup --python tools/blender/build_v22.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import toylib as T  # noqa: E402
import bodylib as B  # noqa: E402
import build_props as P  # noqa: E402
import build_staff as S  # noqa: E402
import build_kitchen as K  # noqa: E402
import build_customers as C  # noqa: E402
import build_main_characters as M  # noqa: E402
import render_portraits as RP  # noqa: E402
from mathutils import Vector as V  # noqa: E402

at = P.at
MODELS = os.path.join(T.ROOT, "assets", "models")
FBX = os.path.join(T.ROOT, "assets", "export", "fbx")
RENDERS = os.path.join(T.ROOT, "assets", "renders")


# ------------------------------------------------------------------ yardımcılar
def drop(*names):
    """Birleştirmeden önce adı verilen parçaları siler ('Ad*' = önek)."""
    for o in list(bpy.context.scene.objects):
        if o.type != "MESH":
            continue
        for n in names:
            if (n.endswith("*") and o.name.startswith(n[:-1])) or o.name == n or o.name.startswith(n + "."):
                bpy.data.objects.remove(o, do_unlink=True)
                break


def unjoined(builder, *args):
    """Kendi içinde join_all çağıran kurucuyu (Barlo, Pip) parçaları ayrık bırakarak çalıştırır."""
    real = T.join_all
    T.join_all = lambda *a, **k: None
    try:
        return builder(*args)
    finally:
        T.join_all = real


def leaf(name, color, loc, r=0.05, rot=(0, 0, 0)):
    T.sphere(name, color, loc, r=r, scale=(1.4, 1.0, 0.3), rot=rot, seg=6, rings=4)


def pumpkin(name, p, x, y, z, r, color="Ates"):
    T.sphere(name, color, p(x, y, z), r=r, scale=(1.1, 1.1, 0.8), seg=10, rings=7)
    for k in range(4):
        a = k * math.pi / 4
        T.torus(f"{name}_Rib{k}", color, p(x, y, z), major=r * 0.96, minor=r * 0.07, scale=(1.1, 1.1, 0.8),
                rot=(90, 0, math.degrees(a)), seg=12, mseg=4)
    T.cyl(f"{name}_Stem", "KoyuMese", p(x, y, z + r * 0.85), r=r * 0.12, r2=r * 0.07, depth=r * 0.35, verts=5)


def flower(name, p, x, y, z, color, r=0.04):
    T.sphere(name, color, p(x, y, z), r=r, scale=(1, 1, 0.55), seg=8, rings=5)
    T.sphere(f"{name}_Eye", "Altin" if color != "Altin" else "Kirmizi", p(x, y, z + r * 0.35), r=r * 0.38, seg=6, rings=4)


def pine(name, p, x, y, h, color="Yesil", ornaments=True):
    T.cyl(f"{name}_Trunk", "KoyuMese", p(x, y, h * 0.08), r=h * 0.05, depth=h * 0.16, verts=6)
    for i, (z, r) in enumerate(((0.32, 0.36), (0.55, 0.28), (0.76, 0.19))):
        T.cyl(f"{name}_Tier{i}", color, p(x, y, h * z), r=h * r, r2=0.0, depth=h * 0.36, verts=9)
    if ornaments:
        cols = ("Kirmizi", "Altin", "Mavi", "Kirmizi", "Altin", "Lila")
        for i, (a, z, rr) in enumerate(((20, 0.25, 0.3), (140, 0.3, 0.29), (260, 0.22, 0.32), (80, 0.5, 0.21), (200, 0.52, 0.2), (320, 0.7, 0.12))):
            ang = math.radians(a)
            T.sphere(f"{name}_Orn{i}", cols[i], p(x + math.cos(ang) * h * rr, y + math.sin(ang) * h * rr, h * z), r=h * 0.035, seg=6, rings=4)
    T.sphere(f"{name}_Star", "Altin", p(x, y, h * 0.97), r=h * 0.05, scale=(1, 0.4, 1), seg=5, rings=3)


def present(name, p, x, y, s, color, ribbon="Altin"):
    T.box(name, color, p(x, y, s / 2), (s, s, s), bevel=s * 0.06)
    T.box(f"{name}_RA", ribbon, p(x, y, s / 2), (s * 1.02, s * 0.16, s * 1.02))
    T.box(f"{name}_RB", ribbon, p(x, y, s / 2), (s * 0.16, s * 1.02, s * 1.02))
    T.sphere(f"{name}_Bow", ribbon, p(x, y, s * 1.05), r=s * 0.16, scale=(1.4, 0.7, 0.6), seg=6, rings=4)


def wheat(name, p, x, y, z, h=0.6, n=7, tie="Kirmizi"):
    for i in range(n):
        a = (i / n) * math.pi * 2
        dx, dy = math.cos(a) * 0.05, math.sin(a) * 0.05
        top = p(x + dx * 3.2, y + dy * 3.2, z + h)
        T.limb(f"{name}_Stalk{i}", "SacSari", p(x + dx * 0.6, y + dy * 0.6, z), top, r1=0.012, verts=4)
        T.sphere(f"{name}_Ear{i}", "Bal", top, r=0.03, scale=(1, 1, 2.2), seg=6, rings=4)
    T.torus(f"{name}_Tie", tie, p(x, y, z + h * 0.42), major=0.07, minor=0.015, seg=10)


# ------------------------------------------------------------------ sezon içecekleri
def d_elma_sarabi(ox, oy):
    """Hasat: tahta kupada sıcak elma şarabı, elma dilimi ve tarçın."""
    p = at(ox, oy)
    T.cyl("Mug", "Mese", p(0, 0, 0.075), r=0.06, r2=0.066, depth=0.15, verts=10)
    T.torus("MugBand", "KoyuMese", p(0, 0, 0.04), major=0.063, minor=0.007, seg=10)
    T.cyl("Cider", "BalSarabi", p(0, 0, 0.15), r=0.058, depth=0.01, verts=10)
    T.torus("Handle", "KoyuMese", p(0.075, 0, 0.08), major=0.032, minor=0.01, rot=(90, 0, 0), seg=8)
    T.dome("AppleSlice", "Kirmizi", p(-0.04, -0.035, 0.155), r=0.035, scale=(1, 0.35, 1), rot=(0, 0, 20))
    T.limb("Cinnamon", "KoyuMese", p(0.01, 0.02, 0.14), p(0.04, 0.04, 0.24), r1=0.009, verts=5)


def d_sicak_sarap(ox, oy):
    """Kış: krem toprak fincanda baharatlı sıcak şarap, portakal dilimi ve buhar."""
    p = at(ox, oy)
    T.cyl("Cup", "Krem", p(0, 0, 0.06), r=0.052, r2=0.064, depth=0.12, verts=10)
    T.cyl("Wine", "Bordo", p(0, 0, 0.12), r=0.06, depth=0.008, verts=10)
    T.torus("Handle", "Krem", p(0.072, 0, 0.06), major=0.028, minor=0.009, rot=(90, 0, 0), seg=8)
    T.torus("Orange", "Ates", p(-0.03, -0.045, 0.13), major=0.026, minor=0.01, rot=(70, 0, 0), seg=10)
    for i, (x, z, r) in enumerate(((0.0, 0.17, 0.025), (0.02, 0.21, 0.02), (-0.01, 0.245, 0.015))):
        T.sphere(f"Steam{i}", "Kopuk", p(x, 0, z), r=r, seg=6, rings=4)
    T.sphere("Star", "KoyuMese", p(0.03, 0.02, 0.127), r=0.014, scale=(1, 1, 0.3), seg=5, rings=3)


def d_cicek_serbeti(ox, oy):
    """Bahar: uzun cam bardakta pembe çiçek şerbeti, çiçek ve pipet."""
    p = at(ox, oy)
    T.cyl("Glass", "ElfCiyi", p(0, 0, 0.085), r=0.05, r2=0.058, depth=0.17, verts=10)
    T.cyl("Drink", "Pembe", p(0, 0, 0.16), r=0.056, depth=0.012, verts=10)
    T.limb("Straw", "Krem", p(0.015, 0.01, 0.12), p(0.04, 0.025, 0.25), r1=0.007, verts=5)
    flower("Flower", p, -0.035, -0.03, 0.172, "Altin", r=0.028)
    leaf("Leaf", "Yaprak", p(-0.01, -0.05, 0.168), r=0.02)


# ------------------------------------------------------------------ sezon tezgâhı (geçici makine)
def stall(ox, oy, tier, kind):
    """Tekerlekli tahta tezgâh + çizgili tente; tezgâhta sezonun içeceği. Ön yüz −Y."""
    p = at(ox, oy)
    stripe = {"Hasat": "Ates", "Kis": "Kirmizi", "Bahar": "Pembe"}[kind]
    T.box("Cart", "Mese", p(0, 0, 0.42), (0.9, 0.55, 0.5), bevel=0.02)
    T.box("CartTop", "KoyuMese", p(0, 0, 0.69), (0.98, 0.62, 0.05), bevel=0.01)
    T.box("CartFront", P.TRIM[tier], p(0, -0.28, 0.42), (0.7, 0.02, 0.06))
    for s in (1, -1):
        T.cyl(f"Wheel{s}", "KoyuMese", p(s * 0.5, 0.0, 0.2), r=0.2, depth=0.06, rot=(0, 90, 0), verts=12)
        T.cyl(f"Hub{s}", P.TRIM[tier] if tier > 1 else "Tas", p(s * 0.535, 0.0, 0.2), r=0.05, depth=0.03, rot=(0, 90, 0), verts=8)
        for y in (-0.26, 0.26):
            T.box(f"Post{s}{y}", "KoyuMese", p(s * 0.44, y, 1.02), (0.05, 0.05, 0.66))
    # tente: dönüşümlü renkli şeritler
    for i in range(6):
        x = -0.45 + i * 0.18
        T.box(f"Awning{i}", stripe if i % 2 == 0 else "Krem", p(x, 0, 1.36), (0.18, 0.72, 0.04), rot=(10, 0, 0))
        T.cyl(f"Scallop{i}", stripe if i % 2 == 0 else "Krem", p(x, -0.37, 1.29), r=0.06, depth=0.03, rot=(90, 0, 0), verts=8)
    if kind == "Hasat":
        P.barrel(p, "Keg", 0.18, 0.05, 0.86, r=0.14, length=0.3, band=P.TRIM[tier], lying=True)
        for i, (x, y) in enumerate(((-0.3, -0.12), (-0.2, -0.16), (-0.25, -0.02), (-0.12, -0.05))):
            T.sphere(f"Apple{i}", "Kirmizi" if i % 2 == 0 else "Yaprak", p(x, y, 0.76), r=0.05, seg=8, rings=6)
        pumpkin("PumpkinSide", p, 0.42, -0.34, 0.11, 0.11)
    elif kind == "Kis":
        T.cyl("Stove", "KoyuTas", p(0.15, 0.05, 0.8), r=0.13, depth=0.18, verts=10)
        T.sphere("Fire", "Ates", p(0.15, -0.08, 0.78), r=0.04, scale=(1, 0.5, 1), seg=6, rings=4)
        T.sphere("Kettle", "Bakir", p(0.15, 0.05, 0.98), r=0.13, scale=(1, 1, 0.8), seg=10, rings=7)
        T.limb("Spout", "Bakir", p(0.04, 0.0, 0.98), p(-0.06, -0.05, 1.04), r1=0.025, r2=0.015, verts=6)
        for i, z in enumerate((1.12, 1.18)):
            T.sphere(f"Steam{i}", "Kopuk", p(-0.08 - i * 0.03, -0.06, z), r=0.035 - i * 0.008, seg=6, rings=4)
        T.dome("SnowTop", "Kopuk", p(0, 0.05, 1.39), r=0.42, scale=(1.2, 0.9, 0.12))
        pine("PineSide", p, 0.48, -0.33, 0.45)
    else:
        T.cyl("Jug", "ElfCiyi", p(0.15, 0.05, 0.85), r=0.08, r2=0.06, depth=0.26, verts=10)
        T.cyl("JugDrink", "Pembe", p(0.15, 0.05, 0.92), r=0.075, depth=0.1, verts=10)
        for i, (x, y, c) in enumerate(((-0.3, -0.12, "Pembe"), (-0.18, -0.15, "Altin"), (-0.25, 0.0, "Lila"), (-0.1, -0.05, "Krem"))):
            flower(f"Fl{i}", p, x, y, 0.76, c, r=0.045)
        T.cyl("Pot", "Bakir", p(0.43, -0.33, 0.1), r=0.1, r2=0.12, depth=0.2, verts=10)
        for i, c in enumerate(("Pembe", "Altin", "Lila")):
            flower(f"PotFl{i}", p, 0.43 + (i - 1) * 0.06, -0.33 + (i % 2) * 0.04, 0.26, c)
    P.tier_extras(p, tier, 1.42, front_y=-0.3)


def s_sezon_hasat(ox, oy, tier=1):
    stall(ox, oy, tier, "Hasat")


def s_sezon_kis(ox, oy, tier=1):
    stall(ox, oy, tier, "Kis")


def s_sezon_bahar(ox, oy, tier=1):
    stall(ox, oy, tier, "Bahar")


# ------------------------------------------------------------------ sezon süslemesi (kendiliğinden, sezon boyunca)
def hasat_yigin(ox, oy):
    """Saman balyası, kabaklar, başak demeti ve yere dökülmüş yapraklar."""
    p = at(ox, oy)
    T.box("Hay", "Bal", p(0.05, 0.1, 0.2), (0.7, 0.42, 0.4), bevel=0.04)
    for y in (-0.08, 0.28):
        T.box(f"HayTie{y}", "SacKahve", p(0.05, y, 0.2), (0.72, 0.02, 0.41))
    pumpkin("PA", p, -0.12, 0.08, 0.5, 0.15)
    pumpkin("PB", p, 0.2, 0.05, 0.47, 0.11, "Bal")
    pumpkin("PC", p, -0.35, -0.22, 0.12, 0.13)
    pumpkin("PD", p, 0.38, -0.25, 0.09, 0.09)
    wheat("Sheaf", p, -0.45, 0.15, 0.0, h=0.65)
    for i, (x, y, c) in enumerate(((0.0, -0.35, "Ates"), (0.25, -0.42, "Kirmizi"), (-0.2, -0.45, "Bal"), (0.5, 0.0, "Bordo"), (-0.55, -0.1, "Ates"))):
        leaf(f"Leaf{i}", c, p(x, y, 0.05), r=0.06, rot=(0, 0, i * 47))


def hasat_kucuk(ox, oy):
    """İki kabak ve küçük başak."""
    p = at(ox, oy)
    pumpkin("PA", p, 0.0, 0.0, 0.13, 0.14)
    pumpkin("PB", p, 0.2, -0.1, 0.09, 0.09, "Bal")
    wheat("Sheaf", p, -0.18, 0.1, 0.0, h=0.42, n=5)
    leaf("LeafA", "Kirmizi", p(0.1, -0.25, 0.05), r=0.05)


def kis_yigin(ox, oy):
    """Kar yığını, küçük süslü çam, iki hediye paketi."""
    p = at(ox, oy)
    T.dome("Snow", "Kopuk", p(0, 0, 0.0), r=0.5, scale=(1.2, 0.9, 0.38))
    pine("Pine", p, 0.1, 0.12, 1.1)
    present("GiftA", p, -0.38, -0.22, 0.2, "Kirmizi")
    present("GiftB", p, -0.12, -0.3, 0.15, "Mavi", ribbon="Krem")
    T.dome("SnowB", "Kopuk", p(0.45, -0.2, 0.0), r=0.18, scale=(1.3, 1, 0.5))


def kis_kucuk(ox, oy):
    """Küçük kar yığını ve fener."""
    p = at(ox, oy)
    T.dome("Snow", "Kopuk", p(0, 0, 0.0), r=0.3, scale=(1.2, 1, 0.45))
    pine("Pine", p, 0.0, 0.05, 0.6)
    present("Gift", p, 0.25, -0.15, 0.13, "Kirmizi")


def bahar_yigin(ox, oy):
    """Çiçekli fıçı saksı, sulama kabı, kelebekler."""
    p = at(ox, oy)
    T.cyl("Tub", "Mese", p(0, 0, 0.2), r=0.3, r2=0.33, depth=0.4, verts=12)
    for z in (0.08, 0.32):
        T.torus(f"TubBand{z}", "KoyuMese", p(0, 0, z), major=0.31 + z * 0.05, minor=0.016, seg=12)
    T.cyl("Soil", "KoyuMese", p(0, 0, 0.405), r=0.3, depth=0.02, verts=12)
    cols = ("Pembe", "Altin", "Lila", "Krem", "Kirmizi", "Pembe", "Lila")
    for i in range(7):
        a = math.radians(i * 51)
        r = 0.16 if i else 0.0
        x, y = math.cos(a) * r, math.sin(a) * r
        T.limb(f"Stem{i}", "Yesil", p(x, y, 0.4), p(x * 1.3, y * 1.3, 0.62 + (i % 3) * 0.06), r1=0.012, verts=4)
        flower(f"Fl{i}", p, x * 1.3, y * 1.3, 0.64 + (i % 3) * 0.06, cols[i], r=0.06)
        leaf(f"Lf{i}", "Yaprak", p(x * 1.1, y * 1.1, 0.5), r=0.05, rot=(0, 0, i * 40))
    T.cyl("Can", "Bakir", p(0.45, -0.18, 0.1), r=0.08, depth=0.2, verts=10)
    T.limb("CanSpout", "Bakir", p(0.38, -0.18, 0.1), p(0.27, -0.22, 0.24), r1=0.018, r2=0.012, verts=6)
    for i, (x, z, c) in enumerate(((-0.35, 0.9, "Altin"), (0.3, 1.0, "Lila"))):
        for s in (1, -1):
            T.sphere(f"Wing{i}{s}", c, p(x + s * 0.04, 0, z), r=0.045, scale=(1, 0.2, 0.8), rot=(0, s * 20, 0), seg=6, rings=4)


def bahar_kucuk(ox, oy):
    """Küçük çiçek saksısı."""
    p = at(ox, oy)
    T.cyl("Pot", "Bakir", p(0, 0, 0.13), r=0.15, r2=0.18, depth=0.26, verts=10)
    T.cyl("Soil", "KoyuMese", p(0, 0, 0.265), r=0.165, depth=0.02, verts=10)
    for i, c in enumerate(("Pembe", "Altin", "Lila", "Krem")):
        a = math.radians(i * 90 + 20)
        flower(f"Fl{i}", p, math.cos(a) * 0.08, math.sin(a) * 0.08, 0.42 + (i % 2) * 0.05, c, r=0.055)
        leaf(f"Lf{i}", "Yaprak", p(math.cos(a) * 0.1, math.sin(a) * 0.1, 0.32), r=0.05, rot=(0, 0, i * 90))


# ------------------------------------------------------------------ kalıcı sezon dekoru (sezon jetonuyla)
def x_kabak_fener(ox, oy):
    """Hasat: oyulmuş kabak fenerleri ve direkli fener (gece yanar)."""
    p = at(ox, oy)
    pumpkin("Big", p, 0, 0, 0.2, 0.22)
    for s in (1, -1):
        T.cyl(f"EyeL{s}", "Bal", p(s * 0.08, -0.215, 0.25), r=0.035, r2=0.0, depth=0.03, rot=(90, 0, 0), verts=3)
    T.box("Mouth", "Bal", p(0, -0.22, 0.14), (0.16, 0.02, 0.04))
    pumpkin("Small", p, 0.3, -0.12, 0.1, 0.11)
    T.box("Post", "KoyuMese", p(-0.32, 0.1, 0.55), (0.06, 0.06, 1.1))
    T.box("Arm", "KoyuMese", p(-0.22, 0.1, 1.07), (0.25, 0.04, 0.04))
    T.cyl("LampBody", "Ates", p(-0.12, 0.1, 0.95), r=0.07, depth=0.12, verts=8)
    T.cyl("LampTop", "KoyuMese", p(-0.12, 0.1, 1.03), r=0.08, r2=0.02, depth=0.05, verts=8)


def x_basak_demeti(ox, oy):
    """Hasat: kırmızı kurdeleli büyük başak demeti ve iki un çuvalı."""
    p = at(ox, oy)
    wheat("Sheaf", p, 0, 0.05, 0.0, h=1.0, n=11)
    for i, (x, y) in enumerate(((0.3, -0.15), (-0.3, -0.12))):
        T.sphere(f"Sack{i}", "Bez", p(x, y, 0.17), r=0.17, scale=(1, 0.9, 1.1), seg=8, rings=6)
        T.cyl(f"SackTie{i}", "SacKahve", p(x, y, 0.36), r=0.05, depth=0.05, verts=6)
    pumpkin("P", p, 0.05, -0.3, 0.08, 0.08)


def x_kardan_adam(ox, oy):
    """Kış: kardan adam — atkı, silindir şapka, havuç burun, dal kollar."""
    p = at(ox, oy)
    for i, (z, r) in enumerate(((0.24, 0.26), (0.62, 0.19), (0.92, 0.14))):
        T.sphere(f"Ball{i}", "Kopuk", p(0, 0, z), r=r, seg=12, rings=8)
    T.torus("Scarf", "Kirmizi", p(0, 0, 0.79), major=0.12, minor=0.04, seg=12)
    T.box("ScarfTail", "Kirmizi", p(0.09, -0.12, 0.66), (0.06, 0.03, 0.22), rot=(0, 15, 0))
    T.cyl("HatBrim", "Lacivert", p(0, 0, 1.04), r=0.15, depth=0.02, verts=12)
    T.cyl("HatCrown", "Lacivert", p(0, 0, 1.13), r=0.1, depth=0.18, verts=12)
    T.cyl("HatBand", "Kirmizi", p(0, 0, 1.07), r=0.102, depth=0.03, verts=12)
    T.cyl("Nose", "Ates", p(0, -0.18, 0.92), r=0.025, r2=0.0, depth=0.12, rot=(90, 0, 0), verts=6)
    for s in (1, -1):
        T.sphere(f"Eye{s}", "Goz", p(s * 0.05, -0.12, 0.97), r=0.018, seg=6, rings=4)
        T.limb(f"Arm{s}", "KoyuMese", p(s * 0.15, 0, 0.66), p(s * 0.42, -0.02, 0.86), r1=0.015, verts=4)
    for i in range(3):
        T.sphere(f"Coal{i}", "Goz", p(0, -0.19, 0.55 + i * 0.09), r=0.017, seg=6, rings=4)


def x_suslu_cam(ox, oy):
    """Kış: büyük süslü çam ve altında hediyeler."""
    p = at(ox, oy)
    pine("Tree", p, 0, 0, 1.7)
    present("GiftA", p, -0.3, -0.3, 0.2, "Kirmizi")
    present("GiftB", p, 0.28, -0.32, 0.16, "Mavi", ribbon="Krem")
    present("GiftC", p, 0.02, -0.42, 0.12, "Altin", ribbon="Kirmizi")


def x_cicek_araba(ox, oy):
    """Bahar: çiçeklerle dolu küçük el arabası."""
    p = at(ox, oy)
    T.box("Bed", "Mese", p(0, 0, 0.42), (0.8, 0.5, 0.28), bevel=0.02)
    T.box("Rim", "KoyuMese", p(0, 0, 0.57), (0.84, 0.54, 0.04))
    for s in (1, -1):
        T.cyl(f"Wheel{s}", "KoyuMese", p(s * 0.3, -0.28, 0.2), r=0.2, depth=0.05, rot=(90, 0, 0), verts=12)
        T.cyl(f"Hub{s}", "Pirinc", p(s * 0.3, -0.31, 0.2), r=0.04, depth=0.03, rot=(90, 0, 0), verts=8)
        T.limb(f"Handle{s}", "KoyuMese", p(s * 0.2, 0.25, 0.5), p(s * 0.22, 0.6, 0.65), r1=0.02, verts=5)
    cols = ("Pembe", "Altin", "Lila", "Krem", "Kirmizi", "Pembe", "Lila", "Altin", "Krem")
    for i in range(9):
        x, y = -0.28 + (i % 3) * 0.28, -0.14 + (i // 3) * 0.14
        flower(f"Fl{i}", p, x, y, 0.64 + (i % 2) * 0.05, cols[i], r=0.07)
        leaf(f"Lf{i}", "Yaprak", p(x + 0.06, y, 0.6), r=0.06, rot=(0, 0, i * 35))


def x_kus_evi(ox, oy):
    """Bahar: direk üstünde kuş evi, mavi kuş, dibinde çiçekler."""
    p = at(ox, oy)
    T.box("Post", "KoyuMese", p(0, 0, 0.6), (0.07, 0.07, 1.2))
    T.box("House", "Mese", p(0, 0, 1.32), (0.3, 0.26, 0.28), bevel=0.01)
    for s in (1, -1):
        T.box(f"Roof{s}", "Kirmizi", p(s * 0.1, 0, 1.52), (0.26, 0.32, 0.03), rot=(0, s * 40, 0))
    T.cyl("Hole", "Goz", p(0, -0.131, 1.34), r=0.05, depth=0.01, rot=(90, 0, 0), verts=10)
    T.limb("Perch", "KoyuMese", p(0, -0.13, 1.25), p(0, -0.22, 1.25), r1=0.01, verts=4)
    T.sphere("Bird", "Mavi", p(0.05, -0.2, 1.62), r=0.06, scale=(1.2, 0.9, 0.9), seg=8, rings=6)
    T.sphere("BirdHead", "Mavi", p(-0.02, -0.22, 1.68), r=0.04, seg=6, rings=4)
    T.cyl("Beak", "Altin", p(-0.07, -0.23, 1.68), r=0.012, r2=0.0, depth=0.04, rot=(0, -90, 0), verts=4)
    for i, c in enumerate(("Pembe", "Altin", "Lila", "Krem", "Pembe")):
        a = math.radians(i * 72)
        flower(f"Fl{i}", p, math.cos(a) * 0.18, math.sin(a) * 0.18, 0.12 + (i % 2) * 0.05, c, r=0.05)
        leaf(f"Lf{i}", "Yaprak", p(math.cos(a) * 0.2, math.sin(a) * 0.2, 0.06), r=0.06, rot=(0, 0, i * 72))


# ------------------------------------------------------------------ ikon simgeleri
def emblem_hasat(ox, oy):
    p = at(ox, oy)
    pumpkin("P", p, 0, 0, 0.15, 0.17)
    leaf("L", "Yaprak", p(0.08, 0, 0.32), r=0.06, rot=(0, 30, 0))


def emblem_kis(ox, oy):
    p = at(ox, oy)
    T.dome("Snow", "Kopuk", p(0, 0, 0), r=0.3, scale=(1, 0.8, 0.3))
    pine("Pine", p, 0, 0, 0.7)


def emblem_bahar(ox, oy):
    p = at(ox, oy)
    for i in range(6):
        a = math.radians(i * 60)
        T.sphere(f"Petal{i}", "Pembe", p(math.cos(a) * 0.11, 0, 0.3 + math.sin(a) * 0.11), r=0.07, scale=(1, 0.35, 1), seg=8, rings=5)
    T.sphere("Eye", "Altin", p(0, -0.03, 0.3), r=0.06, scale=(1, 0.5, 1), seg=8, rings=5)
    T.limb("Stem", "Yesil", p(0, 0, 0.0), p(0, 0, 0.22), r1=0.015, verts=5)
    leaf("Lf", "Yaprak", p(0.06, 0, 0.1), r=0.05, rot=(0, -30, 0))


def token(ox, oy):
    """Sezon jetonu: mor madalyon, altın kenar ve yıldız."""
    p = at(ox, oy)
    T.cyl("Disc", "Mor", p(0, 0, 0.2), r=0.18, depth=0.04, rot=(90, 0, 0), verts=20)
    T.torus("Rim", "Altin", p(0, 0, 0.2), major=0.18, minor=0.022, rot=(90, 0, 0), seg=20)
    for k in range(5):
        a = math.radians(90 + k * 72)
        T.cyl(f"Ray{k}", "Altin", p(math.cos(a) * 0.05, -0.025, 0.2 + math.sin(a) * 0.05), r=0.035, r2=0.0, depth=0.1,
              rot=(0, 90 - math.degrees(a), 0), verts=4)
    T.sphere("Core", "Altin", p(0, -0.025, 0.2), r=0.045, scale=(1, 0.4, 1), seg=8, rings=5)


# ------------------------------------------------------------------ sezon müşterileri
def c31_hasat(ox):
    """Hasat Köylüsü: çiftçi gövdesi, kucağında kabak, sırtında başak demeti."""
    R = C.c01_ciftci(ox)
    h = R.hand[-1]
    T.sphere("Pumpkin", "Ates", h + V((0.02, -0.08, 0.06)), r=0.11, scale=(1.1, 1.1, 0.8), seg=10, rings=7)
    T.cyl("PumpkinStem", "KoyuMese", h + V((0.02, -0.08, 0.16)), r=0.015, depth=0.05, verts=5)
    for i in range(5):
        a = math.radians(-30 + i * 15)
        T.limb(f"Wheat{i}", "SacSari", (ox + 0.05, R.ry + 0.05, R.tz - 0.1), (ox + 0.05 + math.sin(a) * 0.3, R.ry + 0.1, R.hz + 0.25), r1=0.011, verts=4)
        T.sphere(f"Ear{i}", "Bal", (ox + 0.05 + math.sin(a) * 0.3, R.ry + 0.1, R.hz + 0.28), r=0.025, scale=(1, 1, 2), seg=6, rings=4)
    T.torus("Neck", "Ates", (R.ox, 0, R.tz + R.rz * 0.88), major=R.rx * 0.55, minor=0.022, seg=12)
    return R


def c32_kis(ox):
    """Kış Gezgini: kırmızı palto (beyaz kürk kenar), kürk şapka, yeşil atkı, elinde fener."""
    R = B.body(ox, "M", "Ten1", "Kirmizi", "Lacivert", "KoyuMese", build="fat")
    B.skirt(R, "Kirmizi", bottom=0.14, flare=1.2)
    T.cyl("CoatHem", "Kopuk", (ox, 0, 0.15), r=R.rx * 1.22, depth=0.06, scale=(1, R.ry / R.rx, 1), verts=14)
    T.torus("Scarf", "Yesil", (ox, 0, R.tz + R.rz * 0.85), major=R.rx * 0.6, minor=0.04, seg=12)
    T.box("ScarfTail", "Yesil", (ox + R.rx * 0.35, -R.ry * 0.95, R.tz + R.rz * 0.35), (0.07, 0.03, 0.2))
    T.cyl("FurHat", "Kopuk", (ox, 0, R.hz + R.hr * 0.75), r=R.hr * 1.02, r2=R.hr * 0.95, depth=R.hr * 0.6, verts=12)
    T.sphere("FurTop", "Kopuk", (ox, 0, R.hz + R.hr * 1.05), r=R.hr * 0.95, scale=(1, 1, 0.4), seg=10, rings=6)
    for s in (1, -1):
        T.sphere(f"Flap{s}", "Kopuk", (ox + s * R.hr * 0.9, 0, R.hz + R.hr * 0.1), r=R.hr * 0.3, scale=(0.6, 1, 1.2), seg=8, rings=5)
    B.beard(R, "SacGri", length=0.9, width=0.8)
    h = R.hand[1]
    T.box("Lamp", "Pirinc", h + V((0, -0.04, -0.12)), (0.08, 0.08, 0.12), bevel=0.01)
    T.sphere("LampFire", "Ates", h + V((0, -0.08, -0.12)), r=0.03, seg=6, rings=4)
    return R


def c33_bahar(ox):
    """Bahar Çiçekçisi: pembe elbise, krem önlük, çiçek tacı, kolunda çiçek sepeti."""
    R = B.body(ox, "M", "Ten1", "Pembe", "Pembe", "KoyuMese", sleeves="short")
    B.skirt(R, "Pembe", bottom=0.08, flare=1.3)
    B.apron(R, "Krem", top=0.1, bottom=0.12, width=0.7)
    B.hair(R, "SacKahve", rot=(-22, 0, 0))
    T.sphere("HairBack", "SacKahve", (ox, R.hr * 0.6, R.hz - R.hr * 0.4), r=1, scale=(R.hr * 0.6, R.hr * 0.35, R.hr * 0.8), seg=8, rings=6)
    T.torus("Crown", "Yaprak", (ox, 0, R.hz + R.hr * 0.55), major=R.hr * 0.95, minor=0.014, rot=(-12, 0, 0), seg=16)
    for i, c in enumerate(("Altin", "Krem", "Lila", "Altin", "Krem", "Lila")):
        a = math.radians(-160 + i * 28)
        T.sphere(f"CrownFl{i}", c, (ox + math.cos(a) * R.hr * 0.95, math.sin(a) * R.hr * 0.95, R.hz + R.hr * 0.6), r=0.026, seg=6, rings=4)
    h = R.hand[1]
    T.cyl("Basket", "Mese", h + V((0, -0.06, -0.05)), r=0.08, r2=0.1, depth=0.1, verts=10)
    T.torus("BasketHandle", "KoyuMese", h + V((0, -0.06, 0.0)), major=0.08, minor=0.01, rot=(0, 90, 0), seg=10)
    for i, c in enumerate(("Kirmizi", "Altin", "Lila")):
        T.sphere(f"BasketFl{i}", c, h + V(((i - 1) * 0.05, -0.06, 0.02)), r=0.035, seg=6, rings=4)
    return R


SEASON_CUSTOMERS = [(31, "HasatKoylusu", "Hasat Köylüsü", c31_hasat), (32, "KisGezgini", "Kış Gezgini", c32_kis),
                    (33, "BaharCicekcisi", "Bahar Çiçekçisi", c33_bahar)]


# ------------------------------------------------------------------ kostümler: Barlo
BARLO_HEAD = 0.84
BARLO_HAT = ("Bandana", "Knot", "KnotTailA", "KnotTailB")


def cos_barlo_korsan(ox=0.0):
    """Korsan: üç köşeli şapka, göz bandı, bordo kaptan ceketi, kırmızı kuşak."""
    unjoined(M.build_barlo, ox)
    drop(*BARLO_HAT, "Apron", "ApronBib", "Towel", "Belt", "Buckle")
    hz = BARLO_HEAD
    T.sphere("Coat", "Bordo", (ox, 0, 0.44), r=1, scale=(0.275, 0.228, 0.245), seg=14, rings=10)
    T.box("ShirtFront", "Krem", (ox, -0.215, 0.5), (0.11, 0.03, 0.22), bevel=0.01)
    for s in (1, -1):
        for i in range(3):
            T.sphere(f"CoatBtn{s}{i}", "Altin", (ox + s * 0.09, -0.218, 0.38 + i * 0.08), r=0.016, seg=6, rings=4)
    T.cyl("CoatSkirt", "Bordo", (ox, 0.0, 0.26), r=0.27, r2=0.25, depth=0.16, scale=(1, 0.85, 1), verts=14)
    T.torus("Sash", "Kirmizi", (ox, 0, 0.34), major=0.258, minor=0.03, scale=(1, 0.86, 1), seg=16)
    T.sphere("SashKnot", "Kirmizi", (ox + 0.17, -0.17, 0.33), r=0.04, seg=6, rings=4)
    T.dome("Crown", "Lacivert", (ox, 0.01, hz + 0.08), r=0.2, scale=(1, 1, 0.75))
    T.cyl("Tricorn", "Lacivert", (ox, 0.0, hz + 0.1), r=0.33, depth=0.04, rot=(0, 0, -90), verts=3)
    T.torus("TricornTrim", "Altin", (ox, 0.0, hz + 0.12), major=0.33, minor=0.012, rot=(0, 0, -90), seg=3, mseg=4)
    T.sphere("Skull", "Krem", (ox, -0.19, hz + 0.16), r=0.035, seg=6, rings=4)
    T.torus("PatchBand", "Goz", (ox, 0, hz + 0.05), major=0.213, minor=0.007, rot=(-12, 18, 0), seg=16)
    T.cyl("Patch", "Goz", (ox + 0.08, -0.198, hz + 0.03), r=0.04, depth=0.012, rot=(90, 0, 0), verts=8)


def cos_barlo_sovalye(ox=0.0):
    """Şövalye: miğfer ve kırmızı sorguç, göğüs zırhı, omuzluk, kırmızı arma önlüğü."""
    unjoined(M.build_barlo, ox)
    drop(*BARLO_HAT, "Apron", "ApronBib", "Towel")
    hz = BARLO_HEAD
    T.dome("Helmet", "Tas", (ox, 0.01, hz + 0.02), r=0.232, scale=(1, 1, 1.05))
    T.torus("HelmetRim", "Pirinc", (ox, 0.01, hz + 0.03), major=0.23, minor=0.016, seg=16)
    T.box("HelmetCrest", "Pirinc", (ox, 0.01, hz + 0.2), (0.03, 0.4, 0.06), rot=(0, 0, 0))
    for i in range(4):
        T.sphere(f"Plume{i}", "Kirmizi", (ox, 0.05 + i * 0.05, hz + 0.3 - i * 0.03), r=0.05 - i * 0.006, seg=6, rings=4)
    T.sphere("Breast", "Tas", (ox, -0.02, 0.5), r=1, scale=(0.27, 0.225, 0.2), seg=14, rings=10)
    T.box("Tabard", "Kirmizi", (ox, -0.225, 0.36), (0.24, 0.02, 0.34), bevel=0.006)
    T.sphere("TabardLion", "Altin", (ox, -0.24, 0.42), r=0.045, scale=(1, 0.4, 1.1), seg=8, rings=5)
    for s in (1, -1):
        T.dome(f"Pauldron{s}", "Tas", (ox + s * 0.235, 0, 0.58), r=0.1, scale=(1.1, 1, 0.8), rot=(0, s * 25, 0))
        T.cyl(f"Gauntlet{s}", "Tas", (ox + s * 0.39, -0.03, 0.29), r=0.068, depth=0.08, verts=8)


def cos_barlo_kis(ox=0.0):
    """Kış Festivali: beyaz kürklü kırmızı kış şapkası, ponpon, yeşil-krem atkı, kürk manşetler."""
    unjoined(M.build_barlo, ox)
    drop(*BARLO_HAT)
    hz = BARLO_HEAD
    T.torus("FurBand", "Kopuk", (ox, 0.01, hz + 0.08), major=0.205, minor=0.045, rot=(-8, 0, 0), seg=16, mseg=6)
    T.cyl("WinterHat", "Kirmizi", (ox, 0.04, hz + 0.26), r=0.21, r2=0.05, depth=0.34, rot=(-22, 0, 0), verts=12)
    T.sphere("Pompom", "Kopuk", (ox, 0.12, hz + 0.43), r=0.06, seg=8, rings=6)
    T.torus("Scarf", "Yesil", (ox, 0, 0.64), major=0.16, minor=0.05, seg=14)
    for i in range(3):
        T.box(f"ScarfTail{i}", "Yesil" if i % 2 == 0 else "Krem", (ox - 0.1, -0.2, 0.56 - i * 0.07), (0.08, 0.035, 0.07))
    for s in (1, -1):
        T.torus(f"FurCuff{s}", "Kopuk", (ox + s * 0.35, -0.012, 0.4), major=0.06, minor=0.022, rot=(0, s * 30, 0), seg=10)
    T.sphere("Bell", "Altin", (ox, -0.235, 0.52), r=0.03, seg=8, rings=5)


def cos_barlo_hasat(ox=0.0):
    """Hasat Çiftçisi: hasır şapka, mavi tulum ve askıları, ağızda başak, kırmızı fular."""
    unjoined(M.build_barlo, ox)
    drop(*BARLO_HAT, "Apron", "ApronBib", "Towel")
    hz = BARLO_HEAD
    T.cyl("StrawBrim", "SacSari", (ox, 0.0, hz + 0.11), r=0.37, depth=0.025, rot=(-6, 0, 0), verts=16)
    T.cyl("StrawCrown", "SacSari", (ox, 0.01, hz + 0.19), r=0.19, r2=0.16, depth=0.15, rot=(-6, 0, 0), verts=12)
    T.cyl("StrawBand", "Kirmizi", (ox, 0.01, hz + 0.15), r=0.195, depth=0.04, rot=(-6, 0, 0), verts=12)
    T.cyl("Overalls", "Mavi", (ox, -0.19, 0.3), r=0.21, r2=0.18, depth=0.36, scale=(1, 0.2, 1), verts=12)
    T.box("OverallBib", "Mavi", (ox, -0.2, 0.53), (0.21, 0.03, 0.17), rot=(-18, 0, 0), bevel=0.01)
    T.box("BibPatch", "Bez", (ox + 0.05, -0.222, 0.53), (0.07, 0.01, 0.06), rot=(-18, 0, 0))
    for s in (1, -1):
        T.box(f"Strap{s}", "Mavi", (ox + s * 0.08, -0.12, 0.64), (0.04, 0.18, 0.03), rot=(-30, 0, 0))
        T.sphere(f"StrapBtn{s}", "Pirinc", (ox + s * 0.08, -0.218, 0.6), r=0.014, seg=6, rings=4)
    T.torus("Kerchief", "Kirmizi", (ox, 0, 0.66), major=0.13, minor=0.025, seg=12)
    T.limb("Straw", "SacSari", (ox + 0.04, -0.215, hz - 0.11), (ox + 0.2, -0.3, hz - 0.03), r1=0.008, verts=4)
    T.sphere("StrawEar", "Bal", (ox + 0.21, -0.305, hz - 0.025), r=0.018, scale=(1, 1, 1.8), rot=(0, 50, 0), seg=6, rings=4)


def cos_barlo_kral(ox=0.0):
    """Kral'ın Dostu: mor kadife bere ve tüy, mor pelerin, kakum yaka, altın madalyon zinciri."""
    unjoined(M.build_barlo, ox)
    drop(*BARLO_HAT, "Towel")
    hz = BARLO_HEAD
    T.sphere("Beret", "Mor", (ox + 0.02, 0.01, hz + 0.16), r=0.24, scale=(1.08, 1.05, 0.4), rot=(0, -10, 0), seg=14, rings=8)
    T.sphere("Brooch", "Altin", (ox - 0.12, -0.17, hz + 0.18), r=0.028, seg=6, rings=4)
    T.sphere("Feather", "Krem", (ox - 0.2, 0.0, hz + 0.3), r=1, scale=(0.03, 0.02, 0.17), rot=(0, -40, 0), seg=8, rings=5)
    T.box("Cape", "Mor", (ox, 0.21, 0.4), (0.56, 0.03, 0.56), rot=(8, 0, 0), bevel=0.01)
    T.torus("Ermine", "Kopuk", (ox, 0.0, 0.64), major=0.2, minor=0.05, scale=(1, 0.95, 1), seg=16, mseg=6)
    for i in range(6):
        a = math.radians(i * 60 + 15)
        T.sphere(f"ErmineDot{i}", "Goz", (ox + math.cos(a) * 0.2, math.sin(a) * 0.19, 0.67), r=0.01, seg=5, rings=3)
    T.torus("Chain", "Altin", (ox, -0.08, 0.57), major=0.15, minor=0.011, rot=(35, 0, 0), seg=16)
    T.cyl("Medal", "Altin", (ox, -0.225, 0.47), r=0.045, depth=0.015, rot=(90, 0, 0), verts=10)
    T.sphere("MedalGem", "Kirmizi", (ox, -0.234, 0.47), r=0.018, seg=6, rings=4)


# ------------------------------------------------------------------ kostümler: personel
def cos_pip_yelek(ox=0.0):
    """Garson Pip · Zarif Yelek: bordo yelek, altın düğmeler, papyon, taranmış saç (kasket yok)."""
    unjoined(M.build_pip, ox)
    drop("Vest", "Button_0", "Button_1", "Strap", "Cap", "CapBrim", "CapButton", "HairTuftA", "HairTuftB")
    hz = 0.6
    T.cyl("Vest", "Bordo", (ox, 0, 0.37), r=0.153, r2=0.126, depth=0.14, verts=12)
    for i in range(3):
        T.sphere(f"VestBtn{i}", "Altin", (ox, -0.148 + i * 0.006, 0.33 + i * 0.04), r=0.012, seg=6, rings=4)
    for s in (1, -1):
        T.box(f"Bow{s}", "Lacivert", (ox + s * 0.03, -0.12, 0.45), (0.05, 0.02, 0.04), rot=(0, 0, s * -20), bevel=0.006)
    T.sphere("BowKnot", "Lacivert", (ox, -0.125, 0.45), r=0.014, seg=6, rings=4)
    T.torus("WatchChain", "Altin", (ox + 0.07, -0.13, 0.36), major=0.04, minor=0.005, rot=(80, 0, 0), seg=10)
    T.dome("NeatHair", "SacKizil", (ox, 0.01, hz + 0.02), r=0.2, scale=(1, 1, 0.92), rot=(-20, 0, 0))
    T.box("Part", "SacKizil", (ox - 0.06, -0.12, hz + 0.15), (0.12, 0.05, 0.04), rot=(-40, 0, -15))


def cos_gus_usta(ox=0.0):
    """Barmen Gus · Şık Barmen: silindir şapka, bordo yelek, altın tek gözlük."""
    R = S.st_barmen(ox)
    drop("Vest", "Btn_0", "Btn_1")
    B.vest(R, "Bordo", buttons="Altin")
    B.brim_hat(R, "Lacivert", brim=1.45, crown_r=0.72, crown_h=1.1, z=0.72, band="Kirmizi", crown_top=1.0)
    T.torus("Monocle", "Altin", (ox + R.hr * 0.4, -R.hr * 0.99, R.hz + R.hr * 0.14), major=R.hr * 0.22, minor=0.008, rot=(90, 0, 0), seg=10)
    T.limb("MonocleChain", "Altin", (ox + R.hr * 0.55, -R.hr * 0.98, R.hz), (ox + R.rx * 0.5, -R.ry * 0.95, R.tz + R.rz * 0.4), r1=0.004, verts=4)


def cos_ayla_sef(ox=0.0):
    """Aşçı Ayla · Büyük Şef: kocaman şef şapkası (altın bant), kırmızı fular, altın yıldız rozet."""
    R = K.st_asci(ox)
    drop("ChefBand", "ChefPuff*")
    T.cyl("TallHat", "Kopuk", (ox, 0.02, R.hz + R.hr * 1.3), r=R.hr * 0.88, r2=R.hr * 1.0, depth=R.hr * 1.4, verts=14)
    T.cyl("HatBand", "Altin", (ox, 0.02, R.hz + R.hr * 0.7), r=R.hr * 0.9, depth=R.hr * 0.18, verts=14)
    for i, (x, y) in enumerate(((0, 0), (-0.55, 0.1), (0.55, 0.1), (0, 0.5), (0, -0.45))):
        T.sphere(f"Puff{i}", "Kopuk", (ox + x * R.hr, 0.02 + y * R.hr, R.hz + R.hr * 2.05), r=R.hr * 0.62, seg=10, rings=7)
    T.torus("Kerchief", "Kirmizi", (ox, 0, R.tz + R.rz * 0.88), major=R.rx * 0.55, minor=0.025, seg=12)
    T.sphere("StarBadge", "Altin", (ox - R.rx * 0.45, -R.ry * 0.98, R.tz + R.rz * 0.4), r=0.035, scale=(1, 0.35, 1), seg=5, rings=3)


def cos_nana_bahar(ox=0.0):
    """Temizlikçi Nana · Bahar Çiçeği: çiçek tacı, pembe şal (eşarp yerine)."""
    R = S.st_temizlikci(ox)
    drop("Scarf", "ScarfKnot")
    B.hair(R, "SacGri", rot=(-20, 0, 0))
    T.torus("Crown", "Yaprak", (ox, 0, R.hz + R.hr * 0.6), major=R.hr * 0.98, minor=0.016, rot=(-12, 0, 0), seg=16)
    cols = ("Pembe", "Altin", "Lila", "Krem")
    for i in range(8):
        a = math.radians(-180 + i * 22.5)
        T.sphere(f"CrownFl{i}", cols[i % 4], (ox + math.cos(a) * R.hr * 0.98, math.sin(a) * R.hr * 0.98, R.hz + R.hr * 0.65), r=0.032, seg=6, rings=4)
    T.torus("Shawl", "Pembe", (ox, 0, R.tz + R.rz * 0.8), major=R.rx * 0.75, minor=0.04, scale=(1, R.ry / R.rx, 1), seg=14)
    T.sphere("ShawlKnot", "Pembe", (ox, -R.ry * 0.85, R.tz + R.rz * 0.6), r=0.04, seg=6, rings=4)


COSTUMES = [
    ("Barlo_Korsan", "Korsan", cos_barlo_korsan), ("Barlo_Sovalye", "Şövalye", cos_barlo_sovalye),
    ("Barlo_Kis", "Kış Festivali", cos_barlo_kis), ("Barlo_Hasat", "Hasat Çiftçisi", cos_barlo_hasat),
    ("Barlo_Kral", "Kral'ın Dostu", cos_barlo_kral), ("Pip_Yelek", "Pip · Zarif Yelek", cos_pip_yelek),
    ("Gus_Usta", "Gus · Şık Barmen", cos_gus_usta), ("Ayla_Sef", "Ayla · Büyük Şef", cos_ayla_sef),
    ("Nana_Bahar", "Nana · Bahar Çiçeği", cos_nana_bahar),
]

SEASON_DRINKS = [("ElmaSarabi", "Elma Şarabı", d_elma_sarabi), ("SicakSarap", "Baharatlı Sıcak Şarap", d_sicak_sarap),
                 ("CicekSerbeti", "Çiçek Şerbeti", d_cicek_serbeti)]
STALLS = [("SezonHasat", "Hasat Tezgâhı", s_sezon_hasat), ("SezonKis", "Kış Tezgâhı", s_sezon_kis), ("SezonBahar", "Bahar Tezgâhı", s_sezon_bahar)]
SEASON_SETS = [("Hasat_Yigin", hasat_yigin), ("Hasat_Kucuk", hasat_kucuk), ("Kis_Yigin", kis_yigin), ("Kis_Kucuk", kis_kucuk),
               ("Bahar_Yigin", bahar_yigin), ("Bahar_Kucuk", bahar_kucuk)]
SEASON_DECOR = [("KabakFener", "Kabak Fenerleri", x_kabak_fener), ("BasakDemeti", "Başak Demeti", x_basak_demeti),
                ("KardanAdam", "Kardan Adam", x_kardan_adam), ("SusluCam", "Süslü Çam", x_suslu_cam),
                ("CicekAraba", "Çiçek Arabası", x_cicek_araba), ("KusEvi", "Kuş Evi", x_kus_evi)]
EMBLEMS = [("Season_Hasat", emblem_hasat), ("Season_Kis", emblem_kis), ("Season_Bahar", emblem_bahar), ("SeasonToken", token)]


def export_prop(name, build):
    T.reset_scene()
    build()
    obj = T.join_all(name, smooth_angle=35)
    print(f"[{name}] üçgen: {T.tri_count(obj)}")
    T.save_blend(os.path.join(MODELS, "Season", f"{name}.blend"))
    T.export_fbx(obj, os.path.join(FBX, "props", f"{name}.fbx"))


def export_char(name, build):
    T.reset_scene()
    build()
    obj = T.join_all(name)
    tris = T.tri_count(obj)
    print(f"[{name}] üçgen: {tris}{'' if tris <= 3500 else '  <-- BÜTÇE'}")
    T.save_blend(os.path.join(MODELS, "characters", f"{name}.blend"))
    T.export_fbx(obj, os.path.join(FBX, f"{name}.fbx"))


def sheet(path, builders, spacing, target_z, dist, pitch=14, res=(2400, 900), labels=None):
    T.reset_scene()
    n = len(builders)
    for i, b in enumerate(builders):
        x = (i - (n - 1) / 2) * spacing
        b(x)
        T.join_all(f"PR_sheet_{i}")
        if labels:
            T.label(labels[i], (x, -0.75, 0.02), size=0.1)
    T.setup_render_stage(floor_size=60, floor_color="KoyuTas")
    width = n * spacing + 0.6
    dist = max(dist, (width / 2) / math.tan(math.radians(20)) + 1.0)
    T.render_from(path, target=(0, 0, target_z), pitch=pitch, yaw=0, dist=dist, fov=40, res=res)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    only = argv[0] if argv else ""
    if only in ("", "models"):
        for key, _, fn in SEASON_DRINKS:
            export_prop(f"PR_Drink_{key}", lambda fn=fn: fn(0, 0))
        for key, _, fn in STALLS:
            for t in (1, 2, 3):
                export_prop(f"PR_Station_{key}_T{t}", lambda fn=fn, t=t: fn(0, 0, t))
        for key, fn in SEASON_SETS:
            export_prop(f"PR_Season_{key}", lambda fn=fn: fn(0, 0))
        for key, _, fn in SEASON_DECOR:
            export_prop(f"PR_Decor_{key}", lambda fn=fn: fn(0, 0))
        for num, key, _, fn in SEASON_CUSTOMERS:
            export_char(f"CH_Cust_{num}_{key}", lambda fn=fn: fn(0.0))
        for key, _, fn in COSTUMES:
            export_char(f"CH_Costume_{key}", lambda fn=fn: fn(0.0))
    if only in ("", "icons"):
        for key, _, fn in SEASON_DRINKS:
            RP.shoot(lambda fn=fn: fn(0, 0), key, False)
        for key, _, fn in SEASON_DECOR:
            RP.shoot(lambda fn=fn: fn(0, 0), f"DEC_{key}", False)
        for num, key, _, fn in SEASON_CUSTOMERS:
            RP.shoot(lambda fn=fn: fn(0.0), f"Cust_{num}", True)
        for key, _, fn in COSTUMES:
            RP.shoot(lambda fn=fn: fn(0.0), f"Cos_{key}", True)
        for key, fn in EMBLEMS:
            RP.shoot(lambda fn=fn: fn(0, 0), key, False)
    if only in ("", "sheet"):
        sheet(os.path.join(RENDERS, "Costumes_Sheet.png"),
              [lambda x: unjoined(M.build_barlo, x)] + [(lambda f: (lambda x: f(x)))(fn) for _, _, fn in COSTUMES],
              1.0, 0.6, 7.2, labels=["Barlo"] + [lbl for _, lbl, _ in COSTUMES])
        sheet(os.path.join(RENDERS, "Season_Sheet.png"),
              [(lambda f: (lambda x: f(x, 0, 1)))(fn) for _, _, fn in STALLS]
              + [(lambda f: (lambda x: f(x, 0)))(fn) for _, fn in SEASON_SETS]
              + [(lambda f: (lambda x: f(x, 0)))(fn) for _, _, fn in SEASON_DECOR]
              + [(lambda f: (lambda x: f(x)))(fn) for _, _, _, fn in SEASON_CUSTOMERS],
              1.25, 0.6, 12.5, pitch=22, res=(3000, 900))
    print("BİTTİ")
