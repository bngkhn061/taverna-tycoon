"""
Ana menü arka planı: Aşama 1 "Yol Kenarı Meyhanesi" sahnesi (tüm modellerden kurulur).
Çıktı: assets/renders/Menu_Background.png (1080x1920, dikey) + Menu_Background_Wide.png (1920x1080)
Çalıştırma: blender -b --factory-startup --python tools/blender/render_menu_scene.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import toylib as T  # noqa: E402
import build_props as P  # noqa: E402
import build_customers as C  # noqa: E402
import build_main_characters as M  # noqa: E402
import build_staff as S  # noqa: E402

RENDERS = os.path.join(T.ROOT, "assets", "renders")
_n = [0]


def place(builder, x, y, rotz=0.0, z=0.0, scale=1.0):
    """Herhangi bir modeli orijinde kurar, birleştirir, sonra yerine taşır/döndürür."""
    _n[0] += 1
    before = set(bpy.context.scene.objects)
    builder()
    new = [o for o in bpy.context.scene.objects if o not in before and o.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for o in new:
        o.select_set(True)
    bpy.context.view_layer.objects.active = new[0]
    bpy.ops.object.convert(target="MESH")
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if len(new) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = f"PR_scene_{_n[0]}"
    try:
        bpy.ops.object.shade_smooth_by_angle(angle=math.radians(38))
    except Exception:
        pass
    obj.location = (x, y, z)
    obj.rotation_euler = (0, 0, math.radians(rotz))
    obj.scale = (scale, scale, scale)
    return obj


def cust(num):
    fn = [e for e in C.CUSTOMERS if e[0] == num][0][3]
    return lambda: fn(0.0)


def point_light(name, loc, color, energy, radius=0.2):
    d = bpy.data.lights.new(name, "POINT")
    d.color = T.hex_rgb(color)
    d.energy = energy
    d.shadow_soft_size = radius
    o = bpy.data.objects.new(name, d)
    o.location = loc
    bpy.context.scene.collection.objects.link(o)


def build_scene():
    T.reset_scene()
    # Zemin 6x5 karo
    for ix in range(6):
        for iy in range(5):
            place(lambda: P.b_zemin(0, 0), -2.5 + ix, -1.5 + iy, rotz=0)
    # Arka duvar (y=3), sol duvar (x=-3)
    for x, fn in ((-2.5, P.b_duvar), (-1.5, P.b_pencere), (-0.5, P.b_duvar), (0.5, P.b_duvar), (1.5, P.b_pencere), (2.5, P.b_kapi)):
        place(lambda fn=fn: fn(0, 0), x, 3.1)
    for y, fn in ((-1.5, P.b_duvar), (-0.5, P.b_pencere), (0.5, P.b_duvar), (1.5, P.b_duvar), (2.5, P.b_duvar)):
        place(lambda fn=fn: fn(0, 0), -3.1, y, rotz=90)
    place(lambda: P.b_kose(0, 0), -3.1, 3.1)

    # Dekor
    place(lambda: P.x_somine(0, 0), 0.0, 2.72)
    place(lambda: P.x_mesale(0, 0), -1.0, 2.93)
    place(lambda: P.x_mesale(0, 0), 1.0, 2.93)
    place(lambda: P.x_kalkan(0, 0), -2.93, 1.0, rotz=90)
    place(lambda: P.x_boynuz(0, 0), -2.93, -1.0, rotz=90)
    place(lambda: P.x_saksi(0, 0), -2.55, 2.55)
    place(lambda: P.x_fici_yigini(0, 0), 2.45, -1.2, rotz=-90)
    place(lambda: P.x_hali(0, 0), -1.3, 0.2)
    place(lambda: P.x_flama(0, 0), -2.5, -1.7, rotz=30)

    # Tezgâh + istasyonlar
    place(lambda: P.f_tezgah(0, 0), 1.65, 1.15)
    place(lambda: P.s_bira(0, 0, 1), 1.2, 2.45)
    place(lambda: P.s_pres(0, 0, 1), 2.3, 2.45)
    place(lambda: P.f_sandik(0, 0), 2.6, 0.4, rotz=-90)

    # Masalar
    place(lambda: P.f_masa_kare(0, 0), -1.3, 0.2)
    for dx, dy, r in ((-0.75, 0, -90), (0.75, 0, 90), (0, 0.75, 180)):
        place(lambda: P.f_sandalye(0, 0), -1.3 + dx, 0.2 + dy, rotz=r)
    place(lambda: P.d_bira(0, 0), -1.45, 0.15, z=0.64)
    place(lambda: P.d_bira(0, 0), -1.05, 0.3, z=0.64)
    place(lambda: P.f_masa_yuvarlak(0, 0), 0.3, -0.9)
    for dx, dy in ((-0.6, 0.1), (0.6, 0.1)):
        place(lambda: P.f_tabure(0, 0), 0.3 + dx, -0.9 + dy)
    place(lambda: P.d_sira(0, 0), 0.4, -0.95, z=0.63)

    # Karakterler
    place(lambda: M.build_barlo(0.0), 0.35, 0.65, rotz=-15)
    place(lambda: M.build_pip(0.0), 1.15, -0.25, rotz=-25)
    place(cust(1), -2.05, 0.2, rotz=-60)   # Çiftçi
    place(cust(12), -0.55, 0.45, rotz=40)  # Cüce
    place(cust(4), -0.35, 2.05, rotz=10)   # Ozan, şöminenin önünde
    place(cust(2), -0.4, -1.0, rotz=-30)   # Balıkçı, yuvarlak masada

    # Işık
    T.setup_render_stage(floor=False)
    w = bpy.context.scene.world.node_tree.nodes["Background"]
    w.inputs["Color"].default_value = (*[c ** 2.2 for c in T.hex_rgb("#3A2A3E")], 1)
    w.inputs["Strength"].default_value = 0.7
    sun = bpy.data.objects["Sun"]
    sun.data.energy = 2.2
    sun.rotation_euler = (math.radians(55), 0, math.radians(-30))
    point_light("FireLight", (0.0, 2.2, 0.5), "#FF8A2A", 220, 0.3)
    point_light("TorchL", (-1.0, 2.7, 1.35), "#FFB060", 60)
    point_light("TorchR", (1.0, 2.7, 1.35), "#FFB060", 60)
    point_light("TableGlow", (-1.3, 0.2, 1.4), "#FFD9A0", 80, 0.5)
    try:
        bpy.context.scene.eevee.use_shadows = True
    except Exception:
        pass


if __name__ == "__main__":
    build_scene()
    T.render_from(os.path.join(RENDERS, "Menu_Background.png"), target=(0.0, 0.7, 0.4), pitch=40, yaw=-18,
                  dist=9.0, fov=46, res=(1080, 1920))
    T.render_from(os.path.join(RENDERS, "Menu_Background_Wide.png"), target=(0.0, 0.7, 0.5), pitch=38, yaw=-18,
                  dist=8.4, fov=46, res=(1920, 1080))
    print("BİTTİ")
