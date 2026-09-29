"""Summarizes one complete run of the from-scratch harness.

Reads the files that Run.ps1 wrote, copied into ../Measurements, and writes
Summary.md next to this script. Every table states the median over the
repeated processes; ranges are the smallest and largest process medians.

Usage: python SummarizeResults.py
"""

import json
import re
import statistics
from pathlib import Path

ANALYSIS_DIRECTORY = Path(__file__).resolve().parent
MEASUREMENTS = ANALYSIS_DIRECTORY.parent / "Measurements"


def render_table(headers, rows, right_aligned=()):
    widths = [max(3, len(header)) for header in headers]
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


def nanoseconds(value):
    if value is None:
        return "n/a"
    if value >= 1000.0:
        return f"{value / 1000.0:.2f} µs"
    if value >= 100.0:
        return f"{value:.0f} ns"
    return f"{value:.1f} ns"


def load(pattern, directory=None):
    folder = directory or MEASUREMENTS
    return [json.loads(path.read_text(encoding="utf-8-sig")) for path in sorted(folder.glob(pattern))]


class Suite:
    """Measurements of one suite across its repeated processes."""

    def __init__(self, pattern, directory=None):
        self.processes = load(pattern, directory)
        self.by_name = {}
        for process in self.processes:
            for measurement in process.get("Measurements", []):
                self.by_name.setdefault(measurement["Name"], []).append(measurement)

    def medians(self, name):
        return [measurement["Median"] for measurement in self.by_name.get(name, [])]

    def median(self, name):
        values = self.medians(name)
        return statistics.median(values) if values else None

    def spread(self, name):
        values = self.medians(name)
        return f"{nanoseconds(min(values))} to {nanoseconds(max(values))}" if values else "n/a"

    def fact(self, name, key):
        measurements = self.by_name.get(name)
        return measurements[0]["Facts"].get(key, "") if measurements else ""

    def first(self, name):
        measurements = self.by_name.get(name)
        return measurements[0] if measurements else None

    def unsettled(self):
        return sorted({
            name for name, measurements in self.by_name.items()
            if any(not measurement["WarmupSettled"] for measurement in measurements)})

    def allocating(self):
        return sorted({
            name for name, measurements in self.by_name.items()
            if any(measurement["BytesPerCall"] > 0 for measurement in measurements)})

    def widest_batch_spread(self):
        widest = 0.0
        for measurements in self.by_name.values():
            for measurement in measurements:
                samples = measurement["NanosecondsPerCall"]
                widest = max(widest, max(samples) / min(samples))
        return widest

    def widest_process_spread(self):
        widest, where = 0.0, ""
        for name, measurements in self.by_name.items():
            values = [measurement["Median"] for measurement in measurements]
            if len(values) > 1 and min(values) > 0 and max(values) / min(values) > widest:
                widest, where = max(values) / min(values), name
        return widest, where


TRACKS = ["Circuit", "CircuitFine", "CircuitCompact", "Scattered", "RectangularSegmented", "Rectangular",
          "CircuitFar", "CircuitWide", "CircuitVast", "ScatteredRemote", "Crowded"]
WORKLOADS = ["LapCenter", "LapBesideBarrier", "UniformClear", "UniformMixed", "NearMiss", "Contact", "Outside",
             "EmptyCell", "BusiestClearCell"]


def queries_tables(queries):
    index_rows = []
    for track in TRACKS:
        name = f"{track}/UniformMixed"
        if name not in queries.by_name:
            continue
        index_rows.append([
            track, queries.fact(name, "IndexKind"), queries.fact(name, "Edges"), queries.fact(name, "GridCells"),
            queries.fact(name, "References"), queries.fact(name, "LongEdges"),
        ])
    cost_rows = []
    for track in TRACKS:
        if f"{track}/UniformMixed" not in queries.by_name:
            continue
        cost_rows.append([track] + [nanoseconds(queries.median(f"{track}/{workload}")) for workload in WORKLOADS])
    range_rows = []
    for track in ["Circuit", "CircuitFine", "CircuitWide", "Crowded"]:
        for workload in ["LapCenter", "LapBesideBarrier", "NearMiss", "Contact"]:
            name = f"{track}/{workload}"
            if name in queries.by_name:
                range_rows.append([track, workload, nanoseconds(queries.median(name)), queries.spread(name)])
    linear_rows = []
    for track in TRACKS:
        for workload in ["LapCenter", "UniformMixed"]:
            indexed = queries.median(f"{track}/{workload}")
            linear = queries.median(f"{track}/{workload}/Linear")
            if indexed is None or linear is None:
                continue
            linear_rows.append([
                track, workload, nanoseconds(indexed), nanoseconds(linear),
                queries.spread(f"{track}/{workload}/Linear"), f"{linear / indexed:.1f}x"])
    busiest_rows = []
    for track in TRACKS:
        name = f"{track}/BusiestClearCell"
        if name in queries.by_name:
            busiest_rows.append([track, queries.fact(name, "Candidates"), nanoseconds(queries.median(name))])
    return "\n".join([
        "### Index chosen for each generated track",
        "",
        render_table(["Track", "Index", "Edges", "Grid cells", "References", "Long edges"], index_rows,
                     right_aligned=(2, 3, 4, 5)),
        "",
        "### Median time per query",
        "",
        render_table(["Track"] + WORKLOADS, cost_rows, right_aligned=tuple(range(1, len(WORKLOADS) + 1))),
        "",
        "### Range across processes",
        "",
        render_table(["Track", "Workload", "Median", "Range of process medians"], range_rows, right_aligned=(2, 3)),
        "",
        "### Indexed query against the detector's full scan",
        "",
        render_table(["Track", "Workload", "Indexed", "Full scan", "Full scan range", "Ratio"], linear_rows,
                     right_aligned=(2, 3, 4, 5)),
        "",
        "### Most expensive clear query found",
        "",
        render_table(["Track", "Candidate edges", "Median"], busiest_rows, right_aligned=(1, 2)),
        "",
    ])


