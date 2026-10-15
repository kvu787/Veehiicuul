# Physics

- Veehiicuul mostly doesn't implement real-world physics.
- Movement uses acceleration and velocity, but mass isn't really a thing.
  - Alternatively, every object has a mass of 1.
- Some mechanics may change velocity instantaneously without using acceleration.
  - E.g. a digital boost control may double speed or add a constant speed.
  - This can result in "jerky" looking movement.
- Some mechanics may change position instantaneously without using velocity or acceleration.
  - E.g. moving an analog stick and then clicking it instantly teleports the vehicle to a position.

# Braking

- Braking should only decrease or stop movement, not cause movement.
- When the vehicle is not moving, braking should do nothing.

## Straight-line

- Braking overrides any other input, which means no turning while braking.
- While braking is active, the vehicle moves in a straight lines and slows down to a stop.

## Normal

- This is more like typical car braking.
- Braking can be combined with turning.
- The new velocity is computed with other mechanics, and then braking input decreases the magnitude the new velocity.
