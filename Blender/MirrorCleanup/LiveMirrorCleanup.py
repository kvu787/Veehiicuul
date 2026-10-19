"""Blender 4.5: live, selective seam cleanup after a standard Mirror modifier.

Install this file as an add-on, or run it in Blender's Text Editor with Alt+P.
Use the 3D Viewport sidebar's Mirror Cleanup panel to create an output mesh.
"""

bl_info = {
    'name': 'Live Mirror Cleanup',
    'author': 'Veehiicuul',
    'version': (1, 0, 0),
    'blender': (4, 5, 0),
    'location': '3D Viewport > Sidebar > Mirror Cleanup',
    'description': 'Mirror a half mesh and selectively dissolve center vertices',
    'category': 'Mesh',
}

import math
import bmesh
import bpy
from bpy.app.handlers import persistent


SOURCE_PROPERTY = 'MirrorCleanupSource'
GROUP_NAME = 'RemoveCenter'
_pending = set()
_updating = False


def outputs():
    return [obj for obj in bpy.data.objects if obj.type == 'MESH'
            and isinstance(obj.get(SOURCE_PROPERTY), bpy.types.Object)]


def output_for(obj):
    if obj is None:
        return None
    if isinstance(obj.get(SOURCE_PROPERTY), bpy.types.Object):
        return obj
    return next((output for output in outputs()
                 if output.get(SOURCE_PROPERTY) == obj), None)


def mirror_for(source):
    active = [modifier for modifier in source.modifiers if modifier.show_viewport]
    if not active or active[-1].type != 'MIRROR':
        raise ValueError('The last enabled input modifier must be Mirror. Put Subdivision on the output.')
    modifier = active[-1]
    if sum(modifier.use_axis) != 1:
        raise ValueError('Use one mirror axis at a time.')
    if not modifier.use_mirror_merge:
        raise ValueError('Enable Merge on the Mirror modifier.')
    if not modifier.show_in_editmode:
        raise ValueError('Enable the Mirror modifier in Edit Mode.')
    return modifier


def clean_mesh(mesh, source, mirror, tolerance, angle_limit):
    """Mutate only a generated mesh; preserve all unmarked source vertices."""
    axis = next(index for index, enabled in enumerate(mirror.use_axis) if enabled)
    plane_matrix = (mirror.mirror_object.matrix_world.inverted() @ source.matrix_world
                    if mirror.mirror_object else None)
    group = source.vertex_groups.get(GROUP_NAME)
    bm = bmesh.new()
    try:
        bm.from_mesh(mesh)
        bm.normal_update()
        deform = bm.verts.layers.deform.active

        def on_plane(vertex):
            position = plane_matrix @ vertex.co if plane_matrix else vertex.co
            return abs(position[axis]) <= tolerance

        marked = {vertex for vertex in bm.verts
                  if group is not None and deform is not None
                  and vertex[deform].get(group.index, 0.0) > 0.5
                  and on_plane(vertex)}
        # A crease on the symmetry plane defines the shape and must survive.
        edges = [edge for edge in bm.edges
                 if edge.is_manifold and all(on_plane(vertex) for vertex in edge.verts)
                 and edge.calc_face_angle(math.pi) <= angle_limit]
        if edges:
            bmesh.ops.dissolve_edges(bm, edges=edges, use_verts=False, use_face_split=False)

        removable = []
        for vertex in marked:
            if not vertex.is_valid or len(vertex.link_edges) != 2:
                continue
            directions = [edge.other_vert(vertex).co - vertex.co for edge in vertex.link_edges]
            if min(direction.length for direction in directions) <= tolerance:
                continue
            # Only a straight edge can lose its midpoint without changing shape.
            if abs(math.pi - directions[0].angle(directions[1])) <= angle_limit:
                removable.append(vertex)
        if removable:
            bmesh.ops.dissolve_verts(bm, verts=removable, use_face_split=False,
                                    use_boundary_tear=False)
        bm.normal_update()
        bm.to_mesh(mesh)
        mesh.update()
    finally:
        bm.free()


