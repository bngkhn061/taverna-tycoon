"""
Portre ve dekor ikonları (şeffaf 256px PNG):
  ICO_Cust_NN.png   — 30 müşteri (koleksiyon defteri, seviye kartı)
  ICO_Staff_<Key>.png, ICO_Main_Barlo.png, ICO_Main_Pip.png — personel kartları
  ICO_DEC_<Key>.png — dekor kartları
Çalıştırma: blender -b --factory-startup --python tools/blender/render_portraits.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import toylib as T  # noqa: E402
import build_props as P  # noqa: E402
import build_customers as C  # noqa: E402
import build_staff as S  # noqa: E402
import build_main_characters as M  # noqa: E402

OUT = os.path.join(T.ROOT, "assets", "icons")


def bounds(obj):
    vs = [obj.matrix_world @ v.co for v in obj.data.vertices]
    return (min(v.x for v in vs), max(v.x for v in vs), min(v.y for v in vs), max(v.y for v in vs),
            min(v.z for v in vs), max(v.z for v in vs))


def shoot(builder, name, portrait):
    T.reset_scene()
    builder()
    objs = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    obj = objs[0] if len(objs) == 1 and objs[0].name.startswith("CH_") else T.join_all("ICON_" + name)
    T.setup_render_stage(floor=False)
    sc = bpy.context.scene
    sc.render.film_transparent = True
    sc.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 1.4
    x0, x1, y0, y1, z0, z1 = bounds(obj)
    h, w = z1 - z0, x1 - x0
    if portrait:
        # Baş + omuzlar: modelin üst %55'i
        size = max(h * 0.66, w * 0.8)
        target = ((x0 + x1) / 2, 0, z1 - h * 0.4)
        T.render_from(os.path.join(OUT, f"ICO_{name}.png"), target=target, pitch=8, yaw=-18,
                      dist=size / (2 * math.tan(math.radians(13))) * 1.15, fov=26, res=(256, 256))
    else:
        size = max(h, w)
        T.render_from(os.path.join(OUT, f"ICO_{name}.png"), target=((x0 + x1) / 2, 0, (z0 + z1) / 2), pitch=24, yaw=-20,
                      dist=size / (2 * math.tan(math.radians(13))) * 1.3 + 0.05, fov=26, res=(256, 256))


if __name__ == "__main__":
    for num, key, _, fn in C.CUSTOMERS:
        shoot(lambda fn=fn: fn(0.0), f"Cust_{num:02d}", True)
    for num, key, _, fn in S.STAFF:
        shoot(lambda fn=fn: fn(0.0), f"Staff_{key}", True)
    shoot(lambda: M.build_barlo(0.0), "Main_Barlo", True)
    shoot(lambda: M.build_pip(0.0), "Main_Pip", True)
    for key, _, fn in P.DECOR:
        shoot(lambda fn=fn: fn(0, 0), f"DEC_{key}", False)
    print("BİTTİ")
