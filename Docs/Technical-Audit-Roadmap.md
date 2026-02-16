# PicoPlus.UI — Deep Technical Audit & Milestone-Based Restructuring Roadmap

## Executive Technical Summary

PicoPlus.UI is a Blazor Server application that integrates heavily with external APIs (HubSpot CRM, SMS provider, identity verification). The current implementation is functional but structurally brittle under scale due to:

- **Security-sensitive logic held in client/session state** (role and auth checks driven by browser storage/session wrappers).
- **Service-layer anti-patterns** (silent catches, dynamic JSON parsing, duplicated HubSpot call boilerplate, large orchestration classes).
- **Performance inefficiencies in dashboard and owner workflows** (repeated full-list pulls, repeated in-memory scans, missing caching/invalidations).
- **Weak observability and resiliency** (no HTTP retry/backoff/circuit breaker strategy; no distributed tracing/metrics).
- **No automated tests in repository**, creating high regression risk for critical auth/registration/admin flows.

The roadmap below prioritizes **stabilization and observability first**, then controlled architectural decomposition, then targeted performance tuning and hardening.

---

## Phase 1 — Full Technical Audit

## 1) Performance leaks and inefficiencies

### 1.1 In-memory OTP store can grow unbounded and is not concurrency-safe
- **Root cause**: OTPs are stored in a singleton service dictionary (`AddSingleton<OtpService>`) and cleanup is optional/manual (`CleanupExpiredOtps`) with no scheduler. Dictionary access is unsynchronized.
- **Impact**: Memory growth over time; race conditions under concurrent requests; OTP correctness issues during burst traffic.
- **Severity**: **High**.
- **Code-level examples**:
  - Singleton registration for `OtpService` in startup composition. (`Program.cs`)
  - `_otpStore` as mutable `Dictionary<string, OtpData>` with unsynchronized reads/writes and no periodic cleanup trigger. (`Services/Auth/OtpService.cs`)

### 1.2 Repeated full-data pulls and recomputation in admin services
- **Root cause**: `AdminOwnerService` repeatedly calls `GetAllOwnersAsync()` inside search/get/validate operations, each potentially hitting upstream API and re-materializing all owners.
- **Impact**: Avoidable network round-trips, increased latency, upstream quota pressure.
- **Severity**: **High**.
- **Code-level examples**:
  - `SearchOwnersAsync`, `GetOwnerByIdAsync`, `GetOwnerByEmailAsync`, and `ValidateOwnerAsync` each call `GetAllOwnersAsync()`.

### 1.3 Quadratic-ish dashboard stage aggregation pattern
- **Root cause**: For each pipeline stage, code scans full deal list (`deals.Where(...)`) repeatedly.
- **Impact**: O(stages × deals) CPU overhead; degrades with larger deal volumes.
- **Severity**: **Medium**.
- **Code-level examples**:
  - Nested loop in `LoadPipelineStagesAsync` with per-stage filtering of full `deals`.

### 1.4 Excessive allocations and dynamic parsing in CRM wrappers
- **Root cause**: multiple `dynamic` return types and repeated ad-hoc JSON deserialization; conversion from dynamic objects to strongly typed lists in extension wrappers.
- **Impact**: runtime binder overhead, extra allocations, weaker compile-time guarantees.
- **Severity**: **Medium**.
- **Code-level examples**:
  - `Deal.GetAll`, `Deal.Search`, `Deal.BatchCreate`, `Deal.BatchUpdate` return `dynamic`.
  - `AdminServiceExtensions.GetBatchAsync` reparses dynamic response into typed DTOs.

### 1.5 Potential UI over-refresh patterns in dashboard rendering
- **Root cause**: manual `StateHasChanged()` before async load and full-list rendering (`foreach`) without `@key` hints.
- **Impact**: additional render work and lower diffing efficiency on large lists.
- **Severity**: **Low/Medium** (depends on data scale).
- **Code-level examples**:
  - `LoadDashboard` sets `isLoading = true` then explicitly calls `StateHasChanged()`.
  - stage/activity loops in dashboard UI without explicit keys.

### 1.6 Blocking I/O style utilities in security helper (unused/legacy risk)
- **Root cause**: file operations use synchronous APIs and static path composition.
- **Impact**: thread blocking (if invoked on request path), fragile file path behavior.
- **Severity**: **Low** currently (appears legacy), **High** if activated in hot paths.
- **Code-level examples**:
  - `File.ReadAllBytes` in `DataProtect.LoadUserData`.

