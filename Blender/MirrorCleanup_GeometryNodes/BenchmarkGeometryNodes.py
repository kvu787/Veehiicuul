"""Small repeatable control-grid benchmark; wall times depend on the machine."""
from pathlib import Path
import sys
import time

import bpy

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
import GeometryNodesMirrorCleanup as cleanup

for count in (10, 30):
    vertices = [(-x, y, 0) for y in range(count + 1) for x in range(count + 1)]
    faces = []
    for y in range(count):
        for x in range(count):
            index = y * (count + 1) + x
            faces.append((index, index + 1, index + count + 2, index + count + 1))
    mesh = bpy.data.meshes.new('Grid')
    mesh.from_pydata(vertices, [], faces)
    source = bpy.data.objects.new('Grid', mesh)
    bpy.context.collection.objects.link(source)
    source.modifiers.new('Mirror', 'MIRROR')
    cleanup.setup(source)
    source.vertex_groups[cleanup.MARK].add(list(range(len(vertices))), 1, 'REPLACE')
    times = []
    for iteration in range(4):
        source.data.vertices[-1].co.z = iteration * 0.01
        source.data.update()
        start = time.perf_counter()
        bpy.context.view_layer.update()
        result = source.evaluated_get(bpy.context.evaluated_depsgraph_get()).data
        times.append(time.perf_counter() - start)
        assert len(result.polygons) == 2 * count * count - count
    print('Input faces:', len(faces), 'output faces:', len(result.polygons),
          'warm evaluation milliseconds:', [round(value * 1000, 1) for value in times[1:]])
    bpy.data.objects.remove(source, do_unlink=True)
