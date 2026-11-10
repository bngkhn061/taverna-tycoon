"""
toylib — "Oyuncak Taverna" stili için Blender yardımcıları.
Kurallar: DESIGN_RULES.md. Tüm modeller tek palet dokusunu (T_Palette_Main.png) paylaşır;
her parçanın UV'leri paletteki tek bir renk karesine toplanır.
"""
import bpy
import bmesh
import math
import os
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
TEX_DIR = os.path.join(ROOT, "assets", "textures")
PALETTE_PATH = os.path.join(TEX_DIR, "T_Palette_Main.png")

# Sıra önemli: indeks = palet karesi. Yeni renkleri SADECE sona ekle (eski UV'ler bozulmasın).
PALETTE = [
    # Ortam
    ("Mese", "#8B5A2B"), ("KoyuMese", "#5C3A1E"), ("Bal", "#E0A040"), ("Tas", "#9C9488"),
    ("KoyuTas", "#6B6560"), ("Ates", "#FF8A2A"), ("Bez", "#D9C7A3"),
    # Karakter vurguları
    ("Kirmizi", "#C8443A"), ("Yesil", "#4F7F3A"), ("Mavi", "#3E5FA8"), ("Mor", "#7A4FA8"),
    ("Altin", "#F2C14E"), ("Lacivert", "#2B2F4A"), ("Krem", "#F4E8D0"), ("Bordo", "#8A1E2E"),
    # Tenler
    ("Ten1", "#F6D3B3"), ("Ten2", "#E8B48A"), ("Ten3", "#C68A5E"), ("Ten4", "#8D5A3B"),
    ("Ten5", "#5E3A26"), ("TenOrk", "#7FA05A"), ("TenGoblin", "#9CC45A"), ("TenTrol", "#7E8C9A"),
    ("TenElf", "#F3E4CF"),
    # Saç / göz
    ("SacKizil", "#B5482A"), ("SacKahve", "#5A3A22"), ("SacSari", "#E8C070"),
    ("SacGri", "#B8B4AE"), ("SacKoyu", "#2E221C"), ("Goz", "#2A2220"), ("GozParlak", "#FFFFFF"),
    # İçecekler
    ("Bira", "#E0A040"), ("Sira", "#F2D06B"), ("BalSarabi", "#D88A2A"), ("Stout", "#3A2418"),
    ("Cay", "#7FAF5A"), ("Grog", "#6E8A2A"), ("ElfCiyi", "#A8E0F0"), ("EjderAtesi", "#FF5A1E"),
    # Metaller / doğa (props)
    ("Pirinc", "#C9973A"), ("Bakir", "#B5653A"), ("Yaprak", "#6FA34A"), ("Kopuk", "#FBF3E0"),
    ("Su", "#6FB8D8"), ("Siva", "#E6D6B8"),
]
GRID = 16
CELL = 16
SIZE = GRID * CELL
_INDEX = {name: i for i, (name, _) in enumerate(PALETTE)}


def hex_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def cell_uv(color):
    i = _INDEX[color]
    cx, cy = i % GRID, i // GRID
    return ((cx + 0.5) / GRID, 1.0 - (cy + 0.5) / GRID)


# ---------------------------------------------------------------- sahne / materyal

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def palette_image():
    img = bpy.data.images.get("T_Palette_Main")
    if img:
        return img
    os.makedirs(TEX_DIR, exist_ok=True)
    img = bpy.data.images.new("T_Palette_Main", SIZE, SIZE, alpha=False)
    px = [0.0] * (SIZE * SIZE * 4)
    for i, (_, hx) in enumerate(PALETTE):
        r, g, b = hex_rgb(hx)
        cx, cy = i % GRID, i // GRID
        y0 = SIZE - (cy + 1) * CELL  # 0. satır görselin üstünde
        for y in range(y0, y0 + CELL):
            for x in range(cx * CELL, (cx + 1) * CELL):
                k = (y * SIZE + x) * 4
                px[k:k + 4] = (r, g, b, 1.0)
    img.pixels = px
    img.filepath_raw = PALETTE_PATH
    img.file_format = "PNG"
    img.save()
    img.filepath = PALETTE_PATH
    return img


def palette_material():
    mat = bpy.data.materials.get("M_Palette")
    if mat:
        return mat
    mat = bpy.data.materials.new("M_Palette")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = palette_image()
    tex.interpolation = "Closest"
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.75
    for key in ("Specular IOR Level", "Specular"):
        if key in bsdf.inputs:
            bsdf.inputs[key].default_value = 0.2
            break
    return mat


# ---------------------------------------------------------------- primitifler

def _active():
    return bpy.context.view_layer.objects.active


def _finish(obj, name, color, bevel=0.0):
    obj.name = name
    obj.data.name = name
    if bevel > 0:
        apply_scale(obj)
        m = obj.modifiers.new("Bevel", "BEVEL")
        m.width = bevel
        m.segments = 1
        m.limit_method = "ANGLE"
    if not obj.data.uv_layers:
        obj.data.uv_layers.new(name="UVMap")
    paint(obj, color)
    obj.data.materials.clear()
    obj.data.materials.append(palette_material())
    return obj


def paint(obj, color):
    u, v = cell_uv(color)
    for loop in obj.data.uv_layers.active.data:
        loop.uv = (u, v)


def apply_scale(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)


def _rad(rot):
    return tuple(math.radians(a) for a in rot)


def sphere(name, color, loc, r=0.1, scale=(1, 1, 1), rot=(0, 0, 0), seg=12, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=rings, radius=r,
                                         location=loc, rotation=_rad(rot))
    o = _active()
    o.scale = scale
    return _finish(o, name, color)