---

## 2) Architectural flaws

### 2.1 Authentication/authorization relies on mutable client/session values
- **Root cause**: admin checks are based on stored role string (`user_role`) from session/local state.
- **Impact**: privilege escalation risk if storage/session is tampered; weak trust boundary.
- **Severity**: **Critical**.
- **Code-level examples**:
  - role assignment in login path and session/local persistence.
  - admin authorization handler reading role from session and allowing admin access.

### 2.2 God-class ViewModel with mixed responsibilities
- **Root cause**: `RegisterViewModel` combines identity verification, OTP orchestration, contact creation, image upload, session state, navigation, and UI workflow.
- **Impact**: high coupling, low testability, difficult maintenance, regression-prone edits.
- **Severity**: **High**.
- **Code-level examples**:
  - `RegisterViewModel.cs` is 671 lines and spans multiple bounded contexts.

### 2.3 Service-layer duplication and inconsistent abstraction boundaries
- **Root cause**: each CRM service manually constructs nearly identical HTTP requests/headers/serialization logic.
- **Impact**: duplicated bugs, inconsistent behavior, slower feature iteration.
- **Severity**: **Medium**.
- **Code-level examples**:
  - repeated request construction in `Services/CRM/Objects/Contact.cs` and `Deal.cs`.

### 2.4 Extension helper returning placeholder/empty values hides domain gaps
- **Root cause**: `PropertyHelpers` methods return empty strings where model fields are missing.
- **Impact**: silently wrong analytics/filtering (e.g., owner filtering ineffective), confusing business results.
- **Severity**: **High**.
- **Code-level examples**:
  - `GetOwnerId` always returns empty string while dashboard filter depends on owner ID.

---

## 3) Error-handling weaknesses

### 3.1 Silent failure via blanket catch blocks
- **Root cause**: broad catches returning empty lists/false without logging or context propagation.
- **Impact**: hidden incidents, degraded debugging, data quality ambiguity.
- **Severity**: **High**.
- **Code-level examples**:
  - `AdminServiceExtensions.GetBatchAsync` and `UpdateStageAsync` swallow all exceptions.

### 3.2 Missing resilience policies for external HTTP dependencies
- **Root cause**: HttpClients configured with timeout only; no retry/backoff/circuit breaker/jitter.
- **Impact**: brittle behavior during transient network/provider faults.
- **Severity**: **High**.
- **Code-level examples**:
  - named/typed HttpClient registrations in startup without Polly policies.

### 3.3 Inconsistent exception boundaries
- **Root cause**: mixture of strict `EnsureSuccessStatusCode()` and broad catches at higher layers without structured error types.
- **Impact**: noisy failures in some flows, silent degradation in others.
- **Severity**: **Medium**.

---

## 4) Testing deficiencies

### 4.1 No automated tests present
- **Root cause**: repository currently has no test projects/files.
- **Impact**: no regression safety for auth, registration, admin dashboard, CRM integration mapping.
- **Severity**: **Critical**.
- **Code-level examples**:
  - no `*.Tests` project or test files detected.

### 4.2 Design patterns limit testability
- **Root cause**: very large ViewModels and direct dependency on many concrete services; dynamic payload parsing.
- **Impact**: difficult mocking, complex fixture setup, low unit-test ROI.
- **Severity**: **High**.

---

## 5) Security risks

### 5.1 OTP values logged in plaintext
- **Root cause**: OTP code is logged during generation/storage/validation.
- **Impact**: credential disclosure through logs; compliance and account-takeover risk.
- **Severity**: **Critical**.
- **Code-level examples**:
  - debug/info logs outputting `{Code}`, `{StoredCode}`, `{EnteredCode}` in `OtpService`.

### 5.2 Hardcoded symmetric key in source
- **Root cause**: encryption key constant embedded in `DataProtect`.
- **Impact**: key compromise risk; impossible safe rotation; source leak equals data compromise.
- **Severity**: **Critical**.
- **Code-level examples**:
  - `private const string encryptionKey = ...`.

### 5.3 Static path derived from uninitialized mutable static user ID
- **Root cause**: `userDataFilePath` is static readonly but interpolates `user_id` before it is set.
- **Impact**: incorrect path usage and potential data overwrite collisions.
- **Severity**: **High**.

