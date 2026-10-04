extends SceneTree
## Run with standard Godot --headless --path Veehiicuul --script this file.

var failures: int = 0
var checks: int = 0


func _initialize() -> void:
	call_deferred("run_checks")


func check(condition: bool, message: String) -> void:
	checks += 1
	if not condition:
		failures += 1
		printerr("CHECK FAILED: " + message)


func run_checks() -> void:
	var inputs := GameInput.new()
	inputs.install_actions()
	check(InputMap.action_get_events(&"JoyButtonX")[0].button_index == JOY_BUTTON_X, "Controller X binding")
	check(InputMap.action_get_events(&"KeyEscape")[0].keycode == KEY_ESCAPE, "Escape binding")
	check(GameInput.axial_deadzone(0.04, 0.05, 0.95) == 0.0, "Inner deadzone")
	check(is_equal_approx(GameInput.axial_deadzone(-0.5, 0.05, 0.95), -0.5), "Deadzone remapping")
	check(GameInput.axial_deadzone(1.0, 0.05, 0.95) == 1.0, "Outer deadzone")
	check(TrackCollisionDetector.segments_intersect(Vector2(0, 0), Vector2(2, 0), Vector2(1, 0), Vector2(3, 0)), "Collinear overlap")
	check(TrackCollisionDetector.segments_intersect(Vector2(0, 0), Vector2(1, 0), Vector2(1, 0), Vector2(1, 1)), "Endpoint contact")
	check(not TrackCollisionDetector.segments_intersect(Vector2(0, 0), Vector2(1, 0), Vector2(2, 0), Vector2(3, 0)), "Disjoint collinear segments")
	_check_dynamics(inputs)
	var session := TrackSession.new()
	session.track_names = PackedStringArray(["Ribeye"])
	if not session.open_track(root, 0):
		printerr("Could not initialize test track.")
		quit(1)
		return
	var collisions := CollisionManager.new()
	if not collisions.initialize(session):
		quit(1)
		return
	check(session.cars.size() == 6 and session.car_index == 3, "Six cars; yellow selected initially")
	check(collisions.detector.edge_start.size() == 800, "Ribeye edge count")
	check(collisions.detector.uses_expanded_grid, "Ribeye expanded-grid path")
	var vehicle := VehicleState.new()
	vehicle.initialize(session.placeholder)
	check(not collisions.is_car_colliding(session.car_index, vehicle.position, vehicle.rotation), "Starting pose is clear")
	var camera := GameCamera.new()
	camera.initialize(session, vehicle)
	check(camera.size == 150.0 and not camera.follows_car, "Initial fixed camera")
	inputs.toggle_camera_follow = true
	camera.update(inputs, 0.01)
	camera.apply_to(session, vehicle)
	check(camera.follows_car and camera.size == 45.0, "Follow toggle restores default follow size")
	check(session.camera_pan.position == vehicle.position, "Follow camera pan")
	inputs.toggle_camera_follow = false
	inputs.camera_zoom = 1.0
	camera.update(inputs, 100.0)
	check(camera.size == 281.25, "Zoom maximum")
	inputs.camera_zoom = -1.0
	camera.update(inputs, 100.0)
	check(camera.size == 1.0, "Zoom minimum")
	inputs.reset_camera_zoom = true
	camera.update(inputs, 0.0)
	check(camera.size == 45.0, "Zoom reset")
	inputs.reset_camera_zoom = false
	inputs.camera_zoom = 0.0
	inputs.toggle_camera_follow = true
	camera.update(inputs, 0.0)
	camera.apply_to(session, vehicle)
	check(not camera.follows_car and camera.size == 150.0 and session.camera_pan.position == camera.original_position, "Fixed-camera toggle restores position and size")
	inputs.toggle_camera_follow = false
	inputs.next_car = true
	for expected in [4, 5, 0, 1, 2, 3]:
		check(session.switch_car(inputs) and session.car_index == expected, "Next car with wraparound")
		var visible_count: int = 0
		for car: VehicleConfiguration in session.cars:
			visible_count += int(car.node.visible)
		check(visible_count == 1, "Only selected gameplay car is visible")
	inputs.previous_car = true
	check(not session.switch_car(inputs), "Opposing car inputs cancel")
	inputs.previous_car = false
	inputs.next_car = false
	inputs.previous_track = true
	inputs.next_track = true
	check(not session.switch_track(root, inputs), "Opposing track inputs cancel")
	inputs.previous_track = false
	check(session.switch_track(root, inputs) and session.car_index == 3, "Single-track wrap reloads defaults")
	check(collisions.initialize(session), "Collision state rebuilds after track reload")
	vehicle.initialize(session.placeholder)
	vehicle.position += Vector3(5, 0, 5)
	vehicle.velocity = Vector3(1, 0, 2)
	vehicle.rotation = 1.0
	vehicle.reset()
	check(vehicle.position == session.placeholder.position and vehicle.velocity == Vector3.ZERO and vehicle.rotation == session.placeholder.rotation.y, "Reset restores full pose and velocity")
	_check_collision_reference(collisions, session)
	_check_general_collision_paths()
	session.track.free()
	print("Behavior verification: %d checks, %d failures" % [checks, failures])
	quit(0 if failures == 0 else 1)


