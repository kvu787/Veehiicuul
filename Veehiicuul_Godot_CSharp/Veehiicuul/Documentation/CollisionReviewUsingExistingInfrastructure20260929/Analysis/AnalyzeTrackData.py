"""Describes every collider data file that exists in the application folder.

Reads only existing track data. Reports the properties that decide which
detector index a track receives, and checks the input rules the detector
enforces at load time.

Usage: python AnalyzeTrackData.py
Writes TrackData.md next to this script and prints it.
"""

import json
import math
import statistics
import struct
from fractions import Fraction
from pathlib import Path

ANALYSIS_DIRECTORY = Path(__file__).resolve().parent
PROJECT_DIRECTORY = ANALYSIS_DIRECTORY.parents[2]
MEASUREMENTS = ANALYSIS_DIRECTORY.parent / "Measurements"

# Footprint printed by the native harness for all six Ribeye cars, after the
# manager shortens the front by 0.165.
VEHICLE = (-1.5000004, -2.9925215, 1.5000001, 3.0000002)
# Footprint hard-coded in the standalone harness (PerformanceAnalysis.CarBounds).
HARNESS_VEHICLE = (-1.5, -2.992522, 1.5, 3.0)
HARNESS_NAMES = {
    "Tracks/Ribeye/Ribeye_ColliderData.json": "Ribeye",
    "Tracks/TrackData/Basic_ColliderData.json": "Basic",
    "Tracks/TrackData/ColliderData.example.json": "Example",
    "Tracks/TrackData/Track001_ColliderData.json": "Track001",
    "Tracks/TrackData/Track002_ColliderData.json": "Track002",
    "Tracks/TrackData/Track003_ColliderData.json": "Track003",
    "Tracks/TrackData/Track004_ColliderData.json": "Track004",
    "Tracks/TrackData/Track005_ColliderData.json": "Track005",
    "Testyo/Track009_MiniComb4_ColliderData.json": "Track009MiniComb4",
}
CELL_SIZE_SCALE = 0.5
MAXIMUM_EXPANDED_CELLS = 65536
MAXIMUM_EXPANDED_REFERENCES = 1048576


def to_single(value):
    return struct.unpack("<f", struct.pack("<f", value))[0]


def render_table(headers, rows, right_aligned=()):
    widths = [len(header) for header in headers]
    for row in rows:
        for index, cell in enumerate(row):
            widths[index] = max(widths[index], len(cell))

    def render_row(cells):
        padded = []
        for index, cell in enumerate(cells):
            padded.append(cell.rjust(widths[index]) if index in right_aligned else cell.ljust(widths[index]))
        return "| " + " | ".join(padded) + " |"

    separators = []
    for index, width in enumerate(widths):
        separators.append("-" * (width - 1) + ":" if index in right_aligned else "-" * width)
    lines = [render_row(headers), "| " + " | ".join(separators) + " |"]
    lines.extend(render_row(row) for row in rows)
    return "\n".join(lines)


def load_outlines(path):
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    outlines = []
    inexact = 0
    for outline in data["Outlines"]:
        points = []
        for vertex in outline["Vertices"]:
            x, y = vertex["X"], vertex["Y"]
            single_x, single_y = to_single(x), to_single(y)
            if single_x != x or single_y != y:
                inexact += 1
            points.append((single_x, single_y))
        outlines.append(points)
    extra_keys = sorted(key for key in data if key != "Outlines")
    return outlines, inexact, extra_keys


def edges_of(outlines):
    edges = []
    for outline_index, points in enumerate(outlines):
        for index, a in enumerate(points):
            b = points[(index + 1) % len(points)]
            edges.append((a, b, outline_index, index))
    return edges


def orientation(a, b, c):
    value = ((Fraction(b[0]) - Fraction(a[0])) * (Fraction(c[1]) - Fraction(a[1]))
             - (Fraction(b[1]) - Fraction(a[1])) * (Fraction(c[0]) - Fraction(a[0])))
    return (value > 0) - (value < 0)


def on_segment(a, b, c):
    return (min(a[0], b[0]) <= c[0] <= max(a[0], b[0])
            and min(a[1], b[1]) <= c[1] <= max(a[1], b[1]))


