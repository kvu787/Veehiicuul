"""Check the authored report's local links, table alignment, and Python syntax."""
from pathlib import Path
import ast
import re

root = Path(__file__).resolve().parent
report = (root / "Report.md").read_text(encoding="utf-8")
links = re.findall(r"\]\(([^)]+)\)", report)
local_links = [link for link in links if not link.startswith("https://")]
missing = [link for link in local_links if not (root / link).exists()]
assert not missing, missing
for script in root.glob("*.py"):
    ast.parse(script.read_text(encoding="utf-8-sig"))
tables = re.findall(r"(?:^\|.*\n)+", report, re.M)
for table in tables:
    separators = [tuple(i for i, char in enumerate(line) if char == "|") for line in table.splitlines()]
    assert len(set(separators)) == 1, "Markdown table columns are not aligned"
print(f"Verified {len(local_links)} local links, {len(tables)} aligned tables, and Python syntax.")
