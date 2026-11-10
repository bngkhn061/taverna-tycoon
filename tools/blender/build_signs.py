"""
v1.2 taverna tabelası kademeleri (onaylı mockup design/ui/ui_v12_akis.html S, 2026-11-10):
  Sv1  PR_Decor_Tabela (build_world.py, tahta levha)
  Sv5  PR_Sign_2  Boyalı tabela: koyu pano, bal rengi çerçeve, kupa resmi
  Sv10 PR_Sign_3  Demir çerçeve: demir direk ve kıvrık kol, lacivert pano, asılı fener
  Sv15 PR_Sign_4  Yaldızlı: bordo pano, altın çerçeve, arma
  Sv20 PR_Sign_5  Görkemli: altın direk, mor pano, kalın altın çerçeve, taç ve mücevher, iki fener
Pano hep aynı yerde ve boyutta (merkez z 1.98, 1.4 × 0.62): taverna adı HUD'dan panonun önüne yazılır. Önü −y.
Çalıştırma: blender -b --factory-startup --python tools/blender/build_signs.py
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
FBX = os.path.join(T.ROOT, "assets", "export", "fbx")
RENDERS = os.path.join(T.ROOT, "assets", "renders")


def base(p, post="KoyuMese", arm="KoyuMese", chain="KoyuTas", foot="KoyuTas"):
    T.box("Post", post, p(-0.95, 0, 1.4), (0.14, 0.14, 2.8), bevel=0.02)
    T.box("Foot", foot, p(-0.95, 0, 0.08), (0.4, 0.4, 0.16), bevel=0.02)
    T.box("Arm", arm, p(-0.2, 0, 2.62), (1.6, 0.1, 0.1), bevel=0.015)
    T.box("Brace", arm, p(-0.8, 0, 2.47), (0.3, 0.08, 0.08), rot=(0, 45, 0))   # kısa: panonun önüne geçmesin
    for sx in (-1, 1):
        T.cyl(f"Chain{sx}", chain, p(sx * 0.55, 0, 2.43), r=0.015, depth=0.3, verts=6)


def board(p, face, frame, frame_w=0.05):
    T.box("Board", face, p(0, 0, 1.98), (1.4, 0.07, 0.62), bevel=0.03)
    T.box("Frame", frame, p(0, 0.01, 1.98), (1.4 + 2 * frame_w, 0.05, 0.62 + 2 * frame_w), bevel=0.02)


def lantern(p, name, x, z):
    T.cyl(f"{name}Hook", "KoyuTas", p(x, -0.14, z + 0.14), r=0.012, depth=0.12, verts=4)
    T.box(f"{name}Cage", "KoyuTas", p(x, -0.14, z), (0.14, 0.14, 0.18), bevel=0.01)
    T.sphere(f"{name}Flame", "Ates", p(x, -0.14, z), r=0.055, seg=6, rings=4)
    T.cyl(f"{name}Cap", "KoyuTas", p(x, -0.14, z + 0.11), r=0.09, r2=0.02, depth=0.06, verts=4, rot=(0, 0, 45))


def sign2(ox, oy):
    """Boyalı tabela: koyu pano, bal rengi çerçeve, sol üstte köpüklü kupa resmi."""
    p = at(ox, oy)
    base(p)
    board(p, "KoyuMese", "Bal", 0.06)
    T.box("MugBody", "Bal", p(-0.84, -0.06, 2.38), (0.16, 0.06, 0.2), bevel=0.02)
    T.box("MugFoam", "Kopuk", p(-0.84, -0.07, 2.5), (0.19, 0.06, 0.06), bevel=0.02)
    T.torus("MugHandle", "Bal", p(-0.73, -0.06, 2.38), major=0.05, minor=0.015, rot=(90, 0, 0), seg=8)


def sign3(ox, oy):
    """Demir çerçeve: demir direk, kol ucunda kıvrım, lacivert pano, direkte asılı fener."""
    p = at(ox, oy)
    base(p, post="KoyuTas", arm="Lacivert", chain="KoyuTas", foot="Tas")
    board(p, "Lacivert", "KoyuTas", 0.06)
    T.torus("Scroll", "Lacivert", p(0.62, 0, 2.75), major=0.1, minor=0.02, rot=(90, 0, 0), seg=10)
    T.torus("Scroll2", "Lacivert", p(-0.5, 0, 2.78), major=0.08, minor=0.018, rot=(90, 0, 0), seg=10)
    for sx in (-1, 1):
        T.sphere(f"Rivet{sx}", "Tas", p(sx * 0.7, -0.05, 2.27), r=0.03, seg=6, rings=4)
    lantern(p, "LanternA", -0.95, 1.6)


def sign4(ox, oy):
    """Yaldızlı: bordo pano, altın çerçeve ve kol süsü, panonun üstünde arma."""
    p = at(ox, oy)
    base(p, post="KoyuTas", arm="Altin", chain="Altin")
    board(p, "Bordo", "Altin", 0.07)
    T.box("ShieldBack", "Mavi", p(0, -0.04, 2.47), (0.26, 0.05, 0.28), bevel=0.04)
    T.box("ShieldRim", "Altin", p(0, -0.02, 2.47), (0.3, 0.04, 0.32), bevel=0.04)
    T.sphere("ShieldGem", "Altin", p(0, -0.08, 2.47), r=0.045, seg=6, rings=4)
    T.sphere("PostKnob", "Altin", p(-0.95, 0, 2.86), r=0.09, seg=8, rings=6)


def sign5(ox, oy):
    """Görkemli: altın direk ve kol, mor pano, kalın altın çerçeve, taç + mücevherler, iki fener."""
    p = at(ox, oy)
    base(p, post="Altin", arm="Altin", chain="Altin", foot="Altin")
    board(p, "Mor", "Altin", 0.09)
    T.box("CrownBase", "Altin", p(0, -0.05, 2.45), (0.42, 0.06, 0.1), bevel=0.02)
    for k in range(5):
        x = -0.18 + k * 0.09
        h = 0.16 if k % 2 == 0 else 0.11
        T.cyl(f"CrownTip{k}", "Altin", p(x, -0.05, 2.5 + h / 2), r=0.035, r2=0.0, depth=h, verts=4)
        if k % 2 == 0:
            T.sphere(f"CrownBall{k}", "Altin", p(x, -0.05, 2.5 + h + 0.01), r=0.022, seg=6, rings=4)
    T.sphere("Ruby", "Kirmizi", p(0, -0.1, 2.45), r=0.045, seg=6, rings=4)
    for sx in (-1, 1):
        T.sphere(f"CrownGem{sx}", "ElfCiyi", p(sx * 0.13, -0.09, 2.45), r=0.028, seg=6, rings=4)
    for sx in (-1, 1):
        T.sphere(f"Gem{sx}", "ElfCiyi", p(sx * 0.74, -0.08, 1.98), r=0.04, seg=6, rings=4)
    T.sphere("PostKnob", "Altin", p(-0.95, 0, 2.88), r=0.1, seg=8, rings=6)
    lantern(p, "LanternA", -0.95, 1.6)


ITEMS = [("PR_Sign_2", sign2), ("PR_Sign_3", sign3), ("PR_Sign_4", sign4), ("PR_Sign_5", sign5)]


def export(name, build):
    T.reset_scene()
    build()
    obj = T.join_all(name, smooth_angle=35)
    print(f"[{name}] üçgen: {T.tri_count(obj)}")
    T.save_blend(os.path.join(MODELS, "Decor", f"{name}.blend"))
    T.export_fbx(obj, os.path.join(FBX, "props", f"{name}.fbx"))


if __name__ == "__main__":
    for name, fn in ITEMS:
        export(name, lambda fn=fn: fn(0, 0))
    import build_world as W  # noqa: E402
    T.reset_scene()
    W.tabela(-5.2, 0)
    sign2(-2.6, 0)
    sign3(0.0, 0)
    sign4(2.6, 0)
    sign5(5.2, 0)
    T.join_all("SignSheet")
    T.setup_render_stage(floor_size=40, floor_color="KoyuTas")
    T.render_from(os.path.join(RENDERS, "Signs_Tiers.png"), target=(0.0, 0, 1.6), pitch=10, yaw=0, dist=15.5, fov=46, res=(2400, 900))
    print("BİTTİ")