def paths_table(paths):
    rows = []
    for name in ["LoopOnly", "OriginLookup/Reference", "OriginLookup/Small", "CenterLookup/ShiftedOrigin",
                 "FullScan/Oversized"]:
        full = f"Paths/{name}"
        first = paths.first(full)
        rows.append([
            name, f"{first['Contacts']}/{first['Calls']}", nanoseconds(paths.median(full)), paths.spread(full),
            nanoseconds(paths.median(full + "/Linear")),
        ])
    return render_table(["Route", "Contacts", "Median", "Range", "Full scan"], rows, right_aligned=(1, 2, 3, 4))


def extent_table(extent):
    rows = []
    names = sorted({name.split("/")[1] for name in extent.by_name}, key=lambda text: int(text.replace("Radius", "")))
    for radius in names:
        base = f"Extent/{radius}"
        rows.append([
            radius.replace("Radius", ""),
            extent.fact(f"{base}/LapCenter", "Edges"),
            extent.fact(f"{base}/LapCenter", "IndexKind"),
            extent.fact(f"{base}/LapCenter", "GridCells"),
            extent.fact(f"{base}/LapCenter", "LongEdges"),
            nanoseconds(extent.median(f"{base}/LapCenter")),
            nanoseconds(extent.median(f"{base}/UniformClear")),
            nanoseconds(extent.median(f"{base}/NearMiss")),
            nanoseconds(extent.median(f"{base}/Contact")),
        ])
    return render_table(
        ["Mean radius", "Edges", "Index", "Grid cells", "Long edges", "LapCenter", "UniformClear", "NearMiss",
         "Contact"],
        rows, right_aligned=(0, 1, 3, 4, 5, 6, 7, 8))


def spacing_table(spacing):
    rows = []
    for radius in ["Radius110", "Radius200"]:
        edges = sorted({name.split("/")[2] for name in spacing.by_name if name.split("/")[1] == radius},
                       key=lambda text: float(text.replace("Edge", "")))
        for edge in edges:
            base = f"Spacing/{radius}/{edge}"
            rows.append([
                radius.replace("Radius", ""), edge.replace("Edge", ""),
                spacing.fact(f"{base}/LapCenter", "Edges"),
                spacing.fact(f"{base}/LapCenter", "IndexKind"),
                spacing.fact(f"{base}/LapCenter", "References"),
                spacing.fact(f"{base}/LapCenter", "LongEdges"),
                nanoseconds(spacing.median(f"{base}/LapCenter")),
                nanoseconds(spacing.median(f"{base}/NearMiss")),
                nanoseconds(spacing.median(f"{base}/Contact")),
            ])
    return render_table(
        ["Mean radius", "Edge length", "Edges", "Index", "References", "Long edges", "LapCenter", "NearMiss",
         "Contact"],
        rows, right_aligned=(0, 1, 2, 4, 5, 6, 7, 8))


CELL_SIZE_TRACKS = ["Circuit", "CircuitFine", "CircuitWide", "CircuitVast", "ScatteredRemote", "Crowded"]


def cell_size_rows(cell_size, tracks):
    rows = []
    for track in tracks:
        scales = sorted({name.split("/")[2] for name in cell_size.by_name if name.split("/")[1] == track},
                        key=lambda text: float(text.replace("Scale", "")))
        for scale in scales:
            base = f"CellSize/{track}/{scale}"
            # Circuits are timed along a lap; obstacle fields on clear poses spread over the track.
            clear = f"{base}/LapCenter" if f"{base}/LapCenter" in cell_size.by_name else f"{base}/UniformClear"
            facts = cell_size.first(clear)["Facts"]
            has_long_edges = facts["LongEdges"] != "0"
            if facts["ExpandedGrid"] == "True":
                index = "expanded grid"
            elif facts["References"] == "0":
                index = "long-edge tree only"
            else:
                index = "dense center grid" if facts["DenseGrid"] == "True" else "sparse center grid"
                index += " with long-edge tree" if has_long_edges else ""
            rows.append([
                track, f"{float(scale.replace('Scale', '')):g}", f"{float(facts['CellSize']):g}", index,
                facts["GridCells"], facts["References"], facts["LongEdges"],
                nanoseconds(cell_size.median(clear)),
                nanoseconds(cell_size.median(f"{base}/NearMiss")),
                nanoseconds(cell_size.median(f"{base}/Contact")),
            ])
    return rows


