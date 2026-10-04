class_name TrackCollisionDetector
extends RefCounted
## Perimeter contacts only, including touching endpoints and collinear overlap.
## Coordinates are Blender X/Y = Godot X/negative Z, with clockwise yaw.
## No containment test or swept collision is added to the original behavior.

const MAXIMUM_CELL_COUNT: int = 65536
const MAXIMUM_REFERENCE_COUNT: int = 1048576

var valid: bool = false
var edge_start := PackedVector2Array()
var edge_end := PackedVector2Array()
var edge_bounds: Array[Vector4] = []
var all_bounds := Vector4(INF, INF, -INF, -INF)
var cell_size: float
var origin_x: float
var origin_y: float
var inverse_cell_size: float
var maximum_local_x: float
var maximum_local_y: float
var column_count: int = 0
var row_count: int = 0
var occupied_cell_count: int = 0
var cell_offsets := PackedInt32Array()
var cell_counts := PackedInt32Array()
var edge_references := PackedInt32Array()
var uses_expanded_grid: bool = false

# A bounds tree avoids unbounded grids for sparse or very large tracks.
var tree_bounds: Array[Vector4] = []
var tree_left := PackedInt32Array()
var tree_right := PackedInt32Array()
var tree_start := PackedInt32Array()
var tree_count := PackedInt32Array()
var tree_edges := PackedInt32Array()


func build(data: Dictionary, representative: Vector4, cell_size_scale: float = 0.5) -> bool:
	valid = false
	if not valid_bounds(representative) or not is_finite(cell_size_scale) or cell_size_scale <= 0.0:
		push_error("Vehicle bounds and grid scale must be finite and positive.")
		return false
	if not data.get("Outlines") is Array or data["Outlines"].is_empty():
		push_error("Collider data must contain outlines.")
		return false
	edge_start.clear()
	edge_end.clear()
	edge_bounds.clear()
	all_bounds = Vector4(INF, INF, -INF, -INF)
	for outline: Variant in data["Outlines"]:
		if not outline is Dictionary or not outline.get("Vertices") is Array or outline["Vertices"].size() < 3:
			push_error("Each collider outline needs at least three vertices.")
			return false
		var vertices := PackedVector2Array()
		for vertex: Variant in outline["Vertices"]:
			if not vertex is Dictionary or not JsonUtility.finite_number(vertex.get("X")) or not JsonUtility.finite_number(vertex.get("Y")):
				push_error("Collider coordinates must be finite numbers.")
				return false
			vertices.append(Vector2(vertex["X"], vertex["Y"]))
		for index in range(vertices.size()):
			var first := vertices[index]
			var second := vertices[(index + 1) % vertices.size()]
			if first == second or not first.is_finite() or not second.is_finite():
				push_error("Collider contains a zero-length or nonfinite edge.")
				return false
			edge_start.append(first)
			edge_end.append(second)
			var bounds := segment_bounds(first, second)
			edge_bounds.append(bounds)
			all_bounds = combine_bounds(all_bounds, bounds)
	cell_size = minf(representative.z - representative.x, representative.w - representative.y) * cell_size_scale
	maximum_local_x = maxf(absf(representative.x), absf(representative.z))
	maximum_local_y = maxf(absf(representative.y), absf(representative.w))
	uses_expanded_grid = _build_expanded_grid()
	tree_bounds.clear()
	tree_left.clear()
	tree_right.clear()
	tree_start.clear()
	tree_count.clear()
	tree_edges.clear()
	if not uses_expanded_grid:
		column_count = 0
		row_count = 0
		occupied_cell_count = 0
		var ids: Array[int] = []
		for index in range(edge_start.size()):
			ids.append(index)
		_build_tree(ids)
	valid = true
	return true


