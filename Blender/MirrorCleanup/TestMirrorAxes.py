"""Axis-combination checks called by TestLiveMirrorCleanup.py."""

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector


def create_surface(name, faces, transform):
    coordinates = list(dict.fromkeys(point for face in faces for point in face))
    indices = {point: index for index, point in enumerate(coordinates)}
    mesh = bpy.data.meshes.new(name + 'Mesh')
    mesh.from_pydata([transform @ Vector(point) for point in coordinates], [],
                     [[indices[point] for point in face] for face in faces])
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj, coordinates


def corner_faces(axes):
    lower = [-1 if enabled else 0 for enabled in axes]
    x, y, z = lower
    faces = [((1, y, z), (1, 1, z), (1, 1, 1), (1, y, 1)),
             ((x, 1, z), (x, 1, 1), (1, 1, 1), (1, 1, z)),
             ((x, y, 1), (1, y, 1), (1, 1, 1), (x, 1, 1))]
    for axis, enabled in enumerate(axes):
        if enabled:
            faces.append(tuple(tuple(-1 if index == axis else value
                                     for index, value in enumerate(point))
                               for point in reversed(faces[axis])))
    return faces


def cycle(indices):
    return min(tuple(indices[offset:] + indices[:offset]) for offset in range(len(indices)))


def assert_same_topology(actual, expected):
    assert (len(actual.vertices), len(actual.edges), len(actual.polygons)) == (
        len(expected.vertices), len(expected.edges), len(expected.polygons))
    correspondence = []
    for vertex in actual.vertices:
        nearest = min(expected.vertices, key=lambda other: (vertex.co - other.co).length)
        assert (vertex.co - nearest.co).length < 0.00002
        correspondence.append(nearest.index)
    assert len(set(correspondence)) == len(correspondence)
    assert sorted(tuple(sorted(correspondence[index] for index in edge.vertices))
                  for edge in actual.edges) == sorted(tuple(sorted(edge.vertices)) for edge in expected.edges)
    assert sorted(cycle([correspondence[index] for index in face.vertices])
                  for face in actual.polygons) == sorted(cycle(list(face.vertices)) for face in expected.polygons)


def assert_same_subdivision(actual, expected):
    graph = bpy.context.evaluated_depsgraph_get()
    points = [vertex.co.copy() for vertex in actual.evaluated_get(graph).data.vertices]
    targets = [vertex.co.copy() for vertex in expected.evaluated_get(graph).data.vertices]
    assert len(points) == len(targets)
    assert max(min((point - target).length for target in targets) for point in points) < 0.00002


def assert_symmetric(mesh, axes, transform):
    inverse = transform.inverted()
    points = [inverse @ vertex.co for vertex in mesh.vertices]
    for point in points:
        for axis, enabled in enumerate(axes):
            if enabled:
                reflected = point.copy()
                reflected[axis] *= -1
                assert min((reflected - other).length for other in points) < 0.00002


def remove_objects(objects):
    for obj in objects:
        mesh = obj.data if obj.type == 'MESH' else None
        bpy.data.objects.remove(obj, do_unlink=True)
        if mesh is not None and mesh.users == 0:
            bpy.data.meshes.remove(mesh)


