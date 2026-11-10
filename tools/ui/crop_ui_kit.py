"""
UI kit görüntüsünü parçalara keser (Blender Python, PIL gerekmez).
Çalıştırma: blender -b --factory-startup --python tools/ui/crop_ui_kit.py
Çıktı: assets/ui/<parça>.png + assets/ui/ui_slices.json (9-dilim kenarları, 2x piksel)
"""
import json
import os

import bpy
import numpy as np

import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
# İsteğe bağlı kit klasörü: blender ... --python crop_ui_kit.py -- kit_v3  (varsayılan: kit)
_args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
KIT = os.path.join(ROOT, "design", "ui", _args[0] if _args else "kit")
OUT = os.path.join(ROOT, "assets", "ui")
SLICES_JSON = "ui_slices.json" if not _args else f"ui_slices_{_args[0]}.json"

meta = json.load(open(os.path.join(KIT, "ui_kit.json"), encoding="utf-8"))
img = bpy.data.images.load(os.path.join(KIT, "ui_kit.png"))
W, H = img.size
px = np.array(img.pixels[:], dtype=np.float32).reshape(H, W, 4)  # alt satır önce (Blender)
os.makedirs(OUT, exist_ok=True)
slices = {}
for name, r in meta["parts"].items():
    x, y, w, h = r["x"], r["y"], r["w"], r["h"]
    x, y = min(x, W - 1), min(y, H - 1)
    w, h = min(w, W - x), min(h, H - y)
    y0 = H - (y + h)
    crop = px[y0:y0 + h, x:x + w, :].copy()
    out = bpy.data.images.new(name, w, h, alpha=True)
    out.pixels = crop.ravel().tolist()
    out.filepath_raw = os.path.join(OUT, name + ".png")
    out.file_format = "PNG"
    out.save()
    bpy.data.images.remove(out)
    slices[name] = {"w": w, "h": h, "slice": r["slice"]}
json.dump(slices, open(os.path.join(OUT, SLICES_JSON), "w", encoding="utf-8"), indent=1)
print("BİTTİ", len(slices), "parça")
