import bpy
import math
import os
import sys
from mathutils import Vector


def material(name, color, metallic=0.0, roughness=0.45, emission=None):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    principled = mat.node_tree.nodes.get("Principled BSDF")
    principled.inputs["Base Color"].default_value = (*color, 1.0)
    principled.inputs["Metallic"].default_value = metallic
    principled.inputs["Roughness"].default_value = roughness
    if emission:
        principled.inputs["Emission Color"].default_value = (*emission, 1.0)
        principled.inputs["Emission Strength"].default_value = 2.5
    return mat


def mesh_object(name, vertices, faces, mat):
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def cube(name, location, scale, mat, bevel=0.08):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = (scale[0] * 0.5, scale[1] * 0.5, scale[2] * 0.5)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    if bevel > 0:
        modifier = obj.modifiers.new("Soft edges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
    return obj


def cylinder(name, location, radius, depth, mat, rotation=(0.0, 0.0, 0.0), vertices=16):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    bevel = obj.modifiers.new("Edge bevel", "BEVEL")
    bevel.width = min(radius * 0.18, 0.06)
    bevel.segments = 2
    return obj


def cylinder_between(name, start, end, radius, mat, vertices=12):
    start = Vector(start)
    end = Vector(end)
    direction = end - start
    midpoint = (start + end) * 0.5
    obj = cylinder(name, midpoint, radius, direction.length, mat, vertices=vertices)
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    return obj


def rope(name, points, radius, mat):
    curve = bpy.data.curves.new(name + "Curve", "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 2
    curve.bevel_depth = radius
    curve.bevel_resolution = 2
    spline = curve.splines.new("BEZIER")
    spline.bezier_points.add(len(points) - 1)
    for point, coordinate in zip(spline.bezier_points, points):
        point.co = coordinate
        point.handle_left_type = "AUTO"
        point.handle_right_type = "AUTO"
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj


def make_hull(hull_mat):
    sections = [
        (-5.6, 2.15, 1.25, -0.75),
        (-3.2, 2.55, 1.55, -1.25),
        (0.0, 2.75, 1.65, -1.55),
        (3.4, 2.20, 1.15, -1.15),
        (5.8, 0.16, 0.08, 0.05),
    ]
    vertices = []
    for y, top_width, bottom_width, bottom_z in sections:
        vertices.extend([
            (-top_width, y, 1.05),
            (top_width, y, 1.05),
            (-bottom_width, y, bottom_z),
            (bottom_width, y, bottom_z),
        ])

    faces = []
    for index in range(len(sections) - 1):
        a = index * 4
        b = (index + 1) * 4
        faces.extend([
            (a, b, b + 2, a + 2),
            (a + 1, a + 3, b + 3, b + 1),
            (a, a + 1, b + 1, b),
            (a + 2, b + 2, b + 3, a + 3),
        ])
    faces.append((0, 2, 3, 1))
    last = (len(sections) - 1) * 4
    faces.append((last, last + 1, last + 3, last + 2))

    hull = mesh_object("Hull_Main", vertices, faces, hull_mat)
    bevel = hull.modifiers.new("Hull bevel", "BEVEL")
    bevel.width = 0.18
    bevel.segments = 3
    weighted = hull.modifiers.new("Weighted normals", "WEIGHTED_NORMAL")
    weighted.keep_sharp = True
    return hull


def make_sail(name, y, z, width, height, mat, billow=0.32):
    columns = 6
    rows = 5
    vertices = []
    faces = []
    for row in range(rows + 1):
        v = row / rows
        current_width = width * (0.92 - v * 0.16)
        for column in range(columns + 1):
            u = column / columns
            x = (u - 0.5) * current_width
            bulge = math.sin(u * math.pi) * math.sin(v * math.pi) * billow
            vertices.append((x, y - bulge, z - v * height))
    for row in range(rows):
        for column in range(columns):
            a = row * (columns + 1) + column
            faces.append((a, a + 1, a + columns + 2, a + columns + 1))
    sail = mesh_object(name, vertices, faces, mat)
    solidify = sail.modifiers.new("Cloth thickness", "SOLIDIFY")
    solidify.thickness = 0.035
    bevel = sail.modifiers.new("Sail edge", "BEVEL")
    bevel.width = 0.025
    bevel.segments = 2
    return sail


def main(output_path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0

    hull_mat = material("Hull Navy", (0.025, 0.09, 0.18), metallic=0.18, roughness=0.34)
    wood_mat = material("Warm Deck", (0.33, 0.13, 0.035), roughness=0.48)
    dark_wood = material("Dark Wood", (0.10, 0.035, 0.012), roughness=0.44)
    gold_mat = material("Brass Trim", (0.78, 0.40, 0.07), metallic=0.75, roughness=0.24)
    sail_mat = material("Sail Navy", (0.035, 0.16, 0.38), roughness=0.62)
    cannon_mat = material("Iron", (0.025, 0.03, 0.035), metallic=0.82, roughness=0.24)
    window_mat = material("Lantern Glass", (0.8, 0.22, 0.025), metallic=0.05, roughness=0.12, emission=(1.0, 0.18, 0.015))
    rope_mat = material("Rigging", (0.055, 0.025, 0.01), roughness=0.75)

    make_hull(hull_mat)
    cube("Deck_Main", (0, -0.15, 1.13), (4.7, 9.1, 0.26), wood_mat, 0.12)
    cube("Gold_Waterline", (0, -0.15, 0.42), (5.15, 8.8, 0.12), gold_mat, 0.02)
    cube("Cabin_Main", (0, -4.0, 2.1), (4.15, 2.15, 1.95), hull_mat, 0.16)
    cube("Gold_CabinRoof", (0, -4.0, 3.12), (4.55, 2.55, 0.24), gold_mat, 0.08)
    cube("Cabin_Balcony", (0, -5.05, 1.85), (4.65, 0.30, 0.22), gold_mat, 0.05)

    for x in (-1.25, 0.0, 1.25):
        cube("Window_Glow", (x, -5.09, 2.18), (0.68, 0.12, 0.72), window_mat, 0.06)

    # Rails and posts.
    for x in (-2.25, 2.25):
        cube("Gold_Rail", (x, -0.3, 1.72), (0.10, 8.5, 0.12), gold_mat, 0.025)
        for y in (-3.9, -2.2, -0.5, 1.2, 2.9, 4.2):
            cylinder("Rail_Post", (x, y, 1.48), 0.055, 0.65, gold_mat)

    # Masts, yards and curved sails.
    mast_specs = [
        ("Main", 0.85, 10.5, 3),
        ("Rear", -2.65, 8.4, 2),
        ("Fore", 3.35, 7.6, 2),
    ]
    mast_tops = []
    for mast_name, y, height, sail_count in mast_specs:
        cylinder("Mast_" + mast_name, (0, y, 1.0 + height * 0.5), 0.17, height, dark_wood)
        mast_tops.append((0, y, 1.0 + height))
        for index in range(sail_count):
            yard_z = 5.3 + index * 2.0 if mast_name == "Main" else 4.7 + index * 2.0
            width = (5.8 if mast_name == "Main" else 4.5) - index * 0.7
            cylinder_between("Yard_" + mast_name, (-width * 0.55, y, yard_z), (width * 0.55, y, yard_z), 0.09, dark_wood)
            make_sail("Sail_" + mast_name, y, yard_z - 0.12, width, 1.62, sail_mat, 0.38)

    # Bow sprit and bowsprit sail.
    cylinder_between("Bowsprit", (0, 4.2, 1.7), (0, 8.7, 3.25), 0.14, dark_wood)
    make_sail("Sail_Bow", 6.6, 3.1, 2.7, 1.7, sail_mat, 0.26)

    # Broadside cannon batteries.
    for side in (-1, 1):
        for index in range(5):
            y = -2.9 + index * 1.45
            cylinder(
                "Cannon_Port" if side < 0 else "Cannon_Starboard",
                (side * 2.65, y, 1.45),
                0.16,
                1.15,
                cannon_mat,
                rotation=(0, math.pi / 2, 0),
                vertices=16,
            )
            cube("Gunport_Gold", (side * 2.56, y, 1.42), (0.10, 0.58, 0.58), gold_mat, 0.025)

    # Rigging from mastheads to hull, plus mast stays.
    for index, top in enumerate(mast_tops):
        y = top[1]
        rope("Rigging_Port", [top, (-2.3, y - 0.8, 1.55)], 0.027, rope_mat)
        rope("Rigging_Starboard", [top, (2.3, y - 0.8, 1.55)], 0.027, rope_mat)
        if index < len(mast_tops) - 1:
            rope("Mast_Stay", [top, mast_tops[index + 1]], 0.035, rope_mat)

    # Decorative stern lanterns and pennant.
    for x in (-1.75, 1.75):
        cylinder("Lantern_Post", (x, -5.05, 3.75), 0.055, 1.2, gold_mat)
        cube("Lantern_Glow", (x, -5.05, 4.28), (0.34, 0.34, 0.48), window_mat, 0.08)

    flag_vertices = [
        (0, -2.62, 9.65),
        (2.1, -2.62, 9.48),
        (1.72, -2.82, 8.84),
        (0, -2.62, 8.95),
    ]
    mesh_object("Flag_Navy", flag_vertices, [(0, 1, 2, 3)], sail_mat)

    # A centered root keeps Unity transforms predictable.
    root = bpy.data.objects.new("StylizedGalleon", None)
    bpy.context.collection.objects.link(root)
    for obj in list(bpy.context.collection.objects):
        if obj != root and obj.parent is None:
            obj.parent = root

    bpy.ops.object.select_all(action="SELECT")
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=output_path,
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=True,
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
    )
    print("Exported stylized galleon to " + output_path)


if __name__ == "__main__":
    separator = sys.argv.index("--") if "--" in sys.argv else len(sys.argv)
    arguments = sys.argv[separator + 1:]
    if not arguments:
        raise SystemExit("Output FBX path is required")
    main(os.path.abspath(arguments[0]))
