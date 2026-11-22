"""
v2.4 ortam sesleri (oyuncu testi 2026-11-22 madde 2: "oyunda cızırtı, tiz, titreme"; ortam seslerinden şüphe).

Eski ortam döngüleri (crowd_low/high, fire, day = kuşlar, night = cırcır böceği) kaldırılır; yerine iki yumuşak döngü:
  amb/room.ogg    sıcak oda tonu: kahverengi gürültü + ocak uğultusu (alçak geçirgen 450 Hz), yavaş nefes alan düzey.
  amb/murmur.ogg  uzak kalabalık uğultusu: 16 "ses" (gırtlak darbesi + iki formant, hece zarfı) üst üste; 1.8 kHz alçak
                  geçirgen, 0.5 sn yankı bulanıklığı. Tek tek söz seçilmez, tiz çıtırtı yok (2 kHz üstü ≈ 0).
Her ikisi tek kanal, 24 sn, ek yeri görünmez döngü (son 2 sn başa eşit güçle karışır), tepe ≤ −6 dBFS.
Unity'de normalleştirilmez (TavernaImport: amb için normalize kapalı); düzeyi Sfx.RoomDay/RoomNight/MurmurMax belirler.

Çalıştırma (numpy için Blender'ın Python'u; ffmpeg PATH'te ya da FFMPEG ortam değişkeninde):
  "/c/Program Files/Blender Foundation/Blender 5.2/5.2/python/bin/python.exe" -I tools/audio/build_amb_v24.py assets/audio
"""
import os
import subprocess
import sys

import numpy as np

SR = 44100
LOOP = 24.0
XF = 2.0
FF = os.environ.get("FFMPEG", "ffmpeg")
rng = np.random.default_rng(2411)


def fftlen(n):
    return 1 << int(np.ceil(np.log2(max(2, n))))


def shape(x, fn):
    """Sıfır fazlı spektral şekillendirme: fn(frekans) → kazanç."""
    n = len(x)
    N = fftlen(2 * n)
    X = np.fft.rfft(x, N)
    f = np.fft.rfftfreq(N, 1 / SR)
    return np.fft.irfft(X * fn(f), N)[:n]


def lowpass(x, fc, order=4):
    return shape(x, lambda f: 1 / np.sqrt(1 + (f / fc) ** (2 * order)))


def highpass(x, fc, order=2):
    return shape(x, lambda f: (f / fc) ** order / np.sqrt(1 + (f / fc) ** (2 * order)))


def smooth_noise(n, rate_hz):
    """0–1 arası yavaş değişen rastgele eğri (düzey "nefesi")."""
    k = max(4, int(n / SR * rate_hz) + 4)
    pts = rng.uniform(0, 1, k)
    xs = np.linspace(0, n, k)
    return np.interp(np.arange(n), xs, pts)


def loop_xfade(x, loop_n, xf_n):
    """x uzunluğu loop_n + xf_n: son xf_n örnek eşit güçle başa karışır → kesintisiz döngü."""
    out = x[:loop_n].copy()
    t = np.linspace(0, np.pi / 2, xf_n)
    out[:xf_n] = x[:xf_n] * np.sin(t) + x[loop_n:loop_n + xf_n] * np.cos(t)
    return out


def norm_rms(x, rms_db, peak_db=-6.0):
    x = x - np.mean(x)
    r = np.sqrt(np.mean(x ** 2)) or 1.0
    x = x * (10 ** (rms_db / 20) / r)
    p = np.max(np.abs(x))
    lim = 10 ** (peak_db / 20)
    if p > lim:
        x = x * (lim / p)
    return x


def room(seconds):
    n = int(seconds * SR)
    white = rng.standard_normal(n)
    brown = np.cumsum(white)
    brown -= np.convolve(brown, np.ones(4410) / 4410, mode="same")   # sızıntı: düşük frekans kayması yok
    brown = lowpass(highpass(brown, 35), 450)
    # ocak uğultusu: 70–160 Hz bandı, çok yavaş dalgalanır (çıtırtı yok)
    hum = shape(rng.standard_normal(n), lambda f: np.exp(-((np.log(np.maximum(f, 1) / 110)) ** 2) / (2 * 0.35 ** 2)))
    breath = 0.75 + 0.25 * smooth_noise(n, 0.15)
    x = (brown / np.std(brown) + 0.6 * hum / np.std(hum)) * breath
    return lowpass(x, 450)


