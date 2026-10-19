"""Create MirrorWorkflowLive.blend without changing MirrorWorkflow.blend."""

import importlib.util
from pathlib import Path
import sys

import bpy


directory = Path(__file__).resolve().parent
sys.dont_write_bytecode = True
specification = importlib.util.spec_from_file_location('LiveMirrorCleanup', directory / 'LiveMirrorCleanup.py')
cleanup = importlib.util.module_from_spec(specification)
specification.loader.exec_module(cleanup)
bpy.ops.wm.open_mainfile(filepath=str(directory.parent / 'MirrorWorkflow.blend'))
source = bpy.data.objects['AttemptedInput']
output = cleanup.create_output(source)
source.vertex_groups[cleanup.GROUP_NAME].add([4], 1.0, 'REPLACE')
cleanup.refresh_output(output)
subdivision = output.modifiers.new('Subdivision', 'SUBSURF')
subdivision.levels = 2
subdivision.render_levels = 2
bpy.ops.object.select_all(action='DESELECT')
source.select_set(True)
bpy.context.view_layer.objects.active = source
script = bpy.data.texts.get('LiveMirrorCleanup.py') or bpy.data.texts.new('LiveMirrorCleanup.py')
script.clear()
script.write((directory / 'LiveMirrorCleanup.py').read_text(encoding='utf-8'))
instructions = bpy.data.texts.get('MirrorCleanupInstructions') or bpy.data.texts.new('MirrorCleanupInstructions')
instructions.clear()
instructions.write('Open LiveMirrorCleanup.py in a Blender Text Editor and press Alt+P.\n'
                   'Then open the 3D Viewport sidebar (N) and select Mirror Cleanup.\n'
                   'Edit AttemptedInput. The generated AttemptedInputOutput updates automatically.\n'
                   'Select center vertices and choose Remove Center or Keep Center.\n'
                   'The rear center is marked RemoveCenter; the raised center is kept.\n'
                   'Subdivision is on the generated output. Disable it to inspect the base topology.\n'
                   'The original MirrorWorkflow.blend was not changed.\n')
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(directory.parent / 'MirrorWorkflowLive.blend'))
print('Created', directory.parent / 'MirrorWorkflowLive.blend')
