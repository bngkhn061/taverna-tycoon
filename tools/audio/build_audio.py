"""Taverna Tycoon ses üretimi (v1.1).

Kenney CC0 efektlerini seçip katmanlar, 3 müzik parçası besteler (lavta, flüt, keman,
bodhran, tef), oyuncak-dil konuşma sesleri ve kalabalık / ateş / gece / gündüz ortam
döngülerini sentezler. Hepsi .ogg olarak assets/audio altına yazılır.

Çalıştır (numpy için Blender'ın Python'u):
  "<Blender>/5.2/python/bin/python.exe" -I tools/audio/build_audio.py <kenney_x_dir> assets/audio
FFMPEG ortam değişkeni ffmpeg.exe yolunu verir.
"""
import math
import os
import shutil
import subprocess
import sys

import numpy as np

SR = 44100
KEN, OUT = sys.argv[1], sys.argv[2]
FF = os.environ.get("FFMPEG", "ffmpeg")
rng = np.random.default_rng(1907)

# ---------------------------------------------------------------- dosya g/ç
_files = {}
for root, _, names in os.walk(KEN):
    for nm in names:
        if nm.endswith(".ogg"):
            _files[nm[:-4]] = os.path.join(root, nm)


def load(name):
    raw = subprocess.run([FF, "-v", "error", "-i", _files[name], "-f", "f32le", "-ac", "1", "-ar", str(SR), "-"],
                         capture_output=True, check=True).stdout
    return trim(np.frombuffer(raw, dtype=np.float32).astype(np.float64))


def save(rel, x, peak=0.85, q=5):
    x = np.asarray(x, dtype=np.float64)
    m = np.max(np.abs(x)) or 1.0
    if peak:
        x = x * (peak / m)
    ch = 1 if x.ndim == 1 else 2
    path = os.path.join(OUT, rel + ".ogg")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    subprocess.run([FF, "-v", "error", "-y", "-f", "f32le", "-ar", str(SR), "-ac", str(ch), "-i", "-",
                    "-c:a", "libvorbis", "-q:a", str(q), path], input=x.astype(np.float32).tobytes(), check=True)
    return path


def trim(x, thr=0.0015):
    idx = np.nonzero(np.abs(x) > thr * (np.max(np.abs(x)) or 1) * 10)[0]
    if len(idx) == 0:
        return x
    x = x[max(0, idx[0] - 40): idx[-1] + int(0.03 * SR)].copy()
    f = min(len(x), int(0.01 * SR))
    x[-f:] *= np.linspace(1, 0, f)
    return x


# ---------------------------------------------------------------- DSP yardımcıları
def fftlen(n):
    return 1 << int(math.ceil(math.log2(max(2, n))))


def shape(x, fn):
    """Sıfır-fazlı spektral şekillendirme: fn(frekans dizisi) -> kazanç."""
    n = len(x)
    N = fftlen(2 * n)
    X = np.fft.rfft(x, N)
    f = np.fft.rfftfreq(N, 1 / SR)
    return np.fft.irfft(X * fn(f), N)[:n]


def lowpass(x, fc):
    return shape(x, lambda f: 1 / np.sqrt(1 + (f / fc) ** 4))


def highpass(x, fc):
    return shape(x, lambda f: (f / fc) ** 2 / np.sqrt(1 + (f / fc) ** 4))


def band(x, fc, width=0.5):
    return shape(x, lambda f: np.exp(-(np.log(np.maximum(f, 1) / fc) ** 2) / (2 * width ** 2)))


def peak(f, fc, bw, g):
    return g / (1 + ((f - fc) / (bw / 2)) ** 2)


def repitch(x, r):
    if abs(r - 1) < 1e-6:
        return x
    pos = np.arange(0, len(x) - 1, r)
    return np.interp(pos, np.arange(len(x)), x)


def comb(x, d, g):
    y = x.copy()
    for s in range(d, len(y), d):
        e = min(s + d, len(y))
        y[s:e] += g * y[s - d:e - d]
    return y


def allpass(x, d, g):
    y = -g * x
    y[d:] += x[:-d]
    for s in range(d, len(y), d):
        e = min(s + d, len(y))
        y[s:e] += g * y[s - d:e - d]
    return y


def reverb(x, wet=0.2, room=1.0, damp=4500, spread=0):
    short = len(x) < 4 * SR
    if short:                              # kısa efekt: kuyruk için yer aç, sonra kırp
        x = np.concatenate([x, np.zeros(int(0.8 * room * SR))])
    combs = [1116, 1188, 1277, 1356, 1422, 1491]
    r = np.zeros(len(x))
    for d in combs:
        r += comb(x, int((d + spread) * room), 0.8)
    r /= len(combs)
    for d in (556, 441, 341):
        r = allpass(r, int((d + spread) * room), 0.5)
    y = x + wet * lowpass(r, damp)
    return trim(y) if short else y


def reverb_st(st, wet=0.2, room=1.0, damp=4500):
    return np.stack([reverb(st[:, 0], wet, room, damp, 0), reverb(st[:, 1], wet, room, damp, 23)], axis=1)


def place(parts, extra=0.0):
    """parts: [(dizi, saniye, kazanç)] -> karışım (mono)."""
    end = max(int(t * SR) + len(x) for x, t, _ in parts) + int(extra * SR)
    out = np.zeros(end)
    for x, t, g in parts:
        i = int(t * SR)
        out[i:i + len(x)] += g * x
    return out


def loopify(x, L):
    out = x[:L].copy()
    tail = x[L:]
    k = min(len(tail), L)
    out[:k] += tail[:k]
    return out


def softclip(x, drive=1.0):
    return np.tanh(x * drive) / np.tanh(drive)


def env_ar(n, a, r):
    t = np.arange(n) / SR
    dur = n / SR
    return np.minimum(1, t / max(a, 1e-4)) * np.clip((dur - t) / max(r, 1e-4), 0, 1)


