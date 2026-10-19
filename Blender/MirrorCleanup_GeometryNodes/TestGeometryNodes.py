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
    # Match each expected point once, so duplicate actual points cannot hide a
    # missing point elsewhere on the subdivided surface.
    remaining = list(expected.vertices)
    for vertex in actual.vertices:
        closest = min(range(len(remaining)), key=lambda index: (vertex.co - remaining[index].co).length)
        assert (vertex.co - remaining.pop(closest).co).length < 0.00002


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
    assert len(source.animation_data.drivers) == 5
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


def test_small_creases():
    for domain in ('POINT', 'EDGE'):
        source, target, mirror, modifier = fixtures()
        name = 'crease_vert' if domain == 'POINT' else 'crease_edge'
        crease = source.data.attributes.new(name, 'FLOAT', domain)
        # Both the direct crease check and edge-to-point averaging must preserve
        # positive weights below Blender Compare's default epsilon of 0.001.
        crease.data[4].value = 0.0001
        actual = evaluated(source)
        assert len(actual.vertices) == 7, (domain, len(actual.vertices))
        assert sum(item.value > 0 for item in actual.attributes[name].data) == (1 if domain == 'POINT' else 2)
        bpy.data.objects.remove(source, do_unlink=True)
        bpy.data.objects.remove(target, do_unlink=True)
    print('PASS: small positive point and edge creases preserve marked vertices')


def test_point_attributes():
    source, target, mirror, modifier = fixtures()
    attributes = [('Scalar', 'FLOAT', 'value', lambda i: i / 10),
                  ('Integer', 'INT', 'value', lambda i: 16777217 + i),
                  ('SmallInteger', 'INT8', 'value', lambda i: i - 2),
                  ('Direction', 'FLOAT_VECTOR', 'vector', lambda i: (i, i + 0.25, i + 0.5)),
                  ('Coordinates', 'FLOAT2', 'vector', lambda i: (i + 0.25, i + 0.5)),
                  ('Color', 'FLOAT_COLOR', 'color', lambda i: (i / 10, 0.5, 0.75, 1)),
                  ('ByteColor', 'BYTE_COLOR', 'color', lambda i: (i / 10, 0.5, 0.75, 1)),
                  ('Flag', 'BOOLEAN', 'value', lambda i: i % 2 == 0),
                  ('Rotation', 'QUATERNION', 'value', lambda i: Euler((i / 10, 0, 0)).to_quaternion())]
    for name, kind, property_name, make_value in attributes:
        attribute = source.data.attributes.new(name, kind, 'POINT')
        for index, item in enumerate(attribute.data):
            setattr(item, property_name, make_value(index))
    influence = source.vertex_groups.new(name='Influence')
    for index in range(len(source.data.vertices)):
        influence.add([index], (index + 1) / 10, 'REPLACE')
    cleanup.setup(source)
    actual = evaluated(source)
    assert_topology(actual, target.data)
    for vertex in actual.vertices:
        point = Vector((-abs(vertex.co.x), vertex.co.y, vertex.co.z))
        original = min(source.data.vertices, key=lambda item: (item.co - point).length)
        assert (original.co - point).length < 0.00001
        for name, kind, property_name, unused in attributes:
            assert actual.attributes[name].data_type == kind
            expected_value = getattr(source.data.attributes[name].data[original.index], property_name)
            actual_value = getattr(actual.attributes[name].data[vertex.index], property_name)
            if kind in ('INT', 'INT8', 'BOOLEAN'):
                assert actual_value == expected_value, (name, actual_value, expected_value)
            elif kind == 'FLOAT':
                assert abs(actual_value - expected_value) < 0.000001
            else:
                assert all(abs(a - b) < 0.000001 for a, b in zip(actual_value, expected_value)), (name, actual_value, expected_value)
        weights = {item.group: item.weight for item in vertex.groups}
        assert abs(weights[influence.index] - (original.index + 1) / 10) < 0.000001
    assert not any(attribute.name.startswith(cleanup.PREFIX) for attribute in actual.attributes)
    bpy.data.objects.remove(source, do_unlink=True)
    bpy.data.objects.remove(target, do_unlink=True)
    print('PASS: all nine supported point attribute types and vertex groups survive by identity')


