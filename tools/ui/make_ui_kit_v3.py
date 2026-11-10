"""
UI kit v3 (Mürekkep Çizgi) üretici: design/ui/ui_v3_ink.html'deki görünümü oyunda kullanmak için
gölgeli paneller/butonlar (9-dilim) ve elle çizilmiş ikonları ayrı PNG'lere çevirir.

Gölgesiz parçalar (kartlar, çipler, sekmeler, çubuklar) PNG'ye gerek duymaz: Theme.uss'te
kenarlık + köşe yuvarlama ile çizilir. Sadece sert ofset gölgesi olanlar ve ikonlar buradan gelir.

1) python tools/ui/make_ui_kit_v3.py           → design/ui/kit_v3/ui_kit.html/.json/.png
2) blender -b --factory-startup --python tools/ui/crop_ui_kit.py -- kit_v3   → assets/ui/v3_*.png
3) Unity: Taverna > Her Şeyi Kur (assets/ui → UI/Images/Kit ve Resources/Kit)
"""
import json
import os
import re
import subprocess

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MOCKUP = os.path.join(ROOT, "design", "ui", "ui_v3_ink.html")
KIT_DIR = os.path.join(ROOT, "design", "ui", "kit_v3")
KIT_HTML = os.path.join(KIT_DIR, "ui_kit.html")
KIT_JSON = os.path.join(KIT_DIR, "ui_kit.json")
KIT_PNG = os.path.join(KIT_DIR, "ui_kit.png")
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
SCALE = 2

src = open(MOCKUP, encoding="utf-8").read()
style = re.search(r"<style>(.*?)</style>", src, re.S).group(1)
symbols = re.search(r'(<svg width="0" height="0".*?</svg>)', src, re.S).group(1)
# Taslakta olmayan ek ikonlar: hedef oku, temizlik süpürgesi
EXTRA = """<svg width="0" height="0" style="position:absolute"><defs>
<symbol id="i-arrow" viewBox="0 0 48 48"><path style="fill:var(--f1)" d="M24 43 L7 23 H16.5 V5 H31.5 V23 H41 Z"/></symbol>
<symbol id="i-broom" viewBox="0 0 48 48"><path fill="none" d="M33 6 L22 26"/><path style="fill:var(--f1)" d="M17 23 L29 30 L24 42 Q14 41 8 35 Z"/><path fill="none" d="M14 33 L19 30 M17 37 L22 33"/></symbol>
</defs></svg>"""

# Gölge payı: sağ/alt 4 dp sert gölge + 2 dp güvenlik
PAD = (2, 2, 8, 8)
PANEL = '<div class="panel" style="width:100%;height:100%;{}"></div>'
PARTS = [
    # ad, iç HTML, genişlik, yükseklik, 9-dilim [sol, üst, sağ, alt] dp (gölge payı ayrıca eklenir)
    ("v3_panel", PANEL.format(""), 80, 60, [16, 16, 16, 16]),
    ("v3_panel_mus", PANEL.format("background:var(--mus)"), 80, 60, [16, 16, 16, 16]),
    ("v3_panel_brick", PANEL.format("background:var(--brick)"), 80, 60, [16, 16, 16, 16]),
    ("v3_panel_teal", PANEL.format("background:var(--teal)"), 80, 60, [16, 16, 16, 16]),
    ("v3_panel_plum", PANEL.format("background:var(--plum)"), 80, 60, [16, 16, 16, 16]),
    ("v3_panel_off", PANEL.format("background:#cfc3ad;border-color:#8a7d6c;box-shadow:4px 4px 0 #8a7d6c"), 80, 60, [16, 16, 16, 16]),
    ("v3_circle", '<div class="panel" style="width:100%;height:100%;border-radius:50%"></div>', 64, 64, None),
    ("v3_circle_mus", '<div class="panel" style="width:100%;height:100%;border-radius:50%;background:var(--mus)"></div>', 64, 64, None),
]
ICONS = ["coin", "gem", "star", "star0", "shop", "quest", "staff", "hammer", "gear", "mug", "wine", "plus", "close", "play",
         "door", "book", "cal", "clock", "bolt", "barrel", "plant", "speed", "stock", "quality", "lock", "check", "music",
         "sound", "vibe", "globe", "brush", "bell", "home", "trash", "moon", "sun", "gift", "horn", "person", "arrow", "broom"]
