# Technical Debt & Issue Tracker

This document tracks technical debt and issues found during the project analysis.  
Last Updated: November 13, 2025

---

## 🔴 Critical (P0) - Fix Immediately

### SEC-001: Exposed API Credentials
- **Category**: Security
- **Severity**: Critical
- **File**: `appsettings.json`
- **Description**: HubSpot Personal Access Token is hardcoded and exposed in source control
- **Impact**: Unauthorized access to HubSpot CRM data
- **Effort**: 2 hours
- **Action Items**:
  - [ ] Revoke token `pat-na1-e9bac190-531b-409f-8819-c53e0f6dfebf` in HubSpot
  - [ ] Generate new token
  - [ ] Add token to User Secrets for development
  - [ ] Configure environment variables for production
  - [ ] Update `.gitignore` to prevent future commits
  - [ ] Clean git history of exposed token
  - [ ] Update deployment pipeline to inject secrets
- **Owner**: TBD
- **Due Date**: ASAP

---

## ⚠️ High Priority (P1) - Fix Within 1 Week

### QA-001: Nullable Reference Type Warnings
- **Category**: Code Quality
- **Severity**: High
- **Count**: 545 warnings
- **Description**: DTOs lack proper nullable annotations
- **Impact**: Potential NullReferenceException at runtime
- **Effort**: 8 hours
- **Action Items**:
  - [ ] Add `required` modifier to non-nullable properties
  - [ ] Add `?` to nullable properties
  - [ ] Initialize properties with default values where appropriate
  - [ ] Verify fix reduces warnings to 0
- **Files Affected**:
  - `Models/CRM/Piplines.Dto.cs`
  - `Models/CRM/Owners.Dto.cs`
  - `Models/CRM/Objects/Deal.Dto.cs`
  - `Models/CRM/Objects/Contact.Dto.cs`
  - All other DTO files
- **Owner**: TBD
- **Due Date**: Week 1

### SEC-002: Missing Error Handling
- **Category**: Security/Reliability
- **Severity**: High
- **Description**: Service methods lack try-catch blocks for API calls
- **Impact**: Unhandled exceptions crash application
- **Effort**: 4 hours
- **Action Items**:
  - [ ] Add try-catch blocks to all service methods
  - [ ] Log errors with context
  - [ ] Return appropriate error responses
  - [ ] Add retry logic for transient failures
- **Files Affected**: All files in `Services/` directory
- **Owner**: TBD
- **Due Date**: Week 1

### TEST-001: No Test Coverage
- **Category**: Testing
- **Severity**: High
- **Description**: Zero test coverage
- **Impact**: No safety net for refactoring, high risk of regressions
- **Effort**: 16 hours (initial setup + tests)
- **Action Items**:
  - [ ] Create test project (xUnit)
  - [ ] Add unit tests for Deal service
  - [ ] Add unit tests for Contact service
  - [ ] Add unit tests for Pipeline service
  - [ ] Configure test execution in CI/CD
  - [ ] Set up code coverage reporting
  - [ ] Target: 60% code coverage
- **Owner**: TBD
- **Due Date**: Week 2

### SEC-003: No Input Validation
- **Category**: Security
- **Severity**: High
- **Description**: User inputs not validated before API calls
- **Impact**: Potential injection attacks, malformed data
- **Effort**: 6 hours
- **Action Items**:
  - [ ] Add validation attributes to models
  - [ ] Implement FluentValidation
  - [ ] Add client-side validation
  - [ ] Add server-side validation
  - [ ] Test with malicious inputs
- **Owner**: TBD
- **Due Date**: Week 2

### CODE-001: Duplicate Configuration Loading
- **Category**: Code Quality
- **Severity**: High (easy fix)
- **File**: `Program.cs`
- **Description**: `appsettings.json` loaded twice (lines 8 and 11)
- **Impact**: Unnecessary overhead
- **Effort**: 5 minutes
- **Action Items**:
  - [ ] Remove duplicate line
  - [ ] Verify app still works
- **Owner**: TBD
- **Due Date**: Week 1

---

## 📋 Medium Priority (P2) - Fix Within 1 Month

### CODE-002: Mixed JSON Libraries
- **Category**: Code Quality
- **Severity**: Medium
- **Description**: Uses both System.Text.Json and Newtonsoft.Json
- **Impact**: Increased bundle size, inconsistency
- **Effort**: 4 hours
- **Action Items**:
  - [ ] Audit usage of both libraries
  - [ ] Standardize on System.Text.Json
  - [ ] Update serialization calls
  - [ ] Remove Newtonsoft.Json if not needed
  - [ ] Test all serialization scenarios
- **Owner**: TBD
- **Due Date**: Month 1

### CODE-003: File Naming Typos
- **Category**: Code Quality
- **Severity**: Low
- **Description**: Typos in file names
- **Impact**: Confusion for developers
- **Effort**: 1 hour
- **Action Items**:
  - [ ] Rename `Piplines.Dto.cs` → `Pipelines.Dto.cs`
  - [ ] Rename `Engagments/` → `Engagements/`
  - [ ] Update all references
  - [ ] Update namespaces
- **Files**:
  - `Models/CRM/Piplines.Dto.cs`
  - `Models/CRM/Engagments/`
  - `Services/CRM/Engagements/`
- **Owner**: TBD
- **Due Date**: Month 1

