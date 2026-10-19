"""Rebuild the example using only files beside this script and stock Blender."""
import json
from pathlib import Path
import sys

import bpy
from mathutils import Quaternion

sys.dont_write_bytecode = True
DIRECTORY = Path(__file__).resolve().parent
sys.path.insert(0, str(DIRECTORY))
import GeometryNodesMirrorCleanup as cleanup

bpy.ops.wm.read_factory_settings(use_empty=True)
fixture = json.loads((DIRECTORY / 'ExampleMeshes.json').read_text())
for name, item in fixture.items():
    mesh = bpy.data.meshes.new(name + 'Mesh')
    mesh.from_pydata(item['Vertices'], item['Edges'], item['Faces'])
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = item['Location']
    if name == 'AttemptedInput':
        mirror = obj.modifiers.new('Mirror', 'MIRROR')
        mirror.use_clip = True
        mirror.show_in_editmode = True
        cleanup.setup(obj)
        obj.vertex_groups[cleanup.MARK].add([4], 1.0, 'REPLACE')
    subdivision = obj.modifiers.new('Subdivision', 'SUBSURF')
    subdivision.levels = 2
    subdivision.render_levels = 2
    subdivision.show_in_editmode = True

source = bpy.data.objects['AttemptedInput']
source.select_set(True)
bpy.context.view_layer.objects.active = source
source.show_wire = True
source.show_all_edges = True
source['Workflow'] = 'Edit this object. Mirror -> Selective Mirror Cleanup -> Subdivision.'
source['CenterControl'] = 'RemoveCenter vertex group: weight 1 requests removal; remove membership to keep.'
bpy.data.objects['DesiredOutput']['Workflow'] = 'Independent reference only. The input does not depend on this object.'
for area in bpy.context.screen.areas:
    if area.type == 'PROPERTIES':
        area.spaces.active.context = 'MODIFIER'
    elif area.type == 'VIEW_3D':
        space = area.spaces.active
        space.region_3d.view_distance = 9
        space.region_3d.view_location = (0, 0, 0)
        space.region_3d.view_rotation = Quaternion((0.882, 0.45, 0.069, 0.137)).normalized()
        space.region_3d.view_perspective = 'ORTHO'
        space.shading.type = 'SOLID'
        space.overlay.show_floor = False

instructions = bpy.data.texts.new('ReadMe')
instructions.write((DIRECTORY / 'ReadMe.md').read_text(encoding='utf-8'))
script = bpy.data.texts.new('GeometryNodesMirrorCleanup.py')
script.write((DIRECTORY / 'GeometryNodesMirrorCleanup.py').read_text(encoding='utf-8'))
script.use_module = False
bpy.context.scene['MirrorCleanupExampleVersion'] = cleanup.VERSION
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(DIRECTORY / 'MirrorWorkflowGeometryNodes.blend'))
print('Saved self-contained example:', DIRECTORY / 'MirrorWorkflowGeometryNodes.blend')
