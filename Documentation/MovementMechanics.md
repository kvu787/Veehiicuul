# Gamepad controls

- Any control that isn't directional such as triggers and buttons are vehicle-relative
  - This means that holding the right trigger always means "move the vehicle forward".
- By default, the orientation of the analog stick is basically "point in the direction you want the vehicle to move"
  - This is called "view-relative" orientation.
  - This means that pushing the stick up doesn't always mean "move the vehicle forward".
  - This means the stick vector is translated through the camera, world, and vehicle rotation to get the vehicle-relative stick input.
- Alternatively, the analog sticks can be configured to use "vehicle-relative" orientation.
- Digital d-pads have eight effective directions and can be mapped to an analog stick control
  - Smoothing may be applied so that the magnitude isn't always 1

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

# Core movement

## Simple forward/reverse and turn

- This is the simplest car-like movement model.
- Forward/reverse input sets the car's forward/reverse speed.
- Left/right input sets the car's clockwise/counterclockwise angular speed.
- There is no need for braking because the car immediately stops when there is no input.
- Movement looks some what abrupt because acceleration isn't used.

## Simple acceleration

- This is the simplest smooth-looking movement model.
- Stick input corresponds to applying an acceleration with the given direction and magnitude.
- The rotation of the vehicle isn't controlled by input at all.
  - The rotation is set to the direction of velocity.
- Braking is needed to easily stop the vehicle.
  - Without braking, using acceleration inputs to try and stop the vehicle will result in continual tiny overshoots at very low speeds.