### PERF-001: No Caching Strategy
- **Category**: Performance
- **Severity**: Medium
- **Description**: No caching for frequently accessed data
- **Impact**: Unnecessary API calls, slow response times
- **Effort**: 8 hours
- **Action Items**:
  - [ ] Implement IMemoryCache for pipelines
  - [ ] Cache product catalog
  - [ ] Cache owner list
  - [ ] Add cache invalidation strategy
  - [ ] Add cache metrics/monitoring
- **Owner**: TBD
- **Due Date**: Month 1

### PERF-002: No Pagination
- **Category**: Performance
- **Severity**: Medium
- **Description**: Hardcoded limit=100 in searches, no UI pagination
- **Impact**: Performance issues with large datasets
- **Effort**: 6 hours
- **Action Items**:
  - [ ] Implement pagination in Contact search
  - [ ] Implement pagination in Deal search
  - [ ] Add MudTable with pagination
  - [ ] Make page size configurable
- **Owner**: TBD
- **Due Date**: Month 1

### DOC-001: Missing Documentation
- **Category**: Documentation
- **Severity**: Medium
- **Description**: No README, no code documentation
- **Impact**: Difficult onboarding, maintenance
- **Effort**: 4 hours
- **Action Items**:
  - [ ] Create README.md with setup instructions
  - [ ] Document environment variables
  - [ ] Add XML documentation to public APIs
  - [ ] Create architecture diagram
  - [ ] Document HubSpot integration patterns
- **Owner**: TBD
- **Due Date**: Month 1

### PKG-001: Outdated Packages
- **Category**: Maintenance
- **Severity**: Medium
- **Description**: Several packages have newer versions
- **Impact**: Missing bug fixes and features
- **Effort**: 2 hours
- **Action Items**:
  - [ ] Update MudBlazor 8.9.0 → 8.14.0
  - [ ] Update Blazor.PersianDatePicker 3.5.1 → 3.7.2
  - [ ] Update Telegram.Bot 22.6.0 → 22.7.5
  - [ ] Update Newtonsoft.Json 13.0.3 → 13.0.4
  - [ ] Test thoroughly after each update
- **Owner**: TBD
- **Due Date**: Month 1

---

## 📝 Low Priority (P3) - Nice to Have

### OPS-001: Missing Health Checks
- **Category**: Operations
- **Severity**: Low
- **Description**: No health check endpoints
- **Impact**: Cannot monitor application health
- **Effort**: 2 hours
- **Action Items**:
  - [ ] Add health check endpoint
  - [ ] Check HubSpot API connectivity
  - [ ] Add to deployment pipeline
- **Owner**: TBD
- **Due Date**: Month 2

### PERF-003: jQuery Dependency
- **Category**: Performance
- **Severity**: Low
- **Description**: jQuery included but may not be needed
- **Impact**: Unnecessary bundle size
- **Effort**: 2 hours
- **Action Items**:
  - [ ] Audit jQuery usage
  - [ ] Replace with vanilla JS or Blazor JS interop
  - [ ] Remove jQuery package
  - [ ] Test all UI interactions
- **Owner**: TBD
- **Due Date**: Month 2

### OPS-002: Missing Monitoring
- **Category**: Operations
- **Severity**: Low
- **Description**: No application monitoring or insights
- **Impact**: Cannot diagnose production issues
- **Effort**: 4 hours
- **Action Items**:
  - [ ] Add Application Insights
  - [ ] Configure logging sink
  - [ ] Add custom events for key operations
  - [ ] Create dashboards
- **Owner**: TBD
- **Due Date**: Month 3

### SEC-004: Security Headers
- **Category**: Security
- **Severity**: Low
- **Description**: Missing security headers (CSP, X-Frame-Options, etc.)
- **Impact**: Potential XSS, clickjacking attacks
- **Effort**: 2 hours
- **Action Items**:
  - [ ] Add Content-Security-Policy
  - [ ] Add X-Frame-Options
  - [ ] Add X-Content-Type-Options
  - [ ] Test with security scanner
- **Owner**: TBD
- **Due Date**: Month 3

---

## 📊 Summary Statistics

| Priority | Count | Estimated Total Effort |
|----------|-------|------------------------|
| Critical (P0) | 1 | 2 hours |
| High (P1) | 5 | 39 hours |
| Medium (P2) | 6 | 25 hours |
| Low (P3) | 4 | 10 hours |
| **TOTAL** | **16** | **76 hours** |

---

## 🎯 Suggested Sprint Plan

### Sprint 1 (Week 1) - Security & Critical Fixes
- SEC-001: Secure API credentials
- CODE-001: Remove duplicate config loading
- QA-001: Start fixing nullable warnings
- SEC-002: Add error handling

**Effort**: ~15 hours

### Sprint 2 (Week 2) - Testing & Validation
- QA-001: Complete nullable warnings
- TEST-001: Add test project and initial tests
- SEC-003: Add input validation

**Effort**: ~26 hours

### Sprint 3 (Month 1) - Quality & Performance
- CODE-002: Standardize JSON library
- CODE-003: Fix file naming typos
- PERF-001: Implement caching
- PERF-002: Add pagination
- DOC-001: Add documentation
- PKG-001: Update packages

**Effort**: ~25 hours

### Sprint 4+ (Month 2+) - Operations & Nice-to-Haves
- OPS-001: Health checks
- PERF-003: Remove jQuery
- OPS-002: Add monitoring
- SEC-004: Security headers

**Effort**: ~10 hours

---

## 📋 Notes

- Priorities may shift based on business needs
- Effort estimates are approximate
- Some items can be worked on in parallel
- Security items should not be postponed
- Testing should be added incrementally with each feature

---

**Last Updated**: November 13, 2025  
**Next Review**: After Sprint 1 completion
