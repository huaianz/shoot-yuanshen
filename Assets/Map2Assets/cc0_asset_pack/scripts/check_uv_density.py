"""
check_uv_density.py -- prove texel density is uniform across the pack.

Two outputs:

  1. A printed table of measured UV units per metre for every asset, with the
     spread between the loosest and tightest. Anything above ~1.05x means a
     tiling material will visibly change scale between pieces.

  2. docs/uv_density_check.png -- a render of representative assets carrying a
     checker driven by UV0. If density is uniform, every checker square is the
     same real-world size on every asset, regardless of how large the asset is.

Up to v1.2 the spread was 8.7x, because each mesh was smart-projected into its
own 0-1 square. SM_Roof_Pitched_4m sat at 0.074 UV/m and SM_Debris_Scatter at
0.641, so one concrete material rendered nearly nine times coarser on a roof
than on a pile of debris.

Run:
    blender -b --python scripts/check_uv_density.py
"""

import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit_config as cfg          # noqa: E402
import modkit_lib as L            # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BLEND = os.path.join(ROOT, "source", "ModKit.blend")
OUT = os.path.join(ROOT, "docs", "uv_density_check.png")

# One large flat piece, one small flat piece, one tall piece, two curved props
# and a junction -- if density holds across these it holds across the pack.
SAMPLES = ["SM_Wall_Straight_4m", "SM_Floor_4m", "SM_Pillar_3m",
           "SM_Crate_Small", "SM_Barrel", "SM_Roof_Corner_4m"]

CHECKER_SCALE = 8.0
TOLERANCE = 1.05          # acceptable max/min density ratio


def report():
    """Measure every mesh and print the table. Returns the spread ratio."""
    target = 1.0 / float(cfg.UV_TILE_METRES)
    rows = []
    for ob in bpy.data.objects:
        if ob.type == "MESH" and ob.data.uv_layers:
            rows.append((L.measure_texel_density(ob, uv_layer=0), ob.name))
    rows.sort()

    print("\nTEXEL DENSITY -- target %.4f UV/m (%.2f m per texture repeat)"
          % (target, cfg.UV_TILE_METRES))
    print("%-28s %10s %8s" % ("asset", "UV/m", "vs target"))
    for density, name in rows:
        print("%-28s %10.4f %7.1f%%"
              % (name, density, (density / target - 1.0) * 100.0))

    if not rows:
        print("  ! no meshes with UVs found")
        return 0.0
    spread = rows[-1][0] / rows[0][0] if rows[0][0] > 0 else float("inf")
    print("\nSPREAD %.3fx  (%s %.4f -> %s %.4f)"
          % (spread, rows[0][1], rows[0][0], rows[-1][1], rows[-1][0]))
    print("RESULT %s (tolerance %.2fx)"
          % ("PASS" if spread <= TOLERANCE else "FAIL", TOLERANCE))
    return spread


def checker_material():
    mat = bpy.data.materials.new("M_UVCheck")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    chk = nt.nodes.new("ShaderNodeTexChecker")
    uvmap = nt.nodes.new("ShaderNodeUVMap")
    uvmap.uv_map = "UVMap"
    chk.inputs["Scale"].default_value = CHECKER_SCALE
    chk.inputs["Color1"].default_value = (0.84, 0.84, 0.85, 1)
    chk.inputs["Color2"].default_value = (0.16, 0.42, 0.62, 1)
    bsdf.inputs["Roughness"].default_value = 0.62
    nt.links.new(uvmap.outputs["UV"], chk.inputs["Vector"])
    nt.links.new(chk.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def bounds(objects):
    pts = []
    for ob in objects:
        pts += [ob.matrix_world @ Vector(c) for c in ob.bound_box]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts),
                 min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts),
                 max(p.z for p in pts)))
    return lo, hi


def lay_out(names, gap=1.4):
    """Stand each sample on z=0 and space them along X."""
    cursor = 0.0
    shown = []
    for name in names:
        ob = bpy.data.objects.get(name)
        if ob is None:
            print("  ! sample %s not in the .blend" % name)
            continue
        ob.location = (0, 0, 0)
        bpy.context.view_layer.update()
        lo, hi = bounds([ob])
        ob.location = (cursor - lo.x, -(lo.y + hi.y) * 0.5, -lo.z)
        cursor += (hi.x - lo.x) + gap
        shown.append(ob)
    return shown


def frame(camera, cam_data, objects, margin=1.34):
    """Pull the camera back far enough to fit everything, with margin."""
    lo, hi = bounds(objects)
    centre = (lo + hi) * 0.5
    span = hi - lo
    scene = bpy.context.scene
    aspect = scene.render.resolution_x / scene.render.resolution_y

    h_fov = 2.0 * math.atan(0.5 * cam_data.sensor_width / cam_data.lens)
    v_fov = 2.0 * math.atan(math.tan(h_fov / 2.0) / aspect)
    need_h = (span.x * 0.5) / math.tan(h_fov / 2.0)
    need_v = (max(span.z, span.y) * 0.5) / math.tan(v_fov / 2.0)
    dist = max(need_h, need_v) * margin

    camera.location = (centre.x, centre.y - dist * 0.92, centre.z + dist * 0.30)
    direction = centre - Vector(camera.location)
    camera.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()


def render():
    mat = checker_material()
    for ob in list(bpy.data.objects):
        if ob.type != "MESH":
            continue
        if ob.name in SAMPLES:
            ob.data.materials.clear()
            ob.data.materials.append(mat)
        else:
            bpy.data.objects.remove(ob, do_unlink=True)

    shown = lay_out(SAMPLES)
    if not shown:
        print("  ! nothing to render")
        return

    for stale in ("Camera", "Light", "Sun"):
        ob = bpy.data.objects.get(stale)
        if ob:
            bpy.data.objects.remove(ob, do_unlink=True)

    scene = bpy.context.scene
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 720

    cam_data = bpy.data.cameras.new("QA_Camera")
    cam_data.lens = 50
    camera = bpy.data.objects.new("QA_Camera", cam_data)
    bpy.context.collection.objects.link(camera)
    scene.camera = camera
    frame(camera, cam_data, shown)

    sun_data = bpy.data.lights.new("QA_Sun", type='SUN')
    sun_data.energy = 3.6
    sun_data.angle = math.radians(3.0)
    sun = bpy.data.objects.new("QA_Sun", sun_data)
    bpy.context.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(48), math.radians(10), math.radians(140))

    fill_data = bpy.data.lights.new("QA_Fill", type='SUN')
    fill_data.energy = 1.1
    fill = bpy.data.objects.new("QA_Fill", fill_data)
    bpy.context.collection.objects.link(fill)
    fill.rotation_euler = (math.radians(62), 0, math.radians(-40))

    world = bpy.data.worlds.new("QA_World")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (
        0.055, 0.062, 0.070, 1)
    scene.world = world

    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.film_transparent = False
    try:
        scene.eevee.taa_render_samples = 64
    except AttributeError:
        pass
    scene.render.filepath = OUT
    bpy.ops.render.render(write_still=True)
    print("RENDERED %s" % OUT)


if __name__ == "__main__":
    bpy.ops.wm.open_mainfile(filepath=BLEND)
    spread = report()
    render()
    print("DONE spread=%.3fx" % spread)
