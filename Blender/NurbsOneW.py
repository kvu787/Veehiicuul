"""Blender 4.5.x: set selected NURBS control-point W coordinates to 1.

Run from Blender's Text Editor (Alt+P).
Object Mode: all NURBS points in selected Curve/Surface objects.
Edit Mode: only selected NURBS points, across all objects being edited.
Bezier and Poly splines, XYZ coordinates, radius and tilt are unchanged.
"""

import bpy


def set_nurbs_w_to_one(context):
    editing = context.mode in {'EDIT_CURVE', 'EDIT_SURFACE'}
    if context.mode != 'OBJECT' and not editing:
        raise RuntimeError('Use Object Mode or Curve/Surface Edit Mode.')

    objects = list(context.objects_in_mode if editing else context.selected_objects)
    objects = [obj for obj in objects if obj.type in {'CURVE', 'SURFACE'}]
    changed = 0
    seen = set()

    # Curve Edit Mode has a separate edit buffer. Flush it before writing to
    # Curve.splines, then re-enter Edit Mode to load the updated coordinates.
    if editing:
        bpy.ops.object.mode_set(mode='OBJECT')
    try:
        for obj in objects:
            curve = obj.data
            if curve.as_pointer() in seen or not curve.is_editable:
                continue
            seen.add(curve.as_pointer())
            for spline in curve.splines:
                if spline.type != 'NURBS':
                    continue
                for point in spline.points:
                    if editing and not point.select:
                        continue
                    if point.co[3] != 1.0:
                        point.co[3] = 1.0
                        changed += 1
            curve.update_tag()
    finally:
        if editing:
            bpy.ops.object.mode_set(mode='EDIT')
    return changed


class CURVE_OT_set_selected_nurbs_w_to_one(bpy.types.Operator):
    bl_idname = 'curve.set_selected_nurbs_w_to_one'
    bl_label = 'Set Selected NURBS W to 1'
    bl_description = 'Set NURBS control-point W to exactly 1 in the selection'
    bl_options = {'REGISTER', 'UNDO'}

    @classmethod
    def poll(cls, context):
        return context.mode in {'OBJECT', 'EDIT_CURVE', 'EDIT_SURFACE'}

    def execute(self, context):
        count = set_nurbs_w_to_one(context)
        self.report({'INFO'}, f'Set W to 1 on {count} NURBS control point(s)')
        return {'FINISHED'}


def register():
    # Allow running this text repeatedly during the same Blender session.
    old_class = getattr(bpy.types, 'CURVE_OT_set_selected_nurbs_w_to_one', None)
    if old_class is not None:
        bpy.utils.unregister_class(old_class)
    bpy.utils.register_class(CURVE_OT_set_selected_nurbs_w_to_one)


if __name__ == '__main__':
    register()
    bpy.ops.curve.set_selected_nurbs_w_to_one()