def test_material_slots():
    for override in (False, True):
        source, target, mirror, modifier = fixtures()
        material = bpy.data.materials.new('MeshMaterial')
        source.data.materials.append(material)
        source.data.materials.append(material)
        source.material_slots[0].material = None
        if override:
            material = bpy.data.materials.new('ObjectMaterial')
            source.material_slots[1].link = 'OBJECT'
            source.material_slots[1].material = material
        for face in source.data.polygons:
            face.material_index = 0 if face.index == 3 else 1
        cleanup.setup(source)
        actual = evaluated(source)
        for face in actual.polygons:
            bottom = all(abs(actual.vertices[index].co.z) < 0.000001 for index in face.vertices)
            assert actual.materials[face.material_index] == (None if bottom else material)
        bpy.data.objects.remove(source, do_unlink=True)
        bpy.data.objects.remove(target, do_unlink=True)
    print('PASS: empty slots and mesh/object material assignments on rebuilt and untouched faces')


def test_retained_interior_geometry():
    # Exercise the reusable graph directly on a four-face plane. Connectivity
    # around a retained spoke must not turn the whole region into a single ngon.
    vertices = [(0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0), (-1, 1, 0),
                (-1, 0, 0), (-1, -1, 0), (0, -1, 0), (1, -1, 0)]
    faces = [[0, 1, 2, 3], [0, 3, 4, 5], [0, 5, 6, 7], [0, 7, 8, 1]]
    for feature in ('crease_edge', 'sharp_edge', 'Wire'):
        source = create_mesh('RetainedInterior', vertices + ([(0, 0, 1)] if feature == 'Wire' else []),
                             faces, [(0, 9)] if feature == 'Wire' else [])
        group = source.vertex_groups.new(name=cleanup.MARK)
        group.add(list(range(len(source.data.vertices))), 1, 'REPLACE')
        if feature != 'Wire':
            attribute = source.data.attributes.new(feature, 'FLOAT' if feature == 'crease_edge' else 'BOOLEAN', 'EDGE')
            for edge in source.data.edges:
                if set(edge.vertices) == {0, 1}:
                    attribute.data[edge.index].value = 0.8 if feature == 'crease_edge' else True
        # Unsupported custom edge/face/corner values must also survive fallback.
        for name, domain in (('EdgeLabel', 'EDGE'), ('FaceLabel', 'FACE'), ('CornerLabel', 'CORNER')):
            attribute = source.data.attributes.new(name, 'INT', domain)
            for index, item in enumerate(attribute.data):
                item.value = index + 1
        expected = evaluated(source)
        modifier = source.modifiers.new('Cleanup', 'NODES')
        modifier.node_group = cleanup.build_group(source)
        set_input(modifier, 'Mirror Y', True)
        actual = evaluated(source)
        assert_topology(actual, expected)
        assert set(actual.attributes.keys()) == set(expected.attributes.keys())
        for name in ('EdgeLabel', 'FaceLabel', 'CornerLabel'):
            assert [item.value for item in actual.attributes[name].data] == [item.value for item in expected.attributes[name].data]
        if feature != 'Wire':
            assert [item.value for item in actual.attributes[feature].data] == [item.value for item in expected.attributes[feature].data]
        bpy.data.objects.remove(source, do_unlink=True)
    print('PASS: creased/sharp interior edges and attached wires trigger complete attribute-preserving fallback')


def test_empty_and_wire_meshes():
    for vertices, edges in (([], []), ([(0, 0, 0)], []),
                            ([(0, 0, 0), (-1, 0, 0), (-2, 0, 0)], [[0, 1], [1, 2]])):
        source = create_mesh('Wire', vertices, [], edges)
        source.modifiers.new('Mirror', 'MIRROR')
        expected = evaluated(source)
        cleanup.setup(source)
        assert_topology(evaluated(source), expected)
        bpy.data.objects.remove(source, do_unlink=True)
    print('PASS: empty, point-only, and unmarked wire-only meshes')


def wire_graph(vertices, edges, faces=()):
    source = create_mesh('WireCase', vertices, faces, edges)
    source.vertex_groups.new(name=cleanup.MARK).add(list(range(len(vertices))), 1, 'REPLACE')
    identity = source.data.attributes.new('Identity', 'INT', 'POINT')
    for index, item in enumerate(identity.data):
        item.value = index
    modifier = source.modifiers.new('Cleanup', 'NODES')
    modifier.node_group = cleanup.build_group(source)
    set_input(modifier, 'Mirror X', False)
    set_input(modifier, 'Mirror Z', True)
    return source, modifier