def segments_intersect(a, b, c, d):
    if (max(a[0], b[0]) < min(c[0], d[0]) or max(c[0], d[0]) < min(a[0], b[0])
            or max(a[1], b[1]) < min(c[1], d[1]) or max(c[1], d[1]) < min(a[1], b[1])):
        return False
    first, second = orientation(a, b, c), orientation(a, b, d)
    third, fourth = orientation(c, d, a), orientation(c, d, b)
    if first == 0 and on_segment(a, b, c):
        return True
    if second == 0 and on_segment(a, b, d):
        return True
    if third == 0 and on_segment(c, d, a):
        return True
    if fourth == 0 and on_segment(c, d, b):
        return True
    return first * second < 0 and third * fourth < 0


def count_improper_intersections(outlines):
    """Counts edge pairs that touch or cross, excluding shared endpoints of neighbors."""
    edges = edges_of(outlines)
    order = sorted(range(len(edges)), key=lambda index: min(edges[index][0][0], edges[index][1][0]))
    crossings = 0
    for position, first_index in enumerate(order):
        a, b, first_outline, first_vertex = edges[first_index]
        first_maximum = max(a[0], b[0])
        for second_index in order[position + 1:]:
            c, d, second_outline, second_vertex = edges[second_index]
            if min(c[0], d[0]) > first_maximum:
                break
            if first_outline == second_outline:
                count = len(outlines[first_outline])
                difference = abs(first_vertex - second_vertex)
                if difference == 1 or difference == count - 1:
                    continue
            if segments_intersect(a, b, c, d):
                crossings += 1
    return crossings


def signed_area(points):
    total = 0.0
    for index, a in enumerate(points):
        b = points[(index + 1) % len(points)]
        total += a[0] * b[1] - b[0] * a[1]
    return total * 0.5


def single_bit_increment(value):
    bits = struct.unpack("<i", struct.pack("<f", value))[0]
    if value == 0.0:
        return struct.unpack("<f", struct.pack("<i", 1))[0]
    bits += 1 if value > 0.0 else -1
    return struct.unpack("<f", struct.pack("<i", bits))[0]


def expanded_grid_prediction(bounds, edges, vehicle):
    """Repeats the arithmetic of ExpandedGrid.TryCreate, including outward rounding."""
    vehicle = tuple(to_single(value) for value in vehicle)
    maximum_local_x = max(abs(vehicle[0]), abs(vehicle[2]))
    maximum_local_y = max(abs(vehicle[1]), abs(vehicle[3]))
    cell_size = min(vehicle[2] - vehicle[0], vehicle[3] - vehicle[1]) * CELL_SIZE_SCALE
    radius = math.sqrt(maximum_local_x * maximum_local_x + maximum_local_y * maximum_local_y)
    coordinate_magnitude = max(abs(bounds[0]), abs(bounds[2]), abs(bounds[1]), abs(bounds[3])) + radius
    magnitude = to_single(coordinate_magnitude)
    rounding_margin = 2.0 * (single_bit_increment(magnitude) - magnitude)
    radius = math.nextafter(radius + rounding_margin, math.inf)
    origin_x = math.nextafter(bounds[0] - radius, -math.inf)
    origin_y = math.nextafter(bounds[1] - radius, -math.inf)
    inverse = 1.0 / cell_size
    columns = math.floor((math.nextafter(bounds[2] + radius, math.inf) - origin_x) * inverse) + 1
    rows = math.floor((math.nextafter(bounds[3] + radius, math.inf) - origin_y) * inverse) + 1

    def cell(value, origin, count):
        return min(max(math.floor((value - origin) * inverse), 0), count - 1)

    references = 0
    occupied = set()
    for a, b, _, _ in edges:
        minimum_x = cell(math.nextafter(min(a[0], b[0]) - radius, -math.inf), origin_x, columns)
        minimum_y = cell(math.nextafter(min(a[1], b[1]) - radius, -math.inf), origin_y, rows)
        maximum_x = cell(math.nextafter(max(a[0], b[0]) + radius, math.inf), origin_x, columns)
        maximum_y = cell(math.nextafter(max(a[1], b[1]) + radius, math.inf), origin_y, rows)
        references += (maximum_x - minimum_x + 1) * (maximum_y - minimum_y + 1)
        if columns * rows <= 4 * MAXIMUM_EXPANDED_CELLS:
            for row in range(minimum_y, maximum_y + 1):
                for column in range(minimum_x, maximum_x + 1):
                    occupied.add(row * columns + column)
    return cell_size, radius, columns, rows, references, len(occupied)


