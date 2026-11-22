"""
v2.4 Paket 6 — giriş sineması, kral ve kralın hediyesi (GAME_DESIGN.md §18).
  Kral (taçlı, cüppeli, kakum yakalı, asalı):     CH_King
  Saray köşesi (kürsü, taht, kırmızı halı, iki sancak, iki şamdan): PR_Intro_Taht
  Barlo'nun el arabası (çuval, fıçı, sandık, tencere, ip):  PR_Intro_Araba
  Harap han (yıkık duvarlar, çökük çatı, çakılı kapı, eğri tabela, moloz, ot): PR_Intro_Harabe
  Fermanın rulosu (elde, mühürlü):                 PR_Intro_Ferman
  Tabela merdiveni:                                PR_Intro_Merdiven
  Kralın hediyesi, duvar dekoru:                   PR_Decor_KraliyetFermani (arma + açık ferman + mühür + kurdele)
  İkonlar: ICO_King (portre), ICO_DEC_KraliyetFermani
  Önizleme: assets/renders/V24_Sheet.png
Stil ve palet: DESIGN_RULES.md, CHARACTERS.md (tek palet dokusu, oyuncak oranları, düşük çokgen).
Çalıştırma: blender -b --factory-startup --python tools/blender/build_v24.py [-- models|icons|sheet]
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402,F401
import toylib as T  # noqa: E402
import bodylib as B  # noqa: E402
import build_props as P  # noqa: E402
import render_portraits as RP  # noqa: E402
from mathutils import Vector as V  # noqa: E402

at = P.at
MODELS = os.path.join(T.ROOT, "assets", "models")
FBX = os.path.join(T.ROOT, "assets", "export", "fbx")
RENDERS = os.path.join(T.ROOT, "assets", "renders")


# ------------------------------------------------------------------ kral
def crown(cx, cy, z, r, h, tips=5):
    """Altın taç: bant, sivri uçlar (uçlarında top), önde kırmızı, yanlarda mavi taş, içi bordo kadife."""
    T.dome("CrownVelvet", "Bordo", (cx, cy, z + h * 0.35), r=r * 0.92, scale=(1, 1, 0.75))
    T.cyl("CrownBand", "Altin", (cx, cy, z + h * 0.25), r=r, r2=r * 1.06, depth=h * 0.5, verts=14)
    T.torus("CrownRim", "Altin", (cx, cy, z + 0.005), major=r * 1.0, minor=0.012, seg=14)
    for i in range(tips):
        a = math.radians(-90 + i * 360 / tips)
        x, y = cx + math.cos(a) * r * 0.98, cy + math.sin(a) * r * 0.98
        T.cyl(f"CrownTip{i}", "Altin", (x, y, z + h * 0.5 + h * 0.3), r=r * 0.2, r2=0.0, depth=h * 0.62, verts=4)
        T.sphere(f"CrownBall{i}", "Altin", (x, y, z + h * 1.15), r=r * 0.08, seg=6, rings=4)
    T.sphere("CrownGemF", "Kirmizi", (cx, cy - r * 1.05, z + h * 0.25), r=r * 0.15, scale=(1, 0.5, 1), seg=8, rings=5)
    for s in (1, -1):
        T.sphere(f"CrownGem{s}", "Mavi", (cx + s * r * 0.74, cy - r * 0.74, z + h * 0.25), r=r * 0.1, scale=(1, 0.5, 1), seg=6, rings=4)
    T.sphere("CrownTopOrb", "Altin", (cx, cy, z + h * 0.95), r=r * 0.14, seg=8, rings=5)
    T.box("CrownCrossV", "Altin", (cx, cy, z + h * 1.18), (r * 0.06, r * 0.06, r * 0.3))
    T.box("CrownCrossH", "Altin", (cx, cy, z + h * 1.2), (r * 0.2, r * 0.06, r * 0.06))


def king(ox=0.0):
    """Kral: dolgun, beyaz sakallı; mor tunik, yere değen bordo cüppe, kakum yaka ve kenar, altın kemer, taç ve asa."""
    R = B.body(ox, "M", "Ten2", "Mor", "Mor", "KoyuMese", build="fat", nose="big")
    # yere değen cüppe (eteği hafif açılır) ve önde açık tunik şeridi
    T.cyl("Robe", "Bordo", (ox, 0.02, (R.tz + 0.02) / 2), r=R.rx * 1.38, r2=R.rx * 0.98, depth=R.tz - 0.02,
          scale=(1, R.ry / R.rx, 1), verts=16)
    T.box("RobeFront", "Mor", (ox, -R.ry * 1.18, R.tz * 0.5), (R.rx * 0.5, 0.02, R.tz * 0.9), bevel=0.005)
    T.torus("RobeHem", "Kopuk", (ox, 0.02, 0.035), major=R.rx * 1.38, minor=0.03, scale=(1, R.ry / R.rx, 1), seg=14, mseg=4)
    # omuz pelerini (arkaya dökülen) ve kakum yaka
    T.box("Mantle", "Kirmizi", (ox, R.ry * 0.95, (R.sz + 0.05) / 2), (R.rx * 2.4, 0.035, R.sz - 0.02), rot=(5, 0, 0), bevel=0.012)
    T.torus("Ermine", "Kopuk", (ox, 0.0, R.sz + 0.02), major=R.rx * 0.95, minor=0.055, scale=(1, R.ry / R.rx, 1), seg=14, mseg=4)
    for i in range(7):
        a = math.radians(-90 + (i - 3) * 32)
        T.sphere(f"ErmineDot{i}", "Goz", (ox + math.cos(a) * R.rx * 0.95, math.sin(a) * R.ry * 0.95, R.sz + 0.06), r=0.011, seg=5, rings=3)
    B.belt(R, "Altin", buckle="Kirmizi", z=-0.25, minor=0.026)
    # madalyon zinciri
    T.torus("Chain", "Altin", (ox, -R.ry * 0.45, R.tz + R.rz * 0.62), major=R.rx * 0.55, minor=0.009, rot=(38, 0, 0), seg=10)
    T.cyl("Medal", "Altin", (ox, -R.ry * 1.03, R.tz + R.rz * 0.12), r=0.04, depth=0.014, rot=(90, 0, 0), verts=10)
    T.sphere("MedalGem", "Kirmizi", (ox, -R.ry * 1.07, R.tz + R.rz * 0.12), r=0.016, seg=6, rings=4)
    # beyaz saç, sakal, bıyık, kaşlar
    T.dome("Hair", "SacGri", (ox, 0.03, R.hz - R.hr * 0.05), r=R.hr * 1.07, rot=(-28, 0, 0), seg=10, rings=6)
    B.beard(R, "SacGri", length=1.35, width=0.92)
    B.mustache(R, "SacGri", size=1.2, tilt=10)
    for s in (1, -1):
        T.box(f"Brow{s}", "SacGri", (ox + s * R.hr * 0.4, -R.hr * 0.9, R.hz + R.hr * 0.36), (R.hr * 0.36, 0.02, R.hr * 0.09),
              rot=(0, s * -10, 0), bevel=0.008)
    crown(ox, 0.0, R.top - R.hr * 0.22, R.hr * 0.78, R.hr * 0.55)
    # asa (sağ el = −X tarafı ekranda solda) ve küre
    p = R.hand[-1]
    T.limb("Scepter", "Altin", (p.x - 0.01, p.y - 0.02, p.z - 0.2), (p.x - 0.02, p.y - 0.04, p.z + 0.42), r1=0.016)
    T.sphere("ScepterOrb", "Altin", (p.x - 0.02, p.y - 0.04, p.z + 0.46), r=0.05, seg=8, rings=6)
    T.sphere("ScepterGem", "Kirmizi", (p.x - 0.02, p.y - 0.085, p.z + 0.46), r=0.022, seg=6, rings=4)
    T.torus("ScepterRing", "Altin", (p.x - 0.015, p.y - 0.03, p.z + 0.08), major=0.024, minor=0.008, seg=8)
    return R


# ------------------------------------------------------------------ saray köşesi
def banner(name, p, x, y, h=2.3):
    T.cyl(f"{name}_Pole", "KoyuMese", p(x, y, h / 2), r=0.03, depth=h, verts=8)
    T.sphere(f"{name}_Top", "Altin", p(x, y, h + 0.04), r=0.05, seg=8, rings=5)
    T.box(f"{name}_Bar", "Altin", p(x, y - 0.03, h - 0.08), (0.62, 0.03, 0.03))
    T.box(f"{name}_Cloth", "Bordo", p(x, y - 0.04, h - 0.62), (0.56, 0.02, 1.0), bevel=0.006)
    T.cyl(f"{name}_Tail", "Bordo", p(x, y - 0.04, h - 1.2), r=0.28, r2=0.0, depth=0.18, rot=(90, 0, 0), scale=(1, 1, 0.3), verts=3)
    T.box(f"{name}_Stripe", "Altin", p(x, y - 0.055, h - 0.2), (0.56, 0.01, 0.05))
    # sancakta küçük taç
    T.cyl(f"{name}_Crown", "Altin", p(x, y - 0.06, h - 0.62), r=0.12, r2=0.14, depth=0.09, rot=(90, 0, 0), scale=(1, 1, 0.25), verts=10)
    for i, dx in enumerate((-0.1, 0.0, 0.1)):
        T.cyl(f"{name}_CrownTip{i}", "Altin", p(x + dx, y - 0.06, h - 0.52 + (0.03 if dx == 0 else 0)), r=0.03, r2=0.0, depth=0.1, verts=4)


def candle_stand(name, p, x, y):
    T.cyl(f"{name}_Base", "Pirinc", p(x, y, 0.04), r=0.14, r2=0.1, depth=0.08, verts=10)
    T.cyl(f"{name}_Stem", "Pirinc", p(x, y, 0.55), r=0.03, depth=1.0, verts=8)
    T.cyl(f"{name}_Cup", "Pirinc", p(x, y, 1.07), r=0.09, r2=0.05, depth=0.06, verts=10)
    T.cyl(f"{name}_Candle", "Krem", p(x, y, 1.18), r=0.035, depth=0.16, verts=8)
    T.sphere(f"{name}_Flame", "Ates", p(x, y, 1.3), r=0.035, scale=(1, 1, 1.8), seg=6, rings=4)


def throne_set(ox=0.0, oy=0.0):
    """Kürsü (iki basamak) üzerinde taht; önde kırmızı halı; iki yanda sancak ve şamdan. Ön yüz −Y. Taht oturağı z ≈ 0.62."""
    p = at(ox, oy)
    T.box("DaisLow", "Tas", p(0, 0.35, 0.08), (3.0, 2.2, 0.16), bevel=0.02)
    T.box("DaisHigh", "Tas", p(0, 0.6, 0.24), (2.2, 1.4, 0.16), bevel=0.02)
    T.box("DaisTrimL", "Altin", p(0, -0.75, 0.165), (3.0, 0.03, 0.02))
    T.box("DaisTrimH", "Altin", p(0, -0.1, 0.325), (2.2, 0.03, 0.02))
    # halı: kürsüden öne doğru uzanır, altın kenarlı
    T.box("Carpet", "Kirmizi", p(0, -2.4, 0.012), (1.1, 3.5, 0.02))
    T.box("CarpetStep", "Kirmizi", p(0, -0.42, 0.17), (1.1, 0.7, 0.02))
    for s in (1, -1):
        T.box(f"CarpetEdge{s}", "Altin", p(s * 0.54, -2.4, 0.024), (0.04, 3.5, 0.01))
    # taht
    tz = 0.32
    T.box("Seat", "Altin", p(0, 0.62, tz + 0.2), (0.9, 0.75, 0.4), bevel=0.03)
    T.box("Cushion", "Bordo", p(0, 0.6, tz + 0.43), (0.78, 0.66, 0.08), bevel=0.03)
    T.box("Back", "Altin", p(0, 0.95, tz + 0.95), (0.9, 0.12, 1.15), bevel=0.03)
    T.box("BackPad", "Bordo", p(0, 0.88, tz + 0.9), (0.68, 0.04, 0.9), bevel=0.02)
    T.cyl("BackArch", "Altin", p(0, 0.95, tz + 1.55), r=0.45, depth=0.12, rot=(90, 0, 0), scale=(1, 0.55, 1), verts=14)
    for s in (1, -1):
        T.box(f"Arm{s}", "Altin", p(s * 0.47, 0.6, tz + 0.55), (0.1, 0.72, 0.1), bevel=0.02)
        T.sphere(f"ArmKnob{s}", "Altin", p(s * 0.47, 0.22, tz + 0.58), r=0.07, seg=8, rings=6)
        T.sphere(f"BackKnob{s}", "Altin", p(s * 0.45, 0.95, tz + 1.6), r=0.07, seg=8, rings=6)
    T.sphere("BackGem", "Kirmizi", p(0, 0.88, tz + 1.55), r=0.08, scale=(1, 0.5, 1), seg=8, rings=5)
    crown(p(0, 0.86, 0)[0], p(0, 0.86, 0)[1], tz + 1.72, 0.16, 0.14)
    banner("BannerL", p, -1.55, 0.95)
    banner("BannerR", p, 1.55, 0.95)
    candle_stand("CandleL", p, -1.2, -0.55)
    candle_stand("CandleR", p, 1.2, -0.55)


# ------------------------------------------------------------------ el arabası
def handcart(ox=0.0, oy=0.0):
    """Barlo'nun eşyaları: iki tekerlekli el arabası. Kollar −Y yönüne (Barlo önde çeker)."""
    p = at(ox, oy)
    T.box("Bed", "Mese", p(0, 0.15, 0.42), (0.8, 1.0, 0.08), bevel=0.01)
    for s in (1, -1):
        T.box(f"Side{s}", "KoyuMese", p(s * 0.4, 0.15, 0.53), (0.04, 1.0, 0.18))
        T.cyl(f"Wheel{s}", "KoyuMese", p(s * 0.48, 0.3, 0.26), r=0.26, depth=0.06, rot=(0, 90, 0), verts=12)
        T.cyl(f"Hub{s}", "Tas", p(s * 0.52, 0.3, 0.26), r=0.06, depth=0.04, rot=(0, 90, 0), verts=8)
        T.limb(f"Handle{s}", "KoyuMese", p(s * 0.32, -0.3, 0.45), p(s * 0.3, -0.95, 0.55), r1=0.025)
    T.box("Back", "KoyuMese", p(0, 0.65, 0.55), (0.8, 0.04, 0.22))
    T.limb("Grip", "Mese", p(-0.3, -0.95, 0.55), p(0.3, -0.95, 0.55), r1=0.022)
    # yük
    T.cyl("Barrel", "Mese", p(-0.18, 0.4, 0.71), r=0.17, r2=0.15, depth=0.42, verts=10)
    T.torus("BarrelBand", "Tas", p(-0.18, 0.4, 0.78), major=0.165, minor=0.012, seg=10)
    T.box("Crate", "Bal", p(0.18, 0.4, 0.66), (0.32, 0.32, 0.3), bevel=0.01)
    T.box("CrateSlat", "Mese", p(0.18, 0.235, 0.66), (0.32, 0.01, 0.06))
    T.sphere("Sack1", "Bez", p(0.0, -0.05, 0.6), r=1, scale=(0.2, 0.16, 0.16), seg=10, rings=7)
    T.sphere("Sack2", "Bez", p(0.2, 0.05, 0.86), r=1, scale=(0.16, 0.13, 0.13), rot=(0, 20, 0), seg=10, rings=7)
    T.cyl("Pot", "KoyuTas", p(-0.15, 0.0, 0.92), r=0.11, r2=0.09, depth=0.12, verts=10)
    T.torus("PotRim", "KoyuTas", p(-0.15, 0.0, 0.98), major=0.11, minor=0.012, seg=10)
    T.box("Bedroll", "Kirmizi", p(0.0, 0.42, 0.98), (0.5, 0.18, 0.14), bevel=0.05)
    T.torus("Rope", "SacSari", p(0.0, 0.42, 0.98), major=0.12, minor=0.012, rot=(0, 90, 0), seg=10)
    T.cyl("Mug", "Bira", p(0.25, -0.02, 0.73), r=0.04, depth=0.09, verts=8)
    T.sphere("MugFoam", "Kopuk", p(0.25, -0.02, 0.785), r=0.04, scale=(1, 1, 0.4), seg=6, rings=4)


