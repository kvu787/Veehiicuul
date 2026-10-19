"""Run on the prepared .blend in a fresh Blender process with autoexec disabled."""
from pathlib import Path
import sys

import bpy

sys.dont_write_bytecode = True
DIRECTORY = Path(__file__).resolve().parent
sys.path.insert(0, str(DIRECTORY))
from TestGeometryNodes import assert_surface, assert_topology, cleanup, create_mesh, evaluated

source = bpy.data.objects['AttemptedInput']
reference = bpy.data.objects['DesiredOutput']
assert [modifier.type for modifier in source.modifiers] == ['MIRROR', 'NODES', 'SUBSURF']
assert len([obj for obj in bpy.data.objects if obj.type == 'MESH']) == 2
assert len(source.data.vertices) == 5
assert not bpy.context.preferences.filepaths.use_scripts_auto_execute
assert bpy.context.scene['MirrorCleanupExampleVersion'] == cleanup.VERSION
assert source.modifiers['Selective Mirror Cleanup'].node_group['MirrorCleanupVersion'] == cleanup.VERSION
assert len(source.animation_data.drivers) == 5
for text_name, file_name in (('ReadMe', 'ReadMe.md'), ('GeometryNodesMirrorCleanup.py', 'GeometryNodesMirrorCleanup.py')):
    assert bpy.data.texts[text_name].as_string() == (DIRECTORY / file_name).read_text(encoding='utf-8'), file_name + ' is stale in the saved example'
assert not bpy.data.texts['GeometryNodesMirrorCleanup.py'].use_module

def points(obj):
    obj.update_tag()
    bpy.context.view_layer.update()
    return [vertex.co.copy() for vertex in obj.evaluated_get(bpy.context.evaluated_depsgraph_get()).data.vertices]

actual, expected = points(source), points(reference)
assert len(actual) == len(expected) == 74
assert_surface(evaluated(source), evaluated(reference))
source.vertex_groups['RemoveCenter'].remove([4])
assert len(points(source)) == 82
source.vertex_groups['RemoveCenter'].add([4], 1, 'REPLACE')
assert len(points(source)) == 74
source.modifiers['Subdivision'].show_viewport = False
reference.modifiers['Subdivision'].show_viewport = False
assert_topology(evaluated(source), evaluated(reference))
source.modifiers['Mirror'].use_axis = (False, False, False)
assert len(points(source)) == 5
source.modifiers['Mirror'].use_axis = (True, False, False)
assert len(points(source)) == 6
source.modifiers['Mirror'].show_viewport = False
assert_topology(evaluated(source), source.data)
source.modifiers['Mirror'].show_render = False
source.modifiers['Mirror'].show_viewport = True
assert_topology(evaluated(source), evaluated(reference))
source.modifiers['Mirror'].show_render = True
assert all(curve.is_valid for curve in source.animation_data.drivers)
# Exercise the saved wire path without constructing any node groups. The
# example's graph works on a different mesh with no Subdivision modifier.
wire = create_mesh('SavedWireCheck', [(-1, 0, 0), (0, 0, 0)], [], [(0, 1)])
wire.modifiers.new('Mirror', 'MIRROR')
modifier = wire.modifiers.new('Cleanup', 'NODES')
modifier.node_group = source.modifiers['Selective Mirror Cleanup'].node_group
wire.vertex_groups.new(name=cleanup.MARK).add([1], 1, 'REPLACE')
expected_wire = create_mesh('ExpectedWire', [(-1, 0, 0), (1, 0, 0)], [], [(0, 1)])
assert_topology(evaluated(wire), expected_wire.data)
assert not list(modifier.node_warnings)
wire.vertex_groups[cleanup.MARK].remove([1])
assert len(evaluated(wire).vertices) == 3
print('PASS: saved wire graph removes marked center points without Subdivision, warnings, or reconstruction by Python')
print('PASS: fresh process, current embedded source/documentation, saved graph, scripts autoexec disabled, exact topology, matching subdivision, live group, axis, and visibility changes')
