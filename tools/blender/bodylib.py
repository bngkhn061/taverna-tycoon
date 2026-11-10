"""
bodylib — ortak taban gövdeler (BODY_S / BODY_M / BODY_L) ve sık kullanılan aksesuarlar.
Tüm müşteriler buradan türetilir; aynı oranlar = ileride aynı iskelet ve animasyonlar.
"""
from types import SimpleNamespace
from mathutils import Vector
import toylib as T

BODIES = {
    "S": dict(leg_h=0.12, foot=(0.11, 0.18, 0.07), torso=(0.15, 0.13, 0.15), head=0.18, arm=0.24, hand=0.048, leg_r=0.042),
    "M": dict(leg_h=0.26, foot=(0.13, 0.2, 0.08), torso=(0.2, 0.17, 0.23), head=0.2, arm=0.34, hand=0.062, leg_r=0.055),
    "L": dict(leg_h=0.36, foot=(0.16, 0.25, 0.1), torso=(0.3, 0.25, 0.33), head=0.21, arm=0.46, hand=0.085, leg_r=0.08),
}
BUILDS = {"normal": (1, 1, 1), "fat": (1.25, 1.3, 1.05), "thin": (0.8, 0.85, 1.05), "huge": (1.45, 1.4, 1.15)}


def body(ox, kind, skin, shirt, pants, shoes, build="normal", sleeves="long", ears="normal",
         nose="ball", head_scale=1.0, legs=True, eyes=True, forearm=None):
    b = BODIES[kind]
    bx, by, bz = BUILDS[build]
    rx, ry, rz = b["torso"][0] * bx, b["torso"][1] * by, b["torso"][2] * bz
    lh, hr, a, f = b["leg_h"], b["head"] * head_scale, b["arm"], b["foot"]
    tz = lh + rz * 0.8
    hz = tz + rz + hr * 0.85
    sx, sz = rx * 0.9, tz + rz * 0.5
    lx = max(rx * 0.42, f[0] * 0.6)
    R = SimpleNamespace(ox=ox, kind=kind, hz=hz, hr=hr, tz=tz, rx=rx, ry=ry, rz=rz, sx=sx, sz=sz,
                        lh=lh, lx=lx, top=hz + hr * 0.95, hand={}, skin=skin, foot=f)
    if legs:
        for s in (1, -1):
            T.box(f"Foot_{s}", shoes, (ox + s * lx, -0.02, f[2] / 2), f, bevel=f[2] * 0.35)
            T.cyl(f"Leg_{s}", pants, (ox + s * lx, 0, (f[2] * 0.6 + tz) / 2), r=b["leg_r"],
                  depth=tz - f[2] * 0.6, verts=8)
    T.sphere("Torso", shirt, (ox, 0, tz), r=1, scale=(rx, ry, rz), seg=12, rings=8)
    for s in (1, -1):
        sh = Vector((ox + s * sx, 0, sz))
        el = sh + Vector((s * 0.38, -0.05, -0.92)) * (a / 2)
        wr = el + Vector((s * 0.2, -0.12, -0.97)) * (a / 2)
        T.limb(f"UpperArm_{s}", shirt, sh, el, r1=a * 0.2, r2=a * 0.17)
        fc = forearm or (shirt if sleeves == "long" else skin)
        T.limb(f"Forearm_{s}", fc, el, wr, r1=a * 0.15, r2=a * 0.16)
        hp = wr + Vector((s * 0.01, -0.01, -b["hand"] * 0.6))
        T.sphere(f"Hand_{s}", skin, hp, r=b["hand"], seg=8, rings=6)
        R.hand[s] = hp
    T.sphere("Head", skin, (ox, 0, hz), r=hr, scale=(1, 0.95, 0.95), seg=12, rings=9)
    for s in (1, -1):
        if ears == "normal":
            T.sphere(f"Ear_{s}", skin, (ox + s * hr * 0.97, 0.01, hz - hr * 0.1), r=hr * 0.24,
                     scale=(0.5, 1, 1.1), seg=8, rings=6)
        elif ears == "pointy":
            T.limb(f"Ear_{s}", skin, (ox + s * hr * 0.85, 0.02, hz), (ox + s * hr * 1.55, 0.08, hz + hr * 0.5),
                   r1=hr * 0.17, r2=0.0, verts=6)
        elif ears == "long":
            T.limb(f"Ear_{s}", skin, (ox + s * hr * 0.85, 0.02, hz), (ox + s * hr * 2.0, 0.06, hz + hr * 0.25),
                   r1=hr * 0.24, r2=0.0, verts=6)
    if nose == "ball":
        T.sphere("Nose", skin, (ox, -hr * 0.95, hz - hr * 0.15), r=hr * 0.24, seg=8, rings=6)
    elif nose == "big":
        T.sphere("Nose", skin, (ox, -hr * 0.97, hz - hr * 0.18), r=hr * 0.34, scale=(1, 0.9, 0.9), seg=10, rings=7)
    elif nose == "long":
        T.limb("Nose", skin, (ox, -hr * 0.8, hz - hr * 0.08), (ox, -hr * 1.6, hz - hr * 0.32),
               r1=hr * 0.14, r2=hr * 0.05, verts=6)
    if eyes:
        er = hr * 0.14
        for s in (1, -1):
            ex, ey, ez = ox + s * hr * 0.4, -hr * 0.86, hz + hr * 0.14
            T.sphere(f"Eye_{s}", "Goz", (ex, ey, ez), r=er, scale=(0.8, 0.5, 1.3), seg=8, rings=6)
            T.sphere(f"EyeHi_{s}", "GozParlak", (ex + er * 0.3, ey - er * 0.45, ez + er * 0.5), r=er * 0.3, seg=6, rings=4)
    return R