def voice(n):
    """Tek konuşmacı: hece hece gırtlak darbesi + iki formant (yumuşak, anlamsız)."""
    out = np.zeros(n)
    f0 = rng.uniform(95, 210)
    t = int(rng.uniform(0, 1.5) * SR)
    while t < n:
        if rng.random() < 0.18:   # cümle arası
            t += int(rng.uniform(0.4, 1.4) * SR)
            continue
        dur = int(rng.uniform(0.11, 0.28) * SR)
        if t + dur >= n:
            break
        # gırtlak darbe dizisi (perde hece içinde hafif kayar)
        ff = f0 * (1 + 0.06 * rng.standard_normal()) * np.linspace(1.0, rng.uniform(0.9, 1.08), dur)
        ph = np.cumsum(ff / SR)
        src = (ph % 1.0) - 0.5
        src = np.diff(np.concatenate([[0.0], src]))   # türev: darbe
        f1, f2 = rng.uniform(320, 780), rng.uniform(900, 1650)
        seg = shape(src, lambda f: np.exp(-((f - f1) / 110) ** 2) + 0.55 * np.exp(-((f - f2) / 170) ** 2))
        env = np.sin(np.linspace(0, np.pi, dur)) ** 1.5
        out[t:t + dur] += seg * env
        t += dur + int(rng.uniform(0.02, 0.12) * SR)
    s = np.std(out)
    return out / s if s > 0 else out


def murmur(seconds, voices=16):
    n = int(seconds * SR)
    x = np.zeros(n)
    for _ in range(voices):
        x += voice(n) * rng.uniform(0.5, 1.0)
    # oda yankısı: üstel sönen, alçak geçirgen gürültü ile evrişim (0.5 sn) → sözler bulanıklaşır, uğultu olur
    ir_n = int(0.5 * SR)
    ir = rng.standard_normal(ir_n) * np.exp(-np.linspace(0, 6, ir_n))
    ir = lowpass(ir, 1500)
    ir /= np.sqrt(np.sum(ir ** 2))
    N = fftlen(n + ir_n)
    wet = np.fft.irfft(np.fft.rfft(x, N) * np.fft.rfft(ir, N), N)[:n]
    y = 0.45 * x + 0.85 * wet
    y = lowpass(highpass(y, 140), 1800)
    swell = 0.8 + 0.2 * smooth_noise(n, 0.2)   # kalabalık hafifçe kabarır / söner
    return y * swell


def encode(path, x, q=5):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    subprocess.run([FF, "-v", "error", "-y", "-f", "f32le", "-ar", str(SR), "-ac", "1", "-i", "-",
                    "-c:a", "libvorbis", "-q:a", str(q), path], input=x.astype(np.float32).tobytes(), check=True)


def stats(name, x):
    f = np.fft.rfftfreq(len(x), 1 / SR)
    p = np.abs(np.fft.rfft(x)) ** 2
    hf2 = 100 * p[f >= 2000].sum() / p[f > 20].sum()
    hf6 = 100 * p[f >= 6000].sum() / p[f > 20].sum()
    rms = 20 * np.log10(np.sqrt(np.mean(x ** 2)))
    pk = 20 * np.log10(np.max(np.abs(x)))
    seam = abs(x[0] - x[-1])
    print(f"{name}: {len(x) / SR:.1f} sn · RMS {rms:.1f} dBFS · tepe {pk:.1f} dBFS · 2 kHz üstü %{hf2:.3f} · 6 kHz üstü %{hf6:.4f} · ek yeri farkı {seam:.4f}")


def main():
    out = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else "assets/audio")
    loop_n, xf_n = int(LOOP * SR), int(XF * SR)
    r = norm_rms(loop_xfade(room(LOOP + XF), loop_n, xf_n), -24.0)
    m = norm_rms(loop_xfade(murmur(LOOP + XF), loop_n, xf_n), -22.0)
    stats("room", r)
    stats("murmur", m)
    encode(os.path.join(out, "amb", "room.ogg"), r)
    encode(os.path.join(out, "amb", "murmur.ogg"), m)
    print("yazıldı:", os.path.join(out, "amb"))


if __name__ == "__main__":
    main()