# ---------------------------------------------------------------- notalar
_NOTE = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}


def hz(n):
    semis = _NOTE[n[0]] + n[1:-1].count("#") - n[1:-1].count("b")
    midi = 12 * (int(n[-1]) + 1) + semis
    return 440.0 * 2 ** ((midi - 69) / 12)


def mel(*bars):
    out = []
    for s in bars:
        for tok in s.split():
            n, _, d = tok.partition(":")
            out.append((n, float(d) if d else 1.0))
    return out


# ---------------------------------------------------------------- enstrümanlar
_pc = {}


def pluck(f, dur, bright=0.5):
    """Karplus-Strong tel (lavta). Tamsayı gecikme hatası yeniden örneklemeyle düzeltilir."""
    key = (round(f, 2), round(dur, 2), bright)
    if key in _pc:
        return _pc[key]
    N = max(4, int(SR / f))
    r = f / (SR / N)                      # >= 1: gerçek perdeye çıkmak için hızlandır
    L = int(dur * SR * r) + 2
    y = np.zeros(L + N)
    burst = rng.uniform(-1, 1, N)
    for _ in range(int((1 - bright) * 5)):
        burst = 0.5 * (burst + np.roll(burst, 1))
    y[:N] = burst - burst.mean()
    k = 0.5 * (0.9935 + 0.005 * bright)
    for s in range(N, L + N, N):
        e = min(s + N, L + N)
        a = y[s - N:e - N]
        b = np.concatenate(([0.0], y[s - N:e - N - 1])) if s == N else y[s - N - 1:e - N - 1]
        y[s:e] = k * (a + b)
    out = repitch(y[:L], r)[:int(dur * SR)]
    fl = min(len(out), int(0.04 * SR))
    out[-fl:] *= np.linspace(1, 0, fl)
    _pc[key] = out
    return out


def flute(f, dur):
    n = int(dur * SR)
    t = np.arange(n) / SR
    vd = np.clip((t - 0.12) / 0.25, 0, 1)
    fm = f * (1 + 0.007 * np.sin(2 * np.pi * 5.0 * t) * vd)
    ph = 2 * np.pi * np.cumsum(fm) / SR
    s = np.sin(ph) + 0.22 * np.sin(2 * ph) + 0.07 * np.sin(3 * ph) + 0.03 * np.sin(4 * ph)
    breath = rng.standard_normal(n) * 0.05 * (0.6 + 0.4 * np.sin(ph))
    env = env_ar(n, min(0.05, dur * 0.3), min(0.08, dur * 0.3)) * (0.88 + 0.12 * np.minimum(1, t / 0.3))
    return (s + breath) * env


def fiddle(f, dur):
    n = int(dur * SR)
    t = np.arange(n) / SR
    vd = np.clip((t - 0.1) / 0.2, 0, 1)
    fm = f * (1 + 0.009 * np.sin(2 * np.pi * 6.0 * t) * vd)
    ph = 2 * np.pi * np.cumsum(fm) / SR
    K = int(min(18, 7000 / f))
    s = sum(np.sin(k * ph) / k for k in range(1, K + 1))
    bow = rng.standard_normal(n) * 0.04
    env = env_ar(n, min(0.025, dur * 0.2), min(0.05, dur * 0.3)) * (1 + 0.3 * np.exp(-t / 0.05))
    return (s + bow) * env


def pad(freqs, dur):
    n = int(dur * SR)
    t = np.arange(n) / SR
    out = np.zeros(n)
    for f in freqs:
        for det in (0.997, 1.003):
            ph = 2 * np.pi * f * det * t + rng.uniform(0, 6.28)
            out += sum(np.sin(k * ph) / k ** 1.6 for k in range(1, 9))
    return out * env_ar(n, 0.5, 0.7) * (1 + 0.08 * np.sin(2 * np.pi * 0.3 * t))


def _bodhran(acc):
    n = int(0.5 * SR)
    t = np.arange(n) / SR
    f = 60 + 95 * np.exp(-t / 0.03)
    s = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 0.17)
    thump = band(rng.standard_normal(n), 160, 0.5) * np.exp(-t / 0.02) * 0.5
    skin = band(rng.standard_normal(n), 1300, 0.4) * np.exp(-t / 0.012) * 0.25
    return (s + thump + skin) * acc


def _tamb():
    n = int(0.25 * SR)
    t = np.arange(n) / SR
    nz = highpass(rng.standard_normal(n), 6500)
    env = np.exp(-t / 0.05) + 0.6 * np.exp(-np.maximum(t - 0.02, 0) / 0.06) * (t > 0.02)
    return nz * env * 0.5


DRUM = [_bodhran(1.0) for _ in range(4)]
TAMB = [_tamb() for _ in range(4)]


def drum(acc=1.0):
    return DRUM[rng.integers(4)] * acc


def tamb(acc=1.0):
    return TAMB[rng.integers(4)] * acc


CHORD = {  # lavta açılımı (ilk nota bas)
    "D": ["D3", "A3", "D4", "F#4"], "G": ["G2", "D3", "G3", "B3"], "A": ["A2", "E3", "A3", "C#4"],
    "Em": ["E2", "B2", "E3", "G3"], "Bm": ["B2", "F#3", "B3", "D4"], "C": ["C3", "G3", "C4", "E4"],
    "Am": ["A2", "E3", "A3", "C4"], "F": ["F2", "C3", "F3", "A3"], "E": ["E2", "B2", "E3", "G#3"],
    "Dm": ["D3", "A3", "D4", "F4"],
}


def bass_hz(n):
    """Bas notası 70-140 Hz aralığına katlanır (telefon hoparlöründe duyulsun, çamurlaşmasın)."""
    f = hz(n)
    while f > 140:
        f /= 2
    while f < 70:
        f *= 2
    return f