def cell_size_table(cell_size, later=None):
    rows = cell_size_rows(cell_size, CELL_SIZE_TRACKS)
    if later is not None:
        # Tracks that joined this sweep after the timing run come from the later process.
        measured = {row[0] for row in rows}
        rows += cell_size_rows(later, [track for track in CELL_SIZE_TRACKS if track not in measured])
    return ("Clear is LapCenter on circuits and UniformClear on obstacle fields.\n\n" + render_table(
        ["Track", "Scale", "Cell size", "Index", "Grid cells", "References", "Long edges", "Clear",
         "NearMiss", "Contact"],
        rows, right_aligned=(1, 2, 4, 5, 6, 7, 8, 9)))


def fit_line(points):
    count = len(points)
    mean_x = sum(x for x, _ in points) / count
    mean_y = sum(y for _, y in points) / count
    slope = sum((x - mean_x) * (y - mean_y) for x, y in points) / sum((x - mean_x) ** 2 for x, _ in points)
    return slope, mean_y - slope * mean_x


def breakdown_tables(breakdown):
    rows = []
    rejected_points, tested_points = [], []
    counts = sorted({int(re.search(r"/(\d+)Edges", name).group(1)) for name in breakdown.by_name if "Edges" in name})
    for count in counts:
        rejected = breakdown.median(f"Breakdown/BoxRejected/{count}Edges")
        tested = breakdown.median(f"Breakdown/OrientationTested/{count}Edges")
        rejected_points.append((count, rejected))
        tested_points.append((count, tested))
        rows.append([str(count), nanoseconds(rejected), nanoseconds(tested)])
    rejected_slope, rejected_base = fit_line(rejected_points)
    tested_slope, tested_base = fit_line(tested_points)
    route_rows = []
    for name, label in [
        ("Breakdown/Orientation/FilterDecides", "Binary64 filter decides"),
        ("Breakdown/Orientation/ExactlyCollinear", "Exactly collinear, 128-bit integers"),
        ("Breakdown/Orientation/Span30Through128Bit", "Exponent span 30, 128-bit integers"),
        ("Breakdown/Orientation/Span60ThroughLargeIntegers", "Exponent span 60, large integers"),
        ("Breakdown/Orientation/SubnormalInputs", "Subnormal inputs"),
        ("Breakdown/ExactRoute/OrdinaryInputs", "Exact routine alone, ordinary inputs"),
    ]:
        first = breakdown.first(name)
        route_rows.append([label, nanoseconds(breakdown.median(name)), breakdown.spread(name),
                           f"{first['BytesPerCall']:.0f}"])
    return "\n".join([
        "### Cost per candidate edge",
        "",
        render_table(["Candidate edges", "All rejected by bounding box", "All need orientation tests"], rows,
                     right_aligned=(0, 1, 2)),
        "",
        f"Least-squares lines: bounding-box rejection costs {rejected_slope:.2f} ns per edge on a base of "
        f"{rejected_base:.1f} ns; orientation testing costs {tested_slope:.1f} ns per edge on a base of "
        f"{tested_base:.1f} ns.",
        "",
        "### Orientation routes, through the compiled accessor",
        "",
        render_table(["Route", "Median", "Range", "Bytes per call"], route_rows, right_aligned=(1, 2, 3)),
        "",
    ])


def construction_table():
    processes = load("Measure_construction_*.json")
    names = [entry["Name"] for entry in processes[0]["Constructions"]]
    rows = []
    for name in names:
        entries = [entry for process in processes for entry in process["Constructions"] if entry["Name"] == name]
        medians = [entry["MedianMilliseconds"] for entry in entries]
        rows.append([
            name, entries[0]["IndexKind"], str(entries[0]["Edges"]), str(entries[0]["GridCells"]),
            str(entries[0]["References"]),
            f"{statistics.median(medians):.3f} ms", f"{min(medians):.3f} to {max(medians):.3f} ms",
            str(entries[0]["AllocatedBytes"]), str(statistics.median([entry["RetainedBytes"] for entry in entries])),
        ])
    return render_table(
        ["Track", "Index", "Edges", "Grid cells", "References", "Median", "Range", "Allocated bytes",
         "Retained bytes"],
        rows, right_aligned=(2, 3, 4, 5, 6, 7, 8))


def single_call_table():
    processes = load("Measure_single_*.json")
    names = [entry["Name"] for entry in processes[0]["SingleCalls"]]
    rows = []
    for name in names:
        entries = [entry for process in processes for entry in process["SingleCalls"] if entry["Name"] == name]
        _, track, workload, evicted = name.split("/")
        net = [entry["NetMeanNanoseconds"] for entry in entries]
        rows.append([
            track, workload, evicted.replace("Evict", "").replace("MB", " MB"),
            nanoseconds(statistics.median(net)), f"{nanoseconds(min(net))} to {nanoseconds(max(net))}",
            nanoseconds(statistics.median([entry["TimerMeanNanoseconds"] for entry in entries])),
            nanoseconds(statistics.median([entry["Percentile99Nanoseconds"] for entry in entries])),
            nanoseconds(max(entry["MaximumNanoseconds"] for entry in entries)),
        ])
    return render_table(
        ["Track", "Workload", "Rewritten between calls", "Net mean", "Range of net means", "Timer mean",
         "99th percentile", "Largest"],
        rows, right_aligned=(2, 3, 4, 5, 6, 7))


