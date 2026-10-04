class_name JsonUtility
extends RefCounted


static func read_dictionary(path: String) -> Dictionary:
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		push_error("Cannot open '%s': %s" % [path, error_string(FileAccess.get_open_error())])
		return {}
	var parser := JSON.new()
	var result := parser.parse(file.get_as_text())
	if result != OK or not parser.data is Dictionary:
		push_error("Invalid JSON object in '%s', line %d: %s" % [path, parser.get_error_line(), parser.get_error_message()])
		return {}
	return parser.data


static func finite_number(value: Variant) -> bool:
	return (value is float or value is int) and is_finite(float(value))