def strum(notes, dur, gain=1.0, gap=0.018, bright=0.5, up=False):
    seq = list(reversed(notes)) if up else notes
    parts = [(pluck(hz(n), dur, bright), i * gap, gain * (0.85 + 0.15 * i / len(seq))) for i, n in enumerate(seq)]
    return place(parts)


# ---------------------------------------------------------------- karıştırıcı
class Mix:
    def __init__(self, seconds):
        self.n = int(seconds * SR)
        self.tracks = {}

    def tr(self, name, pan=0.0, eq=None):
        if name not in self.tracks:
            self.tracks[name] = [np.zeros(self.n), pan, eq]
        return name

    def add(self, name, x, t, g=1.0, human=True):
        buf = self.tracks[name][0]
        if human:
            t += rng.normal(0, 0.005)
            g *= rng.uniform(0.9, 1.05)
        i = max(0, int(t * SR))
        if i >= self.n:
            return
        e = min(self.n, i + len(x))
        buf[i:e] += g * x[:e - i]

    def render(self, wet, room=1.0):
        st = np.zeros((self.n, 2))
        for buf, pan, eq in self.tracks.values():
            if eq:
                buf = shape(buf, eq)
            a = (pan + 1) * math.pi / 4
            st[:, 0] += buf * math.cos(a)
            st[:, 1] += buf * math.sin(a)
        st = reverb_st(st, wet, room)
        return st


LUTE_EQ = lambda f: (0.8 + peak(f, 220, 160, 0.4) + peak(f, 520, 300, 0.4) + peak(f, 2500, 1500, 0.25)) / np.sqrt(1 + (f / 5000) ** 4)
FIDDLE_EQ = lambda f: (0.5 + peak(f, 290, 160, 0.8) + peak(f, 1000, 500, 0.6) + peak(f, 2800, 1400, 0.5)) / np.sqrt(1 + (f / 5500) ** 4)
FLUTE_EQ = lambda f: 1 / np.sqrt(1 + (f / 6500) ** 4) * (1 - 0.7 / (1 + (f / 200) ** 2))
DRUM_EQ = lambda f: (f / 70) ** 2 / np.sqrt(1 + (f / 70) ** 4) / np.sqrt(1 + (f / 3000) ** 2)


def lead(mx, name, notes, t0, unit, kind, gain, octave=0):
    t = t0
    for n, d in notes:
        if n != "r":
            f = hz(n) * 2 ** octave
            dur = d * unit
            if kind == "pluck":
                x = pluck(f, max(0.5, dur * 2.2), 0.75)
            elif kind == "flute":
                x = flute(f, dur * 0.96)
            else:
                x = fiddle(f, dur * 0.94)
            mx.add(name, x, t, gain)
        t += d * unit
    return t


def chord_at(chords, bar):
    return chords[bar]


def finish(mx, loop_s, wet, name, room=1.0):
    st = mx.render(wet, room)
    L = int(loop_s * SR)
    st = np.stack([loopify(st[:, 0], L), loopify(st[:, 1], L)], axis=1)
    st = np.stack([highpass(st[:, 0], 75), highpass(st[:, 1], 75)], axis=1)
    st *= 0.12 / (np.sqrt(np.mean(st ** 2)) or 1)      # ~-18 dBFS RMS: üç parça aynı yükseklikte
    st = np.tanh(st / 0.9) * 0.9                        # yumuşak tepe sınırlayıcı
    save(f"music/{name}", st, peak=0, q=4)
    print("müzik", name, f"{loop_s:.1f}s")


# ---------------------------------------------------------------- 1) Gündüz: jig (D majör, 6/8)
def song_day():
    e = 60 / (112 * 3)                    # sekizlik
    bar = 6 * e
    A = mel("D5:2 A4 F#4 A4 D5", "F#5:2 E5 D5 A4 F#4", "G4 B4 D5 G5:2 F#5", "E5 C#5 A4 E5:2 C#5",
            "D5:2 A4 F#4 A4 D5", "B4 D5 G5 F#5 E5 D5", "E5 F#5 E5 C#5 B4 A4", "D5:3 D5:2 r")
    B = mel("F#5 A5 F#5 D5 F#5 A5", "B5:2 A5 G5 F#5 E5", "F#5 A5 F#5 D5:2 A4", "E5 F#5 G5 A5:2 E5",
            "F#5 A5 F#5 D5 F#5 A5", "B5 G5 E5 A5 F#5 D5", "E5 C#5 A4 C#5 E5 G5", "D5:3 A4:2 r")
    chA = [["D"], ["D"], ["G"], ["A"], ["D"], ["G"], ["A"], ["D"]]
    chB = [["D"], ["G"], ["D"], ["A"], ["D"], ["G"], ["A"], ["D"]]
    order = [(A, chA), (A, chA), (B, chB), (B, chB)]
    loop = 32 * bar
    mx = Mix(loop + 4)
    for n_, p in (("lute", -0.35), ("bass", 0.0), ("flute", 0.3), ("melody", -0.15), ("drum", 0.1), ("tamb", 0.45)):
        mx.tr(n_, p, {"lute": LUTE_EQ, "bass": LUTE_EQ, "melody": LUTE_EQ, "flute": FLUTE_EQ, "drum": DRUM_EQ}.get(n_))
    t = 0.0
    for p, (m, ch) in enumerate(order):
        if p == 0:
            lead(mx, "melody", m, t, e, "pluck", 0.55)
        else:
            lead(mx, "flute", m, t, e, "flute", 0.32)
            if p == 3:
                lead(mx, "melody", m, t, e, "pluck", 0.25, octave=-1)
        for b in range(8):
            tb = t + b * bar
            c = CHORD[ch[b][0]]
            mx.add("bass", pluck(bass_hz(c[0]), 0.6, 0.3), tb, 0.6)
            mx.add("bass", pluck(bass_hz(c[1]), 0.45, 0.3), tb + 3 * e, 0.45)
            mx.add("lute", strum(c[1:], 0.5, 0.35), tb + 3 * e, 1.0)
            mx.add("lute", strum(c[1:], 0.3, 0.2, up=True), tb + 5 * e, 0.7)
            if p >= 1:
                for k, acc in ((0, 1.0), (2, 0.35), (3, 0.7), (5, 0.35)):
                    mx.add("drum", drum(acc), tb + k * e, 0.5)
            if p >= 2:
                mx.add("tamb", tamb(), tb + 3 * e, 0.35)
                mx.add("tamb", tamb(0.5), tb + 0 * e, 0.3)
        t += 8 * bar
    finish(mx, loop, 0.16, "day_jig")