def variants_table(default):
    variants = [("Default", default)]
    for label in ["NoDynamicProfile", "NoTiering", "EfficiencyCore", "Unpinned"]:
        variants.append((label, Suite(f"Variant_{label}_*.json")))
    rows = []
    for track in ["Circuit", "CircuitWide"]:
        for workload in ["LapCenter", "LapBesideBarrier", "UniformClear", "NearMiss", "Contact"]:
            name = f"{track}/{workload}"
            base = default.median(name)
            cells = [track, workload]
            for _, suite in variants:
                value = suite.median(name)
                cells.append(nanoseconds(value))
            for _, suite in variants[1:]:
                value = suite.median(name)
                cells.append(f"{value / base:.2f}x" if value and base else "n/a")
            rows.append(cells)
    headers = ["Track", "Workload"] + [label for label, _ in variants] + [
        label + " / default" for label, _ in variants[1:]]
    return render_table(headers, rows, right_aligned=tuple(range(2, len(headers))))


def cold_start_table(directory=None):
    rows = []
    for path in sorted((directory or MEASUREMENTS).glob("ColdStart_*.jsonl")):
        entries = [json.loads(line) for line in path.read_text(encoding="utf-8-sig").splitlines() if line.strip()]
        first = [entry["FirstConstructionMicroseconds"] / 1000.0 for entry in entries]
        second = [entry["SecondConstructionMicroseconds"] / 1000.0 for entry in entries]
        query = [entry["FirstQueryMicroseconds"] for entry in entries]
        calls = entries[0]["Calls"]
        later = {}
        for index, call in enumerate(calls):
            later[call] = statistics.median([entry["QueryMicroseconds"][index] for entry in entries])
        rows.append([
            entries[0]["Track"], entries[0]["IndexKind"], str(len(entries)),
            f"{statistics.median(first):.2f} ms", f"{min(first):.2f} to {max(first):.2f} ms",
            f"{statistics.median(second):.2f} ms",
            f"{statistics.median(query):.0f} µs", f"{min(query):.0f} to {max(query):.0f} µs",
            f"{later[10]:.1f} µs", f"{later[60]:.1f} µs", f"{later[300]:.1f} µs",
        ])
    return render_table(
        ["Track", "Index", "Processes", "First construction", "Range", "Second construction", "First query",
         "Range of first query", "Call 10", "Call 60", "Call 300"],
        rows, right_aligned=(2, 3, 4, 5, 6, 7, 8, 9, 10))


def native_tables(directory=None):
    measure = load("NativeMeasure_*.json", directory)
    if not measure:
        return "Not measured on a performance core. See the runs on efficiency cores below.\n"
    by_name = {}
    for process in measure:
        for entry in process["Measurements"]:
            by_name.setdefault(entry["Name"], []).append(entry["Value"])
    rows = []
    for workload in ["LapCenter", "Stationary", "NearMiss", "Contact"]:
        manager = [value["Median"] for value in by_name[f"Native/Manager/{workload}"]]
        detector = [value["Median"] for value in by_name[f"Native/Detector/{workload}"]]
        rows.append([
            workload, nanoseconds(statistics.median(manager)),
            f"{nanoseconds(min(manager))} to {nanoseconds(max(manager))}",
            nanoseconds(statistics.median(detector)),
            f"{nanoseconds(min(detector))} to {nanoseconds(max(detector))}",
            f"{by_name[f'Native/Manager/{workload}'][0]['BytesPerCall']:.0f}",
        ])
    construction = [value["Milliseconds"] for value in by_name["ManagerConstruction"]]
    construction_rows = [[
        str(len(construction)),
        f"{statistics.median([values[0] for values in construction]):.2f} ms",
        f"{min(values[0] for values in construction):.2f} to {max(values[0] for values in construction):.2f} ms",
        f"{statistics.median([values[1] for values in construction]):.2f} ms",
        f"{statistics.median([statistics.median(values[2:]) for values in construction]):.2f} ms",
    ]]
    frame_rows = []
    for label, pattern in [("Headless", "NativeFramesHeadless_*.json"), ("Window", "NativeFramesWindowed_*.json")]:
        processes = load(pattern, directory)
        if not processes:
            continue
        values = [process["Measurements"][0]["Value"] for process in processes]
        net = [value["NetMeanNanoseconds"] for value in values]
        frame_rows.append([
            label, str(len(values)), str(values[0]["Frames"]),
            f"{statistics.median([value['MedianFrameMilliseconds'] for value in values]):.3f} ms",
            nanoseconds(statistics.median(net)), f"{nanoseconds(min(net))} to {nanoseconds(max(net))}",
            nanoseconds(statistics.median([value["TimerMeanNanoseconds"] for value in values])),
            nanoseconds(statistics.median([value["Percentile99Nanoseconds"] for value in values])),
            nanoseconds(max(value["MaximumNanoseconds"] for value in values)),
        ])
    environment = measure[0]["Environment"]
    return "\n".join([
        f"Engine {environment['Engine']}; debug build of the engine: {environment['DebugBuild']}; "
        f"runtime {environment['Framework']}; priority {environment.get('Priority', 'not recorded')}; "
        f"logical processors allowed: {processors_of(environment.get('AffinityMask', 0))}.",
        "",
        "### Hot loops inside the exported release build",
        "",
        render_table(["Workload", "Manager", "Manager range", "Detector alone", "Detector range",
                      "Manager bytes per call"], rows, right_aligned=(1, 2, 3, 4, 5)),
        "",
        "### Constructing the manager with four vehicles",
        "",
        render_table(["Processes", "First", "Range of first", "Second", "Later, median"], construction_rows,
                     right_aligned=(0, 1, 2, 3, 4)),
        "",
        "### One query per rendered frame",
        "",
        render_table(["Display", "Processes", "Frames each", "Median frame time", "Net mean per query",
                      "Range of net means", "Timer mean", "99th percentile", "Largest"], frame_rows,
                     right_aligned=(1, 2, 3, 4, 5, 6, 7, 8)),
        "",
    ])


