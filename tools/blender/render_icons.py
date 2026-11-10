"""
UI ikonları: içecekler + para + elmas (şeffaf arka planlı 256px PNG).
Çıktı: assets/icons/ICO_<Ad>.png
Çalıştırma: blender -b --factory-startup --python tools/blender/render_icons.py
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy  # noqa: E402
import toylib as T  # noqa: E402
import build_props as P  # noqa: E402

OUT = os.path.join(T.ROOT, "assets", "icons")

if __name__ == "__main__":
    for key, _, fn in P.DRINKS:
        T.reset_scene()
        fn(0, 0)
        obj = T.join_all(f"PR_icon_{key}")
        T.setup_render_stage(floor=False)
        sc = bpy.context.scene
        sc.render.film_transparent = True
        sc.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 1.4
        # nesneyi kadraja oturt
        zs = [(obj.matrix_world @ v.co).z for v in obj.data.vertices]
        xs = [(obj.matrix_world @ v.co).x for v in obj.data.vertices]
        h = max(zs) - min(zs)
        w = max(xs) - min(xs)
        size = max(h, w)
        T.render_from(os.path.join(OUT, f"ICO_{key}.png"), target=(0, 0, (max(zs) + min(zs)) / 2), pitch=22, yaw=-20,
                      dist=size / (2 * math.tan(math.radians(13))) * 1.3 + 0.05, fov=26, res=(256, 256))
    print("BİTTİ")