# ---------------------------------------------------------------- 2) Akşam: balad (A minör, 3/4)
def song_evening():
    q = 60 / 74
    bar = 3 * q
    M = mel("A4:2 C5", "B4:2 G4", "C5:2 A4", "B4:3", "E5:2 D5", "D5 C5 B4", "C5:1.5 A4:.5 G#4", "A4:3",
            "E5:2 G5", "D5:2 B4", "C5 E5 A5", "G5:2 E5", "F5:2 E5", "E5 D5 C5", "D5:1.5 C5:.5 B4", "A4:3")
    CH = [[("Am", 3)], [("G", 3)], [("F", 3)], [("E", 3)], [("Am", 3)], [("G", 3)], [("F", 2), ("E", 1)], [("Am", 3)],
          [("C", 3)], [("G", 3)], [("Am", 3)], [("Em", 3)], [("F", 3)], [("C", 3)], [("Dm", 2), ("E", 1)], [("Am", 3)]]
    loop = 32 * bar
    mx = Mix(loop + 5)
    mx.tr("arp", -0.3, LUTE_EQ)
    mx.tr("pad", 0.0, lambda f: 1 / np.sqrt(1 + (f / 1100) ** 4))
    mx.tr("flute", 0.3, FLUTE_EQ)
    mx.tr("melody", -0.1, LUTE_EQ)
    mx.tr("drum", 0.1, DRUM_EQ)
    e = q / 2
    t = 0.0
    for p in range(2):
        if p == 0:
            lead(mx, "melody", M, t, q, "pluck", 0.5)
            lead(mx, "flute", M, t, q, "flute", 0.08, octave=-1)
        else:
            lead(mx, "flute", M, t, q, "flute", 0.3)
        for b in range(16):
            tb = t + b * bar
            beat = 0
            for name, beats in CH[b]:
                c = CHORD[name]
                mx.add("pad", pad([hz(c[0]), hz(c[1])], beats * q + 0.4), tb + beat * q, 0.035)
                pattern = [c[0], c[1], c[2], c[3], c[2], c[1]]
                for k in range(beats * 2):
                    nn = pattern[(beat * 2 + k) % 6]
                    mx.add("arp", pluck(hz(nn), 1.1, 0.35), tb + (beat * 2 + k) * e, 0.42 if k == 0 else 0.3)
                beat += beats
            if p == 1:
                mx.add("drum", drum(0.6), tb, 0.35)
                mx.add("drum", drum(0.25), tb + 2 * q, 0.35)
        t += 16 * bar
    finish(mx, loop, 0.28, "evening_ballad", room=1.15)


# ---------------------------------------------------------------- 3) Yoğun saat: reel (E dorian, 4/4)
def song_rush():
    e = 60 / 152 / 2
    bar = 8 * e
    A = mel("E5 G5 B5 G5 E5 B4 G4 B4", "D5 F#5 A5 F#5 D5 A4 F#4 A4", "E5 G5 B5 G5 A5 G5 F#5 E5",
            "F#5 E5 D5 F#5 B4 D5 F#5 D5", "E5 G5 B5 G5 E5 B4 G4 B4", "D5 F#5 A5 F#5 D5 A4 F#4 A4",
            "G5 E5 C5 E5 F#5 D5 A4 D5", "E5 B4 G4 B4 E5:4")
    B = mel("G5 A5 B5 A5 G5 D5 B4 D5", "F#5 G5 A5 G5 F#5 D5 A4 D5", "E5 F#5 G5 F#5 E5 B4 G4 B4",
            "F#5 E5 D5 C#5 B4 D5 F#5 B5", "C6 B5 A5 G5 E5 G5 C5 E5", "D6 C6 B5 A5 F#5 A5 D5 F#5",
            "E5 F#5 G5 B5 A5 F#5 D5 F#5", "E5:2 B4 G4 E4:4")
    chA = [["Em"], ["D"], ["Em"], ["D", "Bm"], ["Em"], ["D"], ["C", "D"], ["Em"]]
    chB = [["G"], ["D"], ["Em"], ["Bm"], ["C"], ["D"], ["Em", "D"], ["Em"]]
    order = [(A, chA), (B, chB), (A, chA), (B, chB)]
    loop = 32 * bar
    mx = Mix(loop + 4)
    for n_, p in (("fiddle", -0.2), ("flute", 0.35), ("lute", 0.3), ("bass", 0.0), ("drum", 0.0), ("tamb", -0.45)):
        mx.tr(n_, p, {"fiddle": FIDDLE_EQ, "flute": FLUTE_EQ, "lute": LUTE_EQ, "bass": LUTE_EQ, "drum": DRUM_EQ}.get(n_))
    t = 0.0
    for p, (m, ch) in enumerate(order):
        lead(mx, "fiddle", m, t, e, "fiddle", 0.16)
        if p >= 2:
            lead(mx, "flute", m, t, e, "flute", 0.22)
        for b in range(8):
            tb = t + b * bar
            for half, name in enumerate(ch[b] if len(ch[b]) == 2 else ch[b] * 2):
                c = CHORD[name]
                th = tb + half * 4 * e
                mx.add("bass", pluck(bass_hz(c[0]), 0.4, 0.3), th, 0.6)
                mx.add("lute", strum(c[1:], 0.25, 0.3, gap=0.012), th + 2 * e, 0.9)
            for k, acc in ((0, 1.0), (3, 0.5), (4, 0.85), (6, 0.5), (7, 0.3)):
                mx.add("drum", drum(acc), tb + k * e, 0.5)
            for k in range(1, 8, 2):
                mx.add("tamb", tamb(0.9 if k in (3, 7) else 0.5), tb + k * e, 0.4)
        t += 8 * bar
    finish(mx, loop, 0.14, "rush_reel")