def processors_of(mask):
    """Logical processor numbers of an affinity mask, as ranges."""
    numbers = [index for index in range(64) if mask >> index & 1]
    if not numbers:
        return "not recorded"
    ranges, start, previous = [], numbers[0], numbers[0]
    for number in numbers[1:] + [None]:
        if number is not None and number == previous + 1:
            previous = number
            continue
        ranges.append(str(start) if start == previous else f"{start} to {previous}")
        if number is not None:
            start = previous = number
    return ", ".join(ranges)


def processor_use_table(directory):
    """How busy the machine was before and after each run, by kind of core."""
    placement = json.loads((directory / "Placement.json").read_text(encoding="utf-8-sig"))
    groups = [
        ("Performance cores", placement["PerformanceMask"]),
        ("Efficiency cores", placement["EfficiencyMask"]),
        ("Given to the harness", placement["Mask"]),
        ("Timing processor", 1 << placement["TimingProcessor"]),
    ]
    rows = []
    for prefix, label in [("", "Queries, engine, first use"), ("CellSize_", "Cell size"),
                          ("IndexFootprint_", "Index footprint")]:
        for moment in ["Before", "After"]:
            path = directory / f"{prefix}ProcessorUse{moment}.json"
            if not path.exists():
                continue
            record = json.loads(path.read_text(encoding="utf-8-sig"))
            cells = [label, moment, record["Time"][11:]]
            for counter in ["% Processor Utility", "% Processor Time"]:
                for _, mask in groups:
                    values = [value for index, value in enumerate(record[counter]) if mask >> index & 1]
                    cells.append(f"{statistics.mean(values):.1f}")
            rows.append(cells)
    headers = ["Run", "Sample", "Time"]
    headers += [f"Utility, {label.lower()}" for label, _ in groups]
    headers += [f"Busy time, {label.lower()}" for label, _ in groups]
    legend = "; ".join(f"{label.lower()}: logical processors {processors_of(mask)}" for label, mask in groups)
    return (f"Placement: {placement['Description']}; priority={placement['Priority']}.\n\n"
            f"Each sample lasts three seconds. Values are percentages averaged over the group. Utility counts "
            f"work done, so a core above its nominal frequency exceeds 100. Groups: {legend}.\n\n"
            + render_table(headers, rows, right_aligned=tuple(range(2, len(headers)))))


def quality_table(directory):
    """Settling and spread of every timing suite of a run."""
    rows = []
    for name in ["queries", "indexfootprint", "cellsize"]:
        suite = Suite(f"Measure_{name}_*.json", directory)
        if not suite.processes:
            continue
        spreads = []
        for measurements in suite.by_name.values():
            values = [measurement["Median"] for measurement in measurements]
            spreads.append(max(values) / min(values))
        widest_process, widest_where = suite.widest_process_spread()
        rows.append([
            name, str(len(suite.processes)), str(len(suite.by_name)), str(len(suite.unsettled())),
            f"{suite.widest_batch_spread():.2f}", f"{statistics.median(spreads):.3f}", f"{widest_process:.3f}",
            widest_where,
        ])
    return render_table(
        ["Suite", "Processes", "Measurements", "Warmup not settled in some process",
         "Widest batch ratio", "Median ratio between processes", "Widest ratio between processes", "Widest for"],
        rows, right_aligned=(1, 2, 3, 4, 5, 6))