def test_axes(cleanup):
    input_faces = corner_faces((False, False, False))
    for transformed in (False, True):
        created = []
        if transformed:
            source_matrix = (Matrix.Translation((3, -2, 1))
                             @ Euler((0.2, -0.35, 0.1)).to_matrix().to_4x4()
                             @ Matrix.Diagonal((1.2, 0.8, 1.1, 1)))
            mirror_object = bpy.data.objects.new('TestMirrorFrame', None)
            bpy.context.collection.objects.link(mirror_object)
            mirror_object.matrix_world = (Matrix.Translation((3.5, -1, 2))
                                          @ Euler((-0.1, 0.25, -0.4)).to_matrix().to_4x4()
                                          @ Matrix.Diagonal((0.9, 1.1, 1.3, 1)))
            created.append(mirror_object)
            transform = source_matrix.inverted() @ mirror_object.matrix_world
        else:
            source_matrix = Matrix.Identity(4)
            mirror_object = None
            transform = Matrix.Identity(4)
        source, input_coordinates = create_surface('AxisCombinationInput', input_faces, transform)
        source.matrix_world = source_matrix
        mirror = source.modifiers.new('Mirror', 'MIRROR')
        mirror.mirror_object = mirror_object
        output = cleanup.create_output(source)
        created.extend((output, source))
        group = source.vertex_groups[cleanup.GROUP_NAME]
        group.add(list(range(len(source.data.vertices))), 1.0, 'REPLACE')
        subdivision = output.modifiers.new('Subdivision', 'SUBSURF')
        subdivision.levels = 2
        original_coordinates = [vertex.co.copy() for vertex in source.data.vertices]

        for mask in range(8):
            axes = tuple(bool(mask & (1 << axis)) for axis in range(3))
            mirror.use_axis = axes
            bpy.context.view_layer.update()
            assert output.name in cleanup._pending, 'Toggling axes did not queue a live rebuild.'
            cleanup.refresh_pending()
            assert output['MirrorCleanupError'] == ''
            expected, unused_coordinates = create_surface('ExpectedAxes', corner_faces(axes), transform)
            expected_subdivision = expected.modifiers.new('Subdivision', 'SUBSURF')
            expected_subdivision.levels = 2
            assert_same_topology(output.data, expected.data)
            assert_symmetric(output.data, axes, transform)
            assert_same_subdivision(output, expected)

            if sum(axes) >= 2:
                # A kept flat intersection must remain connected, not become a
                # loose vertex or disappear while its four seam edges dissolve.
                intersecting = [axis for axis, enabled in enumerate(axes) if enabled][:2]
                point = tuple(0 if axis in intersecting else 1 for axis in range(3))
                index = input_coordinates.index(point)
                group.remove([index])
                cleanup.refresh_output(output)
                position = transform @ Vector(point)
                center = min(output.data.vertices, key=lambda vertex: (vertex.co - position).length)
                assert (center.co - position).length < 0.00002
                assert sum(center.index in edge.vertices for edge in output.data.edges) == 4
                assert sum(center.index in face.vertices for face in output.data.polygons) == 4
                assert len(output.data.polygons) > len(expected.data.polygons)
                assert_symmetric(output.data, axes, transform)
                group.add([index], 1.0, 'REPLACE')
                cleanup.refresh_output(output)
                assert_same_topology(output.data, expected.data)
                assert_same_subdivision(output, expected)
            remove_objects([expected])
            label = ''.join(axis for axis, enabled in zip('XYZ', axes) if enabled) or 'Off'
            print('PASS: axes', label, 'in', 'transformed mirror-object frame' if transformed else 'local frame')

        assert all((vertex.co - original).length == 0
                   for vertex, original in zip(source.data.vertices, original_coordinates))

        # All axes off is a normal pass-through, including inactive merge/edit settings.
        mirror.use_axis = (False, False, False)
        mirror.use_mirror_merge = False
        mirror.show_in_editmode = False
        bpy.context.view_layer.update()
        cleanup.refresh_pending()
        assert output['MirrorCleanupError'] == ''
        assert_same_topology(output.data, source.data)
        mirror.use_mirror_merge = True
        mirror.show_in_editmode = True
        mirror.use_axis = (True, True, True)
        bpy.context.view_layer.update()
        cleanup.refresh_pending()

        # Newly modeled geometry receives all eight reflections in Edit Mode.
        previous_active = bpy.context.view_layer.objects.active
        selected = list(bpy.context.selected_objects)
        bpy.ops.object.select_all(action='DESELECT')
        source.select_set(True)
        bpy.context.view_layer.objects.active = source
        bpy.ops.object.mode_set(mode='EDIT')
        bm = bmesh.from_edit_mesh(source.data)
        vertices = [bm.verts.new(transform @ Vector(point))
                    for point in ((3, 3, 3), (3.5, 3, 3), (3, 3.5, 3))]
        bm.faces.new(vertices)
        bmesh.update_edit_mesh(source.data)
        bpy.context.view_layer.update()
        assert output.name in cleanup._pending
        cleanup.refresh_pending()
        assert len(output.data.vertices) == 32 and len(output.data.polygons) == 14
        assert_symmetric(output.data, (True, True, True), transform)
        bpy.ops.object.mode_set(mode='OBJECT')
        bpy.ops.object.select_all(action='DESELECT')
        for obj in selected:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = previous_active
        remove_objects(created)
    print('PASS: kept intersection vertices stay connected; marked intersections dissolve before Subdivision')
    print('PASS: all axes off passes through; new Edit Mode geometry mirrors across XYZ')

    # Both endpoints are on an active plane, but not on the same active plane.
    # Their shared diagonal is real input topology and must not be dissolved.
    source, unused_coordinates = create_surface(
        'DifferentPlaneEndpoints',
        [((0, 1, 2), (1, 0, 2), (1, 1, 2)),
         ((1, 0, 2), (0, 1, 2), (0.25, 0.25, 2))], Matrix.Identity(4))
    mirror = source.modifiers.new('Mirror', 'MIRROR')
    mirror.use_axis = (True, True, False)
    output = cleanup.create_output(source)
    group = source.vertex_groups[cleanup.GROUP_NAME]
    group.add(list(range(len(source.data.vertices))), 1.0, 'REPLACE')
    cleanup.refresh_output(output)
    assert len(output.data.polygons) == 8
    assert_symmetric(output.data, (True, True, False), Matrix.Identity(4))
    remove_objects([output, source])
    print('PASS: edges joining different symmetry planes are not mistaken for seam edges')
