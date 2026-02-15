#!/usr/bin/env python3
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
program = (root / 'Program.cs').read_text(encoding='utf-8', errors='ignore')
required = [
    '.AddPresentationLayer()',
    '.AddApplicationLayer()',
    '.AddInfrastructureLayer()',
    'AddScoped<IAdminOwnerService, AdminOwnerService>()',
    'AddScoped<IDashboardService, DashboardService>()',
    'AddScoped<IKanbanService, KanbanService>()',
]
missing = [item for item in required if item not in program]
if missing:
    print('Missing DI registrations:')
    for m in missing:
        print('-', m)
    sys.exit(1)
print('DI registration checks passed.')