def assert_wire_identity(mesh, source, vertices, edges, faces=()):
    identities = [item.value for item in mesh.attributes['Identity'].data]
    assert sorted(identities) == sorted(vertices), identities
    assert sorted(tuple(sorted(identities[i] for i in edge.vertices)) for edge in mesh.edges) == sorted(
        tuple(sorted(edge)) for edge in edges)
    assert sorted(cycle([identities[i] for i in face.vertices]) for face in mesh.polygons) == sorted(
        cycle(list(face)) for face in faces)
    for vertex, original in zip(mesh.vertices, identities):
        assert (vertex.co - source.data.vertices[original].co).length < 0.000001
    assert not any(attribute.name.startswith(cleanup.PREFIX) for attribute in mesh.attributes)


def test_wire_topology():
    cases = [
        ('Consecutive', [(-2, 0, 0), (-1, 0, 0), (0, 0, 0), (1, 0, 0), (2, 0, 0)],
         [(0, 1), (1, 2), (2, 3), (3, 4)], (), [0, 4], [(0, 4)], ()),
        ('ClosedLoop', [(-1, -1, 0), (0, -1, 0), (1, -1, 0), (1, 0, 0),
                        (1, 1, 0), (0, 1, 0), (-1, 1, 0), (-1, 0, 0)],
         [(i, (i + 1) % 8) for i in range(8)], (), [0, 2, 4, 6], [(0, 2), (2, 4), (4, 6), (6, 0)], ()),
        ('Branch', [(-2, 0, 0), (-1, 0, 0), (0, 0, 0), (1, 0, 0), (2, 0, 0), (0, 1, 0)],
         [(0, 1), (1, 2), (2, 3), (3, 4), (2, 5)], (), [0, 2, 4, 5], [(0, 2), (2, 4), (2, 5)], ()),
        ('FaceAttachment', [(-2, 0, 0), (-1, 0, 0), (0, 0, 0), (1, 0, 0),
                            (2, 0, 0), (0, 1, 0), (1, 1, 0)],
         [(0, 1), (1, 2), (2, 3), (3, 4)], [(2, 5, 6)], [0, 2, 4, 5, 6],
         [(0, 2), (2, 4), (2, 5), (5, 6), (6, 2)], [(2, 5, 6)]),
        ('CoincidentChains', [(-1, 0, 0), (0, 0, 0), (1, 0, 0)] * 2,
         [(0, 1), (1, 2), (3, 4), (4, 5)], (), [0, 2, 3, 5], [(0, 2), (3, 5)], ()),
        ('BendAndIsolatedPoint', [(-1, 0, 0), (0, 1, 0), (1, 0, 0), (4, 0, 0)],
         [(0, 1), (1, 2)], (), [0, 1, 2, 3], [(0, 1), (1, 2)], ()),
        ('OffPlane', [(-1, 0, 1), (0, 0, 1), (1, 0, 1)],
         [(0, 1), (1, 2)], (), [0, 1, 2], [(0, 1), (1, 2)], ()),
        ('ZeroLength', [(-1, 0, 0), (0, 0, 0), (0, 0, 0)],
         [(0, 1), (1, 2)], (), [0, 1, 2], [(0, 1), (1, 2)], ())]
    for name, points, edges, faces, kept, result_edges, result_faces in cases:
        source, modifier = wire_graph(points, edges, faces)
        assert_wire_identity(evaluated(source), source, kept, result_edges, result_faces)
        assert not list(modifier.node_warnings), (name, [warning.message for warning in modifier.node_warnings])
        if name == 'ClosedLoop':
            set_input(modifier, 'Direction Tolerance', 3)
            assert_wire_identity(evaluated(source), source, list(range(8)), edges)
        bpy.data.objects.remove(source, do_unlink=True)
    print('PASS: open and closed wires, consecutive removals, branches, face attachments, coincident chains, bends, off-plane and zero-length edges')


