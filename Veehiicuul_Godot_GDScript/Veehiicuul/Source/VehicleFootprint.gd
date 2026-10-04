class_name VehicleFootprint
extends RefCounted

var found_geometry: bool = false
var minimum_x: float = INF
var minimum_z: float = INF
var maximum_x: float = -INF
var maximum_z: float = -INF


func measure(vehicle: Node3D) -> Vector4:
	found_geometry = false
	minimum_x = INF
	minimum_z = INF
	maximum_x = -INF
	maximum_z = -INF
	_include_node(vehicle, Transform3D.IDENTITY)
	var x_scale := vehicle.global_basis.x.length()
	var z_scale := vehicle.global_basis.z.length()
	if not found_geometry or not minimum_x < maximum_x or not minimum_z < maximum_z or not is_finite(x_scale) or not is_finite(z_scale) or x_scale <= 0.0 or z_scale <= 0.0:
		push_error("Vehicle '%s' needs finite positive-area mesh geometry and positive planar scale." % vehicle.name)
		return Vector4.ZERO
	# Collision Y is negative Godot Z. The front (+Z) is minimum Y.
	return Vector4(minimum_x * x_scale, -maximum_z * z_scale, maximum_x * x_scale, -minimum_z * z_scale)


func _include_node(node: Node, node_to_vehicle: Transform3D) -> void:
	if node is MeshInstance3D and node.mesh != null:
		var bounds: AABB = node.get_aabb() if node.custom_aabb == AABB() else node.custom_aabb
		for endpoint in range(8):
			var point := node_to_vehicle * bounds.get_endpoint(endpoint)
			if not is_finite(point.x) or not is_finite(point.z):
				push_error("Vehicle mesh has nonfinite bounds.")
				minimum_x = -INF
				return
			minimum_x = minf(minimum_x, point.x)
			minimum_z = minf(minimum_z, point.z)
			maximum_x = maxf(maximum_x, point.x)
			maximum_z = maxf(maximum_z, point.z)
			found_geometry = true
	for child: Node in node.get_children():
		var child_transform: Transform3D = node_to_vehicle * child.transform if child is Node3D else node_to_vehicle
		_include_node(child, child_transform)
