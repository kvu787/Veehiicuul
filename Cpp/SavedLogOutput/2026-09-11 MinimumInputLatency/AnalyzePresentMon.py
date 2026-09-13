"""Reproduce capture statistics and chart. Requires numpy, pandas, reportlab.
Run: python AnalyzePresentMon.py
Input CSV/log files are read only. Generated files stay beside this script.
"""
from pathlib import Path
import csv
import hashlib
import json
import math
import platform
import re
import numpy as np
import pandas as pd

ROOT = Path(__file__).resolve().parent
SESSIONS = ["2026-09-11_13-59-13", "2026-09-11_15-18-36"]
CATEGORICAL = ["Application", "ProcessID", "SwapChainAddress", "PresentRuntime",
               "SyncInterval", "PresentFlags", "AllowsTearing", "PresentMode"]
PERCENTILES = [0, 1, 5, 50, 90, 95, 99, 99.9, 99.99, 100]

def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()

def describe(values):
    values = np.asarray(values, dtype=float)
    valid = values[np.isfinite(values)]
    result = {"Count": len(values), "Available": len(valid), "Missing": len(values)-len(valid)}
    if len(valid):
        result.update(Mean=float(valid.mean()), StandardDeviation=float(valid.std()),
                      Sum=float(valid.sum()), Percentiles={
                          str(p): float(v) for p, v in zip(PERCENTILES, np.percentile(valid, PERCENTILES, method="linear"))})
    return result

