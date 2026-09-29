"""Summarizes the collision measurements saved by the existing verification harness.

Reads only files that the existing infrastructure produced:
- Today's kernel samples in ../Measurements (KernelDefault*.json, KernelNoTiering*.json).
- Today's native Godot summaries in ../Measurements (Native*.txt).
- Historical samples in ../../CollisionOptimization20260929/Measurements.
- Historical samples in ../../CollisionPerformance20260929/Measurements.

Usage: python SummarizeMeasurements.py
Writes Summary.md next to this script and prints it.
"""

import json
import re
import statistics
from pathlib import Path

ANALYSIS_DIRECTORY = Path(__file__).resolve().parent
REPORT_DIRECTORY = ANALYSIS_DIRECTORY.parent
TODAY = REPORT_DIRECTORY / "Measurements"
OPTIMIZATION = REPORT_DIRECTORY.parent / "CollisionOptimization20260929" / "Measurements"
PERFORMANCE = REPORT_DIRECTORY.parent / "CollisionPerformance20260929" / "Measurements"

QUERY_CASES = [
    "Ribeye/Mixed",
    "Ribeye/ClearInsideBounds",
    "Ribeye/Contact",
    "Ribeye/ExactVertexContact",
    "Ribeye/Spawn",
    "Ribeye/OutsideBounds",
    "Ribeye/HugeContainingRectangle",
]

NATIVE_CASES = [
    "OptimizedMovingQuery",
    "OptimizedApplyAndQuery",
    "BaselineApplyAndQuery",
    "ApplyOnly",
    "OptimizedContactApplyAndQuery",
    "BaselineContactApplyAndQuery",
    "OptimizedStationarySpawn",
    "BaselineStationarySpawn",
    "OptimizedStationaryContact",
    "BaselineStationaryContact",
]


def load_processes(directory, pattern):
    return [json.loads(path.read_text(encoding="utf-8-sig")) for path in sorted(directory.glob(pattern))]


def results_by_name(process, kind):
    return {result["Name"]: result for result in process["Results"] if result["Kind"] == kind}


def format_nanoseconds(value):
    if value >= 1000.0:
        return f"{value / 1000.0:.2f} µs"
    return f"{value:.1f} ns"


def render_table(headers, rows, right_aligned=()):
    widths = [len(header) for header in headers]
    for row in rows:
        for index, cell in enumerate(row):
            widths[index] = max(widths[index], len(cell))
    lines = []

    def render_row(cells):
        padded = []
        for index, cell in enumerate(cells):
            padded.append(cell.rjust(widths[index]) if index in right_aligned else cell.ljust(widths[index]))
        return "| " + " | ".join(padded) + " |"

    lines.append(render_row(headers))
    separators = []
    for index, width in enumerate(widths):
        separators.append("-" * (width - 1) + ":" if index in right_aligned else "-" * width)
    lines.append("| " + " | ".join(separators) + " |")
    for row in rows:
        lines.append(render_row(row))
    return "\n".join(lines)


def process_medians(processes, case, field="MedianNanoseconds"):
    values = []
    for process in processes:
        result = results_by_name(process, "Query").get(case)
        if result is not None:
            values.append(result[field])
    return values


def summarize(values):
    return statistics.median(values), min(values), max(values)


def coefficient_of_variation(samples):
    mean = statistics.fmean(samples)
    return statistics.pstdev(samples) / mean if mean else 0.0


def kernel_comparison(today, historical, no_tiering):
    rows = []
    for case in QUERY_CASES:
        today_median, today_low, today_high = summarize(process_medians(today, case))
        old_median, old_low, old_high = summarize(process_medians(historical, case))
        untiered_median, untiered_low, untiered_high = summarize(process_medians(no_tiering, case))
        rows.append([
            case,
            format_nanoseconds(old_median),
            f"{format_nanoseconds(old_low)} to {format_nanoseconds(old_high)}",
            format_nanoseconds(today_median),
            f"{format_nanoseconds(today_low)} to {format_nanoseconds(today_high)}",
            f"{(today_median / old_median - 1.0) * 100.0:+.1f}%",
            format_nanoseconds(untiered_median),
            f"{untiered_median / today_median:.2f}x",
        ])
    return render_table(
        ["Ribeye query", "Saved median", "Saved range", "Today median", "Today range",
         "Today vs saved", "No tiering", "No tiering / default"],
        rows, right_aligned=(1, 2, 3, 4, 5, 6, 7))


def linear_comparison(today):
    rows = []
    for case in QUERY_CASES:
        indexed, _, _ = summarize(process_medians(today, case))
        linear, linear_low, linear_high = summarize(process_medians(today, case, "LinearMedianNanoseconds"))
        rows.append([
            case,
            format_nanoseconds(indexed),
            format_nanoseconds(linear),
            f"{format_nanoseconds(linear_low)} to {format_nanoseconds(linear_high)}",
            f"{linear / indexed:.2f}x",
        ])
    return render_table(
        ["Ribeye query", "Indexed median", "Linear median", "Linear range", "Linear / indexed"],
        rows, right_aligned=(1, 2, 3, 4))


