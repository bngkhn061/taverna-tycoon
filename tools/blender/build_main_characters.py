"""
Ana karakterler: Barlo (Taverna Ustası) ve Pip (Çırak). Tasarım: CHARACTERS.md §A.
Çalıştırma:
  blender -b --factory-startup --python tools/blender/build_main_characters.py
Çıktılar: assets/models/characters/*.blend, assets/export/fbx/*.fbx, assets/renders/*.png
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import toylib as T  # noqa: E402

MODELS = os.path.join(T.ROOT, "assets", "models", "characters")
FBX = os.path.join(T.ROOT, "assets", "export", "fbx")
RENDERS = os.path.join(T.ROOT, "assets", "renders")


def eyes(ox, z, y, dx, r=0.028, prefix="Eye"):
    for s in (1, -1):
        T.sphere(f"{prefix}_{s}", "Goz", (ox + s * dx, y, z), r=r, scale=(0.8, 0.5, 1.3), seg=8, rings=6)
        T.sphere(f"{prefix}Hi_{s}", "GozParlak", (ox + s * dx + 0.008, y - 0.012, z + 0.014), r=r * 0.3, seg=6, rings=4)


# ------------------------------------------------------------------ Barlo
def build_barlo(ox=0.0):
    skin, hair = "Ten2", "SacKizil"
    for s in (1, -1):
        x = s * 0.11
        T.box(f"Boot_{s}", "KoyuMese", (ox + x, -0.03, 0.05), (0.14, 0.22, 0.1), bevel=0.03)
        T.cyl(f"Leg_{s}", "KoyuMese", (ox + x, 0, 0.17), r=0.065, depth=0.16, verts=8)

    T.sphere("Torso", "Krem", (ox, 0, 0.45), r=1, scale=(0.26, 0.215, 0.23), seg=14, rings=10)
    T.cyl("Apron", "Kirmizi", (ox, -0.19, 0.27), r=0.2, r2=0.17, depth=0.32, scale=(1, 0.18, 1), verts=12)
    T.box("ApronBib", "Kirmizi", (ox, -0.19, 0.54), (0.2, 0.03, 0.15), rot=(-20, 0, 0), bevel=0.01)
    T.torus("Belt", "KoyuMese", (ox, 0, 0.44), major=0.245, minor=0.026, scale=(1, 0.9, 1), seg=16)
    T.box("Buckle", "Bal", (ox, -0.232, 0.44), (0.08, 0.03, 0.065), bevel=0.01)
    T.box("Towel", "Bez", (ox + 0.19, -0.14, 0.33), (0.09, 0.02, 0.16), rot=(0, 0, 35), bevel=0.008)

    # Kollar (A-pozu, sıvalı kollar)
    for s in (1, -1):
        sh, el, wr = (ox + s * 0.22, 0, 0.57), (ox + s * 0.34, -0.01, 0.42), (ox + s * 0.39, -0.03, 0.28)
        T.limb(f"Sleeve_{s}", "Krem", sh, el, r1=0.075, r2=0.065)
        T.limb(f"Cuff_{s}", "Krem", (sh[0] + (el[0] - sh[0]) * 0.8, el[1], sh[2] + (el[2] - sh[2]) * 0.8),
               (el[0] + s * 0.01, el[1], el[2] - 0.02), r1=0.08)
        T.limb(f"Forearm_{s}", skin, el, wr, r1=0.05, r2=0.062)
        T.sphere(f"Hand_{s}", skin, (ox + s * 0.405, -0.04, 0.22), r=0.075, scale=(0.9, 1, 1.05), seg=10, rings=7)
        T.sphere(f"Thumb_{s}", skin, (ox + s * 0.37, -0.1, 0.25), r=0.03, seg=6, rings=4)

    # Kafa
    hz = 0.84
    T.sphere("Head", skin, (ox, 0, hz), r=0.21, scale=(1, 0.95, 0.95), seg=14, rings=10)
    for s in (1, -1):
        T.sphere(f"Ear_{s}", skin, (ox + s * 0.205, 0.01, hz - 0.02), r=0.05, scale=(0.5, 1, 1.1), seg=8, rings=6)
    T.sphere("Nose", skin, (ox, -0.205, hz - 0.04), r=0.055, scale=(1, 0.9, 0.9), seg=10, rings=7)
    eyes(ox, hz + 0.03, -0.18, 0.08)
    for s in (1, -1):
        T.box(f"Brow_{s}", hair, (ox + s * 0.08, -0.185, hz + 0.095), (0.075, 0.025, 0.022), rot=(0, s * -12, 0), bevel=0.006)
        # Kıvrık bıyık: ana kütle + yukarı kıvrılan uç
        T.sphere(f"Stache_{s}", hair, (ox + s * 0.085, -0.205, hz - 0.085), r=1,
                 scale=(0.105, 0.045, 0.04), rot=(0, s * 14, 0), seg=10, rings=6)
        T.sphere(f"StacheTip_{s}", hair, (ox + s * 0.17, -0.195, hz - 0.06), r=0.03, scale=(0.9, 0.8, 1.3), seg=8, rings=5)

    # Bandana + tepede düğüm
    T.dome("Bandana", "Kirmizi", (ox, 0.02, hz + 0.04), r=0.222, scale=(1, 0.98, 1), rot=(-28, 0, 0))
    T.sphere("Knot", "Kirmizi", (ox + 0.02, 0.07, hz + 0.215), r=0.048, seg=8, rings=6)
    T.sphere("KnotTailA", "Kirmizi", (ox + 0.1, 0.08, hz + 0.23), r=1, scale=(0.075, 0.035, 0.028), rot=(0, -30, 10), seg=8, rings=5)
    T.sphere("KnotTailB", "Kirmizi", (ox - 0.06, 0.08, hz + 0.235), r=1, scale=(0.065, 0.032, 0.026), rot=(0, 35, -10), seg=8, rings=5)
    return T.join_all("CH_Main_Barlo")


# ------------------------------------------------------------------ Pip
def build_pip(ox=0.0):
    skin, hair = "Ten1", "SacKizil"
    for s in (1, -1):
        x = s * 0.075
        T.box(f"Shoe_{s}", "Mese", (ox + x, -0.035, 0.04), (0.12, 0.2, 0.08), bevel=0.03)
        for i, c in enumerate(("Krem", "Yesil", "Krem")):  # çizgili çorap
            T.cyl(f"Sock_{s}_{i}", c, (ox + x, 0, 0.1 + i * 0.04), r=0.038, depth=0.04, verts=8)

    T.cyl("Shorts", "Bez", (ox, 0, 0.25), r=0.13, r2=0.14, depth=0.1, verts=12)
    T.sphere("Torso", "Bez", (ox, 0, 0.34), r=1, scale=(0.14, 0.125, 0.14), seg=12, rings=8)
    T.cyl("Vest", "Yesil", (ox, 0, 0.37), r=0.152, r2=0.125, depth=0.13, verts=12)
    for i in range(2):
        T.sphere(f"Button_{i}", "Altin", (ox, -0.145 + i * 0.008, 0.34 + i * 0.05), r=0.014, seg=6, rings=4)
    T.box("Strap", "Bez", (ox + 0.06, -0.142, 0.36), (0.03, 0.02, 0.16), rot=(-12, 0, -8), bevel=0.005)

    # Sırttaki dev tepsi
    T.cyl("Tray", "Tas", (ox, 0.16, 0.4), r=0.25, depth=0.02, rot=(78, 0, 0), verts=16)
    T.torus("TrayRim", "KoyuTas", (ox, 0.162, 0.4), major=0.25, minor=0.015, rot=(78, 0, 0), seg=16, mseg=4)

    for s in (1, -1):
        sh, el, wr = (ox + s * 0.13, 0, 0.41), (ox + s * 0.2, -0.01, 0.32), (ox + s * 0.23, -0.03, 0.24)
        T.limb(f"Sleeve_{s}", "Krem", sh, el, r1=0.045, r2=0.04)
        T.limb(f"Forearm_{s}", skin, el, wr, r1=0.033, r2=0.035)
        T.sphere(f"Hand_{s}", skin, (ox + s * 0.24, -0.035, 0.2), r=0.05, seg=8, rings=6)

    hz = 0.6
    T.sphere("Head", skin, (ox, 0, hz), r=0.19, seg=14, rings=10)
    for s in (1, -1):
        T.sphere(f"Ear_{s}", skin, (ox + s * 0.185, 0.0, hz - 0.02), r=0.045, scale=(0.5, 1, 1.1), seg=8, rings=6)
        for j, (dx, dz) in enumerate(((0.1, -0.03), (0.125, -0.045), (0.085, -0.055))):  # çiller
            T.sphere(f"Freckle_{s}_{j}", "Ten3", (ox + s * dx, -0.165 + abs(dx) * 0.25, hz + dz), r=0.009,
                     scale=(1, 0.4, 1), seg=6, rings=4)
    T.sphere("Nose", skin, (ox, -0.19, hz - 0.03), r=0.035, seg=8, rings=6)
    eyes(ox, hz + 0.02, -0.165, 0.065, r=0.032)

    # Yamuk kasket + siperlik + saç tutamı
    T.dome("Cap", "Mese", (ox + 0.015, 0.0, hz + 0.06), r=0.205, scale=(1.12, 1.1, 0.85), rot=(-8, 8, 0))
    T.cyl("CapBrim", "KoyuMese", (ox + 0.05, -0.18, hz + 0.1), r=0.12, depth=0.016, scale=(1, 0.75, 1),
          rot=(-14, 8, -20), verts=12)
    T.sphere("CapButton", "KoyuMese", (ox + 0.04, 0.0, hz + 0.235), r=0.022, seg=6, rings=4)
    # Kasketin altından fırlamış saç tutamı (kıvrık)
    T.sphere("HairTuftA", hair, (ox - 0.2, -0.06, hz + 0.08), r=0.035, scale=(1.4, 0.8, 0.8), rot=(0, -30, 0), seg=8, rings=5)
    T.sphere("HairTuftB", hair, (ox - 0.245, -0.07, hz + 0.115), r=0.024, seg=6, rings=4)
    return T.join_all("CH_Main_Pip")


# ------------------------------------------------------------------ çalıştır
def produce(builder, name, target_z, dist):
    T.reset_scene()
    obj = builder()
    print(f"[{name}] üçgen sayısı: {T.tri_count(obj)}")
    T.save_blend(os.path.join(MODELS, f"{name}.blend"))
    T.export_fbx(obj, os.path.join(FBX, f"{name}.fbx"))
    T.setup_render_stage()
    T.render_from(os.path.join(RENDERS, f"{name}_front.png"), target=(0, 0, target_z), pitch=8, yaw=-20, dist=dist)


def lineup():
    T.reset_scene()
    build_barlo(ox=-0.38)
    build_pip(ox=0.42)
    T.setup_render_stage()
    T.render_from(os.path.join(RENDERS, "MainDuo_gamecam.png"), target=(0, 0, 0.45), pitch=50, yaw=0, dist=4.2)
    T.render_from(os.path.join(RENDERS, "MainDuo_front.png"), target=(0, 0, 0.5), pitch=10, yaw=-15, dist=3.2)


if __name__ == "__main__":
    produce(build_barlo, "CH_Main_Barlo", 0.55, 2.6)
    produce(build_pip, "CH_Main_Pip", 0.42, 2.0)
    lineup()
    print("BİTTİ")
