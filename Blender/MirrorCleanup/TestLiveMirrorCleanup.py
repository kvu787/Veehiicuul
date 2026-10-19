"""Run with Blender --background --factory-startup --python this file."""

import importlib.util
from pathlib import Path
import sys
import tempfile

import bmesh
import bpy
from mathutils.bvhtree import BVHTree


DIRECTORY = Path(__file__).resolve().parent
sys.dont_write_bytecode = True
specification = importlib.util.spec_from_file_location('LiveMirrorCleanup', DIRECTORY / 'LiveMirrorCleanup.py')
cleanup = importlib.util.module_from_spec(specification)
specification.loader.exec_module(cleanup)


def snapshot(mesh):
    positions = [tuple(round(value, 6) for value in vertex.co) for vertex in mesh.vertices]

    def cycle(indices):
        coordinates = [positions[index] for index in indices]
        return min(tuple(coordinates[offset:] + coordinates[:offset])
                   for offset in range(len(coordinates)))

    return (sorted(positions),
            sorted(tuple(sorted(positions[index] for index in edge.vertices)) for edge in mesh.edges),
            sorted(cycle(face.vertices) for face in mesh.polygons))


def assert_symmetric(mesh):
    positions = {tuple(round(value, 6) for value in vertex.co) for vertex in mesh.vertices}
    for x, y, z in positions:
        assert (-x, y, z) in positions, (x, y, z)


def evaluated_geometry(obj):
    graph = bpy.context.evaluated_depsgraph_get()
    mesh = obj.evaluated_get(graph).data
    return ([vertex.co.copy() for vertex in mesh.vertices],
            [tuple(face.vertices) for face in mesh.polygons])


bpy.ops.wm.open_mainfile(filepath=str(DIRECTORY.parent / 'MirrorWorkflow.blend'))
source = bpy.data.objects['AttemptedInput']
reference = bpy.data.objects['DesiredOutput']
original = snapshot(source.data)
cleanup.register()
output = cleanup.create_output(source)
group = source.vertex_groups[cleanup.GROUP_NAME]
assert (len(output.data.vertices), len(output.data.edges), len(output.data.polygons)) == (7, 10, 5)
group.add([4], 1.0, 'REPLACE')
cleanup.refresh_output(output)
assert snapshot(output.data) == snapshot(reference.data), 'Target topology or face winding differs.'
assert snapshot(source.data) == original, 'The source mesh was changed.'
print('PASS: exact DesiredOutput geometry, edges, faces and winding; input unchanged')

# Subdivision must consume the cleaned control mesh and remain an editable modifier.
subdivision = output.modifiers.new('Subdivision', 'SUBSURF')
subdivision.levels = 2
subdivision.render_levels = 2
target_points, target_faces = evaluated_geometry(reference)
target_surface = BVHTree.FromPolygons(target_points, target_faces)


def assert_subdivision_match():
    points, unused_faces = evaluated_geometry(output)
    assert len(points) == len(target_points) == 74
    assert max(min((point - target).length for target in target_points)
               for point in points) < 0.000001


assert_subdivision_match()
group.remove([4])
cleanup.refresh_output(output)
points, unused_faces = evaluated_geometry(output)
assert len(output.data.vertices) == 7 and len(points) == 82
assert max(target_surface.find_nearest(point)[3] for point in points) > 0.001
group.add([4], 1.0, 'REPLACE')
group.add([3], 1.0, 'REPLACE')
cleanup.refresh_output(output)
points, unused_faces = evaluated_geometry(output)
assert len(output.data.vertices) == 5 and len(points) == 66
assert max(target_surface.find_nearest(point)[3] for point in points) > 0.001
group.remove([3])
cleanup.refresh_output(output)
assert_subdivision_match()
subdivision.levels = 3
points, unused_faces = evaluated_geometry(output)
assert len(points) > 74
assert len(output.data.vertices) == 6 and snapshot(source.data) == original
subdivision.levels = 2
assert_subdivision_match()
print('PASS: live unapplied Subdivision receives cleaned topology; both center choices change the surface')

group.add([3], 1.0, 'REPLACE')
cleanup.refresh_output(output)
assert len(output.data.vertices) == 5
group.remove([4])
cleanup.refresh_output(output)
assert len(output.data.vertices) == 6
positions = {tuple(round(value, 6) for value in vertex.co) for vertex in output.data.vertices}
assert (0.0, 1.0, 0.0) in positions
assert (0.0, -0.05, 0.25) not in positions
group.remove([3])
group.add([4], 1.0, 'REPLACE')
print('PASS: each crossing edge has an independent keep/remove choice')

