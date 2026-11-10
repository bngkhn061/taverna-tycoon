"""
v1.0 yeni bölümler · 2. parti (onay 2026-11-10): Arka Avlu + Ahır (Sv23, salonun sağı, sadece avlu masaları; ahır süs)
ve İskele (Sv26, bahçenin solunda göl kıyısı, balık ızgarası).
Avlu/Ahır: PR_Nature_At · PR_Nature_At2 · PR_Building_AhirBolme · PR_Decor_SamanBalyasi · PR_Decor_Yalak
İskele:    PR_Building_IskeleTahta · PR_Nature_Kayik · PR_Decor_Ag · PR_Nature_Saz · PR_Nature_Nilufer · PR_Decor_BalikciDirek
Yemek:     PR_Drink_IzgaraBalik · PR_Station_Balik_T1..3 · ICO_IzgaraBalik · önizleme assets/renders/Props_Sections2.png
Çalıştırma: blender -b --factory-startup --python tools/blender/build_sections2.py
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


# ------------------------------------------------------------------ avlu / ahır
def at_model(ox, oy, coat="Mese", mane="KoyuMese"):
    """Oyuncak at (yan duruş, başı −y): gövde, dört bacak, boyun, baş, yele, kuyruk."""
    p = at(ox, oy)
    T.sphere("Body", coat, p(0, 0.1, 0.92), r=0.36, scale=(0.95, 1.55, 0.85), seg=10, rings=6)
    for sx in (-1, 1):
        for sy in (-1, 1):
            T.limb(f"Leg{sx}{sy}", coat, p(sx * 0.16, 0.1 + sy * 0.32, 0.8), p(sx * 0.16, 0.1 + sy * 0.32, 0.08), r1=0.085, r2=0.065, verts=6)
            T.cyl(f"Hoof{sx}{sy}", "KoyuTas", p(sx * 0.16, 0.1 + sy * 0.32, 0.04), r=0.065, depth=0.08, verts=8)
    T.limb("Neck", coat, p(0, -0.3, 1.05), p(0, -0.5, 1.45), r1=0.13, r2=0.1, verts=8)
    T.sphere("Head", coat, p(0, -0.62, 1.5), r=0.13, scale=(0.85, 1.6, 0.9), seg=8, rings=6)
    T.sphere("Muzzle", "Krem" if coat != "Krem" else "Tas", p(0, -0.8, 1.44), r=0.08, scale=(0.9, 1.0, 0.8), seg=8, rings=5)
    for s in (-1, 1):
        T.cyl(f"Ear{s}", coat, p(s * 0.06, -0.55, 1.65), r=0.03, r2=0.0, depth=0.1, verts=5)
        T.sphere(f"Eye{s}", "Goz", p(s * 0.1, -0.68, 1.55), r=0.02, seg=5, rings=3)
    for i in range(5):
        T.box(f"Mane{i}", mane, p(0, -0.3 - i * 0.07, 1.15 + i * 0.09), (0.05, 0.08, 0.14), rot=(-35, 0, 0))
    T.limb("Tail", mane, p(0, 0.6, 1.0), p(0, 0.75, 0.55), r1=0.07, r2=0.03, verts=6)
    T.box("Saddle", "Kirmizi", p(0, 0.05, 1.2), (0.42, 0.4, 0.06), bevel=0.02)


def ahir_bolme(ox, oy):
    """Ahır bölmesi (2.5 m eni, 2.4 m derinlik, önü −y açık): arka ve yan tahta duvarlar, eğik çatı, saman rafı."""
    p = at(ox, oy)
    T.box("Back", "Mese", p(0, 1.15, 1.1), (2.5, 0.12, 2.2), bevel=0.02)
    for s in (-1, 1):
        T.box(f"Side{s}", "KoyuMese", p(s * 1.2, 0.1, 0.6), (0.1, 2.1, 1.2), bevel=0.02)
        T.box(f"Post{s}", "KoyuMese", p(s * 1.2, -0.95, 1.2), (0.16, 0.16, 2.4), bevel=0.02)
    T.box("Roof", "Kirmizi", p(0, 0.2, 2.45), (2.8, 2.6, 0.1), rot=(12, 0, 0), bevel=0.02)
    T.box("Beam", "KoyuMese", p(0, -0.95, 2.25), (2.6, 0.16, 0.16))
    T.box("HayRack", "KoyuMese", p(0, 0.95, 1.3), (1.2, 0.3, 0.5))
    T.box("Hay", "Bal", p(0, 0.95, 1.45), (1.1, 0.26, 0.3))
    T.box("Gate", "Mese", p(0, -0.95, 0.55), (1.8, 0.06, 0.12))


def saman_balyasi(ox, oy):
    p = at(ox, oy)
    T.box("Bale", "Bal", p(0, 0, 0.22), (0.9, 0.5, 0.44), bevel=0.04)
    for x in (-0.25, 0.25):
        T.box(f"Twine{x}", "KoyuMese", p(x, 0, 0.22), (0.03, 0.52, 0.46))
    T.box("Bale2", "Bal", p(0.15, 0.05, 0.66), (0.8, 0.45, 0.42), rot=(0, 0, 12), bevel=0.04)


def yalak(ox, oy):
    p = at(ox, oy)
    T.box("Trough", "KoyuMese", p(0, 0, 0.25), (1.2, 0.45, 0.5), bevel=0.03)
    T.box("Water", "Su", p(0, 0, 0.47), (1.08, 0.34, 0.03))
    T.cyl("Bucket", "Mese", p(0.75, 0, 0.18), r=0.15, r2=0.12, depth=0.36, verts=10)


# ------------------------------------------------------------------ iskele
def iskele_tahta(ox, oy):
    """İskele güverte karosu (1 × 1 m): tahtalar, altında suya inen kazıklar (zemin üstü 0.04 m)."""
    p = at(ox, oy)
    T.box("Bed", "KoyuMese", p(0, 0, 0.015), (1.0, 1.0, 0.03))
    for i in range(5):
        T.box(f"Plank{i}", "Mese" if i % 2 else "Ten4", p(-0.4 + i * 0.2, 0, 0.02), (0.195, 1.0, 0.04))
    for sx in (-1, 1):
        T.cyl(f"Pile{sx}", "KoyuMese", p(sx * 0.45, 0.45, -0.4), r=0.06, depth=0.8, verts=6)


def kayik(ox, oy):
    """Küçük ahşap kayık (önü −y), içinde kürekler."""
    p = at(ox, oy)
    T.sphere("Hull", "Mese", p(0, 0, 0.12), r=0.5, scale=(0.9, 2.4, 0.5), seg=12, rings=6)
    T.sphere("Inner", "KoyuMese", p(0, 0, 0.2), r=0.44, scale=(0.82, 2.2, 0.32), seg=12, rings=6)
    T.box("Seat", "Ten4", p(0, 0.1, 0.25), (0.75, 0.16, 0.05))
    for s in (-1, 1):
        T.box(f"Oar{s}", "Mese", p(s * 0.25, -0.2, 0.3), (0.05, 1.3, 0.03), rot=(0, 0, s * 10))
    T.box("Stripe", "Kirmizi", p(0, 0, 0.22), (0.92, 2.2, 0.04))


def ag(ox, oy):
    """Balık ağı: iki direk arasında asılı ağ, yerde sepet."""
    p = at(ox, oy)
    for s in (-1, 1):
        T.cyl(f"Pole{s}", "KoyuMese", p(s * 0.7, 0, 0.8), r=0.04, depth=1.6, verts=6)
    for i in range(6):
        T.box(f"NetH{i}", "Bez", p(0, 0, 0.5 + i * 0.18), (1.4, 0.01, 0.012))
    for i in range(8):
        T.box(f"NetV{i}", "Bez", p(-0.6 + i * 0.17, 0, 0.95), (0.012, 0.01, 0.9))
    T.cyl("Basket", "Mese", p(0.3, -0.35, 0.12), r=0.17, r2=0.14, depth=0.24, verts=8)
    T.sphere("Fish", "Tas", p(0.3, -0.35, 0.26), r=0.06, scale=(2, 0.8, 0.6), seg=6, rings=4)


def saz(ox, oy):
    p = at(ox, oy)
    for i in range(7):
        a = math.radians(i * 51)
        x, y = math.cos(a) * 0.12, math.sin(a) * 0.12
        T.cyl(f"Reed{i}", "Yesil", p(x, y, 0.4), r=0.012, depth=0.8 + (i % 3) * 0.15, rot=(math.cos(a) * 8, math.sin(a) * 8, 0), verts=4)
        if i % 2 == 0:
            T.cyl(f"Cat{i}", "KoyuMese", p(x * 1.1, y * 1.1, 0.85 + (i % 3) * 0.15), r=0.025, depth=0.14, verts=6)


def nilufer(ox, oy):
    p = at(ox, oy)
    for i, (x, y, r) in enumerate(((0, 0, 0.22), (0.4, 0.2, 0.16), (-0.3, 0.3, 0.14))):
        T.cyl(f"Pad{i}", "Yesil", p(x, y, 0.01), r=r, depth=0.015, verts=10)
    T.sphere("Flower", "Krem", p(0.05, 0.02, 0.06), r=0.06, scale=(1, 1, 0.6), seg=6, rings=4)
    T.sphere("Heart", "Altin", p(0.05, 0.02, 0.09), r=0.025, seg=5, rings=3)


def balikci_direk(ox, oy):
    """Bağlama direği + halat."""
    p = at(ox, oy)
    T.cyl("Post", "KoyuMese", p(0, 0, 0.45), r=0.09, depth=0.9, verts=8)
    T.cyl("Cap", "Mese", p(0, 0, 0.92), r=0.1, depth=0.04, verts=8)
    for i in range(3):
        T.torus(f"Rope{i}", "Bez", p(0, 0, 0.6 + i * 0.06), major=0.1, minor=0.018, seg=10)


# ------------------------------------------------------------------ balık
def d_balik(ox, oy):
    p = at(ox, oy)
    T.cyl("Plate", "Krem", p(0, 0, 0.01), r=0.11, depth=0.02, verts=12)
    T.sphere("Fish", "Bakir", p(0, 0, 0.05), r=0.05, scale=(2.0, 0.8, 0.6), seg=8, rings=5)
    T.cyl("Tail", "Bakir", p(0.11, 0, 0.05), r=0.04, r2=0.0, depth=0.05, rot=(0, 90, 0), verts=3)
    T.sphere("Lemon", "Altin", p(-0.07, 0.05, 0.035), r=0.025, seg=6, rings=4)
    for i in range(3):
        T.box(f"Grill{i}", "KoyuTas", p(-0.03 + i * 0.03, 0, 0.078), (0.008, 0.06, 0.004))


def s_balik(ox, oy, tier=1):
    """Balık ızgarası: taş ocak, ızgara teli, şişte balıklar, tütsü dumanı, yanda tuz fıçısı."""
    p = at(ox, oy)
    T.box("Hearth", "Tas", p(0, 0, 0.3), (0.86, 0.5, 0.6), bevel=0.03)
    T.box("Coals", "Ates", p(0, 0, 0.61), (0.7, 0.36, 0.03))
    for i in range(4):
        T.box(f"Wire{i}", "KoyuTas", p(-0.3 + i * 0.2, 0, 0.66), (0.02, 0.44, 0.02))
    for i in range(2 + tier):
        T.sphere(f"Fish{i}", "Bakir", p(-0.25 + i * 0.16, 0, 0.7), r=0.05, scale=(0.8, 2.2, 0.6), seg=6, rings=4)
    for i in range(3):
        T.sphere(f"Smoke{i}", "Tas", p(0.05 * i, 0.05, 0.9 + i * 0.16), r=0.07 + i * 0.02, seg=6, rings=4)
    T.cyl("Barrel", "Mese", p(0.55, 0.05, 0.25), r=0.16, depth=0.5, verts=10)
    T.box("Sign", TRIM[tier] if tier > 1 else "KoyuMese", p(0, -0.27, 0.45), (0.4, 0.03, 0.12), bevel=0.01)
    tier_extras(p, tier, 1.0, front_y=-0.27)


def export(name, sub, build):
    T.reset_scene()
    build()
    obj = T.join_all(name, smooth_angle=35)
    print(f"[{name}] üçgen: {T.tri_count(obj)}")
    T.save_blend(os.path.join(MODELS, sub, f"{name}.blend"))
    T.export_fbx(obj, os.path.join(FBX, f"{name}.fbx"))


PARTS = [("PR_Nature_At", lambda x, y: at_model(x, y)), ("PR_Nature_At2", lambda x, y: at_model(x, y, "Krem", "SacGri")),
         ("PR_Building_AhirBolme", ahir_bolme), ("PR_Decor_SamanBalyasi", saman_balyasi), ("PR_Decor_Yalak", yalak),
         ("PR_Building_IskeleTahta", iskele_tahta), ("PR_Nature_Kayik", kayik), ("PR_Decor_Ag", ag), ("PR_Nature_Saz", saz),
         ("PR_Nature_Nilufer", nilufer), ("PR_Decor_BalikciDirek", balikci_direk)]

if __name__ == "__main__":
    for name, fn in PARTS:
        export(name, "Sections", lambda fn=fn: fn(0, 0))
    export("PR_Drink_IzgaraBalik", "Drink", lambda: d_balik(0, 0))
    for t in (1, 2, 3):
        export(f"PR_Station_Balik_T{t}", "Station", lambda t=t: s_balik(0, 0, t))
    RP.shoot(lambda: d_balik(0, 0), "IzgaraBalik", False)

    T.reset_scene()
    ahir_bolme(-3.0, 1.0); at_model(-3.0, 0.9); saman_balyasi(-1.2, 0.2); yalak(-1.2, -1.0)
    at_model(0.2, 0.5, "Krem", "SacGri")
    for i in range(3):
        for k in range(2):
            iskele_tahta(2.0 + i, -0.5 + k)
    kayik(5.0, 0.2); ag(3.0, 1.4); saz(5.6, -1.2); nilufer(4.4, -1.3); balikci_direk(4.6, 0.6)
    for t in (1, 2, 3):
        s_balik(-0.8 + t * 1.0, -2.2, t)
    d_balik(3.4, -1.6)
    T.join_all("Sections2Sheet")
    T.setup_render_stage(floor_size=40, floor_color="Yesil")
    T.render_from(os.path.join(RENDERS, "Props_Sections2.png"), target=(1.0, -0.4, 0.8), pitch=30, yaw=0, dist=11.0, fov=46, res=(2000, 1100))
    print("BİTTİ")