func _build_expanded_grid() -> bool:
	var radius := sqrt(maximum_local_x * maximum_local_x + maximum_local_y * maximum_local_y)
	var magnitude := maxf(maxf(absf(all_bounds.x), absf(all_bounds.z)), maxf(absf(all_bounds.y), absf(all_bounds.w))) + radius
	if magnitude >= 1e12 or not is_finite(radius):
		return false
	# Conservative float32 corner-rounding allowance, applied only to broad phase.
	radius += maxf(1e-12, magnitude * pow(2.0, -22.0))
	origin_x = all_bounds.x - radius
	origin_y = all_bounds.y - radius
	inverse_cell_size = 1.0 / cell_size
	var columns := floorf((all_bounds.z + radius - origin_x) * inverse_cell_size) + 1.0
	var rows := floorf((all_bounds.w + radius - origin_y) * inverse_cell_size) + 1.0
	if columns <= 0 or rows <= 0 or columns * rows > MAXIMUM_CELL_COUNT:
		return false
	column_count = int(columns)
	row_count = int(rows)
	cell_counts.resize(column_count * row_count)
	cell_counts.fill(0)
	var coverage: Array[Vector4i] = []
	var references: int = 0
	for bounds: Vector4 in edge_bounds:
		var minimum_column := clampi(int(floorf((bounds.x - radius - origin_x) * inverse_cell_size)), 0, column_count - 1)
		var minimum_row := clampi(int(floorf((bounds.y - radius - origin_y) * inverse_cell_size)), 0, row_count - 1)
		var maximum_column := clampi(int(floorf((bounds.z + radius - origin_x) * inverse_cell_size)), 0, column_count - 1)
		var maximum_row := clampi(int(floorf((bounds.w + radius - origin_y) * inverse_cell_size)), 0, row_count - 1)
		references += (maximum_column - minimum_column + 1) * (maximum_row - minimum_row + 1)
		if references > MAXIMUM_REFERENCE_COUNT:
			return false
		coverage.append(Vector4i(minimum_column, minimum_row, maximum_column, maximum_row))
		for row in range(minimum_row, maximum_row + 1):
			for column in range(minimum_column, maximum_column + 1):
				cell_counts[row * column_count + column] += 1
	cell_offsets.resize(cell_counts.size())
	var offset: int = 0
	occupied_cell_count = 0
	for index in range(cell_counts.size()):
		cell_offsets[index] = offset
		offset += cell_counts[index]
		if cell_counts[index] != 0:
			occupied_cell_count += 1
	edge_references.resize(offset)
	var cursors := cell_offsets.duplicate()
	for index in range(coverage.size()):
		var cells := coverage[index]
		for row in range(cells.y, cells.w + 1):
			for column in range(cells.x, cells.z + 1):
				var cell := row * column_count + column
				edge_references[cursors[cell]] = index
				cursors[cell] += 1
	return true


func is_colliding(bounds: Vector4, position: Vector2, clockwise_yaw: float) -> bool:
	if not valid or not valid_bounds(bounds) or not position.is_finite() or not is_finite(clockwise_yaw):
		push_error("Collision query requires valid finite geometry and pose.")
		return false
	var supports_grid := uses_expanded_grid and bounds.x >= -maximum_local_x and bounds.z <= maximum_local_x and bounds.y >= -maximum_local_y and bounds.w <= maximum_local_y
	var cell: int = -1
	if supports_grid:
		var column := (position.x - origin_x) * inverse_cell_size
		var row := (position.y - origin_y) * inverse_cell_size
		if column < 0 or row < 0 or column >= column_count or row >= row_count:
			return false
		cell = int(row) * column_count + int(column)
		if cell_counts[cell] == 0:
			return false
	var cosine := cos(clockwise_yaw)
	var sine := sin(clockwise_yaw)
	var first := transform_corner(bounds.x, bounds.y, position, cosine, sine)
	var second := transform_corner(bounds.z, bounds.y, position, cosine, sine)
	var third := transform_corner(bounds.z, bounds.w, position, cosine, sine)
	var fourth := transform_corner(bounds.x, bounds.w, position, cosine, sine)
	var rectangle_bounds := combine_bounds(segment_bounds(first, second), segment_bounds(third, fourth))
	if not overlaps(all_bounds, rectangle_bounds):
		return false
	if supports_grid:
		for reference in range(cell_offsets[cell], cell_offsets[cell] + cell_counts[cell]):
			if _edge_intersects(edge_references[reference], rectangle_bounds, first, second, third, fourth):
				return true
		return false
	if not tree_bounds.is_empty():
		return _query_tree(0, rectangle_bounds, first, second, third, fourth)
	for index in range(edge_start.size()):
		if _edge_intersects(index, rectangle_bounds, first, second, third, fourth):
			return true
	return false


func _edge_intersects(index: int, bounds: Vector4, first: Vector2, second: Vector2, third: Vector2, fourth: Vector2) -> bool:
	if not overlaps(edge_bounds[index], bounds):
		return false
	var a := edge_start[index]
	var b := edge_end[index]
	return segments_intersect(a, b, first, second) or segments_intersect(a, b, second, third) or segments_intersect(a, b, third, fourth) or segments_intersect(a, b, fourth, first)