# ------------------------------------------------------------------ harap han
def ruin(ox=0.0, oy=0.0):
    """Harap han: 4 × 3 m, yıkık sıva duvarlar, çökük çatı kirişleri, çapraz tahtalı kapı, kırık pencere, eğri tabela, moloz, ot."""
    p = at(ox, oy)
    T.box("Floor", "KoyuTas", p(0, 0, 0.03), (4.2, 3.2, 0.06))
    # arka duvar (tepe kırık) ve yan duvarlar (biri yarım)
    T.box("BackWall", "Siva", p(0, 1.5, 1.2), (4.0, 0.2, 2.4))
    T.box("BackBreak", "Siva", p(1.3, 1.5, 2.55), (1.2, 0.2, 0.3))
    T.box("LeftWall", "Siva", p(-1.95, 0.0, 1.2), (0.2, 3.0, 2.4))
    T.box("RightWallLow", "Siva", p(1.95, 0.4, 0.75), (0.2, 2.2, 1.5))
    T.box("RightWallStub", "Siva", p(1.95, -1.25, 1.1), (0.2, 0.5, 2.2))
    # ön cephe: sol parça, kapı, sağ parça kırık
    T.box("FrontL", "Siva", p(-1.25, -1.5, 1.2), (1.5, 0.2, 2.4))
    T.box("FrontR", "Siva", p(1.3, -1.5, 0.9), (1.3, 0.2, 1.8))
    T.box("FrontTop", "Siva", p(0.05, -1.5, 2.25), (1.0, 0.2, 0.3))
    for (x, z, w, h) in ((-1.25, 0.25, 1.5, 0.5), (1.3, 0.25, 1.3, 0.5)):
        T.box(f"Stone{x}", "Tas", p(x, -1.53, z), (w, 0.22, h))   # taş taban
    for i, (x, z) in enumerate(((-1.7, 1.6), (-0.9, 0.9), (1.6, 1.2), (-1.4, 2.0))):
        T.box(f"Crack{i}", "KoyuTas", p(x, -1.61, z), (0.25, 0.01, 0.04), rot=(0, 30 + i * 25, 0))
    # kapı: kapalı, çapraz tahtalarla çakılı
    T.box("Door", "KoyuMese", p(0.05, -1.55, 1.0), (0.8, 0.08, 1.9))
    T.box("PlankA", "Mese", p(0.05, -1.62, 1.1), (1.05, 0.04, 0.14), rot=(0, 28, 0))
    T.box("PlankB", "Mese", p(0.05, -1.64, 1.1), (1.05, 0.04, 0.14), rot=(0, -24, 0))
    for i, (x, z) in enumerate(((-0.4, 1.35), (0.5, 0.85), (-0.38, 0.86), (0.48, 1.36))):
        T.sphere(f"Nail{i}", "Tas", p(x, -1.67, z), r=0.02, seg=5, rings=3)
    # kırık pencere (sol cephe)
    T.box("WinFrame", "KoyuMese", p(-1.25, -1.62, 1.4), (0.7, 0.04, 0.6))
    T.box("WinHole", "Goz", p(-1.25, -1.63, 1.4), (0.56, 0.02, 0.46))
    T.box("WinShard", "ElfCiyi", p(-1.42, -1.645, 1.52), (0.18, 0.01, 0.16), rot=(0, 25, 0))
    T.box("WinBoard", "Mese", p(-1.2, -1.66, 1.35), (0.72, 0.03, 0.1), rot=(0, -15, 0))
    # çatı: birkaç kiriş, bir kısmı çökük; yırtık kiremit parçası
    for i, x in enumerate((-1.5, -0.5, 0.5, 1.5)):
        sag = (0, 0, 22, 0)[i]
        T.box(f"Beam{i}", "KoyuMese", p(x, 0.0, 2.45 - (0.5 if i == 2 else 0)), (0.12, 3.2, 0.12), rot=(sag, 0, 0))
    T.box("RoofPatch", "Kirmizi", p(-1.0, 0.6, 2.55), (2.0, 1.4, 0.06), rot=(12, 0, 0))
    T.box("RoofPatch2", "Bordo", p(-1.5, -0.6, 2.5), (1.0, 0.9, 0.06), rot=(-10, 0, 5))
    # eğri, boş tabela
    T.limb("SignArm", "KoyuMese", p(-1.9, -1.6, 2.1), p(-2.6, -1.6, 2.1), r1=0.03)
    T.box("SignBoard", "Mese", p(-2.35, -1.6, 1.75), (0.6, 0.05, 0.35), rot=(0, 18, 0), bevel=0.015)
    T.limb("SignChain", "Tas", p(-2.15, -1.6, 2.08), p(-2.12, -1.6, 1.92), r1=0.008)
    # moloz, kırık fıçı, ot, örümcek ağı
    for i, (x, y, r) in enumerate(((1.6, -1.95, 0.16), (2.0, -1.8, 0.11), (1.3, -2.1, 0.09), (-2.3, -1.9, 0.12), (2.3, -1.2, 0.14))):
        T.sphere(f"Rubble{i}", "Tas", p(x, y, r * 0.5), r=r, scale=(1.2, 1.0, 0.7), seg=6, rings=4)
    T.cyl("BrokenBarrel", "Mese", p(-0.9, -2.0, 0.12), r=0.16, depth=0.4, rot=(90, 0, 30), verts=8)
    for i, (x, y) in enumerate(((-1.8, -1.75), (-0.6, -1.75), (0.8, -1.75), (2.1, -1.55), (1.0, -2.2), (-1.5, -2.1))):
        for k in range(3):
            a = math.radians(k * 120 + i * 17)
            T.cyl(f"Weed{i}_{k}", "Yaprak", p(x + math.cos(a) * 0.04, y + math.sin(a) * 0.04, 0.12), r=0.03, r2=0.0, depth=0.24,
                  rot=(math.cos(a) * 18, math.sin(a) * 18, 0), verts=4)
    T.torus("Web", "Kopuk", p(-1.82, -1.62, 2.2), major=0.16, minor=0.004, rot=(90, 0, 0), seg=8, mseg=3)
    T.torus("Web2", "Kopuk", p(-1.82, -1.62, 2.2), major=0.08, minor=0.004, rot=(90, 0, 0), seg=8, mseg=3)


