# SimplePaint input specification

Status: working specification for numerical analysis before implementation changes.
Date: 2026-09-05.

This contract uses the user-facing parameters of K12.gdshader. It does not change the shader implementation, runtime validation, or active paint settings.

| Parameter | Constraint at general epsilon e | Current range, e = 0.01 |
|---|---|---|
| BaseColor R | 0 <= R <= 1 | 0 to 1 |
| BaseColor G | 0 <= G <= 1 | 0 to 1 |
| BaseColor B | 0 <= B <= 1 | 0 to 1 |
| Brightness | e <= Brightness <= 1-e | 0.01 to 0.99 |
| Shift | 0 <= Shift <= 1-e | 0 to 0.99 |
| Rotation | 0 <= Rotation <= 360 | 0 to 360 degrees |
| DarkPoint | 0 <= DarkPoint <= 1-e | 0 to 0.99 |
| LightPoint | e <= LightPoint <= 1 | 0.01 to 1 |

All values must be finite. Bounds are inclusive. RGB values are the existing user-facing sRGB values; the renderer converts them to linear values for the paint calculation. Rotation maps to RotationDegrees in this repository's INI file.

The current value e=0.01 is tentative. A nonempty Brightness interval requires 0 < e <= 0.5. Reducing e is not approved by this analysis as a globally error-free choice.

No additional DarkPoint <= LightPoint requirement has been specified. Reversed ranges and equal endpoints in the overlap are allowed.

**Fixed K12 behavior**

The local reference file is C:/Users/k/Repository/Godot/SimplePaintShaders/Godot/ShaderTest/Shaders/K12.gdshader. Its line 107 uses:

~~~glsl
float remappedFacingRatioSafe = normal.z < 0.01 ? 0.0 : remappedFacingRatio;
~~~

The threshold is fixed at 0.01, rather than being an additional user input. normal.z is the corrected normal's component toward the camera. Below the threshold, the facing value is zero and the tone becomes DarkPoint; the pixel is not discarded.

The DirectX port exposes this value as FacingCutoff. For this specification and its analysis, hold that implementation setting at 0.01. Earlier experiments with FacingCutoff=0 are outside this contract.

**Three separate constants**

- Input margin e=0.01: the tentative parameter constraints above.
- K12 normal threshold q=0.01: fixed shader behavior from the reference.
- Existing DirectX numerical protection delta=0.00001: the current CPU clamps and shader denominator floors.

The first two currently have equal values but independent meanings. Specifying e=0.01 does not request changing the implementation's delta to 0.01.

**Output behavior still requiring specification**

These input constraints still permit a black channel at tone=1 and a white channel at tone=0. The unregularized rational paint formula is undefined at those two intersections. Input constraints alone do not choose their intended colors.

The constraints also do not define an acceptable output-error tolerance. A claim that a smaller epsilon is safe should identify a criterion, such as a maximum linear-channel error or a maximum difference after display encoding, plus the supported arithmetic assumptions.

See [the constrained numerical analysis](../Reports/ShaderNumerics/ConstrainedAnalysis.md) for proofs, measured counterexamples, and the distinction between mathematical domain safety and numerical accuracy.
