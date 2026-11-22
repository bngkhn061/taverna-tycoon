"""
v2.4 ses teşhisi: AudioCapture'ın yazdığı dinleyici kayıtlarını (Unity/captures/v24_audio_*.wav) çözümler.
Yalnızca gözlem yazar (karar vermez): tepe / RMS düzeyi, kırpılan örnek, ani sıçrama (tık), 6 kHz üstü enerji payı,
deneme tonunda (1 kHz) perde oynaması (sent).

Çalıştırma (Blender'ın Python'u, numpy var):
  "/c/Program Files/Blender Foundation/Blender 5.2/5.2/python/bin/python.exe" -I tools/audio/analyze_capture.py \
      Unity/captures/v24_audio_probe_once.wav Unity/captures/v24_audio_mix_once.wav ...
Çıktı: her dosya için bir satır grubu (stdout); --json <yol> ile makine okunur özet.
"""
import json
import struct
import sys

import numpy as np

PROBE_HZ = 1000.0


def read_wav(path):
    """PCM 16 bit ya da IEEE float 32 bit WAV → (örnekler [n, ch] float, örnekleme hızı)."""
    with open(path, "rb") as f:
        data = f.read()
    if data[:4] != b"RIFF" or data[8:12] != b"WAVE":
        raise ValueError("WAV değil: " + path)
    pos, fmt, ch, rate, bits, raw = 12, None, 2, 48000, 32, None
    while pos + 8 <= len(data):
        cid, size = data[pos:pos + 4], struct.unpack("<I", data[pos + 4:pos + 8])[0]
        body = data[pos + 8:pos + 8 + size]
        if cid == b"fmt ":
            fmt, ch, rate = struct.unpack("<HHI", body[:8])
            bits = struct.unpack("<H", body[14:16])[0]
        elif cid == b"data":
            raw = body
        pos += 8 + size + (size & 1)
    if fmt == 3 and bits == 32:
        x = np.frombuffer(raw, dtype="<f4").astype(np.float64)
    elif fmt == 1 and bits == 16:
        x = np.frombuffer(raw, dtype="<i2").astype(np.float64) / 32768.0
    else:
        raise ValueError(f"desteklenmeyen biçim {fmt}/{bits}")
    n = len(x) // ch
    return x[:n * ch].reshape(n, ch), rate


def db(v):
    return 20.0 * np.log10(max(float(v), 1e-12))


def moving_median_abs(x, win):
    """Pencere başına mutlak değerin medyanı (blok blok, hızlı yaklaşık)."""
    n = len(x)
    out = np.empty(n)
    for s in range(0, n, win):
        e = min(n, s + win)
        out[s:e] = np.median(np.abs(x[max(0, s - win):min(n, e + win)]))
    return out


def clicks(mono, rate):
    """
    Tık / ani sıçrama: ikinci fark (x[n] − (x[n−1]+x[n+1])/2) yerel düzeyinin 12 katını ve 0.02'yi aşan noktalar;
    5 ms içindekiler tek olay sayılır. Müzikteki keskin vuruşlar da bir miktar sayılır (önce/sonra karşılaştırması için).
    """
    hp = mono[1:-1] - 0.5 * (mono[:-2] + mono[2:])
    med = moving_median_abs(hp, int(rate * 0.05)) + 1e-6
    idx = np.where((np.abs(hp) > 12.0 * med) & (np.abs(hp) > 0.02))[0]
    events, last = 0, -10 ** 9
    gap = int(rate * 0.005)
    for i in idx:
        if i - last > gap:
            events += 1
        last = i
    return events