func _build_tree(ids: Array[int]) -> int:
	var index := tree_bounds.size()
	var bounds := Vector4(INF, INF, -INF, -INF)
	for edge: int in ids:
		bounds = combine_bounds(bounds, edge_bounds[edge])
	tree_bounds.append(bounds)
	tree_left.append(-1)
	tree_right.append(-1)
	tree_start.append(tree_edges.size())
	tree_count.append(0)
	if ids.size() <= 8:
		tree_count[index] = ids.size()
		for edge: int in ids:
			tree_edges.append(edge)
		return index
	var use_x := bounds.z - bounds.x >= bounds.w - bounds.y
	ids.sort_custom(_compare_centers.bind(use_x))
	var middle := ids.size() >> 1
	var left_ids: Array[int] = []
	var right_ids: Array[int] = []
	left_ids.assign(ids.slice(0, middle))
	right_ids.assign(ids.slice(middle))
	tree_left[index] = _build_tree(left_ids)
	tree_right[index] = _build_tree(right_ids)
	return index


func _compare_centers(first: int, second: int, use_x: bool) -> bool:
	var a := edge_bounds[first]
	var b := edge_bounds[second]
	var center_a := a.x + a.z if use_x else a.y + a.w
	var center_b := b.x + b.z if use_x else b.y + b.w
	return first < second if center_a == center_b else center_a < center_b


func _query_tree(index: int, bounds: Vector4, first: Vector2, second: Vector2, third: Vector2, fourth: Vector2) -> bool:
	if not overlaps(tree_bounds[index], bounds):
		return false
	if tree_count[index] != 0:
		for offset in range(tree_start[index], tree_start[index] + tree_count[index]):
			if _edge_intersects(tree_edges[offset], bounds, first, second, third, fourth):
				return true
		return false
	return _query_tree(tree_left[index], bounds, first, second, third, fourth) or _query_tree(tree_right[index], bounds, first, second, third, fourth)


static func valid_bounds(bounds: Vector4) -> bool:
	return bounds.is_finite() and bounds.x < bounds.z and bounds.y < bounds.w


static func transform_corner(x: float, y: float, position: Vector2, cosine: float, sine: float) -> Vector2:
	return Vector2(position.x + x * cosine + y * sine, position.y - x * sine + y * cosine)


static func segment_bounds(a: Vector2, b: Vector2) -> Vector4:
	return Vector4(minf(a.x, b.x), minf(a.y, b.y), maxf(a.x, b.x), maxf(a.y, b.y))


static func combine_bounds(a: Vector4, b: Vector4) -> Vector4:
	return Vector4(minf(a.x, b.x), minf(a.y, b.y), maxf(a.z, b.z), maxf(a.w, b.w))


static func overlaps(a: Vector4, b: Vector4) -> bool:
	return a.x <= b.z and a.z >= b.x and a.y <= b.w and a.w >= b.y


static func segments_intersect(a: Vector2, b: Vector2, c: Vector2, d: Vector2) -> bool:
	if not overlaps(segment_bounds(a, b), segment_bounds(c, d)):
		return false
	var abc := orientation_sign(a, b, c)
	var abd := orientation_sign(a, b, d)
	if abc != 0 and abc == abd:
		return false
	if (abc == 0 and on_segment(a, b, c)) or (abd == 0 and on_segment(a, b, d)):
		return true
	var cda := orientation_sign(c, d, a)
	var cdb := orientation_sign(c, d, b)
	if (cda == 0 and on_segment(c, d, a)) or (cdb == 0 and on_segment(c, d, b)):
		return true
	return abc != abd and cda != cdb


static func orientation_sign(a: Vector2, b: Vector2, c: Vector2) -> int:
	# Scalar GDScript float arithmetic is double precision; Vector2 is float32.
	var acx: float = float(a.x) - float(c.x)
	var bcx: float = float(b.x) - float(c.x)
	var acy: float = float(a.y) - float(c.y)
	var bcy: float = float(b.y) - float(c.y)
	var determinant := acx * bcy - acy * bcx
	return 1 if determinant > 0.0 else (-1 if determinant < 0.0 else 0)


static func on_segment(a: Vector2, b: Vector2, point: Vector2) -> bool:
	return point.x >= minf(a.x, b.x) and point.x <= maxf(a.x, b.x) and point.y >= minf(a.y, b.y) and point.y <= maxf(a.y, b.y)
