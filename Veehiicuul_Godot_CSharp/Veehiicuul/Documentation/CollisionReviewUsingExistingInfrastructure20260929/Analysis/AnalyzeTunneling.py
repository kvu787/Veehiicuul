"""Estimates when the sampled collision test can miss a barrier on Ribeye.

Reads the existing Ribeye collider data and car settings. Geometry here uses
binary64 arithmetic and is an estimate; it does not replace the exact detector.

The detector tests one pose per frame. A car that moves farther than its own
length between two frames can clear a barrier edge without any sampled pose
touching it. This script measures how much straight track Ribeye offers and
converts that into speeds and frame times.

Usage: python AnalyzeTunneling.py
Writes Tunneling.md next to this script and prints it.
"""

import json
import math
from pathlib import Path

ANALYSIS_DIRECTORY = Path(__file__).resolve().parent
PROJECT_DIRECTORY = ANALYSIS_DIRECTORY.parents[2]
COLLIDER = PROJECT_DIRECTORY / "Tracks" / "Ribeye" / "Ribeye_ColliderData.json"
SETTINGS = PROJECT_DIRECTORY / "Tracks" / "Ribeye" / "Ribeye_Settings.json"

# Production footprint in collision coordinates; model front is minimum Y.
VEHICLE = (-1.5000004, -2.9925215, 1.5000001, 3.0000002)
# Authored spawn pose in collision coordinates, as used by the existing harness.
SPAWN = (117.841125, 61.20298, 1.793361)
GRID_CELL = 8.0
POSITION_STEP = 3.0
HEADING_COUNT = 72
MARCH_STEP = 0.25
FRAME_RATES = [30, 60, 120, 240, 500, 1000]


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


class Track:
    def __init__(self, outlines):
        self.outlines = outlines
        self.edges = []
        for points in outlines:
            for index, a in enumerate(points):
                self.edges.append((a, points[(index + 1) % len(points)]))
        xs = [point[0] for points in outlines for point in points]
        ys = [point[1] for points in outlines for point in points]
        self.bounds = (min(xs), min(ys), max(xs), max(ys))
        self.cells = {}
        for index, (a, b) in enumerate(self.edges):
            for cell in self.cells_for(min(a[0], b[0]), min(a[1], b[1]), max(a[0], b[0]), max(a[1], b[1])):
                self.cells.setdefault(cell, []).append(index)

    @staticmethod
    def cells_for(minimum_x, minimum_y, maximum_x, maximum_y):
        for row in range(math.floor(minimum_y / GRID_CELL), math.floor(maximum_y / GRID_CELL) + 1):
            for column in range(math.floor(minimum_x / GRID_CELL), math.floor(maximum_x / GRID_CELL) + 1):
                yield (column, row)

    def inside_parity(self, x, y):
        crossings = 0
        for a, b in self.edges:
            if (a[1] > y) != (b[1] > y):
                if x < a[0] + (y - a[1]) * (b[0] - a[0]) / (b[1] - a[1]):
                    crossings += 1
        return crossings & 1

    def collides(self, x, y, heading):
        sine, cosine = math.sin(heading), math.cos(heading)
        corners = []
        for local_x, local_y in ((VEHICLE[0], VEHICLE[1]), (VEHICLE[2], VEHICLE[1]),
                                 (VEHICLE[2], VEHICLE[3]), (VEHICLE[0], VEHICLE[3])):
            corners.append((x + local_x * cosine + local_y * sine, y - local_x * sine + local_y * cosine))
        minimum_x = min(corner[0] for corner in corners)
        maximum_x = max(corner[0] for corner in corners)
        minimum_y = min(corner[1] for corner in corners)
        maximum_y = max(corner[1] for corner in corners)
        seen = set()
        for cell in self.cells_for(minimum_x, minimum_y, maximum_x, maximum_y):
            for index in self.cells.get(cell, ()):
                if index in seen:
                    continue
                seen.add(index)
                a, b = self.edges[index]
                if (max(a[0], b[0]) < minimum_x or min(a[0], b[0]) > maximum_x
                        or max(a[1], b[1]) < minimum_y or min(a[1], b[1]) > maximum_y):
                    continue
                for side in range(4):
                    if segments_intersect(a, b, corners[side], corners[(side + 1) % 4]):
                        return True
        return False


def orientation(a, b, c):
    value = (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])
    return (value > 0) - (value < 0)


def segments_intersect(a, b, c, d):
    first, second = orientation(a, b, c), orientation(a, b, d)
    third, fourth = orientation(c, d, a), orientation(c, d, b)
    return first != second and third != fourth


def forward_of(heading):
    # Model front is local minimum Y; clockwise yaw maps local (0, -1) to this direction.
    return -math.sin(heading), -math.cos(heading)


def straight_run(track, x, y, heading, limit=1000.0):
    forward_x, forward_y = forward_of(heading)
    distance = 0.0
    while distance < limit:
        distance += MARCH_STEP
        if track.collides(x + forward_x * distance, y + forward_y * distance, heading):
            return distance - MARCH_STEP
    return limit


def percentile(values, fraction):
    ordered = sorted(values)
    return ordered[min(len(ordered) - 1, math.ceil((len(ordered) - 1) * fraction))]