# ------------------------------------------------------------------ ferman ve merdiven
def scroll_hand(ox=0.0, oy=0.0):
    """Elde tutulan açık ferman: iki ahşap rulo arasında parşömen, kırmızı mühür ve kurdele. Dik durur, ön yüz −Y."""
    p = at(ox, oy)
    T.box("Paper", "Krem", p(0, 0, 0.2), (0.28, 0.01, 0.32))
    for i in range(4):
        T.box(f"Line{i}", "KoyuMese", p(-0.02 + (0.03 if i == 0 else 0), -0.008, 0.31 - i * 0.05), (0.18 - (0.06 if i == 0 else 0), 0.004, 0.012))
    for z in (0.03, 0.37):
        T.cyl(f"Rod{z}", "KoyuMese", p(0, 0, z), r=0.022, depth=0.34, rot=(0, 90, 0), verts=8)
        for s in (1, -1):
            T.sphere(f"Cap{z}{s}", "Altin", p(s * 0.175, 0, z), r=0.028, seg=6, rings=4)
    T.cyl("Seal", "Kirmizi", p(0.06, -0.012, 0.09), r=0.045, depth=0.015, rot=(90, 0, 0), verts=10)
    for s in (1, -1):
        T.box(f"Ribbon{s}", "Kirmizi", p(0.06 + s * 0.02, -0.01, 0.03), (0.025, 0.006, 0.09), rot=(0, s * 15, 0))