def test_wire_features():
    points = [(-1, 0, 0), (0, 0, 0), (1, 0, 0), (-1, 2, 0), (0, 2, 0), (1, 2, 0)]
    edges = [(0, 1), (1, 2), (3, 4), (4, 5)]
    for name, domain, kind, index, value in (('crease_vert', 'POINT', 'FLOAT', 1, 0.0001),
                                           ('crease_edge', 'EDGE', 'FLOAT', 0, 0.0001),
                                           ('sharp_edge', 'EDGE', 'BOOLEAN', 0, True)):
        source, modifier = wire_graph(points, edges)
        feature = source.data.attributes.new(name, kind, domain)
        feature.data[index].value = value
        actual = evaluated(source)
        assert_wire_identity(actual, source, [0, 1, 2, 3, 5], [(0, 1), (1, 2), (3, 5)])
        assert sum(item.value > 0 for item in actual.attributes[name].data) == 1
        identities = [item.value for item in actual.attributes['Identity'].data]
        if domain == 'POINT':
            assert abs(actual.attributes[name].data[identities.index(1)].value - value) < 1e-8
        else:
            index = next(edge.index for edge in actual.edges if {identities[i] for i in edge.vertices} == {0, 1})
            assert abs(actual.attributes[name].data[index].value - value) < 1e-8
        source.data.attributes.remove(feature)
        source.vertex_groups[cleanup.MARK].add([1], 0.5, 'REPLACE')
        assert_wire_identity(evaluated(source), source, [0, 1, 2, 3, 5], [(0, 1), (1, 2), (3, 5)])
        source.vertex_groups[cleanup.MARK].add([1], 1, 'REPLACE')
        assert_wire_identity(evaluated(source), source, [0, 2, 3, 5], [(0, 2), (3, 5)])
        bpy.data.objects.remove(source, do_unlink=True)
    print('PASS: wire point attributes, small creases, sharp edges, weight threshold, and reversible marking')


def test_mirrored_wires():
    for transformed in (False, True):
        frame = None
        transform = Matrix.Identity(4)
        if transformed:
            frame = bpy.data.objects.new('WireFrame', None)
            bpy.context.collection.objects.link(frame)
            frame.matrix_world = Matrix.Translation((1, 2, 3)) @ Euler((0.2, -0.3, 0.4)).to_matrix().to_4x4() @ Matrix.Diagonal((-2, 0.7, 1.1, 1))
            transform = frame.matrix_world.copy()
        for axis in range(3):
            points = [Vector(tuple(distance if i == axis else 0 for i in range(3))) for distance in (0, 1, 2)]
            source = create_mesh('MirroredWire', [transform @ point for point in points], [], [(0, 1), (1, 2)])
            # An empty UV map must not warn on a wire-only object.
            source.data.uv_layers.new(name='UVMap')
            mirror = source.modifiers.new('Mirror', 'MIRROR')
            mirror.use_axis = tuple(i == axis for i in range(3))
            mirror.mirror_object = frame
            modifier = cleanup.setup(source)
            source.vertex_groups[cleanup.MARK].add([0], 1, 'REPLACE')
            expected = create_mesh('ExpectedWire', [transform @ point for point in (-points[2], -points[1], points[1], points[2])],
                                   [], [(0, 1), (1, 2), (2, 3)])
            assert_topology(evaluated(source), expected.data)
            assert not list(modifier.node_warnings), [warning.message for warning in modifier.node_warnings]
            if not transformed and axis == 0:
                # Model a longer wire in Edit Mode while the cleanup is active.
                bpy.ops.object.select_all(action='DESELECT')
                source.select_set(True)
                bpy.context.view_layer.objects.active = source
                bpy.ops.object.mode_set(mode='EDIT')
                try:
                    editable = bmesh.from_edit_mesh(source.data)
                    editable.verts.ensure_lookup_table()
                    endpoint = editable.verts[2]
                    editable.edges.new((endpoint, editable.verts.new((3, 0, 0))))
                    bmesh.update_edit_mesh(source.data)
                    source.update_from_editmode()
                    extended = create_mesh('ExtendedWire', [(x, 0, 0) for x in (-3, -2, -1, 1, 2, 3)],
                                           [], [(i, i + 1) for i in range(5)])
                    assert_topology(evaluated(source), extended.data)
                    bpy.data.objects.remove(extended, do_unlink=True)
                finally:
                    bpy.ops.object.mode_set(mode='OBJECT')
            mirror.use_axis = (False, False, False)
            assert_topology(evaluated(source), source.data)
            mirror.use_axis = tuple(i == axis for i in range(3))
            mirror.show_viewport = False
            assert_topology(evaluated(source), source.data)
            bpy.data.objects.remove(source, do_unlink=True)
            bpy.data.objects.remove(expected, do_unlink=True)
        if frame:
            bpy.data.objects.remove(frame, do_unlink=True)
    print('PASS: marked mirrored wires on X/Y/Z, transformed mirror frame, live Edit Mode additions, axis/viewport bypass, empty UV maps, no spurious warnings')


