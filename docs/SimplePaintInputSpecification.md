# SimplePaint input specification

Status: working specification for numerical analysis before implementation changes.
Date: 2026-09-05. Revision: strict margins on RGB as well as the previously constrained controls.
Implementation examined: 2700715; its shader and renderer source match 44459fa.

This contract uses the user-facing parameters of K12.gdshader. In this repository it applies independently to the six materials in Settings.ini. It does not change shader code, runtime validation, or active paint settings.

| Parameter | Constraint at general epsilon e | Current range, e = 0.01 |
|---|---|---|
| BaseColor R | e < R < 1-e | Strictly between 0.01 and 0.99 |
| BaseColor G | e < G < 1-e | Strictly between 0.01 and 0.99 |
| BaseColor B | e < B < 1-e | Strictly between 0.01 and 0.99 |
| Brightness | e <= Brightness <= 1-e | 0.01 to 0.99, inclusive |
| Shift | 0 <= Shift <= 1-e | 0 to 0.99, inclusive |
| Rotation | 0 <= Rotation <= 360 | 0 to 360 degrees, inclusive |
| DarkPoint | 0 <= DarkPoint <= 1-e | 0 to 0.99, inclusive |
| LightPoint | e <= LightPoint <= 1 | 0.01 to 1, inclusive |

All values must be finite. The strict RGB inequalities are intentional: 0.01 and 0.99 are not allowed RGB values. No DarkPoint <= LightPoint ordering is imposed. Equal tone endpoints are allowed in their overlapping ranges, and reversed ranges remain allowed. The combined intervals are nonempty only for 0 < e < 0.5.

RGB means the existing user-facing sRGB values, not the linear values used in the color formula. The renderer converts BaseColor using SrgbToLinear before computing coefficients. At e=0.01, each mathematical linear channel b therefore satisfies approximately:

~~~text
0.000773993808 < b < 0.977401933806
~~~

Rotation maps to RotationDegrees in each material's INI section. Floating-point parsing and arithmetic may round a written value; strict decimal inequalities alone do not promise a numerical margin beyond these bounds.

**Fixed K12 behavior and separate constants**

The local reference C:/Users/k/Repository/Godot/SimplePaintShaders/Godot/ShaderTest/Shaders/K12.gdshader fixes its normal-facing threshold at 0.01. The DirectX port exposes it globally as FacingCutoff; hold it at 0.01 for this contract. Below that normalized camera-facing component, facing becomes zero and the tone becomes DarkPoint. The pixel is not discarded.

- Input margin e=0.01: the tentative constraints above.
- Facing threshold q=0.01: fixed K12 behavior for this analysis.
- Implementation floor delta=0.00001: the current CPU protections and shader denominator floors.

These have independent meanings. Changing the input specification does not change delta or q.

**Consequences and open accuracy criterion**

The RGB margins exclude exact black/white and the original color formula's undefined endpoint combinations. At tone=0 the intended color is now unambiguously 0; at tone=1 it is unambiguously 1. A positive denominator bound exists throughout the valid mathematical color domain.

At e=0.01, that bound is still below the current 1e-5 floor because of sRGB conversion. The revised analysis therefore does not certify this epsilon as accurate for the implementation. Selecting another margin requires an acceptable output-error tolerance, not just a requirement for finite outputs.

The current runtime still accepts broader ranges. Existing material settings that contain exact 0 or 1 RGB channels are outside this proposed contract. This revision documents the contract without migrating those settings or enforcing it.

See [the RGB-constrained analysis](../Reports/ShaderNumerics/RgbConstrainedAnalysis.md) for proofs, current-shader measurements, and remaining issues. The [earlier analysis](../Reports/ShaderNumerics/ConstrainedAnalysis.md) records the previous proposal with RGB in [0,1].
