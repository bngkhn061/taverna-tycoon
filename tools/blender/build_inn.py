"""
Han / otel odaları (v0.8): merdiven, üst kat iniş ağzı, resepsiyon tezgâhı, 3 kademeli yatak,
dağınık yorgan (sabah), komodin + lamba ve Hizmetçi. Stil: DESIGN_RULES.md "Oyuncak Taverna".
Çıktılar mevcut adlandırmaya uyar:
  PR_Furniture_Merdiven / MerdivenUst / Resepsiyon / Yatak_T1..3 / YorganDaginik / Komodin
  CH_Staff_08_Hizmetci · assets/icons/ICO_Staff_Hizmetci.png · ICO_Yatak.png · assets/renders/Props_Inn.png
Çalıştırma: blender -b --factory-startup --python tools/blender/build_inn.py
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import toylib as T  # noqa: E402
import bodylib as B  # noqa: E402
import build_props as P  # noqa: E402
import render_portraits as RP  # noqa: E402
from mathutils import Vector as V  # noqa: E402

at, TRIM = P.at, P.TRIM
MODELS = os.path.join(T.ROOT, "assets", "models")
FBX = os.path.join(T.ROOT, "assets", "export", "fbx")
RENDERS = os.path.join(T.ROOT, "assets", "renders")


def merdiven(ox, oy):
    """Yukarı çıkan merdiven: önü −y, basamaklar +y yönünde yükselir (1 m eni, 1.6 m boyu)."""
    p = at(ox, oy)
    for i in range(7):
        y = -0.7 + i * 0.22
        T.box(f"Step{i}", "Mese" if i % 2 == 0 else "KoyuMese", p(0, y, 0.11 + i * 0.2), (0.9, 0.24, 0.06), bevel=0.01)
        T.box(f"Riser{i}", "KoyuMese", p(0, y - 0.11, 0.05 + i * 0.2), (0.88, 0.02, 0.2))
    for s in (-1, 1):
        T.box(f"Stringer{s}", "KoyuMese", p(s * 0.47, 0.0, 0.7), (0.06, 1.6, 0.12), rot=(42, 0, 0), bevel=0.01)
        T.box(f"Post{s}", "KoyuMese", p(s * 0.47, -0.72, 0.5), (0.07, 0.07, 1.0), bevel=0.01)
        T.box(f"Rail{s}", "Mese", p(s * 0.47, 0.0, 1.35), (0.05, 1.7, 0.05), rot=(42, 0, 0))
        T.sphere(f"Knob{s}", "Pirinc", p(s * 0.47, -0.72, 1.03), r=0.05, seg=6, rings=4)
    T.box("Landing", "Mese", p(0, 0.75, 1.45), (0.95, 0.35, 0.08), bevel=0.01)


def merdiven_ust(ox, oy):
    """Üst kattaki iniş ağzı: döşemede açıklık + korkuluk, aşağı inen ilk basamaklar."""
    p = at(ox, oy)
    T.box("Hole", "KoyuTas", p(0, 0, 0.005), (1.0, 1.4, 0.01))
    for i in range(3):
        T.box(f"Step{i}", "Mese", p(0, 0.45 - i * 0.3, -0.02 - i * 0.03), (0.86, 0.26, 0.04))
    for s in (-1, 1):
        T.box(f"RailSide{s}", "Mese", p(s * 0.52, 0, 0.75), (0.05, 1.4, 0.05))
        for k in range(3):
            T.box(f"Baluster{s}{k}", "KoyuMese", p(s * 0.52, -0.6 + k * 0.6, 0.38), (0.04, 0.04, 0.75))
    T.box("RailBack", "Mese", p(0, 0.7, 0.75), (1.1, 0.05, 0.05))
    T.box("Sign", "KoyuMese", p(0.62, -0.72, 0.9), (0.04, 0.3, 0.2), bevel=0.01)


def resepsiyon(ox, oy):
    """Resepsiyon tezgâhı: önü −y; zil, misafir defteri, arkada anahtar panosu."""
    p = at(ox, oy)
    T.box("Desk", "Mese", p(0, 0, 0.5), (1.2, 0.5, 1.0), bevel=0.02)
    T.box("Top", "KoyuMese", p(0, 0, 1.02), (1.3, 0.6, 0.05), bevel=0.01)
    T.box("Panel", "KoyuMese", p(0, -0.26, 0.5), (1.0, 0.02, 0.7))
    T.box("Book", "Kirmizi", p(-0.25, -0.05, 1.07), (0.3, 0.22, 0.04), bevel=0.005)
    T.box("BookPage", "Krem", p(-0.25, -0.05, 1.095), (0.27, 0.19, 0.01))
    T.dome("Bell", "Pirinc", p(0.3, -0.05, 1.05), r=0.07)
    T.sphere("BellKnob", "Pirinc", p(0.3, -0.05, 1.13), r=0.02, seg=5, rings=4)
    T.box("KeyBoard", "KoyuMese", p(0, 0.45, 1.3), (1.0, 0.05, 0.6), bevel=0.01)
    for i in range(6):
        x = -0.38 + (i % 3) * 0.38
        z = 1.42 - (i // 3) * 0.25
        T.cyl(f"KeyHook{i}", "Pirinc", p(x, 0.42, z), r=0.012, depth=0.05, rot=(90, 0, 0), verts=5)
        T.box(f"Key{i}", "Altin", p(x, 0.41, z - 0.07), (0.03, 0.01, 0.1))


def yatak(ox, oy, tier=1):
    """Yatak (önü −y, baş ucu +y): 1 saman şilte, 2 ahşap karyola + yorgan, 3 cibinlikli."""
    p = at(ox, oy)
    if tier == 1:
        T.box("Straw", "Bal", p(0, 0, 0.12), (0.9, 1.9, 0.24), bevel=0.05)
        T.box("Sheet", "Bez", p(0, -0.15, 0.25), (0.85, 1.4, 0.04), bevel=0.02)
        T.sphere("Pillow", "Krem", p(0, 0.7, 0.3), r=0.2, scale=(1.6, 0.8, 0.45), seg=8, rings=5)
        return
    T.box("Frame", "Mese" if tier == 2 else "KoyuMese", p(0, 0, 0.22), (1.0, 2.0, 0.2), bevel=0.02)
    for s in (-1, 1):
        for y in (-0.95, 0.95):
            T.box(f"Leg{s}{y}", "KoyuMese", p(s * 0.47, y, 0.1), (0.08, 0.08, 0.2))
    T.box("Headboard", "KoyuMese", p(0, 0.98, 0.65), (1.04, 0.08, 0.8), bevel=0.03)
    T.box("Mattress", "Krem", p(0, 0, 0.38), (0.94, 1.9, 0.14), bevel=0.04)
    quilt = "Kirmizi" if tier == 2 else "Lacivert"
    T.box("Quilt", quilt, p(0, -0.2, 0.47), (0.98, 1.4, 0.06), bevel=0.03)
    T.box("QuiltFold", "Krem", p(0, 0.45, 0.48), (0.96, 0.18, 0.06), bevel=0.02)
    T.sphere("Pillow", "Kopuk", p(0, 0.72, 0.52), r=0.2, scale=(1.8, 0.8, 0.45), seg=8, rings=5)
    if tier == 3:
        for s in (-1, 1):
            for y in (-0.95, 0.95):
                T.box(f"CanopyPost{s}{y}", "KoyuMese", p(s * 0.5, y, 1.05), (0.06, 0.06, 1.7))
        T.box("CanopyTop", "Bordo", p(0, 0, 1.92), (1.1, 2.06, 0.06), bevel=0.01)
        for s in (-1, 1):
            T.box(f"Curtain{s}", "Bordo", p(s * 0.52, 0.6, 1.3), (0.04, 0.6, 1.1))
        T.box("Trim", "Altin", p(0, -1.0, 1.88), (1.1, 0.03, 0.06))
        T.sphere("Gem", "Altin", p(0, 1.03, 1.1), r=0.06, seg=6, rings=4)


def yorgan_daginik(ox, oy):
    """Sabah dağınık yatak: buruşuk yorgan + yere düşmüş yastık (yatağın üstüne bindirilir)."""
    p = at(ox, oy)
    T.sphere("Crumple1", "Krem", p(-0.15, -0.3, 0.55), r=0.3, scale=(1.4, 1.2, 0.35), seg=8, rings=5)
    T.sphere("Crumple2", "Bez", p(0.2, 0.15, 0.55), r=0.25, scale=(1.3, 1.1, 0.35), seg=8, rings=5)
    T.sphere("PillowFloor", "Kopuk", p(0.75, 0.5, 0.08), r=0.2, scale=(1.6, 0.8, 0.45), rot=(0, 0, 30), seg=8, rings=5)
    T.box("Sock", "Kirmizi", p(-0.6, -1.15, 0.03), (0.12, 0.06, 0.04), rot=(0, 0, 20))


def komodin(ox, oy):
    """Komodin + mum lambası."""
    p = at(ox, oy)
    T.box("Stand", "Mese", p(0, 0, 0.25), (0.45, 0.4, 0.5), bevel=0.02)
    T.box("Drawer", "KoyuMese", p(0, -0.2, 0.3), (0.35, 0.02, 0.15))
    T.sphere("Knob", "Pirinc", p(0, -0.22, 0.3), r=0.02, seg=5, rings=4)
    T.cyl("LampBase", "Pirinc", p(0, 0.02, 0.53), r=0.08, depth=0.04, verts=8)
    T.cyl("Candle", "Krem", p(0, 0.02, 0.62), r=0.03, depth=0.14, verts=8)
    T.cyl("Flame", "Ates", p(0, 0.02, 0.73), r=0.02, r2=0.0, depth=0.06, verts=5)
    T.cyl("Glass", "Su", p(0, 0.02, 0.66), r=0.07, depth=0.2, verts=8)


def st_hizmetci(ox):
    """Hizmetçi: elbise + kırmızı başörtü ve önlük (personel kuralı), kolunda çarşaf."""
    R = B.body(ox, "M", "Ten3", "Mavi", "Mavi", "KoyuMese", build="thin", nose="small")
    B.skirt(R, "Mavi", bottom=0.06, flare=1.2)
    B.apron(R, "Kirmizi", top=0.2, bottom=0.12, width=0.8)
    T.dome("Kerchief", "Kirmizi", (ox, 0.02, R.hz + R.hr * 0.1), r=R.hr * 1.08, rot=(-20, 0, 0))
    T.sphere("Bun", "SacKahve", (ox, R.hr * 0.75, R.hz + R.hr * 0.3), r=R.hr * 0.35, seg=6, rings=4)
    h = R.hand[-1]
    T.box("Linen", "Kopuk", h + V((0, -0.08, 0.05)), (0.22, 0.14, 0.1), bevel=0.03)
    T.box("LinenStripe", "Mavi", h + V((0, -0.155, 0.05)), (0.22, 0.01, 0.02))
    return R


def export(name, sub, build):
    T.reset_scene()
    build()
    obj = T.join_all(name, smooth_angle=35)
    print(f"[{name}] üçgen: {T.tri_count(obj)}")
    T.save_blend(os.path.join(MODELS, sub, f"{name}.blend"))
    T.export_fbx(obj, os.path.join(FBX, f"{name}.fbx"))


ITEMS = [("Merdiven", merdiven), ("MerdivenUst", merdiven_ust), ("Resepsiyon", resepsiyon),
         ("YorganDaginik", yorgan_daginik), ("Komodin", komodin)]

if __name__ == "__main__":
    for key, fn in ITEMS:
        export(f"PR_Furniture_{key}", "Furniture", lambda fn=fn: fn(0, 0))
    for t in (1, 2, 3):
        export(f"PR_Furniture_Yatak_T{t}", "Furniture", lambda t=t: yatak(0, 0, t))
    T.reset_scene()
    st_hizmetci(0.0)
    obj = T.join_all("CH_Staff_08_Hizmetci")
    print(f"[{obj.name}] üçgen: {T.tri_count(obj)}")
    T.save_blend(os.path.join(MODELS, "characters", f"{obj.name}.blend"))
    T.export_fbx(obj, os.path.join(FBX, f"{obj.name}.fbx"))

    RP.shoot(lambda: st_hizmetci(0.0), "Staff_Hizmetci", True)
    RP.shoot(lambda: yatak(0, 0, 2), "Yatak", False)

    # Önizleme: merdiven, resepsiyon, 3 yatak (+ dağınık), komodin, üst kat ağzı, hizmetçi
    T.reset_scene()
    merdiven(-3.2, 0.5)
    resepsiyon(-1.6, 0.3)
    for t in (1, 2, 3):
        yatak(-0.2 + (t - 1) * 1.3, 0.6, t)
    yorgan_daginik(1.1, 0.6)
    komodin(3.6, 0.9)
    merdiven_ust(3.6, -0.8)
    st_hizmetci(4.8)
    T.join_all("InnSheet")
    T.setup_render_stage(floor_size=40, floor_color="KoyuTas")
    T.render_from(os.path.join(RENDERS, "Props_Inn.png"), target=(0.6, 0.3, 0.6), pitch=30, yaw=0, dist=9.5, fov=46, res=(2000, 1000))
    print("BİTTİ")
