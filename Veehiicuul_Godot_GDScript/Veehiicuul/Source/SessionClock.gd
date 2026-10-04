class_name SessionClock
extends RefCounted
## Pure GDScript exposes monotonic microseconds, not the raw Windows QPC epoch.
## Launcher UTC/QPC calibration estimates that epoch and is labeled accordingly.

var qpc_frequency: int = 0
var qpc_at_calibration: int = 0
var ticks_at_calibration: int = 0
var calibrated: bool = false


func initialize() -> void:
	var arguments := OS.get_cmdline_user_args()
	var anchor_qpc: int = 0
	var anchor_unix: float = 0.0
	for argument: String in arguments:
		if argument.begins_with("--clock-qpc="):
			anchor_qpc = int(argument.trim_prefix("--clock-qpc="))
		elif argument.begins_with("--clock-frequency="):
			qpc_frequency = int(argument.trim_prefix("--clock-frequency="))
		elif argument.begins_with("--clock-unix="):
			anchor_unix = float(argument.trim_prefix("--clock-unix="))
	ticks_at_calibration = Time.get_ticks_usec()
	var unix_time := Time.get_unix_time_from_system()
	calibrated = anchor_qpc > 0 and anchor_unix > 0.0 and qpc_frequency > 0
	if calibrated:
		qpc_at_calibration = anchor_qpc + int(round((unix_time - anchor_unix) * qpc_frequency))
	print("ProcessId='%d', Clock='Godot monotonic microseconds', TimeMicroseconds='%d', UnixTime='%.6f'" % [OS.get_process_id(), ticks_at_calibration, unix_time])
	if calibrated:
		print("QpcFrequency='%d', EstimatedQpcAtCalibration='%d', ClockCalibration='UTC bridge; estimate, not direct QPC sampling'" % [qpc_frequency, qpc_at_calibration])


func print_stutter_marker() -> void:
	var ticks := Time.get_ticks_usec()
	if calibrated:
		var estimated_qpc := qpc_at_calibration + int(round(float(ticks - ticks_at_calibration) * qpc_frequency / 1000000.0))
		print("EstimatedQPC='%d', TimeMicroseconds='%d', Engine.GetProcessFrames()='%d', Event='Stutter observed by player. Account for player reaction time and latency.'" % [estimated_qpc, ticks, Engine.get_process_frames()])
	else:
		print("TimeMicroseconds='%d', UnixTime='%.6f', Engine.GetProcessFrames()='%d', Event='Stutter observed by player. Account for player reaction time and latency.'" % [ticks, Time.get_unix_time_from_system(), Engine.get_process_frames()])