func _check_dynamics(inputs: GameInput) -> void:
	var car := VehicleConfiguration.new()
	car.forward_acceleration = 2.0
	car.reverse_acceleration = 4.0
	car.left_acceleration = 3.0
	car.right_acceleration = 5.0
	var state := VehicleState.new()
	inputs.acceleration_input = Vector2(0, -1)
	state.update_velocity(inputs, car, 0.0, 1.0)
	check(state.velocity.is_equal_approx(Vector3(0, 0, 2)), "Forward acceleration")
	state.update_position(0.5)
	check(state.position.is_equal_approx(Vector3(0, 0, 1)), "Position integration")
	inputs.brake = 1.0
	state.update_velocity(inputs, car, 0.0, 0.25)
	check(state.velocity.is_equal_approx(Vector3(0, 0, 1)), "Brake deceleration")
	state.update_velocity(inputs, car, 0.0, 1.0)
	check(state.velocity == Vector3.ZERO, "Brake clamps at zero")
	inputs.brake = 0.0
	state.reset()
	inputs.acceleration_input = Vector2(1, 0)
	state.update_velocity(inputs, car, 0.0, 1.0)
	check(state.velocity.is_equal_approx(Vector3(3, 0, 0)) and is_equal_approx(state.rotation, PI / 2), "Lateral acceleration and +Z yaw convention")
	state.reset()
	inputs.acceleration_input = Vector2(0, -1)
	state.update_velocity(inputs, car, PI / 2, 1.0)
	check(state.velocity.is_equal_approx(Vector3(3, 0, 0)), "Camera-relative input")
	state.reset()
	car.velocity_limiter = 0.5
	state.update_velocity(inputs, car, 0.0, 1.0)
	check(is_equal_approx(state.velocity.length(), 0.5), "Velocity limiter")
	inputs.acceleration_input = Vector2.ZERO


func _check_collision_reference(collisions: CollisionManager, session: TrackSession) -> void:
	var random := RandomNumberGenerator.new()
	random.seed = 472
	var detector := collisions.detector
	var hits: int = 0
	var misses: int = 0
	for car_index in range(session.cars.size()):
		for sample in range(160):
			var position: Vector2
			if sample < 100:
				var edge := random.randi_range(0, detector.edge_start.size() - 1)
				position = detector.edge_start[edge].lerp(detector.edge_end[edge], random.randf()) + Vector2(random.randf_range(-4, 4), random.randf_range(-4, 4))
			else:
				position = Vector2(random.randf_range(detector.all_bounds.x - 10, detector.all_bounds.z + 10), random.randf_range(detector.all_bounds.y - 10, detector.all_bounds.w + 10))
			var yaw := random.randf_range(-PI, PI)
			var indexed := detector.is_colliding(collisions.vehicle_bounds[car_index], position, yaw)
			var reference := reference_collision(detector, collisions.vehicle_bounds[car_index], position, yaw)
			check(indexed == reference, "Indexed/independent collision agreement for car %d sample %d" % [car_index, sample])
			hits += int(reference)
			misses += int(not reference)
	check(hits > 0 and misses > 0, "Reference comparisons cover collisions and clear poses")
	print("Ribeye collision reference: %d hits, %d clear poses" % [hits, misses])


