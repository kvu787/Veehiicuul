# NURBS Operator Explanation

## User

```
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
c:\Users\k\Repository\Veehiicuul\Blender\NurbsOneW.py:48:1-69:67
```

explain what this stuff does

## Assistant

I’ll read that section and the surrounding code to explain how it fits together.

## Assistant

That section wraps the actual NURBS-editing function in a **Blender command**, with an availability check, a status message, and undo support.

- **Lines 48–52 — Define the command.** The class inherits from `bpy.types.Operator`, Blender’s command base class. `bl_idname` gives it its Python call name: `bpy.ops.curve.set_selected_nurbs_w_to_one()`. `bl_label` is its display name; `bl_description` is its tooltip. `'REGISTER'` records the operation in Blender’s operator history; `'UNDO'` requests an undo step when it completes. ([Blender documentation](https://docs.blender.org/api/4.3/bpy.types.Operator.html))

- **Lines 54–56 — Decide when it can run.** Blender calls `poll()` to check availability. It returns `True` only in Object Mode, Curve Edit Mode, or Surface Edit Mode. `@classmethod` means this check belongs to the class and receives `cls`, rather than an individual command instance. The check only tests the mode; it does not require any NURBS points to be selected.

- **Lines 58–61 — Run it.** `execute()` calls `set_nurbs_w_to_one(context)`, the function above this section that actually changes the points. It gets back the number changed, displays a message such as “Set W to 1 on 12 NURBS control point(s)”, and returns `{'FINISHED'}` to tell Blender it completed successfully.

- **Lines 64–69 — Install the command into Blender.** `getattr(..., None)` looks for a previously registered version of the class, returning `None` if none exists. If one exists, it is unregistered first. The current class is then registered. This lets you run the script repeatedly in the same Blender session without duplicate-registration errors.

**Registering the command does not execute it.** The lines immediately below your selection do both: line 73 registers it, then line 74 runs it.

## User

can C:\Users\k\Repository\Veehiicuul\Blender\NurbsOneW.py be simplified to run as a simple script without that stuff

## Assistant

I’ll remove the operator and registration code so running the script directly calls the NURBS-editing function.

## Assistant

Done. [NurbsOneW.py](C:/Users/k/Repository/Veehiicuul/Blender/NurbsOneW.py) now runs directly with **Alt+P in Blender’s Text Editor**, without the operator class or registration code.

The point-editing logic is unchanged. It prints the changed-point count to the console and no longer requests its own operator undo step.

Python syntax checked; not run inside Blender.

## User

was there any functionality lost after your change?

## Assistant

I’ll compare the change to distinguish the editing behavior from the Blender command features that were removed.

## Assistant

Yes. The **NURBS editing behavior is unchanged**, but these Blender integration features were removed:

- **Explicit undo support:** the script no longer requests an undo step for its changes. You should not assume Ctrl+Z will undo the script reliably; I haven’t tested that in Blender.
- **On-screen completion message:** the changed-point count now goes to the console instead.
- **Registered command:** the script no longer registers `bpy.ops.curve.set_selected_nurbs_w_to_one()`, or participates in operator history through `REGISTER`.

The mode check remains inside the editing function, and you can still run the script repeatedly.

**Undo support is the main practical loss.** I should have made that tradeoff clearer before removing the operator.

## User

add back undo support

## Assistant

I’ll restore the Blender operator wrapper needed to give the script its own undo step. You’ll still run it with Alt+P.

## Assistant

The undo wrapper is restored. I also found Blender installed locally, so I’m checking that undo restores the original weights in both Object Mode and Edit Mode.

## Assistant

Testing showed that an operator wrapper alone did not create an undo step when run from the Text Editor. I kept the script simple and added explicit undo snapshots instead. In Blender 4.5.14, undo now restores the original weights in both Object Mode and Edit Mode.

## Assistant

Added undo support to [NurbsOneW.py](C:/Users/k/Repository/Veehiicuul/Blender/NurbsOneW.py), keeping it a simple script without an operator wrapper.

Run with **Alt+P**, then **Ctrl+Z in the 3D Viewport** to undo. Verified in Blender 4.5.14 in both Object Mode and Edit Mode.

## User

when creating a simple blender script like this, explain what is needed (if anything) to get proper undo support. cite trustworthy online or documentation sources and actual blender source code at C:\Users\k\Repository\External\blender

## Assistant

I’ll trace how Blender runs Text Editor scripts and records undo, then compare that with the documentation and the approach now used in your script.

## Assistant

The source reveals an important correction: Blender’s Text Editor “Run Script” command already has undo enabled. My earlier test called that command from Python with undo disabled by default, so it did not accurately reproduce Alt+P. I’m rerunning the checks with undo enabled on the outer call.

## Assistant

With the corrected test, the script works without either manual undo call: one undo restores the weights, and redo reapplies them, in both Object Mode and Edit Mode. I’m checking the current version once more to see whether the extra snapshots introduce redundant undo steps.

## Assistant

**For this script, run using Alt+P in Blender’s Text Editor, no additional undo code is needed with normal undo settings.** Blender’s “Run Script” command already supplies it. My earlier explanation—and the test I used to justify adding manual snapshots—was wrong.

The distinction is **how the script is launched**:

1. **A plain script run through the Text Editor**

   Blender defines `TEXT_OT_run_script` with:

   ```cpp
   ot->flag = OPTYPE_REGISTER | OPTYPE_UNDO;
   ```

   Its execution callback runs your Python text. Afterward, Blender’s operator machinery records an undo step for the completed outer operation. Therefore, direct data assignments made during the script can be included without defining your own operator. See [Run Script’s implementation](C:/Users/k/Repository/External/blender/source/blender/editors/space_text/text_ops.cc:895) and [the automatic undo push](C:/Users/k/Repository/External/blender/source/blender/windowmanager/intern/wm_event_system.cc:1300).

   For your intended workflow, the script’s ending only needs:

   ```python
   if __name__ == '__main__':
       count = set_nurbs_w_to_one(bpy.context)
       print(f'Set W to 1 on {count} NURBS control point(s)')
   ```

2. **A custom command launched from Blender’s interface**

   If you choose to create an operator, give it `bl_options = {'REGISTER', 'UNDO'}` and return `{'FINISHED'}` after making changes. The `UNDO` flag enables the automatic undo step; `REGISTER` concerns operator registration/history. This is the standard pattern in [Blender’s operator documentation](https://docs.blender.org/api/5.2/bpy.types.Operator.html#modifying-blender-data-undo).

   You do not need this extra wrapper merely to make an Alt+P script undoable: “Run Script” is already that outer operator.

3. **An operator called directly from Python**

   Python operator calls default to suppressing their automatic undo recording. To enable it for a top-level call, use:

   ```python
   bpy.ops.some_category.some_operator('EXEC_DEFAULT', True)
   ```

   This applies to operators that support undo. It does not independently record arbitrary Python assignments, and nested operators normally leave undo recording to the outer operator. See [the documented undo argument](https://docs.blender.org/api/5.3/bpy.ops.html#undo), [its default of false](C:/Users/k/Repository/External/blender/source/blender/python/intern/bpy_operator_function.cc:115), and [Blender’s suppression logic](C:/Users/k/Repository/External/blender/source/blender/windowmanager/intern/wm_event_system.cc:1999).

4. **Manual `bpy.ops.ed.undo_push()` calls**

   These explicitly save undo states. They are an advanced tool for situations where automatic recording is insufficient—not a standard requirement for every script. Blender’s documentation specifically mentions mode switching as a case that needs care, and labels `undo_push` for internal use. See [the operator guidance](https://docs.blender.org/api/5.2/bpy.types.Operator.html#modifying-blender-data-undo), [the API description](https://docs.blender.org/api/main/bpy.ops.ed.html#bpy.ops.ed.undo_push), and [its implementation](C:/Users/k/Repository/External/blender/source/blender/editors/undo/ed_undo.cc:727).

**What the corrected tests showed:** In Blender 4.5.14, your script without manual pushes passed undo and redo checks in both Object Mode and Curve Edit Mode, including its temporary mode changes. With the current manual pushes, the first undo left the weights unchanged; the second restored them. **Those two calls are redundant for the intended Alt+P workflow and should be removed.**

My earlier test used `bpy.ops.text.run_script()`, which suppressed the outer command’s undo recording. The corrected test used `bpy.ops.text.run_script('EXEC_DEFAULT', True)` to preserve that recording. That testing mistake caused my incorrect conclusion.

Keep **Global Undo enabled** and **Undo Steps above zero**. These settings govern whether Blender can retain the relevant states; see the [Blender manual](https://docs.blender.org/manual/en/4.4/editors/preferences/system.html#memory-limits) and [the Global Undo check in source](C:/Users/k/Repository/External/blender/source/blender/editors/undo/memfile_undo.cc:58).

The local source links refer to checkout `95bb6e2d328`. I also checked the `v4.5.14` source tag: its “Run Script” command has the same undo flag.

## User

okay, so is the current C:\Users\k\Repository\Veehiicuul\Blender\NurbsOneW.py sufficient? i don't want to add any unnecessary complexity or plumbing

## Assistant

The current script has two unnecessary undo calls. I’ll remove them so Alt+P uses Blender’s built-in undo support, keeping the script simple.

## Assistant

**Yes—the current file is sufficient.** I checked it: the unnecessary undo calls are already gone, contrary to my initial update. No further changes are needed.

Run it with **Alt+P in Blender’s Text Editor**, then **Ctrl+Z in the 3D Viewport** to undo, with Global Undo enabled. No custom operator or registration plumbing is needed. This version’s editing logic passed the earlier undo/redo tests in Blender 4.5.14.