### 5.4 Weak authorization boundary
- **Root cause**: trust in `sessionStorage` role for admin gating without server-side identity claims policy.
- **Impact**: elevation of privilege risk.
- **Severity**: **Critical**.

---

## Phase 2 — Performance Improvement Plan

## Target KPIs (90-day horizon)

1. **Memory footprint**
   - Reduce steady-state server memory by **20–30%** during representative load.
   - Keep OTP/session auxiliary stores bounded with max-entry + TTL eviction.

2. **Latency**
   - Admin dashboard API composition p95: reduce from current baseline by **35%** (target p95 < **450ms** under staging load profile).
   - Owner search/list operations: reduce median response time by **50%** via short-lived cache.

3. **Throughput**
   - Increase successful requests/sec for admin dashboard endpoint/render data source by **30%** at same error budget.

4. **External call efficiency**
   - Reduce upstream HubSpot calls per admin dashboard interaction by **40–60%** with batched fetches and cached owner/pipeline metadata.

5. **Error-rate resilience**
   - Keep transient-fault failure rate < **1%** with retries + circuit breaker under induced packet loss / 5xx chaos tests.

## Tooling and measurement stack

- **Profilers**
  - `dotnet-counters` for GC/allocations/threadpool.
  - `dotnet-trace` + Speedscope/PerfView for CPU flame analysis.
  - `dotnet-gcdump` for heap snapshots.

- **Static analyzers**
  - Roslyn analyzers + nullable warnings as errors in CI for critical projects.
  - Security analyzers (credential logging, insecure randomness, hardcoded secrets).

- **Load and resilience testing**
  - k6 scenario suite for login/register/admin dashboard.
  - Fault injection against HubSpot/SMS dependencies (timeouts, 429, 5xx).

- **Observability**
  - OpenTelemetry tracing/metrics/log correlation (request ID + external dependency spans).
  - Prometheus/Grafana dashboard for p50/p95/p99 latency, external call counts, retry counts, and error budgets.

- **Frontend diagnostics (Blazor)**
  - Render count instrumentation for high-frequency components.
  - Browser performance timeline for expensive re-renders.

---

## Phase 3 — Milestone-Based Restructuring Roadmap

## Milestone 1 — Stabilization (Weeks 1–2)
- **Objective**: Eliminate critical security/reliability risks before deeper refactor.
- **Technical scope**:
  - Remove OTP plaintext logging.
  - Replace hardcoded encryption key with configuration-backed secret provider.
  - Enforce server-trusted auth model for admin access.
  - Replace silent catches with structured error logging + typed error results.
- **Task breakdown**:
  1. Introduce security hotfix PR: redact OTP logs and sensitive payloads.
  2. Move keys/tokens to secret manager and rotate any exposed key material.
  3. Implement server-side claims-based authorization for admin routes/components.
  4. Standardize error contracts (`Result<T,Error>` pattern) in service boundaries.
- **Estimated duration**: 10 business days.
- **Dependencies**: none.
- **Acceptance criteria**:
  - No sensitive codes or tokens in logs.
  - Admin access impossible without server-validated role claim.
  - 100% of prior silent catches replaced by logged typed failure paths.
- **Risk assessment**:
  - Medium risk of auth regressions; mitigate with smoke tests and staged rollout.

## Milestone 2 — Observability & Instrumentation (Weeks 2–3)
- **Objective**: Make performance and failures measurable.
- **Technical scope**:
  - OpenTelemetry integration.
  - HTTP dependency metrics and structured logs.
  - Baseline load profile + SLO dashboard.
- **Task breakdown**:
  1. Add tracing middleware and propagate correlation IDs.
  2. Instrument HubSpot/SMS typed clients with dependency spans/tags.
  3. Publish Grafana boards: latency, throughput, error-rate, retries.
  4. Capture baseline benchmark reports for dashboard/login/register flows.
- **Estimated duration**: 5 business days.
- **Dependencies**: Milestone 1 (stable auth/security baseline).
- **Acceptance criteria**:
  - p95 and error-rate visible per endpoint and dependency.
  - Alerting configured for error-budget burn and elevated latency.
- **Risk assessment**:
  - Low; mostly additive.

## Milestone 3 — Core Architectural Refactor (Weeks 3–7)
- **Objective**: Decouple domains and reduce complexity hotspots.
- **Technical scope**:
  - Split `RegisterViewModel` into orchestrator + dedicated domain services.
  - Introduce typed HubSpot gateway layer (remove dynamic).
  - Consolidate duplicated HTTP request construction in shared client abstractions.