def test_attribute_warnings():
    source, target, mirror, modifier = fixtures()
    source.data.uv_layers.new(name='UVMap')
    modifier = cleanup.setup(source)
    assert_topology(evaluated(source), target.data)
    warnings = [warning.message for warning in modifier.node_warnings]
    assert warnings == ['Mirror Cleanup prototype: rebuilt faces do not retain UV maps or other corner attributes.'], warnings
    bpy.data.objects.remove(source, do_unlink=True)
    bpy.data.objects.remove(target, do_unlink=True)
    print('PASS: UV warning remains visible when faces are actually rebuilt')


def test_wire_outline():
    # A quarter outline mirrored over X and Z, with the same connectivity as
    # Spaceship. Keep the fixture local so this suite remains self-contained.
    points = [(3, -1, 1), (2, 1, 2), (1, 2, 3), (3, -2, 1), (3, -2, 0), (0, 2, 3)]
    source = create_mesh('WireOutline', points, [], [(0, 1), (1, 2), (0, 3), (3, 4), (2, 5)])
    source.data.uv_layers.new(name='UVMap')
    mirror = source.modifiers.new('Mirror', 'MIRROR')
    mirror.use_axis = (True, False, True)
    mirrored = evaluated(source)
    modifier = cleanup.setup(source)
    group = source.vertex_groups[cleanup.MARK]
    assert_topology(evaluated(source), mirrored)
    group.add([4], 1, 'REPLACE')
    assert len(evaluated(source).vertices) == 18
    group.add([5], 1, 'REPLACE')
    expected_points = [(x * sign_x, y, z * sign_z) for sign_z in (1, -1) for sign_x in (1, -1)
                       for x, y, z in (points[i] for i in (3, 0, 1, 2))]
    expected_edges = [(offset + i, offset + i + 1) for offset in (0, 4, 8, 12) for i in range(3)]
    expected_edges += [(3, 7), (11, 15), (0, 8), (4, 12)]
    target = create_mesh('ExpectedOutline', expected_points, [], expected_edges)
    assert_topology(evaluated(source), target.data)
    assert not list(modifier.node_warnings)
    group.remove([4, 5])
    assert_topology(evaluated(source), mirrored)
    bpy.data.objects.remove(source, do_unlink=True)
    bpy.data.objects.remove(target, do_unlink=True)
    print('PASS: XZ mirrored outline with reversible 20 -> 18 -> 16 vertex cleanup and no warnings')


def test_warped_faces():
    source = create_mesh('WarpedQuad', [(0, -1, 0), (1, -1, 1), (1, 1, -1), (0, 1, 0)], [[0, 1, 2, 3]])
    source.modifiers.new('Mirror', 'MIRROR')
    expected = evaluated(source)
    assert (expected.polygons[0].normal - expected.polygons[1].normal).length < 0.000001
    cleanup.setup(source)
    assert_topology(evaluated(source), expected)
    bpy.data.objects.remove(source, do_unlink=True)
    print('PASS: warped faces with matching average normals preserve their original topology')


