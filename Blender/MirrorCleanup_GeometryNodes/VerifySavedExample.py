"""Run on the prepared .blend in a fresh Blender process with autoexec disabled."""
import bpy

source = bpy.data.objects['AttemptedInput']
reference = bpy.data.objects['DesiredOutput']
assert [modifier.type for modifier in source.modifiers] == ['MIRROR', 'NODES', 'SUBSURF']
assert len([obj for obj in bpy.data.objects if obj.type == 'MESH']) == 2
assert len(source.data.vertices) == 5
assert not bpy.context.preferences.filepaths.use_scripts_auto_execute

def points(obj):
    obj.update_tag()
    bpy.context.view_layer.update()
    return [vertex.co.copy() for vertex in obj.evaluated_get(bpy.context.evaluated_depsgraph_get()).data.vertices]

actual, expected = points(source), points(reference)
assert len(actual) == len(expected) == 74
assert max(min((point - other).length for other in expected) for point in actual) < 0.00002
source.vertex_groups['RemoveCenter'].remove([4])
assert len(points(source)) == 82
source.vertex_groups['RemoveCenter'].add([4], 1, 'REPLACE')
assert len(points(source)) == 74
source.modifiers['Subdivision'].show_viewport = False
source.modifiers['Mirror'].use_axis = (False, False, False)
assert len(points(source)) == 5
source.modifiers['Mirror'].use_axis = (True, False, False)
assert len(points(source)) == 6
print('PASS: fresh process, saved graph, scripts autoexec disabled, matching subdivision, live group and axis changes')