def ladder(ox=0.0, oy=0.0, h=2.2):
    p = at(ox, oy)
    for s in (1, -1):
        T.box(f"Rail{s}", "Mese", p(s * 0.2, 0, h / 2), (0.05, 0.05, h), rot=(10, 0, 0))
    for i in range(7):
        z = 0.25 + i * (h - 0.4) / 6
        T.box(f"Rung{i}", "KoyuMese", p(0, -math.tan(math.radians(10)) * (z - h / 2), z), (0.4, 0.04, 0.035), rot=(10, 0, 0))


# ------------------------------------------------------------------ kralın hediyesi (duvar dekoru)
def royal_decree(ox=0.0, oy=0.0):
    """
    Kraliyet Fermanı: duvara asılan bordo arma kalkanı (altın taç), altında açık ferman (parşömen, satırlar, kırmızı mühür),
    iki yanda altın kurdele. Duvara yaslanır (arka yüz y = 0), ön yüz −Y. Genişlik ≈ 0.9 m, yükseklik ≈ 1.25 m, alt kenar z = 0.
    """
    p = at(ox, oy)
    # ahşap arka pano
    T.box("Board", "KoyuMese", p(0, -0.02, 0.62), (0.86, 0.04, 1.2), bevel=0.02)
    T.box("BoardTrim", "Altin", p(0, -0.043, 0.62), (0.9, 0.01, 1.24))
    T.box("BoardFace", "KoyuMese", p(0, -0.048, 0.62), (0.82, 0.01, 1.16))
    # arma kalkanı
    T.box("Shield", "Bordo", p(0, -0.07, 0.95), (0.5, 0.04, 0.34), bevel=0.02)
    T.cyl("ShieldTip", "Bordo", p(0, -0.07, 0.72), r=0.25, r2=0.0, depth=0.18, rot=(0, 0, 0), scale=(1, 0.16, 1), verts=4)
    T.box("ShieldRim", "Altin", p(0, -0.06, 0.95), (0.56, 0.03, 0.4), bevel=0.02)
    T.cyl("ShieldRimTip", "Altin", p(0, -0.06, 0.71), r=0.28, r2=0.0, depth=0.2, scale=(1, 0.12, 1), verts=4)
    T.cyl("CrestCrown", "Altin", p(0, -0.1, 0.94), r=0.11, r2=0.13, depth=0.07, rot=(90, 0, 0), scale=(1, 1, 0.25), verts=10)
    for i, dx in enumerate((-0.09, 0.0, 0.09)):
        T.cyl(f"CrestTip{i}", "Altin", p(dx, -0.1, 1.02 + (0.025 if dx == 0 else 0)), r=0.03, r2=0.0, depth=0.1, verts=4)
        T.sphere(f"CrestBall{i}", "Altin", p(dx, -0.1, 1.08 + (0.025 if dx == 0 else 0)), r=0.015, seg=5, rings=3)
    T.sphere("CrestGem", "Kirmizi", p(0, -0.115, 0.93), r=0.022, seg=6, rings=4)
    # açık ferman
    T.box("Paper", "Krem", p(0, -0.07, 0.38), (0.58, 0.012, 0.44))
    for i in range(5):
        w = 0.42 if i else 0.3
        T.box(f"Line{i}", "KoyuMese", p(0, -0.078, 0.53 - i * 0.06), (w, 0.004, 0.014))
    for z in (0.15, 0.61):
        T.cyl(f"Rod{z}", "Mese", p(0, -0.075, z), r=0.025, depth=0.66, rot=(0, 90, 0), verts=8)
        for s in (1, -1):
            T.sphere(f"RodCap{z}{s}", "Altin", p(s * 0.34, -0.075, z), r=0.032, seg=6, rings=4)
    T.cyl("Seal", "Kirmizi", p(0.17, -0.085, 0.24), r=0.055, depth=0.018, rot=(90, 0, 0), verts=12)
    T.cyl("SealMark", "Bordo", p(0.17, -0.095, 0.24), r=0.03, depth=0.006, rot=(90, 0, 0), verts=8)
    for s in (1, -1):
        T.box(f"SealRibbon{s}", "Kirmizi", p(0.17 + s * 0.025, -0.08, 0.15), (0.03, 0.006, 0.12), rot=(0, s * 14, 0))
    # yanlarda altın kurdele
    for s in (1, -1):
        T.box(f"Sash{s}", "Altin", p(s * 0.36, -0.06, 0.95), (0.07, 0.01, 0.42), rot=(0, s * -12, 0))
        T.cyl(f"SashTail{s}", "Altin", p(s * 0.39, -0.06, 0.7), r=0.05, r2=0.0, depth=0.1, rot=(90, 0, 180), scale=(1, 1, 0.2), verts=3)
    # asma kancası
    T.torus("Hook", "Pirinc", p(0, -0.02, 1.27), major=0.04, minor=0.008, rot=(90, 0, 0), seg=8)


