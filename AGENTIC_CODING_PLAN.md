# Agentic coding plan

**Purpose:** Make routine, well-specified work executable end-to-end by an AI coding agent after the human provides a user story, bug report, or Gherkin scenario. The agent should understand the system, translate acceptance criteria into checks, implement the change, run the relevant validation, inspect user-visible behavior, and return evidence. This is a target operating model—not a claim that software can decide tax policy or safely self-release without human oversight.

**Status:** Roadmap only. No implementation work or readiness gates in this document should be read as completed unless marked observed below.

## 1. Target and boundary

### Human supplies

- A story/bug/Gherkin description, desired outcome, and any known constraints.
- Decisions where the expected behavior depends on tax interpretation, privacy policy, product scope, or deployment risk.
- Approval for changes to tax rules, externally visible data handling, production configuration, releases, or merges.

### Agent owns for ordinary work

- Repository orientation and impact analysis.
- Turning acceptance criteria into tests before or alongside implementation.
- Code, translations, synthetic test data, and documentation updates.
- Backend/frontend checks, UI automation, focused security checks, and self-review.
- A concise completion report containing changed files, actual commands/results, UI/test evidence, and unresolved issues.

### Hard stop / ask a human

Stop before implementation (or isolate a safe preparatory change) when the task leaves a materially ambiguous expected result; changes a tax rule, deemed-cost assumption, fee treatment, rounding rule, or transaction classification without an approved decision; requires real user exports or credentials; alters production data/retention/authentication/deployment; weakens a test or security gate to make it pass; or cannot be validated with synthetic evidence. Do not merge, deploy, or publish a tax report automatically. The agent may prepare a proposed patch and explain the decision needed.

The goal is **100% agent execution of the coding-and-verification loop for eligible, adequately specified tasks**, not 100% autonomous product decisions, guaranteed correctness, or unattended release.

## 2. Current readiness — observed evidence

| Area | What exists now | Gap to close |
|---|---|---|
| Project orientation | The original working tree had a local, untracked root `AGENTS.md`; it is not included in this PR. The tracked `.github/copilot-instructions.md`, `README.md`, and `backend/README.md` provide project guidance and architecture. | Track/reconcile repository-wide agent instructions and add an explicit task contract, runbook, and canonical check commands. |
| Backend | ASP.NET Core Minimal API with Domain/Application/Infrastructure/API layers. Main flow is `ReportEndpoints` → `ReportService` → parser/FIFO calculation → PDF ZIP/store. Backend has Domain, Application, and Integration test projects. | Expand edge-case and tax-contract coverage; use an independently reviewed oracle for expected tax outputs. |
| Frontend | React + TypeScript + Vite; Finnish/English translations; Coinmotion upload flow. `frontend/package.json` has lint/build/dev/preview scripts but no test script. | Add component and browser E2E tests and repeatable screenshot/trace artifacts. |
| API integration | `ReportEndpointTests` exercises report generation, download, and year filtering. `POST /report/generate` and `GET /report/download/{reportId}` are implemented. General endpoint exceptions are returned to clients as `ex.Message`. | Exercise malformed/oversized inputs, error redaction, download expiry/reuse, and UI-to-API end-to-end behavior. |
| CI | This PR adds `.github/workflows/ci.yml` for backend restore/build/test and frontend clean install/lint/build. The equivalent commands passed locally; the first GitHub-hosted run is pending. | Verify the first GitHub run, then add E2E and security/dependency policy gates. |
| Financial semantics | The local, untracked `COST_CALCULATION.md` says EUR purchase fees enter cost basis, outputs use midpoint-away-from-zero rounding, and numeric inputs are finite/non-negative. `ReportService.HandleBuy` does not include the fee in acquisition cost and uses default `Math.Round` calls; `FifoQueue` only checks inequalities that do not reject every non-finite value. | Treat this as an unresolved contract/code discrepancy, not an approved tax rule. Obtain human approval, then align contract, implementation, and golden tests before allowing agent-led calculation changes. |
| Report and metric meaning | `FilterByYear` filters year summaries but retains all transaction rows. The API's `total_sales_transactions` metric counts `data.Transactions.Count`, while `total_sales_volume_eur` sums both buy and sell volume fields. | Decide expected year-detail and metric semantics; cover API, PDF, and UI outputs with acceptance tests before agents change them. |
| Parser tests | Tracked tests cover Domain, Application, and API integration; no parser-focused test file is present. `CoinmotionCsvParser` skips short rows and several unrecognized transaction cases. | Specify reject-versus-skip behavior and add parser tests before automating parser behavior changes. |
| Working-tree caution | At inspection, branch `main` was two commits behind `origin/main`; `AGENTS.md`, `COST_CALCULATION.md`, and `.worktrees/` were untracked local state. | Re-check branch and preserve those local items before any roadmap implementation. Do not assume untracked files are part of the upstream project. |