def shared_queries_table(directory, primary):
    """The query suite of a later run against the timing run and against the earlier run on the same core."""
    later = Suite("Measure_queries_*.json", directory)
    earlier = Suite("Variant_EfficiencyCore_*.json")
    rows = []
    ratios_primary, ratios_earlier = [], []
    for name in later.by_name:
        if primary.median(name):
            ratios_primary.append(later.median(name) / primary.median(name))
        if earlier.median(name):
            ratios_earlier.append(later.median(name) / earlier.median(name))
    for track in ["Circuit", "CircuitFine", "CircuitWide", "Crowded"]:
        for workload in ["LapCenter", "LapBesideBarrier", "UniformClear", "NearMiss", "Contact"]:
            name = f"{track}/{workload}"
            if name not in later.by_name:
                continue
            value = later.median(name)
            same_core = earlier.median(name)
            rows.append([
                track, workload, nanoseconds(value), later.spread(name),
                nanoseconds(same_core), f"{value / same_core:.2f}x" if same_core else "n/a",
                nanoseconds(primary.median(name)), f"{value / primary.median(name):.2f}x",
            ])
    summary = (
        f"Processes: {len(later.processes)}. {later.processes[0]['Processor']}.\n\n"
        f"Across all {len(ratios_primary)} query measurements the ratio to the timing run on a performance core has "
        f"median {statistics.median(ratios_primary):.2f}, smallest {min(ratios_primary):.2f}, and largest "
        f"{max(ratios_primary):.2f}. Across the {len(ratios_earlier)} measurements that the timing run repeated on "
        f"the same efficiency core, at high priority, the ratio has median {statistics.median(ratios_earlier):.3f}, "
        f"smallest {min(ratios_earlier):.3f}, and largest {max(ratios_earlier):.3f}.\n\n"
        f"Measurements whose warmup did not settle: {', '.join(later.unsettled()) or 'none'}.")
    return summary + "\n\n" + render_table(
        ["Track", "Workload", "This run", "Range", "Same core, timing run", "Ratio", "Performance core, timing run",
         "Ratio"],
        rows, right_aligned=(2, 3, 4, 5, 6, 7))


def engine_against_console_table(directory):
    """The detector's hot loops inside the engine against the console program on the same core."""
    console = Suite("Measure_queries_*.json", directory)
    by_name = {}
    for process in load("NativeMeasure_*.json", directory):
        for entry in process["Measurements"]:
            by_name.setdefault(entry["Name"], []).append(entry["Value"])
    rows = []
    for workload in ["LapCenter", "NearMiss", "Contact"]:
        engine = statistics.median([value["Median"] for value in by_name[f"Native/Detector/{workload}"]])
        manager = statistics.median([value["Median"] for value in by_name[f"Native/Manager/{workload}"]])
        reference = console.median(f"Circuit/{workload}")
        rows.append([
            workload, nanoseconds(reference), nanoseconds(engine), f"{engine / reference:.2f}x",
            nanoseconds(manager), f"{manager / engine:.2f}x",
        ])
    return render_table(
        ["Workload", "Console program", "Detector in the engine", "Engine / console", "Manager in the engine",
         "Manager / detector"],
        rows, right_aligned=(1, 2, 3, 4, 5))


def index_footprint_table(directory):
    """One circuit indexed for the querying vehicle and for larger vehicles."""
    suite = Suite("Measure_indexfootprint_*.json", directory)
    rows = []
    for index, label in [("Own", "The vehicle itself"), ("OneAndAHalf", "1.5 times the vehicle"),
                         ("Twice", "Twice the vehicle")]:
        for workload in ["LapCenter", "NearMiss", "Contact"]:
            name = f"IndexFootprint/{index}/{workload}"
            own = suite.median(f"IndexFootprint/Own/{workload}")
            rows.append([
                label, suite.fact(name, "IndexFootprint"), f"{float(suite.fact(name, 'CellSize')):g}",
                suite.fact(name, "References"), workload, suite.fact(name, "MeanCandidates"),
                nanoseconds(suite.median(name)), suite.spread(name), f"{suite.median(name) / own:.2f}x",
            ])
    return (f"Processes: {len(suite.processes)}. The query footprint is the reference vehicle in every row.\n\n"
            + render_table(
                ["Track indexed for", "Index footprint", "Cell size", "References", "Workload",
                 "Mean candidates", "Median", "Range", "Against own index"],
                rows, right_aligned=(2, 3, 5, 6, 7, 8)))


def shared_machine_sections(primary):
    directory = MEASUREMENTS / "EfficiencyCoreRun"
    validation = json.loads((directory / "Validation.json").read_text(encoding="utf-8-sig"))
    checks = json.loads((directory / "NativeChecks.json").read_text(encoding="utf-8-sig"))
    return "\n".join([
        "### Conditions",
        "",
        optional(processor_use_table, directory),
        "",
        "### Measurement quality",
        "",
        optional(quality_table, directory),
        "",
        "### Checks repeated with the final harness",
        "",
        f"Console program: {len(validation['Suites'])} suites, {validation['Cases']} cases, "
        f"{validation['Failures']} failures, {validation['Threads']} threads. Engine-side checks: "
        f"{checks['Cases']} cases, {checks['Failures']} failures.",
        "",
        "### Console queries",
        "",
        optional(shared_queries_table, directory, primary),
        "",
        "### Inside the engine",
        "",
        optional(native_tables, directory),
        "### The engine against the console program, same core and conditions",
        "",
        optional(engine_against_console_table, directory),
        "",
        "### Index built for a larger vehicle",
        "",
        optional(index_footprint_table, directory),
        "",
        "### First use in a new process",
        "",
        optional(cold_start_table, directory),
        "",
        "### Cell size",
        "",
        optional(cell_size_table, Suite("Measure_cellsize_*.json", directory)),
        "",
    ])