def describe(path):
    outlines, inexact, extra_keys = load_outlines(path)
    edges = edges_of(outlines)
    lengths = [math.hypot(b[0] - a[0], b[1] - a[1]) for a, b, _, _ in edges]
    xs = [point[0] for points in outlines for point in points]
    ys = [point[1] for points in outlines for point in points]
    bounds = (min(xs), min(ys), max(xs), max(ys))
    cell_size, radius, columns, rows, references, occupied = expanded_grid_prediction(bounds, edges, VEHICLE)
    harness = expanded_grid_prediction(bounds, edges, HARNESS_VEHICLE)
    zero_length = sum(1 for a, b, _, _ in edges if a == b)
    short_outlines = sum(1 for points in outlines if len(points) < 3)
    vertices = [point for points in outlines for point in points]
    repeated = len(vertices) - len(set(vertices))
    longer_than_cell = sum(
        1 for a, b, _, _ in edges if abs(a[0] - b[0]) > cell_size or abs(a[1] - b[1]) > cell_size)
    return {
        "name": path.relative_to(PROJECT_DIRECTORY).as_posix(),
        "outlines": outlines,
        "edges": edges,
        "lengths": lengths,
        "bounds": bounds,
        "inexact": inexact,
        "extra_keys": extra_keys,
        "zero_length": zero_length,
        "short_outlines": short_outlines,
        "repeated": repeated,
        "crossings": count_improper_intersections(outlines),
        "cell_size": cell_size,
        "radius": radius,
        "columns": columns,
        "rows": rows,
        "references": references,
        "occupied": occupied,
        "harness": harness,
        "longer_than_cell": longer_than_cell,
    }


def load_harness_index(name):
    """Reads the index that the unmodified harness binary reported for a collider file."""
    path = MEASUREMENTS / "PerTrack" / (name + ".json")
    if name == "Ribeye":
        path = MEASUREMENTS / "KernelDefault1.json"
    if not path.exists():
        return None
    process = json.loads(path.read_text(encoding="utf-8-sig"))
    for result in process["Results"]:
        if result["Kind"] == "Index" and result["Name"] == "Ribeye":
            return result
    return None


def steady_state(samples):
    """Median of the final ten batches, which excludes batches timed before tier-up completed."""
    return statistics.median(samples[-10:])


def load_harness_queries(name):
    path = MEASUREMENTS / "PerTrack" / (name + ".json")
    if name == "Ribeye":
        path = MEASUREMENTS / "KernelDefault1.json"
    if not path.exists():
        return None
    process = json.loads(path.read_text(encoding="utf-8-sig"))
    return {result["Name"]: result for result in process["Results"] if result["Kind"] == "Query"}