def dispersion(today):
    rows = []
    for case in QUERY_CASES:
        coefficients = []
        spreads = []
        for process in today:
            result = results_by_name(process, "Query")[case]
            samples = result["IndexedNanoseconds"]
            coefficients.append(coefficient_of_variation(samples))
            spreads.append(max(samples) / min(samples))
        rows.append([
            case,
            f"{statistics.median(coefficients) * 100.0:.1f}%",
            f"{max(coefficients) * 100.0:.1f}%",
            f"{statistics.median(spreads):.2f}x",
            f"{max(spreads):.2f}x",
        ])
    return render_table(
        ["Ribeye query", "Median batch CV", "Worst batch CV", "Median max/min", "Worst max/min"],
        rows, right_aligned=(1, 2, 3, 4))


def cell_size_sweep(today, historical):
    rows = []
    for cell_size in ["1.5", "3", "4.5", "6", "9", "12"]:
        case = f"Ribeye/CellSize{cell_size}"
        today_median, today_low, today_high = summarize(process_medians(today, case))
        old_median, _, _ = summarize(process_medians(historical, case))
        index = results_by_name(today[0], "Index")[case]
        rows.append([
            cell_size,
            f"{index['GridColumnCount']}x{index['GridRowCount']}",
            str(index["OccupiedCellCount"]),
            format_nanoseconds(old_median),
            format_nanoseconds(today_median),
            f"{format_nanoseconds(today_low)} to {format_nanoseconds(today_high)}",
        ])
    return render_table(
        ["Cell size", "Grid", "Occupied cells", "Saved mixed median", "Today mixed median", "Today range"],
        rows, right_aligned=(0, 1, 2, 3, 4, 5))


def construction(today, historical, no_tiering):
    rows = []
    for label, processes in [("Saved, default tiering", historical), ("Today, default tiering", today),
                             ("Today, tiering disabled", no_tiering)]:
        medians = []
        allocations = set()
        for process in processes:
            result = results_by_name(process, "Construction")["Ribeye"]
            medians.append(statistics.median(result["Milliseconds"]))
            allocations.update(result["AllocatedBytes"])
        median, low, high = summarize(medians)
        rows.append([
            label,
            str(len(processes)),
            f"{median:.3f} ms",
            f"{low:.3f} to {high:.3f} ms",
            ", ".join(str(value) for value in sorted(allocations)),
        ])
    return render_table(
        ["Ribeye construction", "Processes", "Median of medians", "Range of medians", "Allocated bytes per build"],
        rows, right_aligned=(1, 2, 3, 4))


NATIVE_PATTERN = re.compile(
    r"^NATIVE (?P<name>\w+): median=(?P<median>[\d.]+) ns; batch-p95=(?P<tail>[\d.]+) ns; "
    r"bytes/query=(?P<bytes>[\d.]+); checksum=(?P<checksum>\d+)")


def load_native(directory):
    processes = []
    for path in sorted(directory.glob("Native*.txt")):
        process = {}
        for line in path.read_text(encoding="utf-8-sig").splitlines():
            match = NATIVE_PATTERN.match(line.strip())
            if match:
                process[match["name"]] = {
                    "median": float(match["median"]),
                    "tail": float(match["tail"]),
                    "bytes": float(match["bytes"]),
                    "checksum": int(match["checksum"]),
                }
        processes.append(process)
    return processes


def native_comparison(today, historical):
    rows = []
    for case in NATIVE_CASES:
        today_median, today_low, today_high = summarize([process[case]["median"] for process in today])
        old_median, old_low, old_high = summarize([process[case]["median"] for process in historical])
        worst_bytes = max(process[case]["bytes"] for process in today)
        rows.append([
            case,
            format_nanoseconds(old_median),
            f"{format_nanoseconds(old_low)} to {format_nanoseconds(old_high)}",
            format_nanoseconds(today_median),
            f"{format_nanoseconds(today_low)} to {format_nanoseconds(today_high)}",
            f"{(today_median / old_median - 1.0) * 100.0:+.1f}%",
            f"{worst_bytes:.1f}",
        ])
    return render_table(
        ["Native operation", "Saved median", "Saved range", "Today median", "Today range",
         "Today vs saved", "Bytes per call"],
        rows, right_aligned=(1, 2, 3, 4, 5, 6))


def native_checksums(today):
    rows = []
    for case in ["OptimizedMovingQuery", "OptimizedApplyAndQuery", "BaselineApplyAndQuery"]:
        rows.append([case] + [str(process[case]["checksum"]) for process in today])
    headers = ["Native operation"] + [f"Process {index + 1} contacts" for index in range(len(today))]
    return render_table(headers, rows, right_aligned=tuple(range(1, len(headers))))