def simulation_tables():
    simulation = json.loads((MEASUREMENTS / "Simulation.json").read_text(encoding="utf-8-sig"))
    wall_rows, obstacle_rows = [], []
    for result in simulation["Results"]:
        cells = [
            f"{result['StepPerFrame']:.3f}", f"{result['StepInVehicleLengths']:.2f}",
            f"{result['MissFraction'] * 100.0:.2f}%", f"{result['PredictedMissFraction'] * 100.0:.2f}%",
            f"{result['MeanOverlapAtDetection']:.3f}", f"{result['LargestOverlapAtDetection']:.3f}",
        ]
        if result["Barrier"] == "straight wall":
            wall_rows.append(
                [f"{result['IncidenceDegrees']:.0f}"] + cells
                + [str(result["FramesAfterMiss"]), str(result["ClearFramesAfterMiss"])])
        else:
            obstacle_rows.append([result["Barrier"].replace("square obstacle of side ", "")] + cells)
    return "\n".join([
        f"Vehicle length {simulation['VehicleLength']:.3f}, width {simulation['VehicleWidth']:.3f}; "
        f"{simulation['Trials']} trials per row.",
        "",
        "### Straight wall",
        "",
        render_table(["Incidence, degrees", "Step per frame", "Step in vehicle lengths", "Missed", "Predicted",
                      "Mean overlap at detection", "Largest overlap", "Frames inside the barrier",
                      "Of those, reported clear"],
                     wall_rows, right_aligned=tuple(range(9))),
        "",
        "### Square obstacle narrower than the vehicle, met head on",
        "",
        render_table(["Obstacle side", "Step per frame", "Step in vehicle lengths", "Missed", "Predicted",
                      "Mean overlap at detection", "Largest overlap"], obstacle_rows,
                     right_aligned=tuple(range(7))),
        "",
    ])


def validation_table():
    rows = []
    total_cases = 0
    for path, label in [(MEASUREMENTS / "Validation.json", "Console"), (MEASUREMENTS / "NativeChecks.json", "Engine"),
                        (MEASUREMENTS / "ValidationExtended.json", "Console, extended")]:
        if not path.exists():
            continue
        document = json.loads(path.read_text(encoding="utf-8-sig"))
        for suite in document["Suites"]:
            rows.append([label, suite["Name"], str(suite["Cases"]), str(suite["Failures"])])
            total_cases += suite["Cases"]
    return render_table(["Program", "Suite", "Cases", "Failures"], rows, right_aligned=(2, 3)), total_cases


def validation_groups():
    """Cases per group of suites, for the standard and the extended run."""
    documents = []
    for name in ["Validation.json", "ValidationExtended.json"]:
        path = MEASUREMENTS / name
        documents.append(json.loads(path.read_text(encoding="utf-8-sig")) if path.exists() else None)
    groups = {}
    for column, document in enumerate(documents):
        if document is None:
            continue
        for suite in document["Suites"]:
            group = suite["Name"].split(":")[0]
            entry = groups.setdefault(group, [0, 0, 0, 0, 0])
            entry[column] += suite["Cases"]
            entry[2 + column] += suite["Failures"]
            entry[4] += 1 if column == 0 else 0
    rows = []
    for group, (standard, extended, failed, failed_extended, suites) in groups.items():
        rows.append([group, str(suites), str(standard), str(extended), str(failed + failed_extended)])
    totals = [sum(entry[index] for entry in groups.values()) for index in range(5)]
    rows.append(["Total", str(totals[4]), str(totals[0]), str(totals[1]), str(totals[2] + totals[3])])
    return render_table(["Group", "Suites", "Cases, standard run", "Cases, extended run", "Failures"], rows,
                        right_aligned=(1, 2, 3, 4))


def reproducibility_table():
    """Medians of the timing run against the single later process kept in SecondRun."""
    names = ["Circuit/LapCenter", "Circuit/LapBesideBarrier", "Circuit/UniformClear", "Circuit/NearMiss",
             "Circuit/Contact", "CircuitFine/NearMiss", "CircuitWide/LapCenter", "CircuitWide/NearMiss",
             "Crowded/Contact"]
    final = Suite("Measure_queries_*.json")
    first = Suite("Measure_queries_*.json", MEASUREMENTS / "SecondRun")
    rows = []
    ratios = []
    for name in final.by_name:
        if name in first.by_name and first.median(name) > 0:
            ratios.append(final.median(name) / first.median(name))
    for name in names:
        rows.append([
            name, nanoseconds(first.median(name)), first.spread(name), nanoseconds(final.median(name)),
            final.spread(name), f"{(final.median(name) / first.median(name) - 1.0) * 100.0:+.1f}%",
        ])
    summary = (f"Across all {len(ratios)} query measurements the ratio of the timing run to the later process "
               f"has median {statistics.median(ratios):.3f}, smallest {min(ratios):.3f}, and largest "
               f"{max(ratios):.3f}.")
    return render_table(["Measurement", "Later process", "Later range", "Timing run", "Timing run range",
                         "Timing run against later"], rows, right_aligned=(1, 2, 3, 4, 5)) + "\n\n" + summary