This is a readiness snapshot, not a claim that every future change has been verified. For the initial CI change in this PR, `dotnet test CryptoTaxHelper.sln --configuration Release` passed all 22 backend tests; `npm ci`, `npm run lint`, and `npm run build` passed. `npm ci` reported 12 dependency vulnerabilities (1 low, 3 moderate, 8 high); no automatic fixes were applied. The GitHub-hosted workflow itself has not yet run.

## 3. Standard agent loop

1. **Intake and classify.** Read `AGENTS.md`, this plan, the relevant architecture/docs, and current Git status. Classify as feature, bug, refactor, tax-policy, security/data, or operations work. Never overwrite unrelated changes or worktrees.
2. **Resolve the contract.** Extract observable outcomes from the story. Map each acceptance criterion to a test or explicit manual decision. Convert supplied Gherkin into executable scenarios where appropriate. If expected behavior is not unique, ask rather than invent a policy.
3. **Map impact.** Trace the relevant path across UI, API, application/domain logic, persistence/report generation, translations, and tests. Identify compatibility and privacy risks. Record the files and tests expected to change.
4. **Create an isolated change.** Use a task branch/worktree. Add or update synthetic regression tests first; implement the smallest change that meets the contract. Never use real exports, tokens, or personal financial data as fixtures.
5. **Run the quality gates.** Run narrow tests during development, then the full CI-equivalent backend/frontend checks and applicable security/E2E checks. Preserve exact command output and distinguish pass, fail, and blocked.
6. **Inspect user-visible behavior.** For UI or workflow changes, run the application against deterministic synthetic inputs in a browser. Verify expected state transitions, error states, Finnish and English text, and keyboard/accessibility behavior where relevant. Save Playwright traces/screenshots as CI artifacts, not committed user data.
7. **Self-review.** Inspect the full diff, confirm every acceptance criterion is covered, check for accidental secrets/financial data, audit API errors/logging and tax arithmetic when relevant, and run `git diff --check`. Do not weaken or remove tests to hide defects.
8. **Return evidence.** Report summary, changed files, acceptance-criterion-to-test mapping, commands and real results, browser artifacts, known limitations, and any decision/approval needed. Leave commit, merge, release, and deployment to the existing human approval boundary.

## 4. Phased roadmap

Phases are ordered by dependency. Each phase is a prerequisite for dependable unattended implementation of the next. Individual tasks can proceed in parallel only where their contracts do not conflict.

### Phase 0 — Make the repository a reliable agent workspace

- Reconcile `README.md`, `backend/README.md`, `AGENTS.md`, and `.github/copilot-instructions.md`; name one canonical validation command set and document expected tool versions without machine-specific paths.
- Add or refine root `AGENTS.md` with a task workflow: inspect status first, use isolated branches/worktrees, don't touch unrelated local changes, no commit/push by default, and report exact validation results.
- Document a reliable start/stop/health-check recipe for API and frontend, including the configured API URL and synthetic sample-data policy.
- Add a lightweight task template (section 6) to the repository and reference it from agent instructions.
- Reconcile `COST_CALCULATION.md` with the code and tests; label any undecided tax semantics explicitly rather than treating the document alone as proof of correctness.

**Acceptance:** A new agent can identify the main flow, supported broker(s), applicable tests, safe local startup, and forbidden actions using repository docs alone. All documented commands are verified from a clean checkout, and no local-machine-only path is required.

### Phase 1 — Turn stories into executable contracts

- For each feature/bug, require a minimal reproduction or example, explicit preconditions, expected outcomes, error behavior, and non-goals. Gherkin should describe observable behavior, not prescribe internal implementation.
- Build a traceability convention: each acceptance criterion has an ID, and tests refer to that ID in a name/tag or adjacent comment. Every criterion is either automated or explicitly identified as a human decision.
- Add a policy decision record for each reviewed tax rule: inputs, outputs, source/approver, effective tax year, rounding, and examples. Until approved, agent treats changes to that rule as blocked.
- Resolve whether year-filtered reports should retain all historical transaction details, and define the meaning of transaction-count and volume metrics whose current API names imply sales-only values.
- Define stable user/API error categories so tests can assert behavior without depending on raw exception text.

**Acceptance:** For a representative story, the agent can produce an AC-to-test checklist before code. Ambiguous tax or privacy requirements trigger a question; no policy is inferred from code alone.