bpy.ops.object.select_all(action='DESELECT')
source.select_set(True)
bpy.context.view_layer.objects.active = source
bpy.ops.object.mode_set(mode='EDIT')
bm = bmesh.from_edit_mesh(source.data)
bm.verts.ensure_lookup_table()
for vertex in bm.verts:
    vertex.select_set(False)
bm.verts[4].select_set(True)
assert bpy.ops.mesh.mirror_cleanup_mark(remove=False) == {'FINISHED'}
cleanup.refresh_pending()
assert len(output.data.vertices) == 7
assert bpy.ops.mesh.mirror_cleanup_mark(remove=True) == {'FINISHED'}
cleanup.refresh_pending()
assert snapshot(output.data) == snapshot(reference.data)
assert not cleanup._pending, 'Updating the output queued a feedback rebuild.'
print('PASS: sidebar marking operators and timer updates work in Edit Mode')

# Add geometry to the input without leaving Edit Mode. No output element indices are stored.
bm = bmesh.from_edit_mesh(source.data)
new_vertices = [bm.verts.new(coordinates) for coordinates in
                [(-1, 3, 0), (-1.5, 3, 0), (-1, 3.5, 0)]]
bm.faces.new(new_vertices)
bmesh.update_edit_mesh(source.data)
bpy.context.view_layer.update()
assert output.name in cleanup._pending, 'Dependency updates did not queue the changed input.'
cleanup.refresh_pending()
assert len(output.data.vertices) == 12
assert len(output.data.polygons) == 7
assert_symmetric(output.data)
print('PASS: new Edit Mode geometry automatically reaches both sides of the output')

# Change a mirrored vertex, and ensure non-coplanar seam faces keep their crease.
bm = bmesh.from_edit_mesh(source.data)
bm.verts.ensure_lookup_table()
bm.verts[0].co.z += 0.2
bmesh.update_edit_mesh(source.data)
bpy.context.view_layer.update()
cleanup.refresh_pending()
assert_symmetric(output.data)
assert len(output.data.polygons) > 7, 'A non-coplanar seam crease was dissolved.'
print('PASS: movements update symmetrically and non-coplanar seam creases survive')

bpy.ops.object.mode_set(mode='OBJECT')
source.location.z += 2
bpy.context.view_layer.update()
cleanup.refresh_pending()
assert output.matrix_world == source.matrix_world
print('PASS: object transforms follow the input')

source.modifiers[0].use_mirror_merge = False
bpy.context.view_layer.update()
cleanup.refresh_pending()
assert 'Enable Merge' in output['MirrorCleanupError']
source.modifiers[0].use_mirror_merge = True
bpy.context.view_layer.update()
cleanup.refresh_pending()
assert output['MirrorCleanupError'] == ''
print('PASS: invalid settings are reported and recover after correction')

bpy.ops.object.select_all(action='DESELECT')
output.hide_select = False
output.select_set(True)
bpy.context.view_layer.objects.active = output
bpy.ops.object.mode_set(mode='EDIT')
cleanup._pending.add(output.name)
cleanup.refresh_pending()
assert 'Object Mode' in output['MirrorCleanupError']
bpy.ops.object.mode_set(mode='OBJECT')
bpy.context.view_layer.update()
cleanup.refresh_pending()
assert output['MirrorCleanupError'] == ''
print('PASS: generated output cannot be overwritten while it is in Edit Mode')

axis_specification = importlib.util.spec_from_file_location('TestMirrorAxes', DIRECTORY / 'TestMirrorAxes.py')
axis_tests = importlib.util.module_from_spec(axis_specification)
axis_specification.loader.exec_module(axis_tests)
axis_tests.test_axes(cleanup)

expected = snapshot(output.data)
output_name = output.name
with tempfile.TemporaryDirectory(prefix='MirrorCleanup') as temporary:
    bpy.context.preferences.filepaths.save_version = 0
    file_path = str(Path(temporary) / 'Example.blend')
    bpy.ops.wm.save_as_mainfile(filepath=file_path)
    bpy.ops.wm.open_mainfile(filepath=file_path)
    output = bpy.data.objects[output_name]
    assert output.name in cleanup._pending
    cleanup.refresh_pending()
    assert snapshot(output.data) == expected
    assert output[cleanup.SOURCE_PROPERTY].name == 'AttemptedInput'
print('PASS: saved input/output relationships reload and rebuild correctly')

cleanup.unregister()
assert cleanup.on_update not in bpy.app.handlers.depsgraph_update_post
assert not bpy.app.timers.is_registered(cleanup.refresh_pending)
print('PASS: add-on unregisters cleanly')