def spectrum(mono, rate, nfft=4096):
    """Welch güç yoğunluğu (Hann, %50 örtüşme) → (frekans, güç)."""
    if len(mono) < nfft:
        nfft = 1 << int(np.log2(max(256, len(mono))))
    w = np.hanning(nfft)
    hop = nfft // 2
    acc = np.zeros(nfft // 2 + 1)
    k = 0
    for s in range(0, len(mono) - nfft, hop):
        seg = mono[s:s + nfft] * w
        acc += np.abs(np.fft.rfft(seg)) ** 2
        k += 1
    return np.fft.rfftfreq(nfft, 1.0 / rate), acc / max(1, k)


def hf_share(mono, rate, cut=6000.0):
    f, p = spectrum(mono, rate)
    tot = p[f > 20].sum() + 1e-20
    return float(p[f >= cut].sum() / tot)


def hf_share_per_second(mono, rate, cut=6000.0):
    out = []
    for s in range(0, len(mono) - rate, rate):
        out.append(hf_share(mono[s:s + rate], rate, cut))
    return np.array(out) if out else np.array([0.0])


def probe_pitch(mono, rate):
    """
    1 kHz deneme tonunun anlık frekansı: 8192 örnek pencere, 1024 adım, 800–1200 Hz arasında tepe + parabol ara değerlemesi.
    Dönen: sent sapmaları dizisi (1000 Hz'e göre), ton düzeyi.
    """
    nfft, hop = 8192, 1024
    w = np.hanning(nfft)
    freqs = np.fft.rfftfreq(nfft, 1.0 / rate)
    lo, hi = np.searchsorted(freqs, 800), np.searchsorted(freqs, 1200)
    cents, levels = [], []
    for s in range(0, len(mono) - nfft, hop):
        mag = np.abs(np.fft.rfft(mono[s:s + nfft] * w))
        band = mag[lo:hi]
        k = int(np.argmax(band))
        if band[k] < 1e-3 * nfft:   # ton yok (sessiz pencere)
            continue
        if 0 < k < len(band) - 1:
            a, b, c = np.log(band[k - 1] + 1e-20), np.log(band[k] + 1e-20), np.log(band[k + 1] + 1e-20)
            d = 0.5 * (a - c) / (a - 2 * b + c)
        else:
            d = 0.0
        f = freqs[lo + k] + d * (rate / nfft)
        cents.append(1200.0 * np.log2(f / PROBE_HZ))
        levels.append(band[k])
    return np.array(cents), np.array(levels)


def analyze(path):
    x, rate = read_wav(path)
    mono = x.mean(axis=1)
    peak = float(np.max(np.abs(x))) if len(x) else 0.0
    rms = float(np.sqrt(np.mean(x ** 2))) if len(x) else 0.0
    r = {
        "file": path.replace("\\", "/").split("/")[-1],
        "seconds": round(len(x) / rate, 2),
        "rate": rate,
        "peak_dbfs": round(db(peak), 2),
        "rms_dbfs": round(db(rms), 2),
        "clipped_samples": int(np.sum(np.abs(x) >= 0.999)),
        "over_minus1db": int(np.sum(np.abs(x) >= 10 ** (-1 / 20))),
        "clicks": clicks(mono, rate),
        "clicks_per_min": round(clicks(mono, rate) / max(1e-6, len(x) / rate / 60.0), 1),
        "hf6k_share_pct": round(100.0 * hf_share(mono, rate), 3),
    }
    per = hf_share_per_second(mono, rate)
    r["hf6k_share_max_1s_pct"] = round(100.0 * float(per.max()), 3)
    if "probe" in r["file"]:
        c, lv = probe_pitch(mono, rate)
        if len(c):
            r["probe_windows"] = int(len(c))
            r["probe_cents_mean"] = round(float(np.mean(c)), 2)
            r["probe_cents_max_abs"] = round(float(np.max(np.abs(c))), 2)
            r["probe_cents_p95_abs"] = round(float(np.percentile(np.abs(c), 95)), 2)
            r["probe_cents_std"] = round(float(np.std(c)), 2)
            r["probe_cents_jitter"] = round(float(np.mean(np.abs(np.diff(c)))) if len(c) > 1 else 0.0, 2)
            # tonun dışındaki enerji (perde oynaması yan bantlar üretir): 1 kHz ± 15 Hz dışındaki güç payı
            f, p = spectrum(mono, rate, 16384)
            band = (f > PROBE_HZ - 15) & (f < PROBE_HZ + 15)
            r["probe_offtone_pct"] = round(100.0 * float(p[~band & (f > 20)].sum() / (p[f > 20].sum() + 1e-20)), 3)
        else:
            r["probe_windows"] = 0
    return r


def main(argv):
    out_json = None
    if "--json" in argv:
        i = argv.index("--json")
        out_json = argv[i + 1]
        argv = argv[:i] + argv[i + 2:]
    results = []
    for p in argv:
        r = analyze(p)
        results.append(r)
        print(r["file"])
        for k, v in r.items():
            if k != "file":
                print(f"  {k}: {v}")
    if out_json:
        with open(out_json, "w", encoding="utf-8") as f:
            json.dump(results, f, ensure_ascii=False, indent=2)


if __name__ == "__main__":
    main(sys.argv[1:])
