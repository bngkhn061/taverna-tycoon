"""
UI kit üretici: design/ui/ui_v2_landscape.html'deki görünümü oyunda birebir kullanmak için
her arayüz parçasını (panel, buton, şerit, ikon...) ayrı PNG'ye çevirir.

1) Bu script kit sayfasını (HTML) ve parça koordinatlarını (JSON) üretir.
2) Headless Edge sayfanın şeffaf, 2x ölçekli ekran görüntüsünü alır.
3) tools/ui/crop_ui_kit.py (Blender Python) görüntüyü parçalara keser → assets/ui/*.png

Çalıştırma: python tools/ui/make_ui_kit.py
"""
import json
import os
import re
import subprocess

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MOCKUP = os.path.join(ROOT, "design", "ui", "ui_v2_landscape.html")
KIT_HTML = os.path.join(ROOT, "design", "ui", "kit", "ui_kit.html")
KIT_JSON = os.path.join(ROOT, "design", "ui", "kit", "ui_kit.json")
KIT_PNG = os.path.join(ROOT, "design", "ui", "kit", "ui_kit.png")
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
SCALE = 2

# Taslağın stillerini ve ikon tanımlarını aynen al
src = open(MOCKUP, encoding="utf-8").read()
style = re.search(r"<style>(.*?)</style>", src, re.S).group(1)
symbols = re.search(r'(<svg width="0" height="0".*?</svg>)', src, re.S).group(1)

# (ad, iç HTML, eleman genişliği, yüksekliği, 9-dilim [sol, üst, sağ, alt] dp ya da None)
# Hücre kenar boşluğu gölgeleri de içine alır: sol/üst/sağ 4, alt 12 dp
PAD = (4, 4, 4, 12)
X = '<svg style="width:14px;height:14px"><use href="#x"/></svg>'
PARTS = [
    ("panel_wood", '<div class="wood nails" style="width:100%;height:100%"></div>', 120, 80, [22, 22, 22, 26]),
    ("panel_wood_plain", '<div class="wood" style="width:100%;height:100%"></div>', 120, 80, [20, 20, 20, 26]),
    ("panel_parch", '<div class="parch" style="width:100%;height:100%"></div>', 140, 100, [28, 28, 28, 34]),
    ("btn_green", '<div class="btn b-green" style="width:100%;height:100%"></div>', 120, 48, [20, 24, 20, 24]),
    ("btn_wood", '<div class="btn b-wood" style="width:100%;height:100%"></div>', 120, 48, [20, 24, 20, 24]),
    ("btn_gold", '<div class="btn b-gold" style="width:100%;height:100%"></div>', 120, 48, [20, 24, 20, 24]),
    ("btn_red", '<div class="btn b-red" style="width:100%;height:100%"></div>', 120, 48, [20, 24, 20, 24]),
    ("btn_round", '<div class="btn b-wood round" style="width:100%;height:100%"></div>', 46, 46, None),
    ("pill", '<div class="leather" style="width:100%;height:100%"></div>', 100, 36, [20, 0, 20, 0]),
    ("ribbon", '<div style="position:relative;width:100%;height:100%"><div class="ribbon" style="top:0;left:18px;right:18px;transform:none;position:absolute;height:100%;padding:0"></div></div>', 240, 40, [40, 0, 40, 0]),
    ("shield", '<div class="shield-wrap"><div class="shield"></div></div>', 50, 56, None),
    ("close", '<div class="close" style="position:static">' + X + '</div>', 38, 38, None),
    ("tab_on", '<div class="tab on" style="height:100%"></div>', 80, 26, [12, 12, 12, 12]),
    ("tab_off", '<div class="tab" style="height:100%"></div>', 80, 26, [12, 12, 12, 12]),
    ("bubble", '<div class="bubble" style="position:static"></div>', 44, 44, None),
    ("joy_base", '<div class="joy" style="position:static;opacity:1"></div>', 108, 108, None),
    ("joy_knob", '<div style="width:48px;height:48px;border-radius:50%;background:radial-gradient(circle at 40% 35%, #fff8ea, #D9C7A3);border:3px solid #5C3A1E;box-shadow:0 3px 0 rgba(0,0,0,.35)"></div>', 48, 48, None),
    ("arrow", '<svg style="width:30px;height:38px"><use href="#arrow"/></svg>', 30, 38, None),
    ("rays", '<div style="width:300px;height:300px;border-radius:50%;background:repeating-conic-gradient(from 0deg, rgba(255,220,130,.35) 0 10deg, transparent 10deg 20deg);-webkit-mask:radial-gradient(circle, #000 30%, transparent 70%);mask:radial-gradient(circle, #000 30%, transparent 70%)"></div>', 300, 300, None),
    ("card", '<div style="width:100%;height:100%;border-radius:14px;background:rgba(255,255,255,.45);border:2px solid #E0A040"></div>', 90, 90, [16, 16, 16, 16]),
    ("icobox", '<div class="ico" style="width:100%;height:100%"></div>', 34, 34, None),
]
ICONS = ["gear", "scroll", "shop", "hammer", "book", "play", "door", "music", "sound", "vib", "bell", "globe", "gfx",
         "speed", "stock", "star", "lock", "cal", "x"]
