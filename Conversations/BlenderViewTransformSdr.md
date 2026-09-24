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

## User

i am just trying to get the 3d rendered colors of my models to look roughly like the base color of their Principled BSDF materials.  see "C:\Users\k\Repository\Veehiicuul\Blender\Tracks\Track009_MiniComb4.blend" for example

## Assistant

I’ll inspect the materials, lighting, and color-management settings in that Blender file to see what is changing their appearance.

## Assistant

The file already uses Standard, with exposure 0 and gamma 1. Its saved Material Preview uses the “forest” lighting environment, which can also shift material colors. I’m checking the connected material nodes to distinguish lighting effects from material settings.

## User

"C:\Users\k\Repository\Veehiicuul\Blender\Tracks\Track009_MiniComb4.blend" is an example that already looks "good enough" to me

## Assistant

Understood—I’ll treat that file as an example of the appearance you want, rather than a problem to fix.

## Assistant

**Then yes—keep Standard.** Your example already uses **sRGB, Standard, Look None, Exposure 0, Gamma 1**, and that is a reasonable setup for keeping your rendered models roughly faithful to their Principled BSDF base colors.

AgX deliberately changes contrast and saturation to accommodate a wider range of scene brightness. That can be useful, but its appearance isn’t a requirement for correct rendering. [Blender’s view-transform documentation](https://docs.blender.org/manual/en/latest/render/color_management.html?highlight=filmic)

Your reason for choosing Standard is **the color appearance you want**. The PA278QGV being in SDR mode doesn’t require either choice.