def mutation_areas(name="MutationResults.json"):
    path = MEASUREMENTS / name
    if not path.exists():
        return "Not run."
    areas = {}
    for result in json.loads(path.read_text(encoding="utf-8-sig")):
        if result["Name"] == "Unmodified":
            continue
        entry = areas.setdefault(result["Area"], [0, 0, []])
        entry[0] += 1
        if result["Outcome"] == "Fail":
            entry[1] += 1
        else:
            entry[2].append(result["Name"])
    rows = [[area, str(injected), str(detected), ", ".join(missed) or "none"]
            for area, (injected, detected, missed) in areas.items()]
    rows.append(["Total", str(sum(entry[0] for entry in areas.values())),
                 str(sum(entry[1] for entry in areas.values())), ""])
    return render_table(["Area", "Injected", "Detected", "Not detected"], rows, right_aligned=(1, 2))


def mutation_table(name="MutationResults.json"):
    path = MEASUREMENTS / name
    if not path.exists():
        return "Not run."
    results = json.loads(path.read_text(encoding="utf-8-sig"))
    rows = []
    detected = 0
    for result in results:
        if result["Name"] == "Unmodified":
            continue
        caught = result["Outcome"] == "Fail"
        detected += caught
        suites = result["FailedSuites"] if isinstance(result["FailedSuites"], list) else [result["FailedSuites"]]
        rows.append([
            result["Name"], result["Area"], "Detected" if caught else "Missed", str(result["Failures"]),
            str(len([suite for suite in suites if suite])),
        ])
    return (f"Defects injected: {len(rows)}. Detected: {detected}. Missed: {len(rows) - detected}.\n\n"
            + render_table(["Injected defect", "Area", "Outcome", "Failed checks", "Failed suites"], rows,
                           right_aligned=(3, 4)))


def optional(section, *arguments):
    """Renders a section, or says that its files are missing."""
    try:
        return section(*arguments)
    except (KeyError, IndexError, FileNotFoundError, statistics.StatisticsError, TypeError, ValueError) as error:
        return f"Not measured ({type(error).__name__}: {error})."


def main():
    queries = Suite("Measure_queries_*.json")
    paths = Suite("Measure_paths_*.json")
    extent = Suite("Measure_extent_*.json")
    spacing = Suite("Measure_spacing_*.json")
    cell_size = Suite("Measure_cellsize_*.json")
    breakdown = Suite("Measure_breakdown_*.json")
    suites = [queries, paths, extent, spacing, cell_size, breakdown]
    unsettled = sorted({name for suite in suites for name in suite.unsettled()})
    allocating = sorted({name for suite in suites for name in suite.allocating()})
    widest_batch = max(suite.widest_batch_spread() for suite in suites)
    widest_process, widest_where = max((suite.widest_process_spread() for suite in suites), key=lambda pair: pair[0])
    validation, cases = validation_table()
    environment = queries.processes[0]

    text = "\n".join([
        "# From-scratch run summary",
        "",
        "Generated by `SummarizeResults.py` from the files in `Measurements`.",
        "",
        f"Runtime {environment['Framework']} on {environment['OperatingSystem']}. {environment['Processor']}.",
        f"Processes per timing suite: {len(queries.processes)}. Batches per measurement: "
        f"{environment['SampleCount']}.",
        "",
        "## Measurement quality",
        "",
        f"- Measurements whose warmup did not settle: {', '.join(unsettled) if unsettled else 'none'}.",
        f"- Measurements that allocated: {', '.join(allocating) if allocating else 'none'}.",
        f"- Widest ratio of slowest to fastest batch within one measurement: {widest_batch:.3f}.",
        f"- Widest ratio between process medians: {widest_process:.3f}, for {widest_where}.",
        "",
        "## Agreement with a later process",
        "",
        optional(reproducibility_table),
        "",
        "## Validation",
        "",
        optional(validation_groups),
        "",
        f"Cases in total, every program and run: {cases}.",
        "",
        validation,
        "",
        "## Mutation check of the harness",
        "",
        "### Detector, judged by the console program",
        "",
        mutation_areas(),
        "",
        mutation_table(),
        "",
        "### Manager and footprint, judged by the engine-side checks",
        "",
        mutation_areas("NativeMutationResults.json"),
        "",
        mutation_table("NativeMutationResults.json"),
        "",
        "## Queries",
        "",
        queries_tables(queries),
        "## Routes through the expanded grid",
        "",
        paths_table(paths),
        "",
        "## Track extent",
        "",
        extent_table(extent),
        "",
        "## Edge length",
        "",
        spacing_table(spacing),
        "",
        "## Cell size",
        "",
        cell_size_table(cell_size, Suite("Measure_cellsize_*.json", MEASUREMENTS / "SecondRun")),
        "",
        "## Cost breakdown",
        "",
        breakdown_tables(breakdown),
        "## Construction",
        "",
        optional(construction_table),
        "",
        "## Single calls with displaced caches",
        "",
        optional(single_call_table),
        "",
        "## Runtime settings and cores",
        "",
        optional(variants_table, queries),
        "",
        "## First use in a new process",
        "",
        optional(cold_start_table),
        "",
        "## Inside the engine",
        "",
        optional(native_tables),
        "## Runs on efficiency cores while the machine was in use",
        "",
        optional(shared_machine_sections, queries),
        "## Sampling once per frame",
        "",
        optional(simulation_tables),
    ])
    (ANALYSIS_DIRECTORY / "Summary.md").write_text(text, encoding="utf-8", newline="\n")
    print(text)


if __name__ == "__main__":
    main()