def analyze(name):
    directory = ROOT / name
    paths = sorted(directory.glob("PresentMon part *.csv"))
    assert [int(re.search(r"part (\d+)", p.name)[1]) for p in paths] == list(range(1, 46))
    with paths[0].open(encoding="utf-8-sig", newline="") as stream:
        columns = next(csv.reader(stream))
    frames, manifest, boundaries = [], [], []
    previous_time = None
    total_rows = 0
    for number, path in enumerate(paths):
        frame = pd.read_csv(path, encoding="utf-8-sig", header=0 if number == 0 else None,
                            names=columns, dtype={column: "category" for column in CATEGORICAL},
                            na_values=["NA"], on_bad_lines="error")
        assert len(frame) > 0
        # Every part after 01 begins with data; never drop its first record.
        if previous_time is not None:
            gap = float(frame.TimeInMs.iloc[0] - previous_time)
            recorded = float(frame.MsBetweenPresents.iloc[0])
            boundaries.append({"Part": number + 1, "GlobalRow": total_rows + 1,
                               "TimestampGapMs": gap, "RecordedIntervalMs": recorded,
                               "DifferenceMs": gap - recorded})
        previous_time = frame.TimeInMs.iloc[-1]
        manifest.append({"File": str(path.relative_to(ROOT)).replace("\\", "/"),
                         "Bytes": path.stat().st_size, "DataRows": len(frame),
                         "FirstTimeInMs": float(frame.TimeInMs.iloc[0]),
                         "LastTimeInMs": float(frame.TimeInMs.iloc[-1]), "Sha256": digest(path)})
        total_rows += len(frame)
        frames.append(frame)
    data = pd.concat(frames, ignore_index=True)
    times = data.TimeInMs.to_numpy()
    differences = np.diff(times)
    intervals = data.MsBetweenPresents.to_numpy()[1:]
    assert np.all(np.isfinite(times)) and np.all(differences > 0), "Nonmonotonic/duplicate times"
    assert np.all(np.isfinite(intervals)) and np.all(intervals > 0), "Invalid frame intervals"
    residual = np.abs(differences - intervals)
    assert residual.max() < 0.00021, "Unexplained timestamp/interval discrepancy"
    duration = float((times[-1] - times[0]) / 1000)
    relative = (times - times[0]) / 1000
    seconds = int(math.floor(duration))
    windows = []
    for second in range(seconds):
        lo, hi = np.searchsorted(relative, [second, second+1], side="left")
        local = data.MsBetweenPresents.to_numpy()[lo:hi]
        windows.append({"Capture": name, "StartSecond": second, "EndSecond": second+1,
                        "Presents": int(hi-lo), "MeanIntervalMs": float(local.mean()),
                        "P99IntervalMs": float(np.percentile(local, 99)),
                        "MaximumIntervalMs": float(local.max())})
    blocks = []
    for start in range(0, seconds, 30):
        end = min(start+30, seconds)
        count = sum(w["Presents"] for w in windows[start:end])
        blocks.append({"StartSecond": start, "EndSecond": end, "Presents": count, "FPS": count/(end-start)})
    ordered = np.sort(intervals)
    lows = {str(percent): float(1000 / ordered[-math.ceil(len(ordered)*percent/100):].mean())
            for percent in [1, 0.1]}
    top_indices = np.argsort(intervals)[-10:][::-1] + 1
    outliers = []
    ends = np.cumsum([m["DataRows"] for m in manifest])
    for index in top_indices:
        part_index = int(np.searchsorted(ends, index, side="right"))
        preceding = 0 if part_index == 0 else int(ends[part_index-1])
        entry = {"Capture": name, "ElapsedSecond": float(relative[index]),
                 "DataRow": int(index+1), "Part": part_index+1,
                 "FileLine": int(index-preceding+1+(part_index == 0))}
        for column in ["MsBetweenPresents","MsCPUBusy","MsCPUWait","MsGPUTime","MsGPUBusy",
                       "MsGPUWait","MsInPresentAPI","MsUntilDisplayed"]:
            value = data[column].iloc[index]
            entry[column] = None if pd.isna(value) else float(value)
        outliers.append(entry)
    log_path = directory / "PresentMon.log"
    log = log_path.read_text(encoding="utf-8-sig")
    matches = re.findall(r"BufferFillPct=([\d.]+)% BuffersInUse=(\d+) TotalBuffers=(\d+) EventsLost=(\d+) BuffersLost=(\d+), OverflowedPresents=(\d+)", log)
    status_keys = ["BufferFillPct","BuffersInUse","TotalBuffers","EventsLost","BuffersLost","OverflowedPresents"]
    status = {"Samples": len(matches), "Maximum": {key: max(float(row[i]) for row in matches)
               for i, key in enumerate(status_keys)} if matches else {},
              "OtherLines": [line for line in log.splitlines() if not line.startswith("[ETW Status]") and line.strip()],
              "Sha256": digest(log_path)}
    fps = np.array([w["Presents"] for w in windows])
    result = {
        "Capture": name, "Rows": len(data), "Intervals": len(intervals),
        "FirstTimeInMs": float(times[0]), "LastTimeInMs": float(times[-1]),
        "DurationSeconds": duration, "AverageFPS": len(intervals)/duration,
        "AverageFPSFromIntervals": 1000/float(intervals.mean()),
        "FirstIntervalExcludedMs": float(data.MsBetweenPresents.iloc[0]),
        "FrameIntervalsMs": describe(intervals), "LowFPS": lows,
        "Thresholds": {str(threshold): {
            "IntervalsAbove": int(np.sum(intervals > threshold)),
            "PercentAbove": float(np.mean(intervals > threshold)*100)}
            for threshold in [0.2, 0.25, 0.5, 1, 2, 5, 10, 16.6667]},
        "IntervalsAtOrBelow0Point2MsPercent": float(np.mean(intervals <= 0.2)*100),
        "ElapsedTimeInIntervalsAtOrBelow0Point2MsPercent": float(intervals[intervals <= 0.2].sum()/intervals.sum()*100),
        "CompleteOneSecondWindows": describe(fps),
        "SecondsAtLeast5000FPS": int(np.sum(fps >= 5000)),
        "SecondsBelow5000FPS": [w for w in windows if w["Presents"] < 5000],
        "ThirtySecondBlocks": blocks,
        "Categories": {column: {str(k): int(v) for k, v in data[column].value_counts(dropna=False).items()}
                       for column in CATEGORICAL},
        "CategoryTransitions": {column: int(np.sum(data[column].to_numpy()[1:] != data[column].to_numpy()[:-1]))
                                for column in CATEGORICAL},
        "Metrics": {column: describe(data[column]) for column in columns if column not in CATEGORICAL},
        "Validation": {"StrictlyIncreasingTimestamps": True, "MaximumIntervalTimestampResidualMs": float(residual.max()),
                       "BoundaryCount": len(boundaries), "Boundaries": boundaries},
        "CaptureLog": status, "InputManifest": manifest, "WorstIntervals": outliers,
    }
    print(json.dumps({k: result[k] for k in ["Capture","Rows","DurationSeconds","AverageFPS","LowFPS",
                                           "FrameIntervalsMs","Thresholds","CompleteOneSecondWindows",
                                           "SecondsAtLeast5000FPS","ThirtySecondBlocks","Categories","CaptureLog"]}, indent=2))
    return result, windows, outliers