def refresh_output(output, depsgraph=None):
    global _updating
    if _updating:
        return
    source = output.get(SOURCE_PROPERTY)
    if not isinstance(source, bpy.types.Object) or source.type != 'MESH':
        raise ValueError('The input mesh no longer exists.')
    if output.mode != 'OBJECT':
        raise ValueError('Return the output to Object Mode. Edit the input half mesh instead.')
    mirror = mirror_for(source)
    generated = None
    _updating = True
    try:
        if source.mode == 'EDIT':
            source.update_from_editmode()
        depsgraph = depsgraph or bpy.context.evaluated_depsgraph_get()
        depsgraph.update()
        generated = bpy.data.meshes.new_from_object(
            source.evaluated_get(depsgraph), preserve_all_data_layers=True, depsgraph=depsgraph)
        generated.name = output.name + 'Mesh'
        clean_mesh(generated, source, mirror,
                   output.get('SeamTolerance', 0.00001),
                   output.get('AngleTolerance', 0.0001))
        previous = output.data
        output.data = generated
        generated = None
        output.matrix_world = source.matrix_world.copy()
        output['MirrorCleanupError'] = ''
        if previous.users == 0:
            bpy.data.meshes.remove(previous)
        depsgraph.update()
    finally:
        if generated is not None and generated.users == 0:
            bpy.data.meshes.remove(generated)
        _updating = False


def create_output(source):
    mirror_for(source)
    if output_for(source):
        raise ValueError('This input already has a live output.')
    mesh = bpy.data.meshes.new(source.name + 'OutputMesh')
    output = bpy.data.objects.new(source.name + 'Output', mesh)
    (source.users_collection[0] if source.users_collection else bpy.context.collection).objects.link(output)
    output[SOURCE_PROPERTY] = source
    output['SeamTolerance'] = 0.00001
    output['AngleTolerance'] = 0.0001
    output['MirrorCleanupError'] = ''
    output.hide_select = True
    try:
        refresh_output(output)
    except Exception:
        bpy.data.objects.remove(output, do_unlink=True)
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)
        raise
    source.vertex_groups.get(GROUP_NAME) or source.vertex_groups.new(name=GROUP_NAME)
    source.display_type = 'WIRE'
    source.hide_render = True
    return output


@persistent
def on_update(scene, depsgraph):
    if _updating:
        return
    changed = {update.id.original for update in depsgraph.updates}
    for output in outputs():
        source = output[SOURCE_PROPERTY]
        dependencies = {source, source.data, output}
        for modifier in source.modifiers:
            if modifier.type == 'MIRROR' and modifier.mirror_object:
                dependencies.add(modifier.mirror_object)
        if dependencies.intersection(changed):
            _pending.add(output.name)


@persistent
def queue_all(*unused):
    _pending.clear()
    _pending.update(output.name for output in outputs())


def refresh_pending():
    if _updating or bpy.app.is_job_running('RENDER'):
        return 0.1
    pending = list(_pending)
    _pending.clear()
    for name in pending:
        output = bpy.data.objects.get(name)
        if output is None or not isinstance(output.get(SOURCE_PROPERTY), bpy.types.Object):
            continue
        try:
            refresh_output(output)
        except Exception as error:
            if output.get('MirrorCleanupError') != str(error):
                output['MirrorCleanupError'] = str(error)
                print('Live Mirror Cleanup:', error)
    return 0.1


class MIRRORCLEANUP_OT_create(bpy.types.Operator):
    bl_idname = 'mesh.mirror_cleanup_create'
    bl_label = 'Create Live Output'
    bl_options = {'REGISTER', 'UNDO'}

    def execute(self, context):
        source = context.active_object
        if source is None or source.type != 'MESH':
            self.report({'ERROR'}, 'Select your half mesh first.')
            return {'CANCELLED'}
        try:
            create_output(source)
        except Exception as error:
            self.report({'ERROR'}, str(error))
            return {'CANCELLED'}
        return {'FINISHED'}


class MIRRORCLEANUP_OT_mark(bpy.types.Operator):
    bl_idname = 'mesh.mirror_cleanup_mark'
    bl_label = 'Set Center Vertex Choice'
    bl_options = {'REGISTER', 'UNDO'}

    remove: bpy.props.BoolProperty(default=True)

    @classmethod
    def poll(cls, context):
        return context.mode == 'EDIT_MESH' and output_for(context.active_object) is not None

    def execute(self, context):
        source = context.active_object
        output = output_for(source)
        if source == output:
            self.report({'ERROR'}, 'Edit the input half mesh, not the generated output.')
            return {'CANCELLED'}
        group = source.vertex_groups.get(GROUP_NAME) or source.vertex_groups.new(name=GROUP_NAME)
        bm = bmesh.from_edit_mesh(source.data)
        deform = bm.verts.layers.deform.verify()
        for vertex in bm.verts:
            if vertex.select:
                if self.remove:
                    vertex[deform][group.index] = 1.0
                elif group.index in vertex[deform]:
                    del vertex[deform][group.index]
        bmesh.update_edit_mesh(source.data, loop_triangles=False, destructive=False)
        _pending.add(output.name)
        return {'FINISHED'}