def all_fixtures(path):
    process = json.loads(path.read_text(encoding="utf-8-sig"))
    indexes = results_by_name(process, "Index")
    queries = results_by_name(process, "Query")
    constructions = results_by_name(process, "Construction")
    rows = []
    for name, query_name in [
        ("Ribeye", "Ribeye/Mixed"),
        ("Track001", "Track001/Mixed"),
        ("TiledRibeye/1", "TiledRibeye/1/Clear"),
        ("TiledRibeye/16", "TiledRibeye/16/Clear"),
        ("TiledRibeye/256", "TiledRibeye/256/Clear"),
        ("LongEdges/256", "LongEdges/256/Clear"),
        ("LongEdges/4096", "LongEdges/4096/Clear"),
    ]:
        index = indexes[name]
        query = queries[query_name]
        build = constructions[name]
        rows.append([
            name,
            str(index["EdgeCount"]),
            f"{index['GridColumnCount']}x{index['GridRowCount']}",
            "Dense" if index["UsesDenseGrid"] else "Sparse",
            str(index["OutlierEdgeCount"]),
            query_name.split("/")[-1],
            format_nanoseconds(query["MedianNanoseconds"]),
            format_nanoseconds(query["LinearMedianNanoseconds"]) if query["LinearMedianNanoseconds"] else "not run",
            f"{statistics.median(build['Milliseconds']):.3f} ms",
            str(build["AllocatedBytes"][0]),
        ])
    return render_table(
        ["Fixture", "Edges", "Grid", "Storage", "Long edges", "Workload", "Indexed median", "Linear median",
         "Build median", "Build bytes"],
        rows, right_aligned=(1, 2, 4, 6, 7, 8, 9))


def track001_cases(path):
    process = json.loads(path.read_text(encoding="utf-8-sig"))
    queries = results_by_name(process, "Query")
    rows = []
    for case in ["Mixed", "ClearInsideBounds", "Contact", "ExactVertexContact", "OutsideBounds",
                 "HugeContainingRectangle"]:
        ribeye = queries[f"Ribeye/{case}"]
        track = queries[f"Track001/{case}"]
        rows.append([
            case,
            f"{ribeye['Contacts']}/{ribeye['Queries']}",
            format_nanoseconds(ribeye["MedianNanoseconds"]),
            f"{track['Contacts']}/{track['Queries']}",
            format_nanoseconds(track["MedianNanoseconds"]),
            f"{track['BytesPerQuery']:.1f}",
        ])
    return render_table(
        ["Workload", "Ribeye contacts", "Ribeye median", "Track001 contacts", "Track001 median",
         "Track001 bytes per query"],
        rows, right_aligned=(1, 2, 3, 4, 5))


def first_report_comparison(first_report, historical, today):
    rows = []
    for case in ["Ribeye/Mixed", "Ribeye/ClearInsideBounds", "Ribeye/Contact", "Ribeye/ExactVertexContact",
                 "Ribeye/Spawn", "Ribeye/OutsideBounds"]:
        first, _, _ = summarize(process_medians(first_report, case))
        saved, _, _ = summarize(process_medians(historical, case))
        current, _, _ = summarize(process_medians(today, case))
        rows.append([
            case,
            format_nanoseconds(first),
            format_nanoseconds(saved),
            format_nanoseconds(current),
            f"{first / current:.2f}x",
        ])
    return render_table(
        ["Ribeye query", "Center grid, c59af97", "Expanded grid, saved", "Expanded grid, today",
         "Center grid / today"],
        rows, right_aligned=(1, 2, 3, 4))


def main():
    today = load_processes(TODAY, "KernelDefault*.json")
    no_tiering = load_processes(TODAY, "KernelNoTiering*.json")
    historical = load_processes(OPTIMIZATION, "KernelAfter*.json")
    first_report = load_processes(PERFORMANCE, "PerformanceDefault*.json")
    native_today = load_native(TODAY)
    native_historical = load_native(OPTIMIZATION)

    sections = [
        "# Measurement summary",
        "",
        "Generated by `SummarizeMeasurements.py` from files written by the existing harness.",
        f"Kernel processes: {len(today)} today with default tiering, {len(no_tiering)} today with tiering "
        f"disabled, {len(historical)} saved. Native processes: {len(native_today)} today, "
        f"{len(native_historical)} saved.",
        "",
        "## Kernel queries, today against saved",
        "",
        kernel_comparison(today, historical, no_tiering),
        "",
        "## Kernel queries, indexed against linear scan",
        "",
        linear_comparison(today),
        "",
        "## Dispersion of the 15 timed batches within each process",
        "",
        dispersion(today),
        "",
        "## Expanded-grid cell-size sweep",
        "",
        cell_size_sweep(today, historical),
        "",
        "## Index construction",
        "",
        construction(today, historical, no_tiering),
        "",
        "## Native Godot operations, today against saved",
        "",
        native_comparison(native_today, native_historical),
        "",
        "## Native timed-loop contact counts",
        "",
        native_checksums(native_today),
        "",
        "## Extended fixtures, one process",
        "",
        all_fixtures(TODAY / "AllFixtures.json"),
        "",
        "## Ribeye against Track001, one process",
        "",
        track001_cases(TODAY / "AllFixtures.json"),
        "",
        "## Three generations of Ribeye kernel medians",
        "",
        first_report_comparison(first_report, historical, today),
        "",
    ]
    text = "\n".join(sections)
    (ANALYSIS_DIRECTORY / "Summary.md").write_text(text, encoding="utf-8", newline="\n")
    print(text)


if __name__ == "__main__":
    main()