- **Task breakdown**:
  1. Define bounded contexts: IdentityVerification, OTP, ContactProvisioning, MediaUpload.
  2. Extract each into interface-driven service with narrow contracts.
  3. Refactor CRM clients to typed DTOs and generic response handlers.
  4. Remove placeholder property helpers; align DTOs with real HubSpot schema.
- **Estimated duration**: 4 weeks.
- **Dependencies**: Milestone 2 instrumentation for safe before/after comparisons.
- **Acceptance criteria**:
  - No dynamic return types in CRM API layer.
  - Registration flow split into <=4 focused classes, each <250 LOC.
  - Owner/deal model includes actual owner/pipeline fields; filters produce correct results.
- **Risk assessment**:
  - High due to broad touch surface; mitigate via feature flags and contract tests.

## Milestone 4 — Performance Optimization (Weeks 7–9)
- **Objective**: Achieve KPI targets with data-driven optimization.
- **Technical scope**:
  - Cache owner/pipeline metadata with explicit TTL + invalidation.
  - Optimize dashboard stage aggregation using pre-grouped lookups.
  - Add resilient HTTP policies (retry/backoff/circuit breaker).
  - Bound OTP store with concurrent collection and automatic expiration.
- **Task breakdown**:
  1. Introduce IMemoryCache strategy and keying standard.
  2. Replace repeated list scans with dictionary/group-by strategy.
  3. Apply Polly policies to all external clients.
  4. Load-test and tune thread pool / timeout settings.
- **Estimated duration**: 2 weeks.
- **Dependencies**: Milestone 3 refactor complete.
- **Acceptance criteria**:
  - Dashboard p95 latency improved by >=35% from baseline.
  - External call volume reduced by >=40% per dashboard refresh.
  - No unbounded OTP store growth in 24h soak test.
- **Risk assessment**:
  - Medium; policy tuning can cause retry storms if misconfigured.

## Milestone 5 — Testing & Hardening (Weeks 9–11)
- **Objective**: Build regression safety and reliability confidence.
- **Technical scope**:
  - Introduce unit/integration test suites.
  - Add API contract tests for HubSpot payload mapping.
  - Security test cases for auth/OTP misuse.
- **Task breakdown**:
  1. Create `PicoPlus.Tests` with xUnit/NUnit + mocking framework.
  2. Add integration tests for login/register/admin data composition.
  3. Add snapshot/contract tests for DTO serialization/deserialization.
  4. Add concurrency tests around OTP service.
- **Estimated duration**: 2 weeks.
- **Dependencies**: Milestones 3 and 4.
- **Acceptance criteria**:
  - >=70% coverage on critical services (auth, OTP, admin dashboard, CRM gateway).
  - Integration suite green in CI.
  - Security tests prove no role escalation via client storage tampering.
- **Risk assessment**:
  - Low/Medium; initial test harness effort is front-loaded.

## Milestone 6 — Final Validation & Documentation (Week 12)
- **Objective**: Production readiness sign-off.
- **Technical scope**:
  - End-to-end performance certification.
  - Runbooks, architecture decision records, and migration docs.
  - Post-release monitoring plan.
- **Task breakdown**:
  1. Run full load + soak + chaos validation.
  2. Document architecture v2 and operational playbooks.
  3. Publish rollback strategy and release checklist.
- **Estimated duration**: 1 week.
- **Dependencies**: Milestones 1–5.
- **Acceptance criteria**:
  - KPI targets met in staging with reproducible reports.
  - Operational docs approved by engineering/ops.
  - Zero Sev-1 findings in release readiness review.
- **Risk assessment**:
  - Low if previous milestones complete; medium otherwise.

---

## Final Risk Analysis

### Top risks if no action is taken
1. **Privilege escalation and credential leakage** due to client-trusted roles and OTP logging (**Critical**).
2. **Operational instability under scale** due to fragile external dependency handling and no resilience policies (**High**).
3. **Rising change-failure rate** from large coupled classes and lack of tests (**High**).
4. **Performance plateau** from repeated full-fetch + repeated scans and weak caching strategy (**Medium/High**).

### Key governance recommendations
- Adopt an explicit **error budget/SLO model** before optimization work.
- Enforce architectural guardrails in CI (complexity thresholds, analyzer rules, test gates).
- Require performance baselines and before/after evidence for all major refactors.