# ------------------------------------------------------------------ aksesuarlar
def hair(R, color, rot=(-18, 0, 0), s=1.06, dz=0.04):
    T.dome("Hair", color, (R.ox, 0.01, R.hz + R.hr * dz), r=R.hr * s, rot=rot, seg=12, rings=8)


def brim_hat(R, color, brim=1.8, crown_r=0.92, crown_h=0.6, z=0.5, band=None, rot=(0, 0, 0), crown_top=0.85):
    zb = R.hz + R.hr * z
    T.cyl("HatBrim", color, (R.ox, 0, zb), r=R.hr * brim, depth=0.02, rot=rot, verts=14)
    T.cyl("HatCrown", color, (R.ox, 0, zb + R.hr * crown_h / 2), r=R.hr * crown_r, r2=R.hr * crown_r * crown_top,
          depth=R.hr * crown_h, rot=rot, verts=12)
    if band:
        T.cyl("HatBand", band, (R.ox, 0, zb + R.hr * 0.1), r=R.hr * crown_r * 1.02, depth=R.hr * 0.16, rot=rot, verts=12)


def beard(R, color, length=1.0, width=0.8):
    T.sphere("Beard", color, (R.ox, -R.hr * 0.62, R.hz - R.hr * (0.45 + 0.35 * length)), r=1,
             scale=(R.hr * width, R.hr * 0.5, R.hr * 0.55 * length), seg=8, rings=7)


def mustache(R, color, size=1.0, tilt=0):
    for s in (1, -1):
        T.sphere(f"Stache_{s}", color, (R.ox + s * R.hr * 0.36 * size, -R.hr * 0.98, R.hz - R.hr * 0.38), r=1,
                 scale=(R.hr * 0.42 * size, R.hr * 0.2, R.hr * 0.17 * size), rot=(0, s * 14 + tilt, 0), seg=10, rings=6)


def skirt(R, color, bottom=0.03, flare=1.2, top_z=None):
    tz = R.tz if top_z is None else top_z
    T.cyl("Skirt", color, (R.ox, 0, (tz + bottom) / 2), r=R.rx * flare, r2=R.rx * 0.88, depth=tz - bottom,
          scale=(1, R.ry / R.rx, 1), verts=14)


def apron(R, color, top=0.2, bottom=None, width=0.85):
    """Gövdenin önüne oturan, hafif kavisli önlük."""
    bot = R.lh * 0.45 if bottom is None else bottom
    zt = R.tz + R.rz * top
    T.cyl("Apron", color, (R.ox, -R.ry * 0.82, (zt + bot) / 2), r=R.rx * width, r2=R.rx * width * 0.85,
          depth=zt - bot, scale=(1, 0.2, 1), verts=12)


def belt(R, color, buckle=None, z=-0.15, minor=0.022):
    zz = R.tz + R.rz * z
    T.torus("Belt", color, (R.ox, 0, zz), major=R.rx * 1.0, minor=minor, scale=(1, R.ry / R.rx, 1), seg=14)
    if buckle:
        T.box("Buckle", buckle, (R.ox, -R.ry * 1.02, zz), (0.06, 0.025, 0.05), bevel=0.008)


def glasses(R, color):
    for s in (1, -1):
        T.torus(f"Glass_{s}", color, (R.ox + s * R.hr * 0.4, -R.hr * 0.98, R.hz + R.hr * 0.14),
                major=R.hr * 0.22, minor=0.007, rot=(90, 0, 0), seg=10, mseg=4)


def vest(R, color, buttons=None):
    T.cyl("Vest", color, (R.ox, 0, R.tz + R.rz * 0.15), r=R.rx * 1.03, r2=R.rx * 0.82, depth=R.rz * 1.1,
          scale=(1, R.ry / R.rx, 1), verts=14)
    if buttons:
        for i in range(2):
            T.sphere(f"Btn_{i}", buttons, (R.ox, -R.ry * 1.03, R.tz + R.rz * (0.0 + i * 0.3)), r=0.013, seg=6, rings=4)


def cape(R, color, inner=None, collar=False):
    h = R.sz - 0.06
    T.box("Cape", color, (R.ox, R.ry * 0.95, h / 2 + 0.05), (R.rx * 2.3, 0.03, h), rot=(6, 0, 0), bevel=0.01)
    if inner:
        T.box("CapeIn", inner, (R.ox, R.ry * 0.9, h / 2 + 0.06), (R.rx * 2.15, 0.02, h * 0.97), rot=(6, 0, 0))
    if collar:
        for s in (1, -1):
            T.box(f"Collar_{s}", color, (R.ox + s * R.hr * 0.75, R.hr * 0.1, R.hz - R.hr * 0.25),
                  (R.hr * 0.75, 0.025, R.hr * 1.35), rot=(-10, s * -12, s * -55), bevel=0.005)


def staff(R, side, color, height, r=0.016, base=0.01):
    p = R.hand[side]
    x = p.x + side * 0.015
    T.limb("Staff", color, (x, p.y, base), (x, p.y, height), r1=r)
    return Vector((x, p.y, height))