for ic in ICONS:
    PARTS.append((f"icon_{ic}", f'<svg style="width:32px;height:32px"><use href="#{ic}"/></svg>', 32, 32, None))


def build():
    os.makedirs(os.path.dirname(KIT_HTML), exist_ok=True)
    x, y, row_h, width = 10, 10, 0, 900
    cells, rects = [], {}
    for name, inner, w, h, sl in PARTS:
        cw, ch = w + PAD[0] + PAD[2], h + PAD[1] + PAD[3]
        if x + cw > width:
            x, y, row_h = 10, y + row_h + 10, 0
        cells.append(f'<div class="cell" style="left:{x}px;top:{y}px;width:{cw}px;height:{ch}px">'
                     f'<div style="position:absolute;left:{PAD[0]}px;top:{PAD[1]}px;width:{w}px;height:{h}px">{inner}</div></div>')
        rects[name] = {"x": x * SCALE, "y": y * SCALE, "w": cw * SCALE, "h": ch * SCALE,
                       "slice": None if sl is None else [(sl[0] + PAD[0]) * SCALE, (sl[1] + PAD[1]) * SCALE,
                                                         (sl[2] + PAD[2]) * SCALE, (sl[3] + PAD[3]) * SCALE]}
        x += cw + 10
        row_h = max(row_h, ch)
    total_h = y + row_h + 10
    html = f"""<!doctype html><html><head><meta charset="utf-8">
<link href="https://fonts.googleapis.com/css2?family=Lilita+One&family=Nunito:wght@700;800;900&display=swap" rel="stylesheet">
<style>{style}
html, body {{ background: transparent !important; margin: 0; }}
.cell {{ position: absolute; }}
.btn::before {{ left: 5px; right: 5px; }}
</style></head><body>{symbols}{''.join(cells)}</body></html>"""
    open(KIT_HTML, "w", encoding="utf-8").write(html)
    json.dump({"scale": SCALE, "width": width * SCALE, "height": total_h * SCALE, "parts": rects},
              open(KIT_JSON, "w", encoding="utf-8"), indent=1)
    return width, total_h


if __name__ == "__main__":
    w, h = build()
    url = "file:///" + KIT_HTML.replace("\\", "/")
    subprocess.run([EDGE, "--headless=new", "--disable-gpu", "--hide-scrollbars", f"--force-device-scale-factor={SCALE}",
                    f"--window-size={w},{h}", "--default-background-color=00000000", "--virtual-time-budget=4000",
                    "--allow-file-access-from-files", f"--screenshot={KIT_PNG}", url],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    print("kit:", KIT_PNG, os.path.exists(KIT_PNG))
