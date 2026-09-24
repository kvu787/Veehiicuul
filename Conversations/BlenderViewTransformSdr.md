# Blender view transform in SDR

## User

in blender, i have to change view transform from AgX to Standard or else the colors will look "off" on my PA278QGV with SDR. is that the right thing to do in blender?

## Assistant

I’ll check Blender’s color-management documentation to explain when Standard is appropriate and how your SDR display fits in.

## Assistant

**Standard can be the right choice for the appearance you want, but SDR does not require it.** AgX is designed to fit a scene’s brightness range onto an SDR display. Your PA278QGV being in SDR mode is therefore not, by itself, a reason to switch. [Blender’s AgX documentation](https://developer.blender.org/docs/release_notes/4.0/color_management/)

The difference you see is expected:

- **AgX** compresses bright values and reduces saturation in highly exposed colors. That helps preserve highlights in realistic lighting, but can make vivid material colors appear paler.
- **Standard** preserves a more direct appearance for colors within the display’s range, but bright values can clip, losing highlight detail. It is useful for flat graphics, unlit artwork, and deliberately saturated styles. [Blender color-management manual](https://docs.blender.org/manual/en/latest/render/color_management.html?highlight=filmic)

**If by “off” you mean “the colors look less saturated than the colors I picked,” choosing Standard is reasonable.** A lit material still won’t necessarily match its color-picker swatch: lighting, shadows, and reflections affect the result.

For a conventional sRGB SDR workflow, start with **Display: sRGB, Exposure: 0, Gamma: 1**, and choose the view transform for your intended rendering style. Monitor calibration and display color-space settings are separate from that choice.

Are you trying to match Blender’s color-picker swatches, an imported image, or how the model looks in Godot?
