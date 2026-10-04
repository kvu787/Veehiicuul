extends Node

const INITIAL_TRACK_INDEX: int = 0
const TRACK_NAMES: Array[String] = ["Ribeye"]
const CAR_CONTROL_TIMEOUT_MICROSECONDS: int = 350000

var input := GameInput.new()
var session := TrackSession.new()
var vehicle := VehicleState.new()
var camera := GameCamera.new()
var collision := CollisionManager.new()
var clock := SessionClock.new()
var control_timeout_start: int = -CAR_CONTROL_TIMEOUT_MICROSECONDS - 1
var initialized: bool = false


func _ready() -> void:
	Input.use_accumulated_input = false
	if "--application-windowed" in OS.get_cmdline_user_args():
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
		DisplayServer.window_set_size(Vector2i(1280, 720))
	clock.initialize()
	PrintInfoUtility.print_information(get_viewport())
	input.install_actions()
	session.track_names = PackedStringArray(TRACK_NAMES)
	if not session.open_track(self, INITIAL_TRACK_INDEX) or not initialize_track():
		quit_with_error("Application initialization failed.")
		return
	initialized = true


func initialize_track() -> bool:
	vehicle.initialize(session.placeholder)
	vehicle.apply_to(session.cars[session.car_index].node)
	camera.initialize(session, vehicle)
	return collision.initialize(session)


func _process(delta: float) -> void:
	if not initialized:
		return
	input.update()
	if input.stutter_observed:
		clock.print_stutter_marker()
	if input.quit_game:
		set_process(false)
		get_tree().quit()
		return
	if session.switch_track(self, input):
		if not initialize_track():
			quit_with_error("Track initialization failed.")
		return
	if not session.valid:
		quit_with_error("Track switch failed.")
		return
	if session.switch_car(input) or input.reset_car or collision.is_car_colliding(session.car_index, vehicle.position, vehicle.rotation):
		control_timeout_start = Time.get_ticks_usec()
		vehicle.reset()
	camera.update(input, delta)
	if Time.get_ticks_usec() - control_timeout_start > CAR_CONTROL_TIMEOUT_MICROSECONDS:
		vehicle.update_velocity(input, session.cars[session.car_index], session.camera_pan.rotation.y, delta)
		vehicle.update_position(delta)
	camera.apply_to(session, vehicle)
	vehicle.apply_to(session.cars[session.car_index].node)


func quit_with_error(message: String) -> void:
	push_error(message)
	set_process(false)
	get_tree().quit(1)
