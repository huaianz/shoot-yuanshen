#!/usr/bin/env python3
"""
Generate simple but usable dungeon interactable 3D models in OBJ format.
Models: Wall Lever, Floor Button, Valve Wheel, Wall Switch Panel
All models include UV coordinates for texturing in Unity.
"""

import os
import math

OUT_DIR = os.path.dirname(os.path.abspath(__file__))


def box(min_x, min_y, min_z, max_x, max_y, max_z):
    """Generate vertices, uvs, faces for a box."""
    v = [
        (min_x, min_y, min_z), (max_x, min_y, min_z),
        (max_x, max_y, min_z), (min_x, max_y, min_z),
        (min_x, min_y, max_z), (max_x, min_y, max_z),
        (max_x, max_y, max_z), (min_x, max_y, max_z),
    ]
    vt = [
        (0, 0), (1, 0), (1, 1), (0, 1),
        (0, 0), (1, 0), (1, 1), (0, 1),
    ]
    faces = [
        (1, 2, 3, 4),   # front
        (5, 8, 7, 6),   # back
        (1, 5, 6, 2),   # bottom
        (4, 3, 7, 8),   # top
        (1, 4, 8, 5),   # left
        (2, 6, 7, 3),   # right
    ]
    return v, vt, faces


def cylinder(radius, height, segments, center_y=0):
    """Generate vertices, uvs, faces for a cylinder along Y axis."""
    v = []
    vt = []
    # side vertices
    for i in range(segments):
        angle = 2 * math.pi * i / segments
        x = radius * math.cos(angle)
        z = radius * math.sin(angle)
        v.append((x, center_y - height / 2, z))
        v.append((x, center_y + height / 2, z))
        u = i / segments
        vt.append((u, 0))
        vt.append((u, 1))
    # top center
    top_center = len(v) + 1
    v.append((0, center_y + height / 2, 0))
    vt.append((0.5, 0.5))
    # bottom center
    bot_center = len(v) + 1
    v.append((0, center_y - height / 2, 0))
    vt.append((0.5, 0.5))

    faces = []
    # side
    for i in range(segments):
        i1 = i * 2 + 1
        i2 = i * 2 + 2
        i3 = ((i + 1) % segments) * 2 + 1
        i4 = ((i + 1) % segments) * 2 + 2
        faces.append((i1, i3, i4, i2))
    # top
    for i in range(segments):
        i1 = i * 2 + 2
        i2 = ((i + 1) % segments) * 2 + 2
        faces.append((top_center, i1, i2))
    # bottom
    for i in range(segments):
        i1 = i * 2 + 1
        i2 = ((i + 1) % segments) * 2 + 1
        faces.append((bot_center, i2, i1))
    return v, vt, faces


def write_obj(filename, vertices, uvs, faces, comment=""):
    """Write an OBJ file."""
    filepath = os.path.join(OUT_DIR, filename)
    with open(filepath, 'w') as f:
        if comment:
            f.write(f"# {comment}\n")
        f.write(f"# Vertices: {len(vertices)}, Faces: {len(faces)}\n\n")
        for v in vertices:
            f.write(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}\n")
        f.write("\n")
        for vt in uvs:
            f.write(f"vt {vt[0]:.6f} {vt[1]:.6f}\n")
        f.write("\n")
        for face in faces:
            if len(face) == 4:
                f.write(f"f {face[0]}/{face[0]} {face[1]}/{face[1]} {face[2]}/{face[2]} {face[3]}/{face[3]}\n")
            else:
                f.write(f"f {face[0]}/{face[0]} {face[1]}/{face[1]} {face[2]}/{face[2]}\n")
    print(f"  Written: {filename} ({len(vertices)} verts, {len(faces)} faces)")


def merge_parts(parts):
    """Merge multiple (vertices, uvs, faces) tuples, offsetting indices."""
    all_v = []
    all_vt = []
    all_f = []
    offset = 0
    for v, vt, f in parts:
        all_v.extend(v)
        all_vt.extend(vt)
        for face in f:
            all_f.append(tuple(idx + offset for idx in face))
        offset += len(v)
    return all_v, all_vt, all_f


def generate_wall_lever():
    """Wall-mounted lever: stone base plate + pivot + wooden handle."""
    print("Generating Wall Lever...")
    parts = []
    # Base plate (stone)
    parts.append(box(-0.3, -0.35, -0.08, 0.3, 0.35, 0.0))
    # Pivot housing (metal ring)
    parts.append(cylinder(0.08, 0.06, 12, center_y=0.05))
    # Lever handle (wood) - angled
    handle_v = [
        (-0.03, 0.02, 0.0), (0.03, 0.02, 0.0),
        (0.03, 0.02, 0.5), (-0.03, 0.02, 0.5),
        (-0.03, 0.08, 0.0), (0.03, 0.08, 0.0),
        (0.03, 0.08, 0.5), (-0.03, 0.08, 0.5),
    ]
    handle_vt = [(0,0),(1,0),(1,1),(0,1),(0,0),(1,0),(1,1),(0,1)]
    handle_faces = [
        (1,2,3,4),(5,8,7,6),(1,5,6,2),(4,3,7,8),(1,4,8,5),(2,6,7,3)
    ]
    parts.append((handle_v, handle_vt, handle_faces))
    # Handle knob (sphere-ish)
    parts.append(cylinder(0.04, 0.06, 8, center_y=0.05))
    v, vt, f = merge_parts(parts)
    write_obj("wall_lever.obj", v, vt, f, "Wall-mounted dungeon lever")