# ---------------------------------------------------------------- oyuncak dil (konuşma)
VOW = {"a": (730, 1090, 2440), "e": (530, 1840, 2480), "i": (300, 2200, 2900), "o": (570, 840, 2410), "u": (320, 870, 2240)}
CSOUND = {"m": (280, 1100, 2300), "n": (280, 1500, 2500), "l": (360, 1300, 2700), "r": (420, 1300, 1700),
          "y": (280, 2200, 3000), "v": (300, 1200, 2400)}
CONS = "bdgkptmnlrshfvy"
CW = np.array([6, 6, 3, 5, 3, 6, 6, 6, 6, 7, 4, 4, 2, 3, 3], float)
CW /= CW.sum()
BANKS = {
    "deep": dict(f0=88, fs=0.88, rate=0.95, breath=0.06),
    "male": dict(f0=122, fs=1.0, rate=1.08, breath=0.05),
    "female": dict(f0=210, fs=1.17, rate=1.15, breath=0.07),
    "tiny": dict(f0=300, fs=1.32, rate=1.45, breath=0.05),
    "old": dict(f0=108, fs=0.96, rate=0.82, breath=0.1, trem=True),
    "posh": dict(f0=150, fs=1.08, rate=1.0, breath=0.04, nasal=True),
}


def formant(x, F, fs, amps=(1.0, 0.55, 0.28), bws=(90, 110, 170), nasal=False):
    def H(f):
        h = np.full_like(f, 0.015)
        for Fk, A, Bk in zip(F, amps, bws):
            h += A / (1 + ((f - Fk * fs) / (Bk * fs / 2)) ** 2)
        if nasal:
            h *= 1 - 0.6 / (1 + ((f - 1000) / 250) ** 2)
            h += 0.3 / (1 + ((f - 250) / 60) ** 2)
        return h
    return shape(x, H)


def contour(x, kind):
    if kind == "fall":
        return 1.07 - 0.2 * x + 0.05 * np.sin(x * 7)
    if kind == "rise":
        return 0.95 + 0.05 * np.sin(x * 6) + 0.3 * np.clip((x - 0.6) / 0.4, 0, 1) ** 2
    if kind == "excl":
        return 0.95 + 0.4 * np.exp(-((x - 0.35) / 0.25) ** 2)
    if kind == "laugh":
        return 1.5 - 0.35 * x
    if kind == "grumble":
        return 0.85 - 0.18 * x
    return np.ones_like(x)


def phrase(bank, sylls, kind="fall", mul=1.0, breathy=0.0, creak=0.0, speed=1.0, gap=0.12):
    B = BANKS[bank]
    rate = B["rate"] * speed
    segs, t = [], 0.03
    for c, v, stress in sylls:
        if c:
            cd = {"s": 0.075, "h": 0.05, "f": 0.06}.get(c, 0.045) / rate
            segs.append(("c", c, t, cd))
            t += cd * 0.85
        if v:
            vd = rng.uniform(0.07, 0.13) / rate * (1.4 if stress else 1.0)
            segs.append(("v", v, t, vd, stress))
            t += vd
        if rng.random() < gap:
            t += 0.06 / rate
    total = t + 0.1
    n = int(total * SR)
    tt = np.arange(n) / SR
    f0 = B["f0"] * mul * contour(tt / total, kind)
    for s in segs:                         # vurgulu hecede perde tümseği
        if s[0] == "v" and s[4]:
            f0 *= 1 + 0.13 * np.exp(-((tt - s[2] - s[3] / 2) / (s[3] * 0.6)) ** 2)
    wob = np.cumsum(rng.standard_normal(n)) / SR
    f0 *= 1 + 0.02 * (wob - wob.mean()) / (np.std(wob) + 1e-9) * 0.5
    if B.get("trem"):
        f0 *= 1 + 0.025 * np.sin(2 * np.pi * 5.5 * tt)
    ph = 2 * np.pi * np.cumsum(f0) / SR
    K = int(5200 / f0.max())
    src = sum(np.sin(k * ph) / k ** 1.1 for k in range(1, K + 1))
    if creak:
        src *= 1 - creak * (0.5 + 0.5 * np.sign(np.sin(ph / 2)))
    src = src + rng.standard_normal(n) * (B["breath"] + breathy) * 3
    out = np.zeros(n)
    ov = int(0.025 * SR)
    for s in segs:
        i0 = int(s[2] * SR)
        L = min(n - i0, int(s[3] * SR) + ov)
        if L <= 8:
            continue
        w = env_ar(L, 0.015, 0.03)
        if s[0] == "v":
            g = rng.uniform(0.8, 1.0) * (1.25 if s[4] else 1.0)
            out[i0:i0 + L] += g * formant(src[i0:i0 + L] * w, VOW[s[1]], B["fs"], nasal=B.get("nasal", False))
            continue
        c = s[1]
        if c in CSOUND:
            out[i0:i0 + L] += 0.45 * formant(src[i0:i0 + L] * w, CSOUND[c], B["fs"], amps=(1, 0.15, 0.08))
        elif c in "bdg":
            vb = lowpass(src[i0:i0 + L], 300) * env_ar(L, 0.005, 0.02) * 0.4
            bl = int(0.012 * SR)
            burst = band(rng.standard_normal(bl), {"b": 700, "d": 3500, "g": 2200}[c], 0.5) * np.exp(-np.arange(bl) / (0.004 * SR))
            out[i0:i0 + L] += vb
            j = min(n, i0 + L - bl)
            out[j:j + bl] += 0.15 * burst[:n - j]
        elif c in "ptk":
            bl = int(0.015 * SR)
            burst = band(rng.standard_normal(bl), {"p": 800, "t": 4500, "k": 2200}[c], 0.5) * np.exp(-np.arange(bl) / (0.005 * SR))
            j = min(n - bl, i0 + L - bl)
            out[j:j + bl] += 0.2 * burst
        else:
            fc = {"s": 6500, "h": 1800, "f": 4000}[c]
            nz = band(rng.standard_normal(L), fc, 0.6 if c != "h" else 1.0) * env_ar(L, 0.02, 0.03)
            out[i0:i0 + L] += {"s": 0.07, "h": 0.06, "f": 0.04}[c] * nz
    out = highpass(out, 70)
    return trim(out)