def main():
    data = json.loads(COLLIDER.read_text(encoding="utf-8-sig"))
    outlines = [[(vertex["X"], vertex["Y"]) for vertex in outline["Vertices"]] for outline in data["Outlines"]]
    track = Track(outlines)
    settings = json.loads(SETTINGS.read_text(encoding="utf-8-sig"))
    length = VEHICLE[3] - VEHICLE[1]
    width = VEHICLE[2] - VEHICLE[0]

    spawn_parity = track.inside_parity(SPAWN[0], SPAWN[1])
    spawn_run = straight_run(track, SPAWN[0], SPAWN[1], SPAWN[2])

    runs = []
    clear_poses = 0
    positions = 0
    x = track.bounds[0]
    while x <= track.bounds[2]:
        y = track.bounds[1]
        while y <= track.bounds[3]:
            if track.inside_parity(x, y) == spawn_parity:
                positions += 1
                for heading_index in range(HEADING_COUNT):
                    heading = heading_index * math.tau / HEADING_COUNT
                    if track.collides(x, y, heading):
                        continue
                    clear_poses += 1
                    runs.append((straight_run(track, x, y, heading), x, y, heading))
            y += POSITION_STEP
        x += POSITION_STEP

    runs.sort(reverse=True)
    longest = runs[0]
    distances = [run[0] for run in runs]

    run_rows = [
        ["Drivable sample positions", str(positions)],
        ["Clear sampled poses", str(clear_poses)],
        ["Median straight run", f"{percentile(distances, 0.5):.1f}"],
        ["95th percentile straight run", f"{percentile(distances, 0.95):.1f}"],
        ["Longest straight run", f"{longest[0]:.1f}"],
        ["Longest run start", f"({longest[1]:.1f}, {longest[2]:.1f}), yaw {longest[3]:.3f}"],
        ["Straight run from spawn", f"{spawn_run:.1f}"],
    ]

    threshold_rows = []
    for rate in FRAME_RATES:
        speed = length * rate
        threshold_rows.append([
            str(rate),
            f"{1000.0 / rate:.2f} ms",
            f"{speed:.0f}",
        ])

    car_rows = []
    for car in settings["Cars"]:
        forward = car["Dynamic"]["AccelerationMap"]["Forward"]
        limiter = car["Dynamic"].get("VelocityLimiter", 0)
        top_speed = math.sqrt(2.0 * forward * longest[0])
        spawn_speed = math.sqrt(2.0 * forward * spawn_run)
        frame_time = length / top_speed
        cells = [
            car["GameObjectName"],
            f"{forward:g}",
            "none" if limiter == 0 else f"{limiter:g}",
            f"{top_speed:.0f}",
            f"{frame_time * 1000.0:.1f} ms",
            f"{1.0 / frame_time:.0f}",
            f"{spawn_speed:.0f}",
            f"{length / spawn_speed * 1000.0:.1f} ms",
        ]
        car_rows.append(cells)

    distance_rows = []
    for car in settings["Cars"]:
        forward = car["Dynamic"]["AccelerationMap"]["Forward"]
        cells = [car["GameObjectName"]]
        for rate in [30, 60, 120, 240]:
            speed = length * rate
            distance = speed * speed / (2.0 * forward)
            reachable = distance <= longest[0]
            cells.append(f"{distance:.0f}" + (" (fits)" if reachable else ""))
        distance_rows.append(cells)

    text = "\n".join([
        "# Sampling limits on Ribeye",
        "",
        "Generated by `AnalyzeTunneling.py` from the existing Ribeye collider data and settings.",
        f"Vehicle length {length:.4f}, width {width:.4f}. Position step {POSITION_STEP}, "
        f"{HEADING_COUNT} headings, march step {MARCH_STEP}.",
        "",
        "## Straight track available",
        "",
        "A straight run is the distance a clear car can travel along its own heading before",
        "its perimeter touches a barrier.",
        "",
        render_table(["Quantity", "Value"], run_rows, right_aligned=(1,)),
        "",
        "## Speed at which one frame moves the car by its own length",
        "",
        render_table(["Frames per second", "Frame time", "Speed, units per second"], threshold_rows,
                     right_aligned=(0, 1, 2)),
        "",
        "## Each car accelerating from rest",
        "",
        "`Top speed` assumes full forward acceleration over the longest straight run.",
        "`Frame time` is the longest frame that still keeps one step shorter than the car.",
        "",
        render_table(
            ["Car", "Forward acceleration", "Velocity limiter", "Top speed, longest run", "Frame time",
             "Frames per second", "Top speed, spawn run", "Frame time, spawn run"],
            car_rows, right_aligned=(1, 2, 3, 4, 5, 6, 7)),
        "",
        "## Straight distance needed to reach the one-length-per-frame speed",
        "",
        f"`(fits)` marks distances no longer than the longest straight run of {longest[0]:.1f}.",
        "",
        render_table(["Car", "At 30 FPS", "At 60 FPS", "At 120 FPS", "At 240 FPS"], distance_rows,
                     right_aligned=(1, 2, 3, 4)),
        "",
    ])
    (ANALYSIS_DIRECTORY / "Tunneling.md").write_text(text, encoding="utf-8", newline="\n")
    print(text)


if __name__ == "__main__":
    main()
