#!/usr/bin/env python3
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
requirements = {
    'Views/Admin/OwnerSelect.razor': '@inject IAdminOwnerService OwnerService',
    'Views/Admin/Dashboard.razor': '@inject IDashboardService DashboardService',
    'Views/Admin/Analytics.razor': '@inject IDashboardService DashboardService',
    'Views/Admin/Kanban.razor': '@inject IKanbanService KanbanService',
    'Components/Pages/Home.razor': '@inject ResolveLandingRouteUseCase ResolveLandingRoute',
}
missing = []
for rel, needle in requirements.items():
    p = root / rel
    text = p.read_text(encoding='utf-8', errors='ignore') if p.exists() else ''
    if needle not in text:
        missing.append(f'{rel}: missing "{needle}"')
if missing:
    print('Workflow contract checks failed:')
    for m in missing:
        print('-', m)
    sys.exit(1)
print('Workflow contract checks passed.')