def words(nsyl):
    out = []
    for _ in range(nsyl):
        c = str(rng.choice(list(CONS), p=CW)) if rng.random() < 0.85 else ""
        v = str(rng.choice(list("aeiou"), p=[0.3, 0.25, 0.15, 0.2, 0.1]))
        out.append((c, v, rng.random() < 0.25))
    return out


def laugh(bank, vowel=None):
    v = vowel or {"tiny": "i", "female": "e", "old": "o", "deep": "o"}.get(bank, "a")
    nh = int(rng.integers(4, 7))
    return phrase(bank, [("h", v, i == 0) for i in range(nh)], "laugh", breathy=0.12, speed=1.35, gap=0.0)


def voices():
    made = 0
    for bank in BANKS:
        for i in range(6):
            kind = ["fall", "fall", "rise", "excl", "fall", "rise"][i]
            x = phrase(bank, words(int(rng.integers(3, 8))), kind)
            save(f"voice/{bank}_chat_{i + 1}", reverb(x, 0.06, 0.6), 0.8)
            made += 1
        for i in range(2):
            save(f"voice/{bank}_laugh_{i + 1}", reverb(laugh(bank), 0.06, 0.6), 0.8)
            hx = phrase(bank, [("h", "e", True)] if i == 0 else [("", "o", True), ("h", "o", False)], "excl", mul=1.1)
            save(f"voice/{bank}_happy_{i + 1}", reverb(hx, 0.06, 0.6), 0.8)
            gx = phrase(bank, [("m", "u", False), ("m", "o", True)] if i == 0 else [("b", "u", False), ("r", "u", True), ("m", "", False)],
                        "grumble", creak=0.45, speed=0.75, mul=0.95)
            save(f"voice/{bank}_grumble_{i + 1}", reverb(gx, 0.06, 0.6), 0.8)
            ox = phrase(bank, words(2) + [("", "a", True)], "excl", speed=1.1)
            save(f"voice/{bank}_order_{i + 1}", reverb(ox, 0.06, 0.6), 0.8)
            made += 4
    print("konuşma klibi", made)


# ---------------------------------------------------------------- ortam döngüleri
def stereo_scatter(L, events, wet, damp=3000, room=1.2):
    st = np.zeros((L + 6 * SR, 2))
    for x, t, g, pan in events:
        i = int(t * SR)
        a = (pan + 1) * math.pi / 4
        e = min(len(st), i + len(x))
        st[i:e, 0] += g * math.cos(a) * x[:e - i]
        st[i:e, 1] += g * math.sin(a) * x[:e - i]
    st = np.stack([lowpass(st[:, 0], damp), lowpass(st[:, 1], damp)], axis=1)
    st = reverb_st(st, wet, room)
    return np.stack([loopify(st[:, 0], L), loopify(st[:, 1], L)], axis=1)


def crowd(name, seconds, n_chat, n_laugh, n_clink):
    L = int(seconds * SR)
    ev = []
    banks = list(BANKS)
    clinks = [load(f"impactGlass_light_00{i}") for i in range(5)]
    for _ in range(n_chat):
        b = banks[rng.integers(len(banks))]
        ev.append((phrase(b, words(int(rng.integers(3, 9))), ["fall", "rise", "excl"][rng.integers(3)]),
                   rng.uniform(0, seconds), rng.uniform(0.25, 0.6), rng.uniform(-0.8, 0.8)))
    for _ in range(n_laugh):
        ev.append((laugh(banks[rng.integers(len(banks))]), rng.uniform(0, seconds), rng.uniform(0.3, 0.55), rng.uniform(-0.8, 0.8)))
    for _ in range(n_clink):
        ev.append((repitch(clinks[rng.integers(5)], rng.uniform(0.85, 1.15)), rng.uniform(0, seconds), rng.uniform(0.15, 0.35), rng.uniform(-0.9, 0.9)))
    st = stereo_scatter(L, ev, 0.35, damp=2600)
    save(f"amb/{name}", st, 0.7, q=3)
    print("ortam", name)


def fire(seconds=14):
    L = int(seconds * SR)
    rumble = lowpass(rng.standard_normal(L + SR), 180) * 0.6
    rumble *= 1 + 0.3 * np.sin(2 * np.pi * 0.23 * np.arange(len(rumble)) / SR)
    ev = []
    for _ in range(int(seconds * 14)):
        n = int(rng.uniform(0.001, 0.004) * SR)
        x = highpass(rng.standard_normal(n + 64), 2500)[:n] * np.exp(-np.arange(n) / (0.0008 * SR))
        ev.append((x, rng.uniform(0, seconds), rng.uniform(0.2, 1.0) ** 2, rng.uniform(-0.3, 0.3)))
    for _ in range(int(seconds * 0.8)):
        n = int(0.03 * SR)
        x = band(rng.standard_normal(n), rng.uniform(900, 2500), 0.5) * np.exp(-np.arange(n) / (0.006 * SR))
        ev.append((x, rng.uniform(0, seconds), 1.2, rng.uniform(-0.3, 0.3)))
    st = stereo_scatter(L, ev, 0.1, damp=7000, room=0.6)
    st[:, 0] += loopify(rumble, L) * 0.4
    st[:, 1] += loopify(rumble, L) * 0.4
    save("amb/fire", st, 0.6, q=3)