# ------------------------------------------------------------------ çıktılar
PROPS = [("PR_Intro_Taht", throne_set), ("PR_Intro_Araba", handcart), ("PR_Intro_Harabe", ruin),
         ("PR_Intro_Ferman", scroll_hand), ("PR_Intro_Merdiven", ladder), ("PR_Decor_KraliyetFermani", royal_decree)]


def export_prop(name, build):
    T.reset_scene()
    build()
    obj = T.join_all(name, smooth_angle=35)
    print(f"[{name}] üçgen: {T.tri_count(obj)}")
    T.save_blend(os.path.join(MODELS, "V24", f"{name}.blend"))
    T.export_fbx(obj, os.path.join(FBX, "props", f"{name}.fbx"))


def export_char(name, build):
    T.reset_scene()
    build()
    obj = T.join_all(name)
    tris = T.tri_count(obj)
    print(f"[{name}] üçgen: {tris}{'' if tris <= 3500 else '  <-- BÜTÇE'}")
    T.save_blend(os.path.join(MODELS, "characters", f"{name}.blend"))
    T.export_fbx(obj, os.path.join(FBX, f"{name}.fbx"))


def sheet(path):
    T.reset_scene()
    king(-3.4)
    T.join_all("PR_sheet_king")
    throne_set(0.0, 0.6)
    T.join_all("PR_sheet_throne")
    king(0.0)
    T.join_all("PR_sheet_king2")
    handcart(3.4, -0.2)
    T.join_all("PR_sheet_cart")
    royal_decree(-5.0, 1.0)
    T.join_all("PR_sheet_decree")
    ruin(7.6, 0.6)
    T.join_all("PR_sheet_ruin")
    scroll_hand(-2.2, -0.8)
    T.join_all("PR_sheet_scroll")
    for i, (txt, x) in enumerate((("Kral", -3.4), ("Taht", 0.0), ("El arabası", 3.4), ("Kraliyet Fermanı", -5.0), ("Harap han", 7.6), ("Ferman", -2.2))):
        T.label(txt, (x, -2.6, 0.02), size=0.22)
    T.setup_render_stage(floor_size=60, floor_color="KoyuTas")
    T.render_from(path, target=(1.3, 0, 1.0), pitch=16, yaw=0, dist=20.5, fov=40, res=(3000, 1100))


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    only = argv[0] if argv else ""
    if only in ("", "models"):
        export_char("CH_King", lambda: king(0.0))
        for name, fn in PROPS:
            export_prop(name, lambda fn=fn: fn(0.0, 0.0))
    if only in ("", "icons"):
        RP.shoot(lambda: king(0.0), "King", True)
        RP.shoot(lambda: royal_decree(0.0, 0.0), "DEC_KraliyetFermani", False)
    if only in ("", "sheet"):
        sheet(os.path.join(RENDERS, "V24_Sheet.png"))
    print("BİTTİ")