def test_setup_lifecycle():
    source, target, mirror, modifier = fixtures()
    subdivision = add_subdivision(source)
    last_mirror = source.modifiers.new('Last "Mirror" \\', 'MIRROR')
    source.modifiers.move(list(source.modifiers).index(last_mirror), 1)
    mirror.use_axis = (False, False, False)
    last_mirror.use_axis = (True, False, False)
    source.driver_add('location', 2).driver.expression = '0'
    set_input(modifier, 'Plane Tolerance', 0.00002)
    set_input(modifier, 'Direction Tolerance', 0.0002)
    modifier.show_render = False
    previous_tree = modifier.node_group
    previous_groups = set(bpy.data.node_groups)
    original_builder = cleanup.build_group

    def failing_builder(obj):
        original_builder(obj)
        raise RuntimeError('Deliberate construction failure')

    cleanup.build_group = failing_builder
    try:
        try:
            cleanup.setup(source)
        except RuntimeError as error:
            assert str(error) == 'Deliberate construction failure'
        else:
            raise AssertionError('Expected the simulated construction failure')
    finally:
        cleanup.build_group = original_builder
    assert modifier in list(source.modifiers) and modifier.node_group == previous_tree
    assert set(bpy.data.node_groups) == previous_groups
    assert len(source.animation_data.drivers) == 6

    # The old node group may also be used by a duplicated object.
    other = source.copy()
    bpy.context.collection.objects.link(other)
    modifier = cleanup.setup(source)
    assert previous_tree in list(bpy.data.node_groups)
    assert [item.type for item in source.modifiers] == ['MIRROR', 'MIRROR', 'NODES', 'SUBSURF']
    assert source.modifiers[-1] == subdivision
    assert len(source.animation_data.drivers) == 6
    assert not modifier.show_render
    for socket in modifier.node_group.interface.items_tree:
        if socket.item_type == 'SOCKET' and socket.in_out == 'INPUT':
            if socket.name in ('Plane Tolerance', 'Direction Tolerance'):
                assert abs(modifier[socket.identifier] - (0.00002 if socket.name == 'Plane Tolerance' else 0.0002)) < 1e-9
    subdivision.show_viewport = False
    assert_topology(evaluated(source), target.data)
    assert all(curve.is_valid for curve in source.animation_data.drivers)
    last_mirror.use_axis = (False, False, False)
    assert_topology(evaluated(source), source.data)
    last_mirror.use_axis = (True, False, False)
    assert_topology(evaluated(source), target.data)

    for invalid_state in ('Merge', 'EditMode'):
        previous_groups = set(bpy.data.node_groups)
        if invalid_state == 'Merge':
            last_mirror.use_mirror_merge = False
        else:
            bpy.ops.object.select_all(action='DESELECT')
            source.select_set(True)
            bpy.context.view_layer.objects.active = source
            bpy.ops.object.mode_set(mode='EDIT')
        try:
            try:
                cleanup.setup(source)
            except ValueError:
                pass
            else:
                raise AssertionError('Expected setup to reject ' + invalid_state)
            assert modifier in list(source.modifiers)
            assert set(bpy.data.node_groups) == previous_groups
        finally:
            last_mirror.use_mirror_merge = True
            if source.mode != 'OBJECT':
                bpy.ops.object.mode_set(mode='OBJECT')
    bpy.data.objects.remove(other, do_unlink=True)
    bpy.data.objects.remove(source, do_unlink=True)
    bpy.data.objects.remove(target, do_unlink=True)
    print('PASS: setup rollback, shared groups, retained settings, last-Mirror drivers, unrelated modifiers/drivers, invalid setup states')


def test_attribute_name_validation():
    source, target, mirror, modifier = fixtures()
    tree = modifier.node_group
    previous_groups = set(bpy.data.node_groups)
    previous_drivers = list(source.animation_data.drivers)

    def rejected():
        try:
            cleanup.setup(source)
        except ValueError as error:
            assert 'attribute' in str(error)
        else:
            raise AssertionError('Expected conflicting attribute names to be rejected')
        assert modifier in list(source.modifiers) and modifier.node_group == tree
        assert set(bpy.data.node_groups) == previous_groups
        assert list(source.animation_data.drivers) == previous_drivers

    for name in (*cleanup.MESH_ATTRIBUTES, '.select_vert', cleanup.PREFIX + 'Vertex'):
        group = source.vertex_groups.new(name=name)
        group.add([0, 1, 2, 3, 4], 0.25, 'REPLACE')
        rejected()
        source.vertex_groups.remove(group)
    attribute = source.data.attributes.new(cleanup.PREFIX + 'Vertex', 'INT', 'POINT')
    rejected()
    source.data.attributes.remove(attribute)
    attribute = source.data.attributes.new('crease_edge', 'FLOAT', 'POINT')
    rejected()
    source.data.attributes.remove(attribute)
    assert_topology(evaluated(source), target.data)
    bpy.data.objects.remove(source, do_unlink=True)
    bpy.data.objects.remove(target, do_unlink=True)

    # A preexisting generic attribute must not silently replace the user-facing
    # RemoveCenter vertex group or cause it to be created under a different name.
    source = create_mesh('ConflictingControl', [(0, 0, 0)], [])
    source.modifiers.new('Mirror', 'MIRROR')
    source.data.attributes.new(cleanup.MARK, 'FLOAT', 'POINT')
    previous_groups = set(bpy.data.node_groups)
    try:
        cleanup.setup(source)
    except ValueError as error:
        assert cleanup.MARK in str(error)
    else:
        raise AssertionError('Expected the reserved control attribute to be rejected')
    assert len(source.vertex_groups) == 0 and len(source.modifiers) == 1
    assert set(bpy.data.node_groups) == previous_groups
    bpy.data.objects.remove(source, do_unlink=True)
    print('PASS: conflicting vertex groups, reserved attributes, and invalid crease domains are rejected without altering setup')


