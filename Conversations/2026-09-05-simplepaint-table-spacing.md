# SimplePaint table spacing

## User

rewrite this table in the simple paint spec md so that it has proper spacing when viewed in plaintext editor:
| Parameter | Abstract domain | Meaning |
|---|---|---|
| R, G, B | Each in (0, 1) | Base color in the existing user-facing sRGB space |
| Brightness | (0, 1) | Positions the base-color anchor on the tone curve |
| Shift | [0, 1) | Warps the facing lobe in the direction selected by Rotation |
| Rotation | [0, 360) degrees | Circular orientation of the shift; equivalent to RotationDegrees in Settings.ini |
| Dark Point | [0, 1) | Tone selected when the warped facing value is zero |
| Light Point | (0, 1] | Tone selected when the warped facing value is one |

## Assistant

I’ll locate the table and pad the columns so they line up in a plaintext editor.

## Assistant

Aligned the table columns in [SimplePaintInputSpecification.md](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/docs/SimplePaintInputSpecification.md). Committed the formatting change and conversation log separately.
