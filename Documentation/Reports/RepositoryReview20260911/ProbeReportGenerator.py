"""Read-only negative probe of the historical report generator.
Synthetic contradictory statistics are substituted in memory only.
RiskReproduced=true means the review's reuse hazard remains present.
"""
from pathlib import Path
from unittest.mock import patch
import contextlib
import io
import json

repository = Path(__file__).resolve().parents[3]
capture = repository / "SavedLogOutput" / "2026-09-11 MinimumInputLatency"
original_read = Path.read_text
generator = capture / "CreateReport.py"
source = generator.read_text(encoding="utf-8")
lines = source.splitlines()
write_lines = [line for line in lines if line.startswith('(ROOT / "Report.md").write_text')]
if len(write_lines) != 1:
    raise RuntimeError("Generator structure changed; inspect it before rerunning this probe.")
source = "\n".join(line for line in lines if line not in write_lines)
data = json.loads((capture / "AnalysisResults.json").read_text(encoding="utf-8"))
for item in data["Captures"]:
    item["AverageFPS"] = 100.0
    item["SecondsAtLeast5000FPS"] = 0
    item["CaptureLog"]["Maximum"]["EventsLost"] = 123
    item["CaptureLog"]["Maximum"]["OverflowedPresents"] = 456
    item["Categories"]["PresentMode"] = {"Composed: Flip": item["Rows"]}
    item["CategoryTransitions"]["PresentMode"] = 1

def read_with_synthetic_results(path, *arguments, **keywords):
    if path == capture / "AnalysisResults.json":
        return json.dumps(data)
    return original_read(path, *arguments, **keywords)

namespace = {"__file__": str(generator), "__name__": "ReportReviewProbe"}
with patch.object(Path, "read_text", read_with_synthetic_results), contextlib.redirect_stdout(io.StringIO()):
    exec(compile(source, str(generator), "exec"), namespace)
report = "\n\n".join(namespace["parts"])
loss_row = next(line for line in report.splitlines() if "Maximum EventsLost /" in line)
observations = {
    "OriginalFilesChanged": False,
    "SyntheticAverageFPS": 100,
    "SyntheticEventsLost": 123,
    "SyntheticOverflowedPresents": 456,
    "IncorrectThroughputNarrative": "sustained approximately 5,600 FPS" in report,
    "IncorrectWindowClaim": "across both captures exceeded 5,000 presents per second." in report,
    "IncorrectZeroLossClaim": "0 / 0 / 0" in loss_row,
    "OutputLossRow": loss_row,
}
observations["RiskReproduced"] = all(observations[key] for key in
    ("IncorrectThroughputNarrative", "IncorrectWindowClaim", "IncorrectZeroLossClaim"))
print(json.dumps(observations, indent=2))
