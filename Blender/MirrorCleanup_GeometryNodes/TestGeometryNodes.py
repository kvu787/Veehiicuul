"""Run with Blender --background --factory-startup --python-exit-code 1 --python."""
import importlib.util
import json
from pathlib import Path
import sys

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

sys.dont_write_bytecode = True
DIRECTORY = Path(__file__).resolve().parent
specification = importlib.util.spec_from_file_location('GeometryNodesMirrorCleanup', DIRECTORY / 'GeometryNodesMirrorCleanup.py')
cleanup = importlib.util.module_from_spec(specification)
specification.loader.exec_module(cleanup)


def create_mesh(name, vertices, faces, edges=()):
    mesh = bpy.data.meshes.new(name + 'Mesh')
    mesh.from_pydata(vertices, edges, faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def fixtures():
    data = json.loads((DIRECTORY / 'ExampleMeshes.json').read_text())
    objects = {}
    for name, item in data.items():
        obj = create_mesh(name, item['Vertices'], item['Faces'], item['Edges'])
        obj.location = item['Location']
        objects[name] = obj
    source = objects['AttemptedInput']
    mirror = source.modifiers.new('Mirror', 'MIRROR')
    mirror.use_clip = True
    modifier = cleanup.setup(source)
    source.vertex_groups[cleanup.MARK].add([4], 1, 'REPLACE')
    return source, objects['DesiredOutput'], mirror, modifier


def evaluated(obj):
    obj.update_tag()
    bpy.context.view_layer.update()
    graph = bpy.context.evaluated_depsgraph_get()
    return bpy.data.meshes.new_from_object(obj.evaluated_get(graph), preserve_all_data_layers=True, depsgraph=graph)


def cycle(values):
    return min(tuple(values[offset:] + values[:offset]) for offset in range(len(values)))


def assert_topology(actual, expected):
    sizes = lambda mesh: (len(mesh.vertices), len(mesh.edges), len(mesh.polygons))
    assert sizes(actual) == sizes(expected), (sizes(actual), sizes(expected))
    mapping = []
    for vertex in actual.vertices:
        target = min(expected.vertices, key=lambda other: (vertex.co - other.co).length)
        assert (vertex.co - target.co).length < 0.00002, (tuple(vertex.co), tuple(target.co))
        mapping.append(target.index)
    assert len(set(mapping)) == len(mapping)
    assert sorted(tuple(sorted(mapping[i] for i in edge.vertices)) for edge in actual.edges) == sorted(
        tuple(sorted(edge.vertices)) for edge in expected.edges), 'Edges differ'
    assert sorted(cycle([mapping[i] for i in face.vertices]) for face in actual.polygons) == sorted(
        cycle(list(face.vertices)) for face in expected.polygons), 'Face boundaries or winding differ'


def assert_surface(actual, expected):
    assert len(actual.vertices) == len(expected.vertices)
    assert max(min((vertex.co - other.co).length for other in expected.vertices)
               for vertex in actual.vertices) < 0.00002


def add_subdivision(obj):
    modifier = obj.modifiers.new('Subdivision', 'SUBSURF')
    modifier.levels = 2
    modifier.render_levels = 2
    return modifier


def corner_faces(axes):
    x, y, z = [-1 if enabled else 0 for enabled in axes]
    faces = [((1, y, z), (1, 1, z), (1, 1, 1), (1, y, 1)),
             ((x, 1, z), (x, 1, 1), (1, 1, 1), (1, 1, z)),
             ((x, y, 1), (1, y, 1), (1, 1, 1), (x, 1, 1))]
    for axis, enabled in enumerate(axes):
        if enabled:
            faces.append(tuple(tuple(-1 if index == axis else value for index, value in enumerate(point))
                               for point in reversed(faces[axis])))
    return faces


def create_surface(name, faces, transform=Matrix.Identity(4)):
    coordinates = list(dict.fromkeys(point for face in faces for point in face))
    indices = {point: index for index, point in enumerate(coordinates)}
    return create_mesh(name, [transform @ Vector(point) for point in coordinates],
                       [[indices[point] for point in face] for face in faces]), coordinates


def set_input(modifier, name, value):
    socket = next(socket for socket in modifier.node_group.interface.items_tree
                  if socket.item_type == 'SOCKET' and socket.in_out == 'INPUT' and socket.name == name)
    modifier[socket.identifier] = value


def test_axes():
    for transformed in (False, True):
        source_matrix = Matrix.Identity(4)
        transform = Matrix.Identity(4)
        mirror_object = None
        if transformed:
            source_matrix = (Matrix.Translation((3, -2, 1)) @ Euler((0.2, -0.35, 0.1)).to_matrix().to_4x4()
                             @ Matrix.Diagonal((1.2, 0.8, 1.1, 1)))
            mirror_object = bpy.data.objects.new('MirrorFrame', None)
            bpy.context.collection.objects.link(mirror_object)
            mirror_object.matrix_world = (Matrix.Translation((3.5, -1, 2)) @ Euler((-0.1, 0.25, -0.4)).to_matrix().to_4x4()
                                          @ Matrix.Diagonal((0.9, 1.1, 1.3, 1)))
            transform = source_matrix.inverted() @ mirror_object.matrix_world
        source, coordinates = create_surface('Octant', corner_faces((False, False, False)), transform)
        source.matrix_world = source_matrix
        mirror = source.modifiers.new('Mirror', 'MIRROR')
        mirror.mirror_object = mirror_object
        modifier = cleanup.setup(source)
        source.vertex_groups[cleanup.MARK].add(list(range(len(coordinates))), 1, 'REPLACE')
        subdivision = add_subdivision(source)
        for mask in range(8):
            axes = tuple(bool(mask & (1 << axis)) for axis in range(3))
            mirror.use_axis = axes
            expected, unused = create_surface('Expected', corner_faces(axes), transform)
            subdivision.show_viewport = False
            actual = evaluated(source)
            assert_topology(actual, expected.data)
            subdivision.show_viewport = True
            add_subdivision(expected)
            assert_surface(evaluated(source), evaluated(expected))
            if sum(axes) >= 2:
                intersecting = [axis for axis, enabled in enumerate(axes) if enabled][:2]
                point = tuple(0 if axis in intersecting else 1 for axis in range(3))
                index = coordinates.index(point)
                source.vertex_groups[cleanup.MARK].remove([index])
                subdivision.show_viewport = False
                actual = evaluated(source)
                center = min(actual.vertices, key=lambda vertex: (vertex.co - transform @ Vector(point)).length)
                assert (center.co - transform @ Vector(point)).length < 0.00002
                assert sum(center.index in edge.vertices for edge in actual.edges) == 4
                assert sum(center.index in face.vertices for face in actual.polygons) == 4
                source.vertex_groups[cleanup.MARK].add([index], 1, 'REPLACE')
            bpy.data.objects.remove(expected, do_unlink=True)
            print('PASS: axes', ''.join(axis for axis, enabled in zip('XYZ', axes) if enabled) or 'none',
                  'transformed', transformed)
        bpy.data.objects.remove(source, do_unlink=True)
        if mirror_object:
            bpy.data.objects.remove(mirror_object, do_unlink=True)


def test_original():
    source, target, mirror, modifier = fixtures()
    actual = evaluated(source)
    assert_topology(actual, target.data)
    assert len(source.data.vertices) == 5
    subdivision = add_subdivision(source)
    add_subdivision(target)
    expected = evaluated(target)
    assert_surface(evaluated(source), expected)
    group = source.vertex_groups[cleanup.MARK]
    group.remove([4])
    kept = evaluated(source)
    assert len(kept.vertices) == 82
    group.add([4], 1, 'REPLACE')
    group.add([3], 1, 'REPLACE')
    removed = evaluated(source)
    assert len(removed.vertices) == 66
    group.remove([3])
    assert_surface(evaluated(source), expected)
    assert [modifier.type for modifier in source.modifiers] == ['MIRROR', 'NODES', 'SUBSURF']
    print('PASS: requested topology, reversible point choices, matching subdivision, single object stack')
    subdivision.show_viewport = False
    # A point carrying a crease remains even if RemoveCenter asks for removal.
    crease = source.data.attributes.new('crease_vert', 'FLOAT', 'POINT')
    crease.data[4].value = 0.6
    actual = evaluated(source)
    assert len(actual.vertices) == 7
    center = min(actual.vertices, key=lambda vertex: (vertex.co - Vector((0, 1, 0))).length)
    assert abs(actual.attributes['crease_vert'].data[center.index].value - 0.6) < 0.00001
    source.data.attributes.remove(crease)
    for name in ('sharp_edge',):
        feature = source.data.attributes.new(name, 'BOOLEAN', 'EDGE')
        feature.data[4].value = True
        actual = evaluated(source)
        assert len(actual.vertices) == 7, (name, len(actual.vertices))
        assert sum(value.value for value in actual.attributes[name].data) == 2
        source.data.attributes.remove(feature)
    edge_crease = source.data.attributes.new('crease_edge', 'FLOAT', 'EDGE')
    edge_crease.data[0].value = 0.8
    actual = evaluated(source)
    assert sum(value.value > 0.79 for value in actual.attributes['crease_edge'].data) == 2
    target_crease = target.data.attributes.new('crease_edge', 'FLOAT', 'EDGE')
    for edge in target.data.edges:
        coordinates = [target.data.vertices[index].co for index in edge.vertices]
        if any(abs(point.x) == 0.25 and point.z == 0.25 for point in coordinates) and any(point.y == -1 for point in coordinates):
            target_crease.data[edge.index].value = 0.8
    subdivision.show_viewport = True
    assert_surface(evaluated(source), evaluated(target))
    print('PASS: point crease protection and preserved edge creases before Subdivision')
    bpy.data.objects.remove(source, do_unlink=True)
    bpy.data.objects.remove(target, do_unlink=True)


def test_boundaries():
    # A region with a hole must fall back to the complete original mirrored mesh.
    source = create_mesh('RingWithHole', [(1, 0, 0), (2, 0, 0), (2, 2, 0),
                                        (0, 2, 0), (0, 1, 0), (1, 1, 0)], [[0, 1, 2, 3, 4, 5]])
    mirror = source.modifiers.new('Mirror', 'MIRROR')
    mirror.use_axis = (True, True, False)
    expected = evaluated(source)
    modifier = cleanup.setup(source)
    source.vertex_groups[cleanup.MARK].add(list(range(6)), 1, 'REPLACE')
    assert_topology(evaluated(source), expected)
    bpy.data.objects.remove(source, do_unlink=True)

    # Coincident but disconnected shells must not be welded together.
    data = json.loads((DIRECTORY / 'ExampleMeshes.json').read_text())['AttemptedInput']
    source = create_mesh('CoincidentShells', data['Vertices'] * 2,
                         data['Faces'] + [[index + 5 for index in face] for face in data['Faces']])
    source.modifiers.new('Mirror', 'MIRROR')
    cleanup.setup(source)
    source.vertex_groups[cleanup.MARK].add([4, 9], 1, 'REPLACE')
    actual = evaluated(source)
    assert (len(actual.vertices), len(actual.edges), len(actual.polygons)) == (12, 18, 10)
    bpy.data.objects.remove(source, do_unlink=True)

    # Concave regions and bent marked boundary vertices retain their shape.
    source = create_mesh('Concave', [(0, -2, 0), (2, -2, 0), (1, 0, 0), (2, 2, 0), (0, 2, 0)], [[0, 1, 2, 3, 4]])
    source.modifiers.new('Mirror', 'MIRROR')
    cleanup.setup(source)
    source.vertex_groups[cleanup.MARK].add(list(range(5)), 1, 'REPLACE')
    expected = create_mesh('ConcaveExpected', [(-2, -2, 0), (2, -2, 0), (1, 0, 0), (2, 2, 0), (-2, 2, 0), (-1, 0, 0)],
                           [[0, 1, 2, 3, 4, 5]])
    assert_topology(evaluated(source), expected.data)
    bpy.data.objects.remove(source, do_unlink=True)
    bpy.data.objects.remove(expected, do_unlink=True)
    source = create_mesh('BentCenter', [(0, 0, 0), (-1, 1, 0), (-1, -1, 0), (0, -1, 0)], [[0, 1, 2, 3]])
    source.modifiers.new('Mirror', 'MIRROR')
    cleanup.setup(source)
    source.vertex_groups[cleanup.MARK].add([0, 3], 1, 'REPLACE')
    actual = evaluated(source)
    assert (len(actual.vertices), len(actual.polygons)) == (5, 1)
    assert any(vertex.co.length < 0.00001 for vertex in actual.vertices)
    bpy.data.objects.remove(source, do_unlink=True)

    # An actual crease along a mirror plane is not a removable seam.
    source = create_mesh('NonCoplanar', [(0, -1, 0), (-1, -1, 1), (-1, 1, 1), (0, 1, 0)], [[0, 1, 2, 3]])
    source.modifiers.new('Mirror', 'MIRROR')
    expected = evaluated(source)
    cleanup.setup(source)
    source.vertex_groups[cleanup.MARK].add([0, 3], 1, 'REPLACE')
    assert_topology(evaluated(source), expected)
    bpy.data.objects.remove(source, do_unlink=True)

    # Creased internal plane intersections must survive marked removal.
    source, coordinates = create_surface('CreasedIntersection', corner_faces((False, False, False)))
    mirror = source.modifiers.new('Mirror', 'MIRROR')
    mirror.use_axis = (True, True, True)
    cleanup.setup(source)
    source.vertex_groups[cleanup.MARK].add(list(range(len(coordinates))), 1, 'REPLACE')
    crease = source.data.attributes.new('crease_vert', 'FLOAT', 'POINT')
    crease.data[coordinates.index((1, 0, 0))].value = 0.5
    actual = evaluated(source)
    assert sum(value.value > 0.49 for value in actual.attributes['crease_vert'].data) == 2
    bpy.data.objects.remove(source, do_unlink=True)
    data = json.loads((DIRECTORY / 'ExampleMeshes.json').read_text())['AttemptedInput']
    source = create_mesh('LooseGeometry', data['Vertices'] + [(-3, 3, 0), (-4, 3, 0), (-5, 3, 0)],
                         data['Faces'], [[5, 6]])
    source.modifiers.new('Mirror', 'MIRROR')
    cleanup.setup(source)
    source.vertex_groups[cleanup.MARK].add([4], 1, 'REPLACE')
    actual = evaluated(source)
    assert (len(actual.vertices), len(actual.edges), len(actual.polygons)) == (12, 11, 5)
    bpy.data.objects.remove(source, do_unlink=True)
    print('PASS: hole fallback, disconnected shells, concavity, bends, non-coplanar seams, creased intersections, loose geometry')


def test_editing_and_materials():
    source, target, mirror, modifier = fixtures()
    first = bpy.data.materials.new('FirstMaterial')
    second = bpy.data.materials.new('SecondMaterial')
    source.data.materials.append(first)
    source.data.materials.append(second)
    source.data.polygons[0].material_index = 1
    source.data.polygons[0].use_smooth = True
    modifier = cleanup.setup(source)
    assert len(source.animation_data.drivers) == 3
    actual = evaluated(source)
    assert first in list(actual.materials) and second in list(actual.materials)
    colored = [face for face in actual.polygons if actual.materials[face.material_index] == second]
    assert len(colored) == 2 and all(face.use_smooth for face in colored), ([m.name if m else None for m in actual.materials], [(f.material_index, f.use_smooth) for f in actual.polygons])
    bpy.ops.object.select_all(action='DESELECT')
    source.select_set(True)
    bpy.context.view_layer.objects.active = source
    bpy.ops.object.mode_set(mode='EDIT')
    mesh = bmesh.from_edit_mesh(source.data)
    mesh.faces.new([mesh.verts.new(point) for point in ((-2, 2, 2), (-3, 2, 2), (-2, 3, 2))])
    bmesh.update_edit_mesh(source.data)
    source.update_from_editmode()
    actual = evaluated(source)
    assert (len(actual.vertices), len(actual.polygons)) == (12, 7)
    bpy.ops.object.mode_set(mode='OBJECT')
    # Animation and viewport edits use the same built-in dependency graph path.
    source.data.vertices[0].co.z = 0.5
    bpy.context.scene.frame_set(2)
    actual = evaluated(source)
    assert sum(abs(vertex.co.z - 0.5) < 0.00001 for vertex in actual.vertices) == 2
    source.data.vertices[0].co.z = 0.25
    modifier.show_viewport = False
    assert len(evaluated(source).vertices) == 13
    modifier.show_viewport = True
    assert len(evaluated(source).vertices) == 12
    bpy.data.objects.remove(source, do_unlink=True)
    bpy.data.objects.remove(target, do_unlink=True)
    print('PASS: materials, smooth shading, repeated setup, live Edit Mode geometry, frame changes, modifier toggle')


def main():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    test_original()
    test_axes()
    test_boundaries()
    test_editing_and_materials()


if __name__ == '__main__':
    main()