def dome(name, color, loc, r=0.1, scale=(1, 1, 1), rot=(0, 0, 0), seg=12, rings=8):
    """Alt yarısı silinmiş küre (şapka, bandana, kask)."""
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=rings, radius=r,
                                         location=loc, rotation=_rad(rot))
    o = _active()
    bm = bmesh.new()
    bm.from_mesh(o.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < -1e-5], context="VERTS")
    bm.to_mesh(o.data)
    bm.free()
    o.scale = scale
    return _finish(o, name, color)


def box(name, color, loc, size=(0.1, 0.1, 0.1), rot=(0, 0, 0), bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=_rad(rot))
    o = _active()
    o.scale = size
    return _finish(o, name, color, bevel)


def cyl(name, color, loc, r=0.05, depth=0.1, r2=None, rot=(0, 0, 0), scale=(1, 1, 1), verts=10):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r, radius2=r if r2 is None else r2,
                                    depth=depth, location=loc, rotation=_rad(rot))
    o = _active()
    o.scale = scale
    return _finish(o, name, color)


def torus(name, color, loc, major=0.1, minor=0.02, rot=(0, 0, 0), scale=(1, 1, 1), seg=14, mseg=4):
    bpy.ops.mesh.primitive_torus_add(major_segments=seg, minor_segments=mseg, major_radius=major,
                                     minor_radius=minor, location=loc, rotation=_rad(rot))
    o = _active()
    o.scale = scale
    return _finish(o, name, color)


def limb(name, color, p1, p2, r1=0.05, r2=None, verts=8):
    """p1'den p2'ye uzanan (konik) silindir — kol, bacak, asa."""
    p1, p2 = Vector(p1), Vector(p2)
    d = p2 - p1
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r1, radius2=r1 if r2 is None else r2,
                                    depth=d.length, location=(p1 + p2) / 2)
    o = _active()
    o.rotation_mode = "QUATERNION"
    o.rotation_quaternion = d.to_track_quat("Z", "Y")
    return _finish(o, name, color)


def mirror_x(p):
    return (-p[0], p[1], p[2])


# ---------------------------------------------------------------- birleştirme / dışa aktarma

def join_all(name, smooth_angle=40):
    # Önceden birleştirilmiş karakterleri (CH_*, PR_*) ve zemini dışarıda bırak
    objs = [o for o in bpy.context.scene.objects
            if o.type == "MESH" and not o.name.startswith(("CH_", "PR_", "Floor"))]
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.convert(target="MESH")
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.join()
    o = _active()
    o.name = name
    o.data.name = name
    try:
        bpy.ops.object.shade_smooth_by_angle(angle=math.radians(smooth_angle))
    except Exception:
        bpy.ops.object.shade_flat()
    return o


def tri_count(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def save_blend(path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=path, relative_remap=True)


def export_fbx(obj, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options="FBX_SCALE_ALL",
                             path_mode="COPY", embed_textures=False, mesh_smooth_type="FACE")


# ---------------------------------------------------------------- önizleme render

def label(text, loc, size=0.11, color="Krem"):
    """Render sayfaları için yazı etiketi (kameraya dönük, ışık almaz)."""
    cu = bpy.data.curves.new(f"LBL_{text}", "FONT")
    cu.body = text
    cu.align_x = "CENTER"
    cu.size = size
    o = bpy.data.objects.new(f"LBL_{text}", cu)
    o.location = loc
    o.rotation_euler = _rad((90, 0, 0))
    mat = bpy.data.materials.get("M_Label") or bpy.data.materials.new("M_Label")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (*[c ** 2.2 for c in hex_rgb(dict(PALETTE)[color])], 1)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(em.outputs[0], out.inputs[0])
    cu.materials.append(mat)
    bpy.context.scene.collection.objects.link(o)
    return o


def setup_render_stage(floor=True, floor_size=8, floor_color="Mese"):
    sc = bpy.context.scene
    for eng in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT", "BLENDER_WORKBENCH"):
        try:
            sc.render.engine = eng
            break
        except TypeError:
            continue
    sc.render.resolution_x = 900
    sc.render.resolution_y = 900
    sc.view_settings.view_transform = "Standard"
    world = bpy.data.worlds.new("W_Taverna")
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (*[c ** 2.2 for c in hex_rgb("#6A5A7A")], 1)
    bg.inputs["Strength"].default_value = 0.9
    sc.world = world
    sun_data = bpy.data.lights.new("Sun", "SUN")
    sun_data.energy = 3.2
    sun_data.color = hex_rgb("#FFE2B8")
    sun_data.angle = math.radians(8)
    sun = bpy.data.objects.new("Sun", sun_data)
    sun.rotation_euler = _rad((50, 0, -35))
    sc.collection.objects.link(sun)
    if floor:
        bpy.ops.mesh.primitive_plane_add(size=floor_size, location=(0, 0, 0))
        f = _active()
        _finish(f, "Floor", floor_color)


def render_from(path, target=(0, 0, 0.5), pitch=50, yaw=-25, dist=3.0, fov=35, res=(900, 900)):
    sc = bpy.context.scene
    sc.render.resolution_x, sc.render.resolution_y = res
    t = Vector(target)
    p, y = math.radians(pitch), math.radians(yaw)
    pos = t + Vector((math.sin(y) * math.cos(p) * dist, -math.cos(y) * math.cos(p) * dist, math.sin(p) * dist))
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.lens_unit = "FOV"
    cam_data.angle = math.radians(fov)
    cam = bpy.data.objects.new("Cam", cam_data)
    cam.location = pos
    cam.rotation_mode = "QUATERNION"
    cam.rotation_quaternion = (t - pos).to_track_quat("-Z", "Y")
    sc.collection.objects.link(cam)
    sc.camera = cam
    os.makedirs(os.path.dirname(path), exist_ok=True)
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)