def test_mirror_visibility():
    # Bisect makes the enabled modifier produce a simple full plane. Disabling
    # Mirror must preserve the original two-face plane even while cleanup is on.
    source = create_mesh('Visibility', [(-1, -1, 0), (0, -1, 0), (1, -1, 0),
                                       (-1, 1, 0), (0, 1, 0), (1, 1, 0)],
                         [[0, 1, 4, 3], [1, 2, 5, 4]])
    for domain in ('EDGE', 'FACE', 'CORNER'):
        attribute = source.data.attributes.new(domain + 'Label', 'INT', domain)
        for index, item in enumerate(attribute.data):
            item.value = index + 1
    mirror = source.modifiers.new('Mirror', 'MIRROR')
    mirror.use_bisect_axis[0] = True
    cleanup.setup(source)
    source.vertex_groups[cleanup.MARK].add([1, 4], 1, 'REPLACE')
    reference = create_mesh('CleanedPlane', [(-1, -1, 0), (1, -1, 0), (1, 1, 0), (-1, 1, 0)],
                            [[0, 1, 2, 3]])

    def assert_visibility(mesh, enabled):
        assert_topology(mesh, reference.data if enabled else source.data)
        if not enabled:
            for domain in ('EDGE', 'FACE', 'CORNER'):
                attribute = mesh.attributes[domain + 'Label']
                assert attribute.domain == domain and attribute.data_type == 'INT'
                assert [item.value for item in attribute.data] == list(range(1, len(attribute.data) + 1))

    # A test-only render engine inspects Blender's actual render dependency graph
    # without a GPU, a physical display, or image-based geometry comparisons.
    rendered = []

    class ValidationEngine(bpy.types.RenderEngine):
        bl_idname = 'MIRROR_CLEANUP_VALIDATION'
        bl_label = 'Mirror cleanup regression validation'

        def render(self, depsgraph):
            assert depsgraph.mode == 'RENDER'
            rendered.append(bpy.data.meshes.new_from_object(source.evaluated_get(depsgraph),
                            preserve_all_data_layers=True, depsgraph=depsgraph))
            result = self.begin_result(0, 0, 1, 1)
            self.end_result(result)

    bpy.utils.register_class(ValidationEngine)
    scene = bpy.context.scene
    previous_engine, previous_camera = scene.render.engine, scene.camera
    camera = bpy.data.objects.new('ValidationCamera', bpy.data.cameras.new('ValidationCamera'))
    scene.collection.objects.link(camera)
    scene.camera = camera
    scene.render.engine = ValidationEngine.bl_idname
    try:
        for viewport, render in ((False, False), (True, False), (False, True), (True, True)):
            mirror.show_viewport, mirror.show_render = viewport, render
            assert_visibility(evaluated(source), viewport)
            bpy.ops.render.render()
            assert len(rendered) == 1, 'The render dependency graph was not evaluated'
            result = rendered.pop()
            assert_visibility(result, render)
            bpy.data.meshes.remove(result)
        assert all(curve.is_valid for curve in source.animation_data.drivers)
    finally:
        scene.render.engine, scene.camera = previous_engine, previous_camera
        bpy.data.objects.remove(camera, do_unlink=True)
        bpy.utils.unregister_class(ValidationEngine)
        bpy.data.objects.remove(source, do_unlink=True)
        bpy.data.objects.remove(reference, do_unlink=True)
    print('PASS: Mirror viewport and render toggles independently bypass cleanup in the correct dependency graph')


def main():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    test_original()
    test_axes()
    test_boundaries()
    test_editing_and_materials()
    test_small_creases()
    test_point_attributes()
    test_material_slots()
    test_retained_interior_geometry()
    test_empty_and_wire_meshes()
    test_wire_topology()
    test_wire_features()
    test_mirrored_wires()
    test_attribute_warnings()
    test_wire_outline()
    test_warped_faces()
    test_setup_lifecycle()
    test_attribute_name_validation()
    test_mirror_visibility()


if __name__ == '__main__':
    main()
