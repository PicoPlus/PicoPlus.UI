#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]

LAYER_RULES = {
    "Application": [r"using\s+PicoPlus\.Infrastructure", r"using\s+PicoPlus\.Services", r"using\s+PicoPlus\.Presentation"],
    "Domain": [r"using\s+PicoPlus\.Application", r"using\s+PicoPlus\.Infrastructure", r"using\s+PicoPlus\.Services", r"using\s+PicoPlus\.Presentation"],
    "Presentation": [r"using\s+PicoPlus\.Services"],
}

violations: list[str] = []
for layer, patterns in LAYER_RULES.items():
    layer_dir = ROOT / layer
    if not layer_dir.exists():
        continue
    for file in layer_dir.rglob("*.cs"):
        text = file.read_text(encoding="utf-8", errors="ignore")
        for pat in patterns:
            if re.search(pat, text):
                violations.append(f"{file.relative_to(ROOT)} violates {layer} boundary with pattern: {pat}")

if violations:
    print("Architecture boundary violations found:")
    for v in violations:
        print(f"- {v}")
    sys.exit(1)

print("Architecture boundary checks passed.")