class MIRRORCLEANUP_OT_refresh(bpy.types.Operator):
    bl_idname = 'mesh.mirror_cleanup_refresh'
    bl_label = 'Refresh Output'

    def execute(self, context):
        output = output_for(context.active_object)
        if output is None:
            return {'CANCELLED'}
        try:
            refresh_output(output)
        except Exception as error:
            self.report({'ERROR'}, str(error))
            return {'CANCELLED'}
        return {'FINISHED'}


class MIRRORCLEANUP_OT_select(bpy.types.Operator):
    bl_idname = 'object.mirror_cleanup_select'
    bl_label = 'Select Mirror Cleanup Object'
    target: bpy.props.EnumProperty(items=[('INPUT', 'Input', ''), ('OUTPUT', 'Output', '')])

    def execute(self, context):
        output = output_for(context.active_object)
        if output is None:
            return {'CANCELLED'}
        if context.mode != 'OBJECT':
            bpy.ops.object.mode_set(mode='OBJECT')
        bpy.ops.object.select_all(action='DESELECT')
        obj = output if self.target == 'OUTPUT' else output[SOURCE_PROPERTY]
        obj.hide_select = False
        obj.hide_set(False)
        obj.select_set(True)
        context.view_layer.objects.active = obj
        return {'FINISHED'}


class MIRRORCLEANUP_PT_panel(bpy.types.Panel):
    bl_label = 'Live Mirror Cleanup'
    bl_idname = 'MIRRORCLEANUP_PT_panel'
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'Mirror Cleanup'

    def draw(self, context):
        layout = self.layout
        output = output_for(context.active_object)
        if output is None:
            layout.label(text='Select a half mesh with a Mirror modifier.')
            layout.operator(MIRRORCLEANUP_OT_create.bl_idname)
            return
        layout.label(text='Input: ' + output[SOURCE_PROPERTY].name)
        layout.label(text='Output: ' + output.name)
        row = layout.row(align=True)
        row.operator(MIRRORCLEANUP_OT_select.bl_idname, text='Select Input').target = 'INPUT'
        row.operator(MIRRORCLEANUP_OT_select.bl_idname, text='Select Output').target = 'OUTPUT'
        layout.label(text='Select center vertices on the input in Edit Mode:')
        row = layout.row(align=True)
        row.operator(MIRRORCLEANUP_OT_mark.bl_idname, text='Remove Center').remove = True
        row.operator(MIRRORCLEANUP_OT_mark.bl_idname, text='Keep Center').remove = False
        layout.operator(MIRRORCLEANUP_OT_refresh.bl_idname)
        layout.label(text='Add Subdivision to the output object.')
        if output.get('MirrorCleanupError'):
            layout.label(text=output['MirrorCleanupError'], icon='ERROR')


CLASSES = (MIRRORCLEANUP_OT_create, MIRRORCLEANUP_OT_mark, MIRRORCLEANUP_OT_refresh,
           MIRRORCLEANUP_OT_select, MIRRORCLEANUP_PT_panel)
HANDLERS = ((bpy.app.handlers.depsgraph_update_post, on_update),
            (bpy.app.handlers.load_post, queue_all),
            (bpy.app.handlers.undo_post, queue_all),
            (bpy.app.handlers.redo_post, queue_all))


def register():
    for cls in CLASSES:
        bpy.utils.register_class(cls)
    for handlers, function in HANDLERS:
        handlers.append(function)
    bpy.app.timers.register(refresh_pending, first_interval=0.1, persistent=True)
    queue_all()


def unregister():
    if bpy.app.timers.is_registered(refresh_pending):
        bpy.app.timers.unregister(refresh_pending)
    for handlers, function in HANDLERS:
        if function in handlers:
            handlers.remove(function)
    for cls in reversed(CLASSES):
        bpy.utils.unregister_class(cls)
    _pending.clear()


if __name__ == '__main__':
    # Running the text again replaces this session's earlier registration.
    previous = bpy.app.driver_namespace.get('LiveMirrorCleanupUnregister')
    if previous:
        previous()
    register()
    bpy.app.driver_namespace['LiveMirrorCleanupUnregister'] = unregister