def generate_floor_button():
    """Floor pressure button: stone base + metal button top."""
    print("Generating Floor Button...")
    parts = []
    # Base ring (stone)
    parts.append(cylinder(0.25, 0.06, 16, center_y=0.03))
    # Inner button (metal, raised)
    parts.append(cylinder(0.18, 0.04, 16, center_y=0.08))
    # Center detail
    parts.append(cylinder(0.08, 0.02, 12, center_y=0.11))
    v, vt, f = merge_parts(parts)
    write_obj("floor_button.obj", v, vt, f, "Floor pressure button")


def generate_valve_wheel():
    """Valve wheel: metal base + spokes + rim."""
    print("Generating Valve Wheel...")
    parts = []
    # Wall mount
    parts.append(box(-0.06, -0.06, -0.05, 0.06, 0.06, 0.0))
    # Central hub
    parts.append(cylinder(0.06, 0.08, 12, center_y=0.04))
    # Rim (torus approximation using small cylinders)
    rim_radius = 0.25
    rim_tube = 0.025
    segments = 24
    for i in range(segments):
        angle = 2 * math.pi * i / segments
        x = rim_radius * math.cos(angle)
        z = rim_radius * math.sin(angle)
        # Small box for each rim segment
        next_angle = 2 * math.pi * (i + 1) / segments
        nx = rim_radius * math.cos(next_angle)
        nz = rim_radius * math.sin(next_angle)
        mid_x = (x + nx) / 2
        mid_z = (z + nz) / 2
        length = math.sqrt((nx - x) ** 2 + (nz - z) ** 2)
        # Use a small cylinder oriented radially
        parts.append(box(
            mid_x - rim_tube, 0.04 - rim_tube, mid_z - rim_tube,
            mid_x + rim_tube, 0.04 + rim_tube, mid_z + rim_tube
        ))
    # Spokes (4)
    for i in range(4):
        angle = math.pi / 2 * i
        dx = math.cos(angle)
        dz = math.sin(angle)
        for t in range(5):
            frac = (t + 0.5) / 5
            x = dx * rim_radius * frac * 0.85
            z = dz * rim_radius * frac * 0.85
            parts.append(box(
                x - 0.015, 0.04 - 0.015, z - 0.015,
                x + 0.015, 0.04 + 0.015, z + 0.015
            ))
    v, vt, f = merge_parts(parts)
    write_obj("valve_wheel.obj", v, vt, f, "Valve wheel mechanism")


def generate_wall_switch_panel():
    """Wall switch panel: stone plate + toggle switch + indicator lights."""
    print("Generating Wall Switch Panel...")
    parts = []
    # Back plate (stone)
    parts.append(box(-0.25, -0.35, -0.06, 0.25, 0.35, 0.0))
    # Switch housing (metal)
    parts.append(box(-0.08, -0.1, 0.0, 0.08, 0.15, 0.06))
    # Toggle lever
    toggle_v = [
        (-0.02, 0.0, 0.04), (0.02, 0.0, 0.04),
        (0.02, 0.0, 0.18), (-0.02, 0.0, 0.18),
        (-0.02, 0.04, 0.04), (0.02, 0.04, 0.04),
        (0.02, 0.04, 0.18), (-0.02, 0.04, 0.18),
    ]
    toggle_vt = [(0,0),(1,0),(1,1),(0,1),(0,0),(1,0),(1,1),(0,1)]
    toggle_faces = [
        (1,2,3,4),(5,8,7,6),(1,5,6,2),(4,3,7,8),(1,4,8,5),(2,6,7,3)
    ]
    parts.append((toggle_v, toggle_vt, toggle_faces))
    # Indicator lights (2)
    parts.append(cylinder(0.025, 0.02, 8, center_y=0.22))
    # Move first light up
    parts.append(cylinder(0.025, 0.02, 8, center_y=-0.2))
    v, vt, f = merge_parts(parts)
    write_obj("wall_switch_panel.obj", v, vt, f, "Wall switch panel with indicator lights")


def generate_chain_pull():
    """Chain pull switch: ceiling mount + chain + handle."""
    print("Generating Chain Pull...")
    parts = []
    # Ceiling mount
    parts.append(cylinder(0.06, 0.04, 12, center_y=-0.02))
    # Chain links (simplified as small boxes)
    for i in range(8):
        y = -0.08 - i * 0.06
        if i % 2 == 0:
            parts.append(box(-0.025, y - 0.025, -0.01, 0.025, y + 0.025, 0.01))
        else:
            parts.append(box(-0.01, y - 0.025, -0.025, 0.01, y + 0.025, 0.025))
    # Pull handle
    parts.append(cylinder(0.04, 0.08, 10, center_y=-0.62))
    v, vt, f = merge_parts(parts)
    write_obj("chain_pull.obj", v, vt, f, "Ceiling chain pull switch")


if __name__ == "__main__":
    print(f"Output directory: {OUT_DIR}")
    print("=" * 50)
    generate_wall_lever()
    generate_floor_button()
    generate_valve_wheel()
    generate_wall_switch_panel()
    generate_chain_pull()
    print("=" * 50)
    print("Done! All models generated.")
