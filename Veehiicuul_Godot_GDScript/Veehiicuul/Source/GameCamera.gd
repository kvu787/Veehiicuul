class_name GameCamera
extends RefCounted

var follows_car: bool = false
var size: float = 150.0
var fixed_size: float
var follow_size: float
var original_position: Vector3
var previous_size: float = -1.0


func initialize(session: TrackSession, vehicle: VehicleState) -> void:
	follows_car = session.follows_car
	fixed_size = session.camera.size
	follow_size = session.follow_camera_size
	original_position = session.camera_pan.position
	size = follow_size if follows_car else fixed_size
	previous_size = -1.0
	apply_to(session, vehicle)


func update(input: GameInput, delta: float) -> void:
	var changed := input.toggle_camera_follow
	if changed:
		follows_car = not follows_car
	if input.camera_zoom != 0.0:
		size = clampf(size + delta * 50.0 * input.camera_zoom, 1.0, 281.25)
	if input.reset_camera_zoom or changed:
		size = follow_size if follows_car else fixed_size


func apply_to(session: TrackSession, vehicle: VehicleState) -> void:
	session.camera_pan.position = vehicle.position if follows_car else original_position
	if size != previous_size:
		session.camera.size = size
		previous_size = size