def main():
    paths = sorted(PROJECT_DIRECTORY.glob("Tracks/**/*ColliderData*.json"))
    paths += sorted(PROJECT_DIRECTORY.glob("Testyo/*ColliderData*.json"))
    tracks = [describe(path) for path in paths]

    shape_rows = []
    for track in tracks:
        bounds = track["bounds"]
        counts = ", ".join(str(len(points)) for points in track["outlines"])
        shape_rows.append([
            track["name"],
            str(len(track["outlines"])),
            counts,
            str(len(track["edges"])),
            f"{bounds[2] - bounds[0]:.1f} x {bounds[3] - bounds[1]:.1f}",
            f"{min(track['lengths']):.3f}",
            f"{statistics.median(track['lengths']):.3f}",
            f"{max(track['lengths']):.3f}",
        ])

    rule_rows = []
    for track in tracks:
        areas = ", ".join("CCW" if signed_area(points) > 0 else "CW" for points in track["outlines"])
        rule_rows.append([
            track["name"],
            str(track["short_outlines"]),
            str(track["zero_length"]),
            str(track["repeated"]),
            str(track["crossings"]),
            str(track["inexact"]),
            areas,
            ", ".join(track["extra_keys"]) or "none",
        ])

    index_rows = []
    for track in tracks:
        cells = track["columns"] * track["rows"]
        fits = cells <= MAXIMUM_EXPANDED_CELLS and track["references"] <= MAXIMUM_EXPANDED_REFERENCES
        index_rows.append([
            track["name"],
            f"{track['columns']}x{track['rows']}",
            str(cells),
            f"{cells / MAXIMUM_EXPANDED_CELLS * 100.0:.1f}%",
            str(track["references"]),
            f"{track['references'] / cells:.2f}",
            str(track["longer_than_cell"]),
            "Expanded grid" if fits else "Center grid and tree",
        ])

    check_rows = []
    for track in tracks:
        name = HARNESS_NAMES[track["name"]]
        index = load_harness_index(name)
        if index is None:
            continue
        _, _, columns, rows, _, occupied = track["harness"]
        fits = columns * rows <= MAXIMUM_EXPANDED_CELLS
        expanded = index["OutlierEdgeCount"] == 0 and index["OrdinaryEdgeCount"] == index["EdgeCount"] \
            and index["UsesDenseGrid"]
        if fits:
            agrees = expanded and (columns, rows, occupied) == (
                index["GridColumnCount"], index["GridRowCount"], index["OccupiedCellCount"])
            predicted = f"{columns}x{rows}, {occupied} occupied"
        else:
            agrees = not expanded
            predicted = f"{columns}x{rows} exceeds the limit"
        check_rows.append([
            track["name"],
            predicted,
            f"{index['GridColumnCount']}x{index['GridRowCount']}, {index['OccupiedCellCount']} occupied",
            "Expanded grid" if expanded else f"Center grid, {index['OutlierEdgeCount']} tree edges",
            "Yes" if agrees else "No",
        ])

    cost_rows = []
    for track in tracks:
        name = HARNESS_NAMES[track["name"]]
        queries = load_harness_queries(name)
        if queries is None:
            continue
        cells = []
        for case in ["Ribeye/Mixed", "Ribeye/ClearInsideBounds", "Ribeye/Contact", "Ribeye/ExactVertexContact"]:
            result = queries[case]
            cells.append(f"{result['MedianNanoseconds']:.1f}")
            cells.append(f"{steady_state(result['IndexedNanoseconds']):.1f}")
        mixed = queries["Ribeye/Mixed"]
        slow = sum(1 for sample in mixed["IndexedNanoseconds"]
                   if sample > 1.5 * steady_state(mixed["IndexedNanoseconds"]))
        cost_rows.append([track["name"]] + cells + [str(slow)])

    text = "\n".join([
        "# Existing collider data",
        "",
        "Generated by `AnalyzeTrackData.py` from the collider files in the application folder.",
        f"Vehicle footprint: {VEHICLE}. Cell size scale: {CELL_SIZE_SCALE}.",
        "",
        "## Shape",
        "",
        render_table(
            ["Collider file", "Outlines", "Vertices per outline", "Edges", "Extent", "Shortest edge",
             "Median edge", "Longest edge"],
            shape_rows, right_aligned=(1, 3, 4, 5, 6, 7)),
        "",
        "## Input rules",
        "",
        "Crossings count edge pairs that touch or cross, excluding the shared endpoint of neighboring",
        "edges. Inexact values are JSON numbers that are not exactly representable in binary32.",
        "",
        render_table(
            ["Collider file", "Outlines under 3 vertices", "Zero-length edges", "Repeated vertices",
             "Crossings", "Inexact values", "Winding", "Other JSON keys"],
            rule_rows, right_aligned=(1, 2, 3, 4, 5)),
        "",
        "## Predicted index for the production footprint",
        "",
        "The prediction repeats the expanded-grid sizing arithmetic, including its outward rounding.",
        f"The limits are {MAXIMUM_EXPANDED_CELLS} cells and {MAXIMUM_EXPANDED_REFERENCES} references.",
        "",
        render_table(
            ["Collider file", "Grid", "Cells", "Share of cell limit", "References", "References per cell",
             "Edges longer than a cell", "Predicted index"],
            index_rows, right_aligned=(1, 2, 3, 4, 5, 6)),
        "",
        "## Prediction against the index the harness built",
        "",
        f"Both columns use the footprint hard-coded in the standalone harness: {HARNESS_VEHICLE}.",
        "",
        render_table(
            ["Collider file", "Predicted", "Harness reported", "Harness index", "Agrees"],
            check_rows),
        "",
        "## Query cost per collider file, one process each",
        "",
        "Nanoseconds per query. `Harness` is the median of all 15 batches, as the harness prints it.",
        "`Steady` is the median of the final ten batches. `Slow batches` counts mixed-workload batches",
        "that took more than 1.5 times the steady value.",
        "",
        render_table(
            ["Collider file", "Mixed harness", "Mixed steady", "Clear harness", "Clear steady",
             "Contact harness", "Contact steady", "Vertex harness", "Vertex steady", "Slow batches"],
            cost_rows, right_aligned=(1, 2, 3, 4, 5, 6, 7, 8, 9)),
        "",
    ])
    (ANALYSIS_DIRECTORY / "TrackData.md").write_text(text, encoding="utf-8", newline="\n")
    print(text)


if __name__ == "__main__":
    main()