def chirp(f0, n_ch=3):
    parts = []
    for i in range(n_ch):
        d = rng.uniform(0.04, 0.09)
        n = int(d * SR)
        t = np.arange(n) / SR
        f = f0 * (1.25 - 0.4 * t / d) * (1 + 0.05 * np.sin(2 * np.pi * 40 * t))
        x = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.sin(np.pi * t / d)
        parts.append((x, i * rng.uniform(0.07, 0.12), 1.0))
    return place(parts)


def birds(seconds=16):
    L = int(seconds * SR)
    ev = [(chirp(rng.uniform(2600, 4600), int(rng.integers(2, 6))), rng.uniform(0, seconds), rng.uniform(0.2, 0.6), rng.uniform(-0.9, 0.9))
          for _ in range(int(seconds * 0.9))]
    st = stereo_scatter(L, ev, 0.2, damp=9000)
    wind = loopify(lowpass(rng.standard_normal(L + SR), 400) * (1 + 0.5 * np.sin(2 * np.pi * 0.07 * np.arange(L + SR) / SR)), L)
    st += wind[:, None] * 0.25
    save("amb/day", st, 0.5, q=3)


def crickets(seconds=16):
    L = int(seconds * SR)
    ev = []
    for c in range(5):
        f = rng.uniform(3900, 5000)
        per = rng.uniform(0.5, 0.9)
        pan = rng.uniform(-0.9, 0.9)
        pulse_n = int(0.012 * SR)
        tp = np.arange(pulse_n) / SR
        pulse = np.sin(2 * np.pi * f * tp) * np.sin(np.pi * tp / tp[-1])
        ch = place([(pulse, k * 0.026, 1.0) for k in range(3)])
        t = rng.uniform(0, per)
        while t < seconds:
            ev.append((ch, t, rng.uniform(0.15, 0.3), pan))
            t += per * rng.uniform(0.95, 1.05)
    st = stereo_scatter(L, ev, 0.3, damp=9000)
    wind = loopify(lowpass(rng.standard_normal(L + SR), 300) * (1 + 0.6 * np.sin(2 * np.pi * 0.05 * np.arange(L + SR) / SR)), L)
    st += wind[:, None] * 0.35
    save("amb/night", st, 0.5, q=3)


# ---------------------------------------------------------------- efektler
def ring(f, dur=0.25):
    n = int(dur * SR)
    t = np.arange(n) / SR
    return (np.sin(2 * np.pi * f * t) * np.exp(-t / 0.12) + 0.5 * np.sin(2 * np.pi * f * 2.76 * t) * np.exp(-t / 0.06)
            + 0.25 * np.sin(2 * np.pi * f * 5.4 * t) * np.exp(-t / 0.03)) * np.minimum(1, t / 0.001)


def bubble(f, d=0.07):
    n = int(d * SR)
    t = np.arange(n) / SR
    fr = f * (1 + 1.2 * t / d)
    return np.sin(2 * np.pi * np.cumsum(fr) / SR) * np.exp(-t / (d * 0.35)) * np.minimum(1, t / 0.002)


def owl():
    def hoo(d, f=370):
        n = int(d * SR)
        t = np.arange(n) / SR
        fr = f * (1 + 0.06 * np.sin(np.pi * t / d))
        ph = 2 * np.pi * np.cumsum(fr) / SR
        return (np.sin(ph) + 0.15 * np.sin(2 * ph)) * env_ar(n, 0.05, 0.12) + band(rng.standard_normal(n), f * 2, 0.3) * 0.08 * env_ar(n, 0.05, 0.1)
    return place([(hoo(0.32), 0, 1), (hoo(0.22, 390), 0.5, 0.8), (hoo(0.55, 360), 0.8, 1)])


def whoosh(d=0.5):
    n = int(d * SR)
    t = np.arange(n) / SR
    nz = rng.standard_normal(n)
    w = np.clip(t / (d * 0.5), 0, 1)
    x = lowpass(nz, 350) * (1 - w) + lowpass(nz, 2800) * w * 0.6
    return x * env_ar(n, 0.06, d * 0.6)