### Phase 2 — Establish a trusted backend and tax-calculation oracle

- Add deterministic synthetic Coinmotion CSV fixtures for supported export variants and parser edge cases (BOM/line endings, quotes, missing/duplicate headers, malformed rows, invalid dates/numbers, unknown transaction types, and boundary-sized files).
- Before calculation changes, reconcile the local `COST_CALCULATION.md` claims against implementation: `ReportService.HandleBuy` currently computes unit cost from fiat amount and crypto quantity without adding the transaction fee, rounding calls omit an explicit midpoint mode, and `FifoQueue` comparisons do not reject all non-finite values (for example, `NaN`). Confirm intended behavior with a human; do not silently treat the untracked document as authoritative policy.
- Add golden end-to-end cases covering multiple FIFO lots, partial sales, fees, transfers, holding-period boundaries, year/time-zone boundaries, insufficient inventory, and rounding. Expected results must be reviewed by a human qualified to approve the intended tax behavior.
- Add property/invariant tests where meaningful (e.g. no negative inventory after valid processing; consumed lot quantities equal requested sell quantities; aggregation reconciles to line items). Add parser fuzz/property tests that prove malformed input fails safely, not that arbitrary CSV is valid.
- Decide whether calculations should use `decimal`; if approved, migrate with before/after golden vectors and explicit rounding rules. Do not let an agent make this change solely to satisfy a style preference.
- Expand API integration tests for request/file limits, bad or empty uploads, year filters, sanitized errors, expired/unknown report IDs, and single-use downloads.

**Acceptance:** Calculation and parser changes fail on a meaningful regression before implementation, pass against approved synthetic reference vectors afterward, and never require personal financial data. Coverage is reported as a diagnostic; acceptance is correctness against reviewed behaviors, not a percentage target.

### Phase 3 — Add visible, repeatable frontend verification

- Add a frontend unit/component test runner and tests for language switching, disclaimer gating, upload validation/progress, success, API failure/retry, and download behavior.
- Add Playwright E2E for the real UI + local API using only synthetic CSVs: select Coinmotion, complete required steps, upload, view metrics/result, download ZIP, inspect expected report files, and verify error cases.
- Make app startup deterministic in CI with readiness checks and teardown; keep test outputs, screenshots, and traces as retained failure artifacts.
- Add accessibility checks for labels, keyboard operation, modal focus/Escape behavior, focus restoration, and announced status/errors. Ensure both `fi` and `en` flows are covered.
- Use stable test IDs only when semantic locators are insufficient; prefer labels and roles. Do not test internals when a user-visible outcome can be asserted.

**Acceptance:** A clean CI run can launch both tiers and execute the E2E path without credentials or external services. A failing browser test provides a trace or screenshot that makes the failure diagnosable.

### Phase 4 — Make quality gates automatic and reproducible

- Expand `.github/workflows/ci.yml` with frontend unit tests, Playwright E2E, and appropriate formatting/security/dependency checks; the baseline in this PR already runs backend restore/build/test and frontend clean install/lint/build.
- Pin or constrain supported SDK/runtime versions in repository configuration; use lockfiles and clean installs. Include dependency audit with an explicit severity policy and documented exception process.
- Run backend, frontend, and E2E jobs on pull requests. Require all mandatory checks before merge; publish test and browser artifacts.
- Add deterministic synthetic fixtures only; add secret scanning and a check that rejects committed real exports/reports. Do not upload source data to third-party test/report services.
- Keep a local `verify`/test command aligned with CI, so the agent can reproduce failures without guessing.

**Acceptance:** A clean clone can run the documented local checks and obtain the same required pass/fail gate as CI. Pull requests cannot pass by skipping or suppressing required checks.

### Phase 5 — Safe agent execution and task lifecycle

- Implement the intake template and standard agent loop with per-task worktrees, bounded file scope, branch naming, and cleanup that never removes untracked user work.
- Require agents to attach a test plan and expected files to the task record before coding. Keep task-specific changes isolated so multiple agents cannot silently collide.
- Permit automatic creation of a branch/PR and requested code/tests/docs for eligible work. Require human approval for tax-rule updates, authentication/retention/deployment changes, dependency exceptions, merge, and release.
- Define recovery behavior: if a test is blocked by environment/tooling, the agent fixes the reproducible setup only when within task scope; otherwise it reports the precise blocker rather than declaring success.
- Track agent outcomes by escaped defects, first-pass acceptance, rework, and task completion—not code volume or raw test coverage.

