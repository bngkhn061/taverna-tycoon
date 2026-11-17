"""v1.9 ses düzeltmesi (oyuncu testi 2026-11-17 madde 2: "ses ve müzik cızırtılı", Android ve iPhone).

1. amb/fire yeniden üretilir (build_audio.fire, yumuşak sürüm): eski döngü sn'de ~46 yüksek frekanslı dürtü içeriyordu.
2. Başlangıcı ya da sonu sıfırda olmayan efekt / konuşma klipleri (|ilk| ya da |son| örnek > 0.02) 2 ms / 5 ms sönümlenir.
3. Döngü ek yeri sıçrayan müzikler (son örnek ≠ ilk örnek) sonda 20 ms sönümlenir (her turda bir tık).
Yalnızca bu dosyalar yeniden kodlanır (Vorbis q6). assets/audio yerinde değişir; TavernaImport.CopySources Resources'a kopyalar.

Çalıştır (numpy için Blender'ın Python'u, FFMPEG ortam değişkeni ffmpeg.exe):
  "<Blender>/5.2/python/bin/python.exe" -I tools/audio/fix_v19.py assets/audio
"""
import glob
import os
import subprocess
import sys
import tempfile

import numpy as np

OUT = os.path.abspath(sys.argv[1])
here = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, here)
_empty = tempfile.mkdtemp()
sys.argv = [sys.argv[0], _empty, OUT]   # build_audio modül düzeyinde Kenney klasörünü ve çıkışı okur
import build_audio as ba  # noqa: E402

FF = ba.FF
EDGE = 0.02


def decode(path):
    ch = 2 if b"stereo" in subprocess.run([FF, "-hide_banner", "-i", path], capture_output=True).stderr else 1
    x = np.frombuffer(subprocess.run([FF, "-v", "error", "-i", path, "-f", "f32le", "-ac", str(ch), "-ar", str(ba.SR), "-"],
                                     capture_output=True, check=True).stdout, dtype=np.float32).astype(np.float64)
    return x.reshape(-1, 2) if ch == 2 else x


def encode(path, x, q=6):
    ch = 1 if x.ndim == 1 else 2
    subprocess.run([FF, "-v", "error", "-y", "-f", "f32le", "-ar", str(ba.SR), "-ac", str(ch), "-i", "-",
                    "-c:a", "libvorbis", "-q:a", str(q), path], input=x.astype(np.float32).tobytes(), check=True)


def first_last(x):
    m = x if x.ndim == 1 else np.max(np.abs(x), axis=1)
    return abs(m[0]), abs(m[-1])


def main():
    ba.fire()
    print("ocak döngüsü yeniden üretildi")
    for sub in ("sfx", "voice"):
        for p in sorted(glob.glob(os.path.join(OUT, sub, "*.ogg"))):
            x = decode(p)
            a, b = first_last(x)
            if a > EDGE or b > EDGE:
                encode(p, ba.edge_fade(x))
                print(f"kenar sönümü: {sub}/{os.path.basename(p)} (ilk {a:.3f}, son {b:.3f})")
    for p in sorted(glob.glob(os.path.join(OUT, "music", "*.ogg"))):
        x = decode(p)
        a, b = first_last(x)
        if a > EDGE or b > EDGE:
            encode(p, ba.edge_fade(x, fade_in=0.005, fade_out=0.02))
            print(f"döngü ek yeri: music/{os.path.basename(p)} (ilk {a:.3f}, son {b:.3f})")


if __name__ == "__main__":
    main()