def sfx():
    S = {}

    def put(name, x, peak=0.85):
        save(f"sfx/{name}", x, peak)
        S[name] = len(x) / SR

    for i, (f, chip) in enumerate(((2350, "chips-collide-1"), (2550, "chips-collide-2"), (2750, "chips-collide-3"))):
        put(f"coin_{i + 1}", place([(load(chip), 0, 0.7), (ring(f), 0.004, 0.45)]), 0.7)
    put("tick_1", load("chip-lay-1"), 0.5)
    put("tick_2", load("chip-lay-2"), 0.5)
    put("pop_1", load("pluck_001"), 0.6)
    put("pop_2", load("pluck_002"), 0.6)
    for i in range(3):
        put(f"serve_{i + 1}", place([(load(f"impactWood_light_00{i}"), 0, 1.0), (load(f"impactGlass_light_00{i + 1}"), 0.008, 0.45)]), 0.8)
    for i in range(3):
        bub = place([(bubble(rng.uniform(380, 900)), rng.uniform(0.02, 0.2), rng.uniform(0.3, 0.6)) for _ in range(5)])
        put(f"pickup_{i + 1}", place([(load(f"impactGlass_light_00{i + 2}"), 0, 0.55), (bub, 0.03, 0.6)]), 0.65)
    put("buy_1", place([(load("handleCoins2"), 0, 1.0), (load("confirmation_001"), 0.05, 0.35)]), 0.8)
    put("buy_2", place([(load("handleCoins2"), 0, 1.0), (load("chips-stack-3"), 0.08, 0.5)]), 0.8)
    put("claim_1", place([(load("handleCoins"), 0, 0.8), (load("confirmation_003"), 0.0, 0.5)]), 0.85)
    put("claim_2", place([(load("handleCoins"), 0, 0.8), (ring(2637), 0.05, 0.3), (ring(3520), 0.13, 0.3)]), 0.85)
    put("error_1", load("error_002"), 0.6)
    for i, nm in enumerate(("dice-throw-1", "dice-throw-2", "dice-throw-3")):
        put(f"dice_{i + 1}", load(nm), 0.8)
    put("door_1", load("doorOpen_1"), 0.8)
    put("door_2", load("doorOpen_2"), 0.8)
    for i in range(2):
        steps = [(load(f"footstep_wood_00{(i + k) % 5}"), k * 0.17, 1.0 - 0.15 * k) for k in range(3)]
        put(f"step_{i + 1}", place(steps + [(load(f"creak{i + 2}"), 0.05, 0.25)]), 0.75)
    for i in range(3):
        put(f"wipe_{i + 1}", load(f"cloth{i + 1}"), 0.65)
    put("clink_1", reverb(place([(load("impactGlass_light_000"), 0, 1.0), (repitch(load("impactGlass_light_003"), 0.92), 0.018, 0.9),
                                 (load("impactWood_light_001"), 0.0, 0.3)]), 0.1, 0.7), 0.85)
    # büyük anlar
    up = [(pluck(hz(n), 1.6, 0.8), i * 0.06, 0.8) for i, n in enumerate(["D4", "F#4", "A4", "D5", "F#5", "A5"])]
    fin = [(strum(["D5", "F#5", "A5", "D6"], 1.4, 0.8, gap=0.012), 0.42, 1.0)]
    tr = [(flute(hz(["A5", "B5"][k % 2]), 0.05), 0.42 + k * 0.05, 0.25) for k in range(6)] + [(flute(hz("D6"), 0.55), 0.72, 0.3)]
    put("levelup", reverb(place(up + fin + tr + [(load("handleCoins"), 0.4, 0.45), (repitch(load("impactBell_heavy_002"), 1.5), 0.42, 0.25)]), 0.15), 0.9)
    bell = load("impactBell_heavy_001")
    roll = [(drum(0.3 + 0.07 * k), 0.55 + sum(0.09 - 0.004 * j for j in range(k)), 0.6) for k in range(11)]
    shout = [(phrase(b, [("h", "e", True)], "excl", mul=1.1), 1.5 + 0.02 * j, 0.5) for j, b in enumerate(("male", "deep", "female"))]
    put("rush", reverb(place([(bell, 0, 0.8), (repitch(bell, 0.95), 0.32, 0.7), (drum(1.0), 1.5, 0.9)] + roll + shout), 0.18), 0.9)
    gmaj = [(pluck(hz(n), 1.2, 0.8), 0.45 + i * 0.05, 0.7) for i, n in enumerate(["G4", "B4", "D5", "G5"])]
    put("unlock", reverb(place([(load("impactPlank_medium_000"), 0, 1.0), (load("impactPlank_medium_002"), 0.2, 0.9)] + gmaj
                               + [(ring(3136), 0.6, 0.3), (load("handleCoins2"), 0.5, 0.4)]), 0.12), 0.9)
    dawn = [(flute(hz(n), d), t, 0.5) for n, t, d in (("D5", 0.2, 0.14), ("F#5", 0.34, 0.14), ("A5", 0.48, 0.14), ("D6", 0.62, 0.7))]
    put("dawn", reverb(place(dawn + [(chirp(3800, 3), 0.0, 0.25), (chirp(4300, 4), 0.9, 0.2),
                                      (strum(["D3", "A3", "D4", "F#4"], 1.6, 0.4, gap=0.04), 0.6, 0.5)]), 0.2), 0.8)
    put("nightfall", reverb(place([(strum(["A2", "E3", "A3", "C4"], 2.2, 0.35, gap=0.05), 0, 0.8), (owl(), 0.5, 0.35)]), 0.25, 1.2), 0.8)
    for i in range(2):
        crack = [(highpass(rng.standard_normal(80), 3000) * np.exp(-np.arange(80) / 20), rng.uniform(0.1, 0.5), rng.uniform(0.3, 0.8)) for _ in range(8)]
        put(f"lantern_{i + 1}", place([(load("metalLatch"), 0, 0.5), (whoosh(), 0.08, 0.9)] + crack), 0.7)
    for i, b in enumerate(("male", "deep")):
        put(f"angry_{i + 1}", reverb(phrase(b, [("m", "u", False), ("m", "o", True)], "grumble", creak=0.45, speed=0.75), 0.06, 0.6), 0.8)
    print("efekt", len(S))


def main():
    os.makedirs(OUT, exist_ok=True)
    sfx()
    voices()
    crowd("crowd_low", 20, 26, 3, 6)
    crowd("crowd_high", 20, 95, 12, 18)
    fire()
    birds()
    crickets()
    # v1.1b: müzik artık RandomMind'ın CC0 kayıtları (assets/audio/CREDITS.md). Sentez parçalar
    # (song_day / song_evening / song_rush) kullanıcı testinde "yapay, tiz ve titrek" bulundu; kod örnek olarak duruyor.
    lic = [os.path.join(r, n) for r, _, ns in os.walk(KEN) for n in ns if n == "License.txt"]
    if lic:
        shutil.copyfile(lic[0], os.path.join(OUT, "LICENSE-kenney.txt"))
    print("bitti")


if __name__ == "__main__":
    main()
