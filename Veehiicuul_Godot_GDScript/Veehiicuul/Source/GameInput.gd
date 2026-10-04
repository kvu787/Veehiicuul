class_name GameInput
extends RefCounted

const JOYPAD_ACTIONS: Dictionary = {
	&"JoyButtonA": JOY_BUTTON_A, &"JoyButtonB": JOY_BUTTON_B,
	&"JoyButtonX": JOY_BUTTON_X, &"JoyButtonY": JOY_BUTTON_Y,
	&"JoyButtonDpadUp": JOY_BUTTON_DPAD_UP, &"JoyButtonDpadDown": JOY_BUTTON_DPAD_DOWN,
	&"JoyButtonDpadLeft": JOY_BUTTON_DPAD_LEFT, &"JoyButtonDpadRight": JOY_BUTTON_DPAD_RIGHT,
	&"JoyButtonLeftShoulder": JOY_BUTTON_LEFT_SHOULDER,
	&"JoyButtonRightShoulder": JOY_BUTTON_RIGHT_SHOULDER,
	&"JoyButtonLeftStick": JOY_BUTTON_LEFT_STICK, &"JoyButtonRightStick": JOY_BUTTON_RIGHT_STICK,
	&"JoyButtonBack": JOY_BUTTON_BACK, &"JoyButtonStart": JOY_BUTTON_START,
}

var brake: float = 0.0
var acceleration_input := Vector2.ZERO
var reset_camera_zoom: bool = false
var quit_game: bool = false
var previous_track: bool = false
var next_track: bool = false
var previous_car: bool = false
var next_car: bool = false
var toggle_camera_follow: bool = false
var reset_car: bool = false
var camera_zoom: float = 0.0
var stutter_observed: bool = false


func install_actions() -> void:
	for action: StringName in JOYPAD_ACTIONS:
		if InputMap.has_action(action):
			InputMap.erase_action(action)
		InputMap.add_action(action)
		var binding := InputEventJoypadButton.new()
		binding.button_index = JOYPAD_ACTIONS[action]
		binding.device = 0
		InputMap.action_add_event(action, binding)
	if not InputMap.has_action(&"KeyEscape"):
		InputMap.add_action(&"KeyEscape")
		var key := InputEventKey.new()
		key.keycode = KEY_ESCAPE
		InputMap.action_add_event(&"KeyEscape", key)
	if not InputMap.has_action(&"MouseButtonMiddle"):
		InputMap.add_action(&"MouseButtonMiddle")
		var mouse := InputEventMouseButton.new()
		mouse.button_index = MOUSE_BUTTON_MIDDLE
		InputMap.action_add_event(&"MouseButtonMiddle", mouse)


func update() -> void:
	previous_car = Input.is_action_just_pressed(&"JoyButtonDpadLeft")
	next_car = Input.is_action_just_pressed(&"JoyButtonDpadRight")
	previous_track = Input.is_action_just_pressed(&"JoyButtonDpadDown")
	next_track = Input.is_action_just_pressed(&"JoyButtonDpadUp")
	quit_game = Input.is_action_just_pressed(&"JoyButtonBack") or Input.is_action_just_pressed(&"KeyEscape")
	toggle_camera_follow = Input.is_action_just_pressed(&"JoyButtonStart")
	reset_car = Input.is_action_just_pressed(&"JoyButtonX")
	acceleration_input = Vector2(Input.get_joy_axis(0, JOY_AXIS_RIGHT_X), -Input.get_joy_axis(0, JOY_AXIS_RIGHT_Y))
	brake = Input.get_joy_axis(0, JOY_AXIS_TRIGGER_LEFT)
	reset_camera_zoom = Input.is_action_just_pressed(&"JoyButtonY")
	stutter_observed = Input.is_action_just_pressed(&"JoyButtonLeftStick")
	camera_zoom = axial_deadzone(Input.get_joy_axis(0, JOY_AXIS_LEFT_Y), 0.0078125, 0.95) if Input.is_action_pressed(&"JoyButtonRightShoulder") else 0.0


static func axial_deadzone(value: float, inner: float, outer: float) -> float:
	var magnitude := absf(value)
	if magnitude < inner:
		return 0.0
	if magnitude > outer:
		return signf(value)
	return signf(value) * (magnitude - inner) / (outer - inner)
