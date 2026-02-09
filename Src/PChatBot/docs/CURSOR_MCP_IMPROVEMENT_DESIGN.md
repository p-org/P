# PChatBot Cursor + MCP Improvement Design

**Status:** Proposed  
**Date:** 2026-02-09  
**Scope:** `Src/PChatBot` (MCP server, service layer, Streamlit/CLI integration, testing, developer UX)

---

## 1) Why this document

PChatBot now has strong building blocks: service-layer architecture, workflow engine, MCP tools/resources, and multi-provider support.  
The next step is to improve the end-to-end Cursor experience so the agent is:

- More reliable across environments
- Easier to reason about and debug
- Faster in iterative workflows
- Better at human-in-the-loop interactions

This design focuses on concrete engineering changes that reduce failures and increase day-to-day usability in Cursor.

---

## 2) Current-state observations

### Strengths

- Service layer (`GenerationService`, `CompilationService`, `FixerService`) enables shared logic across interfaces.
- MCP server offers broad tool coverage (generation, compile/check, fix, workflow, RAG, resources).
- Preview-then-save workflow is aligned with Cursor review and user control.
- Workflow abstractions exist for orchestrated generation/verification.

### Gaps

- MCP tool schemas and result shapes are not yet uniformly versioned or validated at the boundary.
- Workflow/session persistence is limited for long-running or interrupted work.
- Limited operational telemetry for production-quality debugging in IDE context.
- End-to-end MCP tests are light compared with unit tests, especially for failure/human-guidance paths.
- Some docs and examples can drift from actual tool signatures as the API evolves.

---

## 3) Product goals

1. **Cursor-first reliability**  
   Minimize tool-call ambiguity and environment failures.

2. **Human-in-the-loop clarity**  
   Make guidance requests explicit, structured, and resumable.

3. **Deterministic orchestration**  
   Provide reproducible workflow behavior and resumability.

4. **Faster iteration loops**  
   Improve compile/fix/check turnaround with better caching and context handling.

5. **Confidence through testing**  
   Add robust MCP integration and contract tests.

---

## 4) Design principles

- **Strict contracts at boundaries:** every MCP tool should have stable, explicit input/output contracts.
- **Progressive enhancement:** keep existing tools working while layering better metadata and orchestration.
- **Small, composable modules:** split large server logic into focused registries and shared helpers.
- **Observable by default:** log enough to diagnose issues without dumping sensitive payloads.
- **Fail soft, recover quickly:** meaningful error categories + suggested next actions in every failure path.

---

## 5) Proposed improvements

## A. MCP Contract & API Quality

### A1. Introduce tool response envelope standard (v1)

All tools should return:

- `success`
- `error` (if any)
- `metadata`
  - `tool`
  - `operation_id`
  - `timestamp`
  - `provider`
  - `model`
  - `token_usage`

**Status:** Partially implemented (metadata added).  
**Next:** Add contract tests to guarantee this envelope remains stable.

### A2. Add `api_version` and deprecation notices

Add fields:

- `api_version: "1.0"`
- `deprecation_warning` (only when applicable)

This prevents silent breakage in Cursor prompts/workflows when tools evolve.

### A3. Normalize error categories

All tools should include:

- `error_category` (e.g., `environment`, `validation`, `compilation`, `checker`, `llm_provider`, `internal`)
- `retryable` boolean
- `next_actions` list

This gives Cursor/Claude Code better branching behavior during autonomous tool chains.

---

## B. Cursor Interaction Model

### B1. Session-aware operation state

Add session/project correlation fields:

- `session_id` (from caller or generated)
- `project_id` (derived from path)
- `workflow_id` (for workflow-enabled calls)

This simplifies multi-step and multi-project activity in a single Cursor chat.

### B2. Improve human-guidance protocol

For `needs_guidance` responses, standardize:

- `guidance_request.id`
- `guidance_request.context`
- `guidance_request.questions[]`
- `guidance_request.attempted_fixes[]`
- `guidance_request.resume_tool`
- `guidance_request.resume_payload_template`

This avoids ambiguity when the agent asks user questions and resumes later.

### B3. Add “preflight” recommendation mode

Expand `validate_environment` to include:

- missing prerequisites
- suggested commands
- provider-specific checks
- write-permission checks for target project path

This should be the first call in most generation workflows.

---

## C. Workflow Robustness

