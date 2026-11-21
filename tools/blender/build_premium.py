"""
v1.0 Hazine (gerçek para) içeriği, onaylı mockup design/ui/ui_v10_hazine.html (2026-11-10):
  CH_Staff_09_Lirael (Elf garson) · CH_Staff_10_Ignis (Ejder aşçı)
  PR_Decor_KristalAvize · PR_Decor_EjderHeykeli · PR_Decor_AltinFici · PR_Decor_TabelaCerceve
  ikonlar: ICO_Staff_Lirael, ICO_Staff_Ignis, ICO_DEC_KristalAvize, ICO_DEC_EjderHeykeli, ICO_DEC_AltinFici,
           ICO_Prem_AltinTema, ICO_Prem_Cerceve · önizleme assets/renders/Props_Premium.png
Altın tema (Zemin_Altin, Duvar_Altin, Pencere_Altin) build_props.py THEMES içinde: `-- Theme`.
Çalıştırma: blender -b --factory-startup --python tools/blender/build_premium.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import toylib as T  # noqa: E402
import bodylib as B  # noqa: E402
import build_props as P  # noqa: E402
import build_staff as S  # noqa: E402
import render_portraits as RP  # noqa: E402
from mathutils import Vector as V  # noqa: E402

at = P.at
MODELS = os.path.join(T.ROOT, "assets", "models")
FBX = os.path.join(T.ROOT, "assets", "export", "fbx")
RENDERS = os.path.join(T.ROOT, "assets", "renders")


# ------------------------------------------------------------------ efsanevi personel
def st_lirael(ox):
    """Elf garson: uzun ve ince, sivri kulak, uzun sarı saç, yeşil yelek + kırmızı önlük (personel kuralı), 4 bardaklı tepsi."""
    R = B.body(ox, "M", "TenElf", "Krem", "Yesil", "KoyuMese", build="thin", ears="pointy", nose="small")
    B.vest(R, "Yesil", buttons="Altin")
    B.apron(R, "Kirmizi", top=-0.3, bottom=0.12, width=0.75)
    B.hair(R, "SacSari", rot=(-15, 0, 0), s=1.08)
    T.sphere("HairTail", "SacSari", (ox, R.hr * 0.75, R.hz - R.hr * 0.4), r=1, scale=(R.hr * 0.38, R.hr * 0.3, R.hr * 0.9), seg=8, rings=6)
    T.torus("Circlet", "Altin", (ox, 0, R.hz + R.hr * 0.55), major=R.hr * 0.98, minor=0.012, rot=(-12, 0, 0), seg=16)
    T.sphere("Gem", "ElfCiyi", (ox, -R.hr * 0.98, R.hz + R.hr * 0.42), r=0.022, seg=6, rings=4)
    S.tray(R, 1, 4)
    return R


def st_ignis(ox):
    """Ejder aşçı: kırmızı pullu ten, burun-ağız çıkıntısı, altın boynuzlar, şef şapkası, kırmızı önlük, kepçe."""
    R = B.body(ox, "M", "Bordo", "Krem", "KoyuTas", "KoyuMese", build="fat", ears="none", nose="none")
    T.sphere("Snout", "Bordo", (ox, -R.hr * 0.95, R.hz - R.hr * 0.25), r=1, scale=(R.hr * 0.5, R.hr * 0.45, R.hr * 0.32), seg=8, rings=5)
    for s in (1, -1):
        T.sphere(f"Nostril{s}", "Goz", (ox + s * R.hr * 0.16, -R.hr * 1.36, R.hz - R.hr * 0.18), r=0.012, seg=5, rings=3)
        T.cyl(f"Horn{s}", "Altin", (ox + s * R.hr * 0.55, R.hr * 0.1, R.hz + R.hr * 0.75), r=R.hr * 0.16, r2=0.0, depth=R.hr * 0.7,
              rot=(-20, s * 25, 0), verts=6)
    for i in range(3):
        T.cyl(f"Spike{i}", "Altin", (ox, R.hr * 0.85, R.hz + R.hr * (0.3 - i * 0.45)), r=0.03, r2=0.0, depth=0.08, rot=(-90, 0, 0), verts=4)
    B.apron(R, "Kirmizi", top=0.25, bottom=0.1, width=0.85)
    T.cyl("ChefBand", "Kopuk", (ox, 0.04, R.hz + R.hr * 0.7), r=R.hr * 0.8, depth=R.hr * 0.3, verts=12)
    for i, (x, y) in enumerate(((0, 0), (-0.35, 0.1), (0.35, 0.1))):
        T.sphere(f"ChefPuff{i}", "Kopuk", (ox + x * R.hr, 0.04 + y * R.hr, R.hz + R.hr * 1.15), r=R.hr * 0.5, seg=8, rings=6)
    T.cyl("Tail", "Bordo", (ox, R.ry + 0.12, R.lh * 0.7), r=0.07, r2=0.02, depth=0.4, rot=(60, 0, 0), verts=7)
    h = R.hand[1]
    T.limb("Ladle", "Mese", h + V((0, -0.02, -0.05)), h + V((0, -0.05, 0.25)), r1=0.014, verts=5)
    T.sphere("Flame", "Ates", h + V((0, -0.05, 0.3)), r=0.05, scale=(1, 1, 1.5), seg=6, rings=4)
    return R


# ------------------------------------------------------------------ premium dekor
def kristal_avize(ox, oy):
    """Kristal avize (tavana asılı; dekor yuvası y = 0.9 üstüne oturur): altın halka, kristal damlalar, mumlar.
    v2.0 (madde 14): daha küçük ve yüksek — çap 0.56 m (eskiden 0.76 m), gövde 1.45–1.9 m (dünyada ≈ 2.35–2.8 m)."""
    p = at(ox, oy)
    T.cyl("Chain", "Altin", p(0, 0, 1.9), r=0.007, depth=0.3, verts=5)
    T.torus("RingTop", "Altin", p(0, 0, 1.7), major=0.13, minor=0.012, seg=14)
    T.torus("RingBig", "Altin", p(0, 0, 1.6), major=0.27, minor=0.016, seg=18)
    for i in range(3):
        a = math.radians(i * 120)
        T.limb(f"Arm{i}", "Altin", p(0, 0, 1.76), p(math.cos(a) * 0.27, math.sin(a) * 0.27, 1.6), r1=0.006, verts=4)
    for i in range(8):
        a = math.radians(i * 45)
        x, y = math.cos(a) * 0.27, math.sin(a) * 0.27
        T.cyl(f"Candle{i}", "Krem", p(x, y, 1.65), r=0.013, depth=0.065, verts=6)
        T.sphere(f"Flame{i}", "Ates", p(x, y, 1.7), r=0.011, scale=(1, 1, 1.6), seg=5, rings=3)
        T.sphere(f"Crystal{i}", "ElfCiyi", p(x * 0.85, y * 0.85, 1.52), r=0.022, scale=(1, 1, 1.8), seg=6, rings=4)
    T.sphere("CrystalCore", "ElfCiyi", p(0, 0, 1.5), r=0.05, scale=(1, 1, 1.6), seg=8, rings=5)


def ejder_heykeli(ox, oy):
    """Ejder heykeli: taş kaide üstünde kıvrılmış altın ejder, kanatlar açık."""
    p = at(ox, oy)
    T.box("Plinth", "KoyuTas", p(0, 0, 0.25), (0.6, 0.6, 0.5), bevel=0.03)
    T.box("PlinthTop", "Tas", p(0, 0, 0.52), (0.68, 0.68, 0.06), bevel=0.02)
    T.sphere("Body", "Altin", p(0, 0.05, 0.75), r=0.2, scale=(1, 1.3, 0.9), seg=10, rings=6)
    T.limb("Neck", "Altin", p(0, -0.1, 0.8), p(0, -0.22, 1.1), r1=0.08, r2=0.06, verts=8)
    T.sphere("Head", "Altin", p(0, -0.27, 1.15), r=0.09, scale=(1, 1.4, 0.9), seg=8, rings=5)
    for s in (1, -1):
        T.cyl(f"Horn{s}", "Krem", p(s * 0.05, -0.22, 1.24), r=0.02, r2=0.0, depth=0.12, rot=(-30, s * 20, 0), verts=5)
        T.box(f"Wing{s}", "Pirinc", p(s * 0.28, 0.12, 0.95), (0.4, 0.04, 0.32), rot=(0, s * 35, s * 15), bevel=0.01)
        T.sphere(f"Eye{s}", "Kirmizi", p(s * 0.045, -0.36, 1.18), r=0.015, seg=5, rings=3)
    T.limb("Tail", "Altin", p(0, 0.25, 0.68), p(0.18, 0.32, 0.58), r1=0.06, r2=0.02, verts=6)


def altin_fici(ox, oy):
    """Altın çemberli fıçı yığını (başlangıç paketi süsü)."""
    p = at(ox, oy)
    for i, (x, y, z, rot) in enumerate(((-0.28, 0, 0.26, 90), (0.28, 0, 0.26, 90), (0, 0, 0.7, 90))):
        T.cyl(f"Barrel{i}", "Mese", p(x, y, z), r=0.26, depth=0.62, rot=(rot, 0, 0), verts=12)
        for k in (-0.2, 0.2):
            T.cyl(f"Hoop{i}{k}", "Altin", p(x, y + k, z), r=0.268, depth=0.04, rot=(rot, 0, 0), verts=12)
        T.cyl(f"Lid{i}", "KoyuMese", p(x, y - 0.31, z), r=0.22, depth=0.01, rot=(rot, 0, 0), verts=12)
    T.cyl("Tap", "Altin", p(0, -0.36, 0.62), r=0.025, depth=0.1, rot=(90, 0, 0), verts=6)


def tabela_cerceve(ox, oy):
    """Tabela panosunu saran altın çerçeve + taç (PR_Decor_Tabela'nın panosuyla aynı yerde, biraz önde)."""
    p = at(ox, oy)
    for s in (-1, 1):
        T.box(f"Side{s}", "Altin", p(s * 0.76, -0.05, 1.98), (0.07, 0.04, 0.8), bevel=0.01)
        T.box(f"Bar{s}", "Altin", p(0, -0.05, 1.98 + s * 0.38), (1.6, 0.04, 0.07), bevel=0.01)
        T.sphere(f"Knob{s}", "Altin", p(s * 0.76, -0.05, 2.38), r=0.05, seg=6, rings=4)
    T.cyl("CrownBase", "Altin", p(0, -0.05, 2.45), r=0.12, depth=0.06, verts=8)
    for k in range(5):
        a = math.radians(k * 72)
        T.cyl(f"CrownTip{k}", "Altin", p(math.cos(a) * 0.1, -0.05 + math.sin(a) * 0.03, 2.52), r=0.025, r2=0.0, depth=0.1, verts=4)
    T.sphere("Ruby", "Kirmizi", p(0, -0.1, 2.46), r=0.03, seg=6, rings=4)


def export(name, sub, build):
    T.reset_scene()
    build()
    obj = T.join_all(name, smooth_angle=35)
    print(f"[{name}] üçgen: {T.tri_count(obj)}")
    T.save_blend(os.path.join(MODELS, sub, f"{name}.blend"))
    T.export_fbx(obj, os.path.join(FBX, "props" if name.startswith("PR_") else "", f"{name}.fbx"))


if __name__ == "__main__":
    for name, fn in (("CH_Staff_09_Lirael", st_lirael), ("CH_Staff_10_Ignis", st_ignis)):
        T.reset_scene()
        fn(0.0)
        obj = T.join_all(name)
        print(f"[{name}] üçgen: {T.tri_count(obj)}")
        T.save_blend(os.path.join(MODELS, "characters", f"{name}.blend"))
        T.export_fbx(obj, os.path.join(FBX, f"{name}.fbx"))
    for name, fn in (("PR_Decor_KristalAvize", kristal_avize), ("PR_Decor_EjderHeykeli", ejder_heykeli),
                     ("PR_Decor_AltinFici", altin_fici), ("PR_Decor_TabelaCerceve", tabela_cerceve)):
        export(name, "Decor", lambda fn=fn: fn(0, 0))

    RP.shoot(lambda: st_lirael(0.0), "Staff_Lirael", True)
    RP.shoot(lambda: st_ignis(0.0), "Staff_Ignis", True)
    RP.shoot(lambda: kristal_avize(0, 0), "DEC_KristalAvize", False)
    RP.shoot(lambda: ejder_heykeli(0, 0), "DEC_EjderHeykeli", False)
    RP.shoot(lambda: altin_fici(0, 0), "DEC_AltinFici", False)
    RP.shoot(lambda: tabela_cerceve(0, 0), "Prem_Cerceve", False)
    RP.shoot(lambda: (P.b_zemin_altin(0, 0), P.wall_base(at(0, 0.5), theme="Altin")), "Prem_AltinTema", False)

    T.reset_scene()
    st_lirael(-2.4)
    st_ignis(-1.2)
    kristal_avize(0.3, 0)
    ejder_heykeli(1.7, 0)
    altin_fici(3.0, 0)
    P.b_zemin_altin(4.6, 0)
    P.wall_base(at(4.6, 0.5), theme="Altin")
    T.join_all("PremiumSheet")
    T.setup_render_stage(floor_size=40, floor_color="KoyuTas")
    T.render_from(os.path.join(RENDERS, "Props_Premium.png"), target=(1.0, 0, 0.9), pitch=22, yaw=0, dist=8.5, fov=46, res=(2000, 1000))
    print("BİTTİ")
