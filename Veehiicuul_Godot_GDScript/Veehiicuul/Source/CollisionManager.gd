class_name CollisionManager
extends RefCounted

var detector := TrackCollisionDetector.new()
var vehicle_bounds: Array[Vector4] = []
var previous_car_index: int = -1
var previous_position := Vector3.ZERO
var previous_rotation: float = 0.0
var previous_result: bool = false


func initialize(session: TrackSession) -> bool:
	vehicle_bounds.clear()
	previous_car_index = -1
	var footprint := VehicleFootprint.new()
	var representative := Vector4(INF, INF, -INF, -INF)
	for car: VehicleConfiguration in session.cars:
		var bounds := footprint.measure(car.node)
		# Shorten the front (+Z, minimum collision Y) by the original 0.165.
		bounds.y += 0.165
		if not TrackCollisionDetector.valid_bounds(bounds):
			push_error("Invalid collision footprint for car: " + car.model_name)
			return false
		vehicle_bounds.append(bounds)
		representative = TrackCollisionDetector.combine_bounds(representative, bounds)
	var track_name := session.track_names[session.track_index]
	var data := JsonUtility.read_dictionary("res://Tracks/%s/%s_ColliderData.json" % [track_name, track_name])
	if not detector.build(data, representative):
		return false
	print("Built track collision index: edges=%d, cellSize=%.15f, gridCells=%d, occupiedCells=%d, references=%d, expandedGrid=%s" % [detector.edge_start.size(), detector.cell_size, detector.column_count * detector.row_count, detector.occupied_cell_count, detector.edge_references.size(), detector.uses_expanded_grid])
	return true


func is_car_colliding(index: int, position: Vector3, rotation: float) -> bool:
	if index == previous_car_index and position.x == previous_position.x and position.z == previous_position.z and rotation == previous_rotation:
		return previous_result
	previous_result = detector.is_colliding(vehicle_bounds[index], Vector2(position.x, -position.z), -rotation)
	previous_car_index = index
	previous_position = position
	previous_rotation = rotation
	return previous_result
