"""
Logo görselleri (v1.0, onaylı yön "A · Barlo Rozeti", design/ui/logo_concepts.html):
  assets/ui/logo/barlo_face_1024.png  — Barlo portresi, şeffaf, 1024 px (rozet ve uygulama ikonu)
  assets/ui/logo/mug_512.png          — bira kupası, şeffaf, 512 px (açılış animasyonunda tokuşan kupalar)
Çalıştırma: blender -b --factory-startup --python tools/blender/render_logo.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import toylib as T  # noqa: E402
import build_props as P  # noqa: E402
import build_main_characters as M  # noqa: E402
import render_portraits as RP  # noqa: E402

OUT = os.path.join(T.ROOT, "assets", "ui", "logo")
os.makedirs(OUT, exist_ok=True)


def stage(builder):
    T.reset_scene()
    builder()
    objs = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    obj = objs[0] if len(objs) == 1 else T.join_all("LOGO_OBJ")
    T.setup_render_stage(floor=False)
    sc = bpy.context.scene
    sc.render.film_transparent = True
    sc.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 1.4
    return obj


if __name__ == "__main__":
    obj = stage(lambda: M.build_barlo(0.0))
    x0, x1, y0, y1, z0, z1 = RP.bounds(obj)
    h, w = z1 - z0, x1 - x0
    size = max(h * 0.66, w * 0.8)
    T.render_from(os.path.join(OUT, "barlo_face_1024.png"), target=((x0 + x1) / 2, 0, z1 - h * 0.36), pitch=8, yaw=-18,
                  dist=size / (2 * math.tan(math.radians(13))) * 1.42, fov=26, res=(1024, 1024))

    mug = next(fn for key, _, fn in P.DRINKS if key == "Bira") if hasattr(P, "DRINKS") else None
    if mug:
        obj = stage(lambda: mug(0, 0))
        x0, x1, y0, y1, z0, z1 = RP.bounds(obj)
        size = max(z1 - z0, x1 - x0)
        T.render_from(os.path.join(OUT, "mug_512.png"), target=((x0 + x1) / 2, 0, (z0 + z1) / 2), pitch=18, yaw=-20,
                      dist=size / (2 * math.tan(math.radians(13))) * 1.25, fov=26, res=(512, 512))
    print("BİTTİ")