**Acceptance:** Given a well-formed story/bug/Gherkin task, the agent can independently create a reviewable implementation, run gates, attach browser/test evidence, and state any remaining decision. No unattended merge/deploy occurs.

### Phase 6 — Production safeguards (only after operating model is chosen)

- Decide whether the app is local-only, a private single-user service, or multi-user. This decision changes authentication, storage, data isolation, retention, and threat model.
- Before exposing uploads beyond a trusted local boundary, implement and test explicit size/rate limits, bounded report storage/expiry, safe error responses, redacted logs, CORS/HTTPS policy, and report ownership/access controls appropriate to the chosen model.
- Document deletion guarantees, incident response, backups (if persistent data exists), deployment/rollback, and supported browsers/runtime.

**Acceptance:** A human approves the operating model and residual-risk assessment; integration tests prove the agreed isolation, expiry, and deletion behavior. An AI agent may propose these changes but may not deploy them autonomously.

## 5. Quality gates and stop conditions

Every ordinary implementation PR must provide:

1. Acceptance criteria mapped to tests or documented human decisions.
2. Relevant backend and frontend checks passing, plus the complete CI gate before merge.
3. Browser evidence for changed user workflows, or a reason why UI behavior is unaffected.
4. Synthetic fixtures only; no personal exports, secrets, or report data in code, logs, or artifacts.
5. No unrelated changes, no suppressed failing checks, no unexplained contract changes, and a clean diff review.
6. A report that states what actually ran and what was blocked. A blocked check is never a pass.

**Stop/escalate:** unclear acceptance result; any tax-policy change without an approved decision; access to live financial data/credentials; failed or unavailable required gate; security boundary/data-retention change; proposed removal/weakening of a quality check; or any production/merge/release action outside approved automation.

## 6. Reusable task input template

```markdown
## Type
Story | Bug | Gherkin | Refactor

## Goal / observed problem
[One outcome; for a bug include the current behavior and a safe synthetic reproduction.]

## Acceptance criteria
- [ ] AC-1: Given ... when ... then ...
- [ ] AC-2: ...

## Constraints / non-goals
[Browsers, languages, API compatibility, performance, scope boundaries.]

## Data and tax-policy impact
[No tax-policy change | approved decision reference | human decision required. Never attach a real export.]

## Evidence supplied
[Logs/screenshots with sensitive values removed; synthetic fixture location.]

## Human decisions already made
[Exact decisions and links/references.]
```

## 7. Recommended first implementation sequence

1. **Repository baseline issue:** verify and reconcile current docs/commands; add clean-checkout local verification recipe and task template. Do not change application behavior.
2. **CI issue:** add backend + frontend CI and establish that all current tests/builds run from a clean environment. Fix only failures surfaced by that baseline in a separate task.
3. **Reviewed calculation examples issue:** obtain human-approved synthetic FIFO/fee/holding-period/rounding examples; turn them into golden tests before changing calculation code.
4. **Parser resilience issue:** add synthetic parser fixtures for malformed and boundary CSV cases, then implement only behavior specified by those tests.
5. **Frontend test issue:** add component tests and one real browser upload/download journey with retained failure evidence.
6. **API safety issue:** decide local/private/public operating model, then specify and test upload caps, error redaction, retention/expiry, and report access behavior.
7. **Agent loop issue:** add worktree-based task orchestration and PR evidence/report template after the checks above are reliable.

Do not bundle tax calculation changes, production hardening, and agent orchestration into one large change. Each needs its own acceptance contract and rollback/review path.

## 8. Human decisions still required

- Which Coinmotion export versions and transaction types are officially supported?
- Which tax assumptions, fee treatments, time boundaries, and rounding rules are authoritative, and who approves updates?
- Is the intended deployment local-only, private single-user, or multi-user?
- What data retention/deletion guarantees and supported browser/accessibility level are required?
- Which checks are mandatory for merge, and who approves exceptions and releases?

Until these are answered, agents can automate routine implementation and verification around existing behavior, but must not interpret unresolved policy as permission to redefine it.

## 9. Home-server test environment and GitHub Actions deployment (proposed; not deployed)

### Observed constraints

- The GitHub repository is public. At initial inspection, `gh workflow list` showed only GitHub's Dependency Graph workflow; this PR adds baseline project CI but no deployment workflow.
- The repository currently has no Dockerfile or Compose file. The backend maps only the report endpoints; there is no health endpoint. API CORS allows `http://localhost:5173`, and the frontend defaults `VITE_API_URL` to `http://localhost:8000`, which would point at the visitor's own machine if left unchanged in a remote browser.
- The home server already runs several unrelated Compose stacks and Nginx Proxy Manager. Its documented NPM proxy-host inventory is incomplete, so no hostname or port should be assumed.
- Server documentation records that the normal operating account is denied access to `/var/run/docker.sock`. Docker socket or `docker`-group access is root-equivalent. Do not place an ordinary persistent GitHub Actions runner with Docker access on this host.
- The application has no authentication and handles uploaded files/reports in memory. Until limits and access restrictions are implemented, staging must use synthetic data only and remain private.

