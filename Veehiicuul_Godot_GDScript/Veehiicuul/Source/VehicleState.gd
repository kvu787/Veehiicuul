class_name VehicleState
extends RefCounted

var starting_position := Vector3.ZERO
var starting_rotation: float = 0.0
var position := Vector3.ZERO
var rotation: float = 0.0
var velocity := Vector3.ZERO


func initialize(placeholder: Node3D) -> void:
	starting_position = placeholder.position
	starting_rotation = placeholder.rotation.y
	reset()


func update_velocity(input: GameInput, car: VehicleConfiguration, camera_yaw: float, delta: float) -> void:
	if input.brake == 0.0:
		var acceleration := Vector3(input.acceleration_input.x, 0.0, -input.acceleration_input.y)
		acceleration = acceleration.rotated(Vector3.UP, camera_yaw).rotated(Vector3.UP, -rotation)
		acceleration.x = GameInput.axial_deadzone(acceleration.x, 0.10, 0.95)
		acceleration.y = 0.0
		acceleration.z = GameInput.axial_deadzone(acceleration.z, 0.05, 0.95)
		acceleration = acceleration.limit_length(1.0)
		if acceleration != Vector3.ZERO:
			var output := Vector3(acceleration.x * (car.right_acceleration if acceleration.x < 0.0 else car.left_acceleration), 0.0, acceleration.z * (car.reverse_acceleration if acceleration.z < 0.0 else car.forward_acceleration))
			velocity += output.rotated(Vector3.UP, rotation) * delta
	elif velocity != Vector3.ZERO:
		var length_squared := velocity.length_squared()
		if length_squared < 0.0001:
			velocity = Vector3.ZERO
		else:
			var change := -velocity.normalized() * car.reverse_acceleration * input.brake * delta
			velocity = Vector3.ZERO if change.length_squared() >= length_squared else velocity + change
	if car.velocity_limiter > 0.0:
		velocity = velocity.limit_length(car.velocity_limiter)
	if velocity != Vector3.ZERO:
		rotation = atan2(velocity.x, velocity.z)


func update_position(delta: float) -> void:
	position += velocity * delta


func reset() -> void:
	position = starting_position
	rotation = starting_rotation
	velocity = Vector3.ZERO


func apply_to(car: Node3D) -> void:
	car.position = position
	car.rotation = Vector3(0.0, rotation, 0.0)