### C1. Persist workflow state

Persist active/paused workflow state (json) under project temp dir:

- step index
- context snapshot (bounded)
- recent errors
- timestamps

This enables resume after IDE restart or process interruption.

### C2. Idempotent step semantics

Each workflow step should define:

- inputs hash
- side-effect output paths
- safe re-run behavior

This reduces duplicate writes and inconsistent state during retries.

### C3. Partial-success strategy

For large generation flows, return:

- `completed_steps`
- `failed_steps`
- `artifacts_generated`
- `artifacts_skipped`

This lets Cursor continue incrementally instead of restarting whole workflows.

---

## D. Performance and Cost

### D1. Prompt/context budgeting

Implement per-tool token budget controls:

- baseline guide snippets by tool
- adaptive truncation for large context files
- cap for included project files by relevance

### D2. Smart caching

Cache with eviction:

- resource file loads (already present)
- RAG query results by `(query, category, top_k)`
- compile outputs keyed by project hash (short TTL)

### D3. Faster verification loops

Use staged checker strategy:

- quick check (low schedules) during iterative fixing
- full check only when candidate looks stable

---

## E. Testing Strategy (Cursor-centered)

### E1. MCP contract tests

For each tool:

- validate pydantic input schema behavior
- validate response envelope fields
- validate error category conventions

### E2. Golden-path integration tests

Scenarios:

- `validate_environment` -> generate minimal project -> compile
- compile failure -> `fix_compiler_error` -> compile pass
- checker failure -> `fix_checker_error` -> checker re-run

Mock LLM and deterministic fixtures for CI stability.

### E3. Human-guidance tests

Assert:

- tool pauses with `needs_guidance=true`
- response includes structured questions/template
- resume call applies guidance and continues workflow

### E4. Multi-provider smoke matrix

Run minimal smoke cases for:

- snowflake config detection
- anthropic_direct detection
- bedrock fallback

No live external calls required in CI; provider clients should be mocked.

---

## F. Documentation and Prompting for Cursor

### F1. Single source of truth for tool docs

Generate MCP tool docs from actual schema definitions to prevent drift.

### F2. Cursor usage playbooks

Add short runbooks:

- “Generate project from design doc”
- “Fix compile errors interactively”
- “Run verification loop with human guidance”

### F3. Troubleshooting map

Map common failures to:

- likely cause
- diagnostic tool
- expected remediation path

---

## 6) Proposed implementation plan

### Phase 1 (1 week): Contracts + Preflight

- Finalize response envelope + `api_version`
- Add standardized `error_category`, `retryable`, `next_actions`
- Expand and document `validate_environment`
- Add contract tests for top 10 tools

### Phase 2 (1-2 weeks): Workflow resilience

- Workflow state persistence + resume
- Idempotency metadata for critical steps
- Partial-success artifact reporting
- Guidance request template standardization

### Phase 3 (1 week): Performance + verification loop

- Token/context budgeting controls
- RAG/compile short-term caching
- quick-check/full-check staged strategy

### Phase 4 (1 week): Docs + playbooks

- Auto-generated tool docs
- Cursor runbooks
- Troubleshooting matrix and FAQ

---

## 7) Success metrics

- **Reliability:** tool-call failure rate in Cursor sessions
- **Recovery:** percent of failures resolved without restarting full workflow
- **Speed:** median time for generate->compile and fix->recheck loops
- **Guidance quality:** percent of paused runs successfully resumed
- **Dev velocity:** time to add/modify MCP tool with passing contract tests

---

## 8) Risks and mitigations

- **Risk:** schema changes break existing prompts/workflows  
  **Mitigation:** versioned contracts + deprecation warnings.

- **Risk:** workflow persistence stores too much context  
  **Mitigation:** bounded snapshots + redact large/sensitive payloads.

- **Risk:** over-caching returns stale results  
  **Mitigation:** TTL + content hash invalidation.

- **Risk:** CI flakiness from model/provider calls  
  **Mitigation:** deterministic mocks and fixture-driven integration tests.

---

## 9) Immediate next actions

1. Add contract tests for `validate_environment`, `generate_*`, `p_compile`, `fix_*`, `run_workflow`.
2. Implement `api_version` + standardized error fields in tool responses.
3. Add workflow persistence for pause/resume.
4. Update README tool list and examples to match current tool signatures.
