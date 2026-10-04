class_name TrackSession
extends RefCounted

const CAMERA_PATH := "CameraPanAndYaw/CameraYawOffset/CameraPanOffsetAndPitch/Camera"

var valid: bool = false
var track_names: PackedStringArray
var track_index: int = 0
var track: Node3D
var placeholder: MeshInstance3D
var camera_pan: Node3D
var camera: Camera3D
var cars: Array[VehicleConfiguration] = []
var car_index: int = 0
var follows_car: bool = false
var follow_camera_size: float = 45.0


func open_track(parent: Node, index: int) -> bool:
	valid = false
	if index < 0 or index >= track_names.size():
		push_error("Invalid track index.")
		return false
	var track_name := track_names[index]
	var settings := JsonUtility.read_dictionary("res://Tracks/%s/%s_Settings.json" % [track_name, track_name])
	if not settings.get("Cars") is Array or settings["Cars"].is_empty():
		push_error("The track must define at least one car.")
		return false
	if not JsonUtility.finite_number(settings.get("StartCarIndex")) or float(settings["StartCarIndex"]) != floorf(float(settings["StartCarIndex"])):
		push_error("StartCarIndex must be an integer.")
		return false
	var start_index := int(settings["StartCarIndex"])
	if start_index < 0 or start_index >= settings["Cars"].size():
		push_error("StartCarIndex is outside the car list.")
		return false
	if not settings.get("CameraFollowsCarLocation") is bool or not JsonUtility.finite_number(settings.get("FollowCameraSize")) or float(settings["FollowCameraSize"]) <= 0:
		push_error("The track must define CameraFollowsCarLocation and a positive FollowCameraSize.")
		return false
	var next_cars: Array[VehicleConfiguration] = []
	for definition: Variant in settings["Cars"]:
		if not definition is Dictionary:
			push_error("Car settings must be objects.")
			return false
		var car := VehicleConfiguration.from_dictionary(definition)
		if car == null:
			return false
		next_cars.append(car)
	var scene := ResourceLoader.load("res://Tracks/%s/%s_Scene.tscn" % [track_name, track_name], "PackedScene", ResourceLoader.CACHE_MODE_IGNORE) as PackedScene
	if scene == null:
		push_error("Cannot load track scene: " + track_name)
		return false
	var candidate := scene.instantiate() as Node3D
	if candidate == null:
		push_error("Track scene must have a Node3D root.")
		return false
	if not _validate_scene(candidate):
		candidate.free()
		return false
	var next_placeholder := candidate.get_node("Model/SlopeCarPlaceholder") as MeshInstance3D
	if not _validate_placeholder(next_placeholder):
		candidate.free()
		return false
	for car: VehicleConfiguration in next_cars:
		var decoration := candidate.find_child(car.model_name, true, false) as MeshInstance3D
		if decoration == null:
			push_error("Missing car MeshInstance3D: " + car.model_name)
			candidate.free()
			return false
		car.node = decoration.duplicate(0) as MeshInstance3D
		candidate.add_child(car.node)
		car.node.visible = false
	if is_instance_valid(track):
		track.free()
	track = candidate
	parent.add_child(track)
	track_index = index
	cars = next_cars
	car_index = start_index
	cars[car_index].node.visible = true
	placeholder = next_placeholder
	placeholder.visible = false
	camera_pan = track.get_node("CameraPanAndYaw") as Node3D
	camera = track.get_node(CAMERA_PATH) as Camera3D
	follows_car = settings["CameraFollowsCarLocation"]
	follow_camera_size = settings["FollowCameraSize"]
	valid = true
	return true


func switch_track(parent: Node, input: GameInput) -> bool:
	if input.previous_track == input.next_track:
		return false
	var next_index := posmod(track_index + (-1 if input.previous_track else 1), track_names.size())
	return open_track(parent, next_index)


func switch_car(input: GameInput) -> bool:
	if input.previous_car == input.next_car:
		return false
	cars[car_index].node.visible = false
	car_index = posmod(car_index + (1 if input.next_car else -1), cars.size())
	cars[car_index].node.visible = true
	return true


static func _validate_scene(root: Node3D) -> bool:
	if root.name != &"Track":
		push_error("Track scene root must be named Track.")
		return false
	var required := {"WorldEnvironment": "WorldEnvironment", "CameraPanAndYaw": "Node3D", "CameraPanAndYaw/CameraYawOffset": "Node3D", "CameraPanAndYaw/CameraYawOffset/CameraPanOffsetAndPitch": "Node3D", CAMERA_PATH: "Camera3D", "Sunlight": "DirectionalLight3D", "Model": "Node3D", "Model/SlopeCarPlaceholder": "MeshInstance3D"}
	for path: String in required:
		var node := root.get_node_or_null(path)
		if node == null or not node.is_class(required[path]):
			push_error("Missing or incorrect track node: " + path)
			return false
	var model := root.get_node("Model") as Node3D
	if model.transform != Transform3D.IDENTITY:
		push_error("Model must have an identity local transform.")
		return false
	return true


static func _validate_placeholder(node: MeshInstance3D) -> bool:
	if node.position.y != 0.0 or absf(node.rotation.x) >= 1.7453293e-7 or absf(node.rotation.z) >= 1.7453293e-7 or absf(node.scale.x - 1.0) >= 0.00001 or absf(node.scale.y - 1.0) >= 0.00001 or absf(node.scale.z - 1.0) >= 0.00001:
		push_error("SlopeCarPlaceholder must be at ground height with only yaw and unit scale.")
		return false
	return true