def save_table(name, rows):
    with (ROOT / name).open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)

def draw_chart(results, windows):
    # ReportLab is used as a standard charting library; no image generation.
    from reportlab.graphics.shapes import Drawing, String, Rect, Line
    from reportlab.graphics.charts.lineplots import LinePlot
    from reportlab.graphics import renderSVG
    from reportlab.lib import colors
    drawing = Drawing(1000, 670)
    drawing.add(Rect(0, 0, 1000, 670, fillColor=colors.white, strokeColor=None))
    drawing.add(String(65, 634, "Over 5,000 FPS with MinimizeInputLatency", fontName="Helvetica-Bold", fontSize=20))
    drawing.add(String(65, 610, "2560 x 1440 borderless fullscreen. Same running game; captures 79 min 23 s apart.", fontSize=12))
    shades = [colors.HexColor("#126B9E"), colors.HexColor("#13806B")]
    for index, result in enumerate(results):
        group = [w for w in windows if w["Capture"] == result["Capture"]]
        base = 350 if index == 0 else 90
        drawing.add(String(80, base+206, result["Capture"] + "   |   Mean " + f'{result["AverageFPS"]:,.1f}' + " FPS", fontSize=14, fontName="Helvetica-Bold", fillColor=shades[index]))
        plot = LinePlot()
        plot.x, plot.y, plot.width, plot.height = 80, base, 850, 180
        plot.data = [[(w["StartSecond"]+0.5, w["Presents"]) for w in group], [(0,5000),(300,5000)]]
        plot.xValueAxis.valueMin, plot.xValueAxis.valueMax = 0, 300
        plot.xValueAxis.valueSteps = list(range(0,301,30))
        plot.yValueAxis.valueMin, plot.yValueAxis.valueMax = 0, 6500
        plot.yValueAxis.valueSteps = [0,1000,2000,3000,4000,5000,6000]
        plot.xValueAxis.labels.fontSize = plot.yValueAxis.labels.fontSize = 10
        plot.yValueAxis.visibleGrid = True
        plot.yValueAxis.gridStrokeColor = colors.HexColor("#E5EAF0")
        plot.lines[0].strokeColor, plot.lines[0].strokeWidth = shades[index], 1.2
        plot.lines[1].strokeColor, plot.lines[1].strokeWidth = colors.HexColor("#C17B18"), 1.4
        plot.lines[1].strokeDashArray = [5,3]
        drawing.add(plot)
        drawing.add(String(80, base-35, "Seconds from first recorded present", fontSize=11))
        drawing.add(String(5, base+95, "FPS", fontSize=11))
    drawing.add(String(80, 17, "One-second present counts. Dashed line: 5,000 FPS. The interval between captures was not measured.", fontSize=11))
    renderSVG.drawToFile(drawing, str(ROOT / "FrameRateTimeline.svg"))

def main():
    results, windows, outliers = [], [], []
    for name in SESSIONS:
        result, capture_windows, capture_outliers = analyze(name)
        results.append(result)
        windows.extend(capture_windows)
        outliers.extend(capture_outliers)
    combined = {"CapturedRows": sum(r["Rows"] for r in results),
                "MeasuredIntervals": sum(r["Intervals"] for r in results),
                "DurationSeconds": sum(r["DurationSeconds"] for r in results)}
    combined["AverageFPS"] = combined["MeasuredIntervals"]/combined["DurationSeconds"]
    output = {"Method": "FPS=(N-1)*1000/(last TimeInMs-first TimeInMs); no trimming or outlier removal.",
              "PythonVersion": platform.python_version(), "NumpyVersion": np.__version__, "PandasVersion": pd.__version__,
              "Captures": results, "Combined": combined,
              "SecondCaptureFPSChangePercent": (results[1]["AverageFPS"]/results[0]["AverageFPS"]-1)*100}
    (ROOT / "AnalysisResults.json").write_text(json.dumps(output, indent=2, allow_nan=False)+"\n", encoding="utf-8")
    save_table("OneSecondWindows.csv", windows)
    save_table("WorstIntervals.csv", outliers)
    save_table("InputManifest.csv", [row for result in results for row in result["InputManifest"]])
    draw_chart(results, windows)
    print("COMBINED", json.dumps(combined))
    print("FPS change percent", output["SecondCaptureFPSChangePercent"])

if __name__ == "__main__":
    main()
