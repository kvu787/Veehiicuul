class_name VehicleConfiguration
extends RefCounted

var model_name: String
var node: MeshInstance3D
var forward_acceleration: float
var reverse_acceleration: float
var left_acceleration: float
var right_acceleration: float
var velocity_limiter: float = 0.0


static func from_dictionary(data: Dictionary) -> VehicleConfiguration:
	if not data.get("GameObjectName") is String or not data.get("Dynamic") is Dictionary:
		push_error("Each car must define GameObjectName and Dynamic.")
		return null
	var dynamic: Dictionary = data["Dynamic"]
	if not dynamic.get("AccelerationMap") is Dictionary:
		push_error("Each car must define Dynamic.AccelerationMap.")
		return null
	var accelerations: Dictionary = dynamic["AccelerationMap"]
	for direction: String in ["Forward", "Reverse", "Left", "Right"]:
		if not JsonUtility.finite_number(accelerations.get(direction)):
			push_error("Missing or nonfinite car acceleration: " + direction)
			return null
	if not JsonUtility.finite_number(dynamic.get("VelocityLimiter", 0.0)):
		push_error("VelocityLimiter must be finite.")
		return null
	var car := VehicleConfiguration.new()
	car.model_name = data["GameObjectName"]
	car.forward_acceleration = accelerations["Forward"]
	car.reverse_acceleration = accelerations["Reverse"]
	car.left_acceleration = accelerations["Left"]
	car.right_acceleration = accelerations["Right"]
	car.velocity_limiter = dynamic.get("VelocityLimiter", 0.0)
	return car