func _check_general_collision_paths() -> void:
	var data := {"Outlines": [{"Vertices": [{"X": -10, "Y": -10}, {"X": 10, "Y": -10}, {"X": 10, "Y": 10}, {"X": -10, "Y": 10}]}]}
	var bounds := Vector4(-1, -1, 1, 1)
	var detector := TrackCollisionDetector.new()
	check(detector.build(data, bounds), "Synthetic detector initialization")
	check(not detector.is_colliding(bounds, Vector2.ZERO, 0.0), "Fully inside outline does not touch perimeter")
	check(detector.is_colliding(bounds, Vector2(9, 0), 0.0), "Exact border contact")
	check(not detector.is_colliding(bounds, Vector2(11.1, 0), 0.0), "Outside clear pose")
	check(detector.is_colliding(Vector4(-20, -1, 20, 1), Vector2.ZERO, 0.0), "Oversized footprint general path")
	var fallback := TrackCollisionDetector.new()
	check(fallback.build(data, bounds, 0.00001) and not fallback.uses_expanded_grid, "Grid limit selects bounds tree")
	for x in range(-14, 15):
		check(fallback.is_colliding(bounds, Vector2(x, 0), 0.3) == reference_collision(fallback, bounds, Vector2(x, 0), 0.3), "Fallback tree collision agreement")
	var outlines: Array = data["Outlines"].duplicate(true)
	for outline_index in range(5):
		var outline: Dictionary = data["Outlines"][0].duplicate(true)
		for point: Dictionary in outline["Vertices"]:
			point["X"] += 1000000 * (outline_index + 1)
		outlines.append(outline)
	check(fallback.build({"Outlines": outlines}, bounds) and fallback.tree_bounds.size() > 1, "Sparse large-coordinate track uses tree branches")
	for x: float in [0.0, 9.0, 1000009.0, 5000009.0, 8000000.0]:
		check(fallback.is_colliding(bounds, Vector2(x, 0), 0.0) == reference_collision(fallback, bounds, Vector2(x, 0), 0.0), "Large-coordinate bounds-tree agreement")


static func reference_collision(detector: TrackCollisionDetector, bounds: Vector4, position: Vector2, yaw: float) -> bool:
	# Independent corner transform and native segment intersection.
	var transform := Transform2D(-yaw, position)
	var points: Array[Vector2] = [transform * Vector2(bounds.x, bounds.y), transform * Vector2(bounds.z, bounds.y), transform * Vector2(bounds.z, bounds.w), transform * Vector2(bounds.x, bounds.w)]
	for edge in range(detector.edge_start.size()):
		var a := detector.edge_start[edge]
		var b := detector.edge_end[edge]
		for side in range(4):
			var c := points[side]
			var d := points[(side + 1) % 4]
			if Geometry2D.segment_intersects_segment(a, b, c, d) != null:
				return true
			# Native segment API excludes collinear contacts; check them explicitly.
			if (float(b.x) - a.x) * (float(c.y) - a.y) == (float(b.y) - a.y) * (float(c.x) - a.x) and (float(b.x) - a.x) * (float(d.y) - a.y) == (float(b.y) - a.y) * (float(d.x) - a.x):
				if maxf(minf(a.x, b.x), minf(c.x, d.x)) <= minf(maxf(a.x, b.x), maxf(c.x, d.x)) and maxf(minf(a.y, b.y), minf(c.y, d.y)) <= minf(maxf(a.y, b.y), maxf(c.y, d.y)):
					return true
	return false
