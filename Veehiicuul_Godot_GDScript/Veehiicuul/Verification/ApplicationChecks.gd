extends SceneTree

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
	var scene := load("res://Main.tscn") as PackedScene
	var main := scene.instantiate()
	root.add_child(main)
	main.set_process(false)
	# Deferred startup can run inside a physics tick. Exercise _process input
	# from a real process-frame signal so just-pressed events use its clock.
	await process_frame
	check(main.initialized, "Main initializes all subsystems")
	var starting_position: Vector3 = main.vehicle.starting_position
	main.vehicle.position += Vector3(5, 0, 5)
	main.vehicle.velocity = Vector3(1, 0, 0)
	Input.action_press(&"JoyButtonX")
	main._process(0.01)
	check(main.vehicle.position == starting_position and main.vehicle.velocity == Vector3.ZERO, "Main handles manual reset")
	check(Time.get_ticks_usec() - main.control_timeout_start <= 350000, "Reset starts 350 ms timeout")
	Input.action_release(&"JoyButtonX")
	await process_frame
	main.vehicle.velocity = Vector3(1, 0, 0)
	main._process(0.1)
	check(main.vehicle.position == starting_position, "Movement is suppressed during timeout")
	main.control_timeout_start = Time.get_ticks_usec() - 350001
	main._process(0.1)
	check(main.vehicle.position.distance_to(starting_position + Vector3(0.1, 0, 0)) < 0.0001, "Movement resumes after timeout")
	Input.action_press(&"JoyButtonDpadRight")
	main._process(0.0)
	check(main.session.car_index == 4 and main.vehicle.position == starting_position and main.vehicle.velocity == Vector3.ZERO, "Car switch resets position and velocity")
	Input.action_release(&"JoyButtonDpadRight")
	await process_frame
	Input.action_press(&"JoyButtonStart")
	main._process(0.0)
	check(main.camera.follows_car and main.session.camera.size == 45.0, "Main handles camera follow input")
	Input.action_release(&"JoyButtonStart")
	await process_frame
	Input.action_press(&"JoyButtonLeftStick")
	main._process(0.0)
	Input.action_release(&"JoyButtonLeftStick")
	await process_frame
	var detector: TrackCollisionDetector = main.collision.detector
	var first_edge_center: Vector2 = (detector.edge_start[0] + detector.edge_end[0]) * 0.5
	main.vehicle.position = Vector3(first_edge_center.x, 0.0, -first_edge_center.y)
	main.control_timeout_start = -1000000
	main._process(0.0)
	check(main.vehicle.position == starting_position, "Main resets on sampled collision")
	Input.action_press(&"JoyButtonDpadUp")
	main._process(0.0)
	check(main.session.car_index == 3 and not main.camera.follows_car and main.session.camera.size == 150.0, "Track switch reloads default car and camera")
	Input.action_release(&"JoyButtonDpadUp")
	await process_frame
	print("Application verification: %d checks, %d failures" % [checks, failures])
	main.free()
	quit(0 if failures == 0 else 1)
