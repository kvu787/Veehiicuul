# SimplePaint interface and mathematical specification

Status: working specification; numerical input margins and an error tolerance are not yet selected.
Date: 2026-09-05.
Source examined: 2f0659a; the application shader and renderer match the source tested in the previous RGB-margin analysis.

This revision supersedes the earlier tentative shared epsilon of 0.01. The intended K12 visuals and real-number mathematics are the reference. Arithmetic safeguards, coefficient layouts, and approximation thresholds in the current DirectX implementation are not definitions of the shader.

The contract applies independently to each material. Runtime enforcement and material settings have not been changed.

## User interface

| Parameter | Abstract domain | Meaning |
|---|---|---|
| R, G, B | Each in (0, 1) | Base color in the existing user-facing sRGB space |
| Brightness | (0, 1) | Positions the base-color anchor on the tone curve |
| Shift | [0, 1) | Warps the facing lobe in the direction selected by Rotation |
| Rotation | [0, 360) degrees | Circular orientation of the shift; equivalent to RotationDegrees in Settings.ini |
| Dark Point | [0, 1) | Tone selected when the warped facing value is zero |
| Light Point | (0, 1] | Tone selected when the warped facing value is one |

All inputs must be finite. Parentheses exclude an endpoint; brackets include it. These are mathematical domains, not yet a complete rule for which decimal strings or machine values the UI accepts.

No Dark Point <= Light Point ordering is imposed. Reversed tone ranges are valid. Equal endpoints are allowed inside (0, 1) and give a constant color. Dark Point and Light Point control the input of the color curve; they are not final output intensities.

Rotation is periodic: the direction at 360 degrees equals the direction at zero, and [0, 360) is its canonical representation. Shift = 0 makes Rotation irrelevant.

The fixed K12 facing cutoff is q = 0.01. It is not an additional artistic control in this specification and is independent of all future numerical margins. The DirectX port's global FacingCutoff setting must equal q for the conclusions here to apply.

## Core real-number mathematics

The following equations specify one color channel. Apply them independently to red, green, and blue. Products, differences, square roots, and divisions here are exact real-number operations, not prescribed floating-point instruction sequences.

### Color interpretation

Let C be one user-facing sRGB channel and c its linear value. Preserve the existing interpretation:

~~~text
c = C / 12.92                            if C <= 0.04045
c = ((C + 0.055) / 1.055) ^ 2.4         otherwise
~~~

Thus 0 < c < 1. The shader output is linear color; presentation performs the existing sRGB encoding. UI RGB margins must be translated through this conversion when deriving bounds on c.

### Surface facing and shift

The geometric input is a finite, nonzero interpolated normal, expressed in the camera-corrected frame and normalized to unit length. For this repository's orthographic projection this is the view-space frame used by the current renderer. Material rotation is by the negative of the user angle around its Z axis.

After rotation, write the unit normal as (x, y, z). Let s = Shift:

~~~text
r = sqrt(x*x + z*z)
h = sqrt((1-s)*(1+s))

f = 0                              if z < q
f = r*z*h / (r-s*x)                if z >= q
~~~

This is the algebraic reduction of K12's slice/Schlick/remap construction on the admitted branch. Rejected normals select f = 0; the pixel is not discarded. Evaluating the cutoff before the warp avoids irrelevant intermediate singularities in the original construction.

For s = 0 the admitted expression reduces exactly to f = z. For admitted normals, 0 < f <= r <= 1. The denominator is positive. Normals rejected by the cutoff give f = 0.

### Tone and color

Let p = Brightness, d = Dark Point, and l = Light Point:

~~~text
t = d*(1-f) + l*f

A = c*p
B = (1-c)*(1-p)

F(t) = A*t / (A*t + B*(1-t))
~~~

A and B are strictly positive, and 0 <= t <= 1. These equations reproduce the original real-number curve:

~~~text
original numerator   = c*p*t
original denominator = (c-(1-p))*t + (1-p)*(1-c)
                     = A*t + B*(1-t)
~~~

An implementation may use algebraically equivalent representations but must approximate this function over its supported input domain. In particular, a floor replacing a valid positive denominator is not part of the reference function.

## Properties to preserve

For every allowed c and p:

~~~text
F(0) = 0
F(1) = 1
F(1-p) = c
F'(t) = A*B / (A*t + B*(1-t))^2 > 0
~~~

The color curve is analytic in c, p, and t wherever the denominator is positive, including neighborhoods of t = 0 and t = 1 for fixed interior c and p. Changing to an equivalent positive-term expression preserves all its derivatives, curvature, and anchor properties.

Increasing Brightness moves the tone at which the base color appears toward zero. The tone remap retains its endpoint values and its direction, including reversed ranges. Its final endpoint colors are F(d) and F(l).

This smoothness statement concerns the linear color curve. The complete shader contains the inherited hard facing cutoff. It generally jumps at z = q; the rejected branch selects F(d), while the admitted branch can select a different color. It must not be described as globally continuous or globally smooth in the surface normal. Geometry interpolation and the piecewise sRGB conversion have their own regularity limits.

## Accepted non-issue: image sampling

Decision recorded on 2026-09-05: the user understands and accepts finite image sampling, including undersampling of narrow smooth highlights and resulting aliasing or flicker, as a non-issue for SimplePaint.

Do not list sampling among remaining shader or numerical issues, use it to justify input epsilons or changes to the shader mathematics, or introduce antialiasing/filtering work as a requirement. This topic is settled and must not be revisited unless the user explicitly reopens it.

## Numerical contract still to be selected

The mathematical domain excludes singular color corners but is not a uniform floating-point accuracy guarantee. Its closure still contains singular corners, and allowed values can approach them arbitrarily closely.

Select the supported machine domain separately from the equations:

- Define endpoint margins, potentially different for RGB, Brightness, Shift, Dark Point, and Light Point, and potentially asymmetric.
- Specify RGB margins in sRGB and derive their linear-color consequences.
- Define parsing, rounding, storage, and behavior when an input cannot be represented inside the supported domain. The intended policy is explicit validation rather than silently changing a valid requested material.
- Preserve the exact allowed endpoints Shift = 0, Dark Point = 0, and Light Point = 1.
- Choose an output-error criterion, its color space, and whether comparison starts from requested inputs or stored inputs. Finite outputs alone are insufficient.
- Specify the precision and dynamic range required of positive coefficients and intermediates; positivity before conversion does not imply positivity after rounding or underflow.
- State geometric preconditions, including nonzero interpolated normals, and handle invalid geometry separately.

There is no adopted common epsilon in this revision. In particular, the earlier approximately 0.01137 threshold only kept a particular 1e-5 denominator floor inactive under a shared-margin proposal. It is not a mathematical requirement of SimplePaint.

The current runtime accepts values outside this proposed domain, and some active materials contain exact RGB endpoints. Enforcing this contract later will require an explicit choice of margins and adjustment of those materials.

See [the implementation and remaining-issues analysis](../Reports/ShaderNumerics/AbstractContractAnalysis.md). The [previous RGB-margin report](../Reports/ShaderNumerics/RgbConstrainedAnalysis.md) and [earlier unrestricted-RGB report](../Reports/ShaderNumerics/ConstrainedAnalysis.md) remain historical analyses of their stated proposals.
