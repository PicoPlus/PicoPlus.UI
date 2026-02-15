#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGETS = [ROOT / "Views" / "Admin", ROOT / "Components" / "Layout"]
forbidden = [
    r"@inject\s+AdminOwnerService\b",
    r"@inject\s+DashboardService\b",
    r"@inject\s+KanbanService\b",
    r"@using\s+PicoPlus\.Services\.Admin",
]

violations=[]
for d in TARGETS:
    if not d.exists():
        continue
    for f in d.rglob("*.razor"):
        txt=f.read_text(encoding='utf-8', errors='ignore')
        for pat in forbidden:
            if re.search(pat, txt):
                violations.append(f"{f.relative_to(ROOT)} matches forbidden presentation dependency: {pat}")

if violations:
    print("Presentation contract violations found:")
    for v in violations:
        print(f"- {v}")
    sys.exit(1)

print("Presentation contract checks passed.")