### Recommended design

1. **CI on GitHub-hosted runners.** On pull requests, run backend restore/build/tests and frontend clean install/lint/build; add Playwright E2E when Phase 3 is ready. Use `pull_request`, read-only permissions, and no deployment secrets. Never run untrusted pull-request code in `pull_request_target` with write or deployment credentials.
2. **Build immutable images only after checks pass.** On a protected merge to `main`, build the API and web images and publish immutable commit-SHA tags to GHCR. Optionally move a `staging` tag to that verified commit. Limit the publishing job to the minimum `GITHUB_TOKEN` permissions (`contents: read`, `packages: write`). Decide package visibility explicitly: the repository is public, but GHCR package visibility is separate. If private, use a package-read-only credential on the server, never a broad PAT in the workflow.
3. **Create a separate staging stack.** After verifying the path and permissions, use a dedicated Compose project such as `/opt/stacks/coinmotion-transaction-helper-test` (proposed path, not currently present). Keep its Compose file root-owned and separate from the application checkout. Use an internal Compose network, API + static web container, no privileged mode, no Docker socket mounts, non-root users, read-only filesystems where practical, dropped capabilities, resource limits, and no persistent volume for reports. The API store is currently in-memory, so restarts naturally discard generated reports.
4. **Keep the first deployment loopback-only.** Bind the web entrypoint to `127.0.0.1` on a port selected after a live availability check. Access it through a user-initiated SSH tunnel for initial testing. Do not add an NPM host, DNS record, public port-forward, or guessed hostname as part of the first pass. Later, if remote browser access is needed, inspect the live proxy configuration and add a TLS-protected, access-restricted host only after the app has upload limits and an agreed authentication/access policy.
5. **Fix the remote-browser API URL deliberately.** The Vite `VITE_API_URL` value is embedded at build time. Either build with the approved staging origin and serve the frontend with a same-origin `/report/*` reverse proxy to the API, or change the frontend configuration to support a runtime/same-origin API base. Do not leave the default `localhost:8000` in a remotely viewed build. A same-origin route avoids widening API CORS.
6. **Start with operator-triggered deployment; then automate pulls.** First have an authorized operator run a fixed, root-owned deployment script that deploys one tested image digest into the staging Compose project and leaves the previous digest available for rollback. Once health checks and rollback are proven, automate only the staging update: a narrowly scoped server-side systemd timer polls GHCR for the verified `staging` digest and invokes that same fixed script when it changes. This is pull-based, requires no inbound GitHub-to-home-server connection, and needs no runner on the host. Keep automatic deployment limited to commits already merged to `main` after required CI passes; never auto-deploy arbitrary PR branches.
7. **Verify and document each release.** Add a health/readiness endpoint and Compose healthcheck. For each staged digest, verify API health, frontend load, synthetic CSV upload, metrics, ZIP/PDF download, and one-time download behavior; retain only redacted logs and CI artifacts. On failure, restore the previous digest. Update the server service page, inventory, and unknowns ledger after deployment topology or access changes. Keep user uploads out of logs and CI artifacts.

### Rollout order and go/no-go criteria

1. Add and pass GitHub-hosted CI without deployment.
2. Add container build definitions, health checks, an explicit staging API URL/proxy path, request limits, and synthetic browser smoke coverage.
3. Manually deploy one verified SHA to an isolated, loopback-only stack; verify that existing server services and proxy configuration were not changed.
4. Test rollback to the previous SHA and confirm report data is ephemeral.
5. Only then enable the server-side staging poller. Keep production/release deployment out of scope and human-approved.

Do **not** expose the current unauthenticated upload endpoint to the public Internet or install a self-hosted runner with Docker control on the shared home server. A compromised action or pull-request job could otherwise gain host-level control, and the current server account is intentionally denied that access.

### Decisions before implementation

- Whether initial browser access via SSH tunnel is sufficient, or whether a TLS-protected private proxy host is needed.
- Whether GHCR images should be public (no server pull credential) or private (server-side read-only credential).
- Whether staging should update automatically after every passing merge to `main`, or wait for a manual promotion.
- The test environment's retention/access policy and who may upload files; until explicitly changed, use synthetic CSVs only.