IPAD = 4  # ikon çizgileri kutudan biraz taşar
for ic in ICONS:
    PARTS.append((f"v3i_{ic}", f'<div style="position:absolute;left:{IPAD}px;top:{IPAD}px;width:48px;height:48px"><svg class="ic"><use href="#i-{ic}"/></svg></div>',
                  48 + IPAD * 2 - PAD[0] - PAD[2], 48 + IPAD * 2 - PAD[1] - PAD[3], None))
# Kalkan ikonu farklı en-boy oranında
PARTS.append(("v3i_shield", '<svg class="ic" viewBox="0 0 64 72" style="position:absolute;left:4px;top:4px;width:64px;height:72px"><use href="#i-shield"/></svg>',
              72 - PAD[0] - PAD[2], 80 - PAD[1] - PAD[3], None))


def build():
    os.makedirs(KIT_DIR, exist_ok=True)
    x, y, row_h, width = 10, 10, 0, 1000
    cells, rects = [], {}
    for name, inner, w, h, sl in PARTS:
        cw, ch = w + PAD[0] + PAD[2], h + PAD[1] + PAD[3]
        if x + cw > width:
            x, y, row_h = 10, y + row_h + 10, 0
        if name.startswith("v3i_"):
            cells.append(f'<div class="kcell" style="left:{x}px;top:{y}px;width:{cw}px;height:{ch}px">{inner}</div>')
        else:
            cells.append(f'<div class="kcell" style="left:{x}px;top:{y}px;width:{cw}px;height:{ch}px">'
                         f'<div style="position:absolute;left:{PAD[0]}px;top:{PAD[1]}px;width:{w}px;height:{h}px">{inner}</div></div>')
        rects[name] = {"x": x * SCALE, "y": y * SCALE, "w": cw * SCALE, "h": ch * SCALE,
                       "slice": None if sl is None else [(sl[0] + PAD[0]) * SCALE, (sl[1] + PAD[1]) * SCALE,
                                                         (sl[2] + PAD[2]) * SCALE, (sl[3] + PAD[3]) * SCALE]}
        x += cw + 10
        row_h = max(row_h, ch)
    total_h = y + row_h + 10
    html = f"""<!doctype html><html><head><meta charset="utf-8">
<link href="https://fonts.googleapis.com/css2?family=Rubik:wght@800;900&family=Nunito:wght@700;800;900&display=block" rel="stylesheet">
<style>{style}
html, body {{ background: transparent !important; margin: 0; padding: 0; width: auto; }}
.kcell {{ position: absolute; }}
</style></head><body>{symbols}{EXTRA}{''.join(cells)}</body></html>"""
    open(KIT_HTML, "w", encoding="utf-8").write(html)
    json.dump({"scale": SCALE, "width": width * SCALE, "height": total_h * SCALE, "parts": rects},
              open(KIT_JSON, "w", encoding="utf-8"), indent=1)
    return width, total_h


if __name__ == "__main__":
    w, h = build()
    url = "file:///" + KIT_HTML.replace("\\", "/")
    subprocess.run([EDGE, "--headless=new", "--disable-gpu", "--hide-scrollbars", f"--force-device-scale-factor={SCALE}",
                    f"--window-size={w},{h}", "--default-background-color=00000000", "--virtual-time-budget=5000",
                    "--allow-file-access-from-files", f"--screenshot={KIT_PNG}", url],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    print("kit:", KIT_PNG, os.path.exists(KIT_PNG), len(PARTS), "parça")
