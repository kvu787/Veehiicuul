# Staged yaw and spawn review

## User

review the currently staged changes

## Assistant Commentary

I’ll review the staged diff for bugs and regressions, then report any actionable findings with file and line references.

## Assistant Commentary

The yaw sign changes are inconsistent with the unchanged input-rotation math. I’m checking the existing coordinate tests and spawn transforms to pin down the resulting behavior.

## Assistant Commentary

The checks confirm two regressions: forward input can select reverse acceleration, and spawn/reset poses lose parent transforms. The coordinate suite fails, and running the car-state checks separately produces five failures. I’m recording the review in the required conversation log and committing that log separately.

## Assistant

Found two regressions:

::code-comment{title="[P1] Keep the yaw convention consistent with acceleration calculations" body="Get2DRotation now returns the opposite yaw sign, but CarStateManager still uses the original Rotate2D convention when converting acceleration between world and car space. When moving along +X, forward input consequently becomes car-local -Z and selects Reverse instead of Forward acceleration. Diagonal input also produces incorrect acceleration directions. Preserve the previous convention, or update all consumers consistently, including Get2DRotationQuaternion." file="C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Extensions/Vector3Extensions.cs" start=32 priority=1}

::code-comment{title="[P2] Preserve the placeholder’s world-space spawn pose" body="Position and Rotation omit the placeholder’s parent transforms, but Apply still writes a world-space pose through SetPositionAndRotation. Translating or rotating the Model parent therefore makes the car spawn and reset at the wrong position or heading. Keep using GlobalPosition and GlobalRotation; the existing parent-transform regression check now fails." file="C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Car/CarStateManager.cs" start=40 end=41 priority=2}

Validation: the coordinate suite fails at “Model right heading”; running the car-state checks separately yields **5 failures out of 10**. No application files changed.
