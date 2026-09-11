"""Independent streaming verification; standard-library only.
Rechecks every raw CSV record using integer 0.0001 ms ticks, counts windows,
and verifies reported row counts, aggregate rates, thresholds, and outlier locations.
"""
from pathlib import Path
from collections import Counter
import csv
import json
import math

ROOT = Path(__file__).resolve().parent
results = json.loads((ROOT / "AnalysisResults.json").read_text(encoding="utf-8"))
summaries = []
for expected in results["Captures"]:
    paths = sorted((ROOT / expected["Capture"]).glob("PresentMon part *.csv"))
    count = 0
    previous = first = None
    interval_sum = 0
    windows = Counter()
    thresholds = Counter()
    identities = Counter()
    maximum = 0
    for part, path in enumerate(paths, 1):
        with path.open(encoding="utf-8-sig", newline="") as stream:
            reader = csv.reader(stream)
            if part == 1:
                header = next(reader)
            for row in reader:
                assert len(row) == len(header), (path, count, "Column count")
                timestamp = round(float(row[8])*10000)
                interval = round(float(row[10])*10000)
                if previous is not None:
                    assert timestamp > previous
                    assert timestamp-previous == interval, (path, count, "Interval discontinuity")
                    interval_sum += interval
                    maximum = max(maximum, interval)
                    for limit in [0.2,0.25,0.5,1,2,5,10,16.6667]:
                        thresholds[str(limit)] += interval > round(limit*10000)
                else:
                    first = timestamp
                previous = timestamp
                windows[(timestamp-first)//10000000] += 1
                identities[tuple(row[:8])] += 1
                count += 1
    assert count == expected["Rows"]
    assert len(identities) == 1
    assert interval_sum == previous-first
    duration = (previous-first)/10000000
    average = (count-1)/duration
    assert math.isclose(average, expected["AverageFPS"], abs_tol=1e-9)
    assert maximum/10000 == expected["FrameIntervalsMs"]["Percentiles"]["100"]
    full_seconds = math.floor(duration)
    values = [windows[second] for second in range(full_seconds)]
    assert min(values) == expected["CompleteOneSecondWindows"]["Percentiles"]["0"]
    assert max(values) == expected["CompleteOneSecondWindows"]["Percentiles"]["100"]
    assert sum(value >= 5000 for value in values) == expected["SecondsAtLeast5000FPS"]
    for limit, count_above in thresholds.items():
        assert count_above == expected["Thresholds"][limit]["IntervalsAbove"]
    for outlier in expected["WorstIntervals"]:
        path = paths[outlier["Part"]-1]
        with path.open(encoding="utf-8-sig") as stream:
            for line_number, line in enumerate(stream, 1):
                if line_number == outlier["FileLine"]:
                    assert round(float(next(csv.reader([line]))[10])*10000) == round(outlier["MsBetweenPresents"]*10000)
                    break
            else:
                raise AssertionError("Outlier location missing")
    summaries.append({"Capture": expected["Capture"], "VerifiedRows": count,
                      "AverageFPS": average, "CompleteSeconds": full_seconds,
                      "MinimumOneSecondFPS": min(values), "MaximumOneSecondFPS": max(values),
                      "IntervalSumTicks": interval_sum, "Status": "Passed"})
(ROOT / "VerificationResults.json").write_text(json.dumps(summaries, indent=2)+"\n", encoding="utf-8")
print(json.dumps(summaries, indent=2))
