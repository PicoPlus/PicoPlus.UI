# Release Test Cases and Results

## Scope
Testing for the migrated Clean Architecture boundaries, Bootstrap integration, and critical admin/user routing workflows.

## Automated checks

| ID | Test case | Command | Expected | Result |
|---|---|---|---|---|
| ARCH-01 | Layer boundaries disallow forbidden cross-layer dependencies | `python tests/architecture_boundary_check.py` | No violations | ✅ Pass |
| ARCH-02 | Presentation admin pages use contracts instead of concrete admin services | `python tests/presentation_contract_check.py` | No violations | ✅ Pass |
| DI-01 | Startup DI includes layer modules and admin interface bindings | `python tests/di_registration_check.py` | All required registrations present | ✅ Pass |
| UI-01 | Bootstrap latest/theme references present and legacy css files removed | `python tests/bootstrap_ui_check.py` | All checks true | ✅ Pass |
| WF-01 | Critical pages bind to use-cases/contracts (`Home`, admin pages) | `python tests/workflow_contract_check.py` | All required injections present | ✅ Pass |
| BUILD-01 | Build validation | `dotnet build` | Build succeeds | ⚠️ Blocked (`dotnet` SDK unavailable) |

## Manual QA (browser)

| ID | Scenario | Expected | Result |
|---|---|---|---|
| MQA-01 | Open `/` and verify Bootstrap shell + redirect workflow | UI loads and routing works | ⚠️ Blocked: no app server available in environment |
| MQA-02 | Open `/admin/dashboard`, `/admin/owner-select`, `/admin/kanban` | Pages render and use contract-backed services | ⚠️ Blocked: no app server available in environment |
| MQA-03 | Validate Bootstrap layout/components visually | Responsive navbar/cards/tables render correctly | ⚠️ Blocked: no app server available in environment |

## Notes
- Environment limitation: `dotnet` CLI is missing, so runtime build/run validation is not executable in this container.
- Playwright/browser automation cannot connect because no local web server is running on `127.0.0.1:5000`.
