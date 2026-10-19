"""Render the saved example without saving any camera or display changes."""
from pathlib import Path

import bpy
from mathutils import Vector

directory = Path(__file__).resolve().parent
scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
scene.render.resolution_x = 1200
scene.render.resolution_y = 650
scene.render.resolution_percentage = 100
scene.display.shading.light = 'STUDIO'
scene.display.shading.color_type = 'SINGLE'
scene.display.shading.single_color = (0.55, 0.68, 0.8)
scene.display.shading.show_shadows = True
scene.display.shading.show_cavity = True
scene.display.shading.cavity_type = 'BOTH'
scene.display.shading.show_object_outline = True
scene.display.shading.background_type = 'WORLD'
scene.world = scene.world or bpy.data.worlds.new('PreviewWorld')
scene.world.color = (0.065, 0.065, 0.065)
camera = bpy.data.objects.new('PreviewCamera', bpy.data.cameras.new('PreviewCamera'))
scene.collection.objects.link(camera)
camera.location = (0, -7, 6)
camera.rotation_euler = (Vector((0, 0, 0)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 8
scene.camera = camera
for x, label in ((-2, 'Geometry Nodes result'), (2, 'DesiredOutput reference')):
    text = bpy.data.curves.new(label, 'FONT')
    text.body = label
    text.align_x = 'CENTER'
    text.size = 0.19
    obj = bpy.data.objects.new(label, text)
    scene.collection.objects.link(obj)
    obj.location = (x, -1.9, -0.1)
    obj.rotation_euler = camera.rotation_euler
scene.render.image_settings.file_format = 'PNG'
scene.render.filepath = str(directory / 'Preview.png')
bpy.ops.render.render(write_still=True)
assert (directory / 'Preview.png').is_file()
print('Rendered the native modifier stack:', directory / 'Preview.png')
