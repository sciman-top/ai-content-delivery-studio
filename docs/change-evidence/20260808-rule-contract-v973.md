# Global rule 9.73 project-contract evidence

- Repository: `ai-content-delivery-studio`
- Scope: project rule mapping only; no business-code or host-runtime mutation.
- Official basis: current Codex AGENTS loading/precedence and rules semantics; Claude platform delta remains separately verified.
- Git profile: baseline=`main`; upstream=`origin/main`.
- Before AGENTS SHA-256: `74334F4691D9F07B7DC41E563A96AE5A95E73D956B27A535D70E18D2E08732A8`
- After AGENTS SHA-256: `22C29AC320D03C89EB653BB37FF702736E348EE740D1ACA12673DA3772FE2021`
- Planned gate: `pwsh -NoProfile -File scripts/verify-repo.ps1`
- Current verification: canonical gate passed; build 0 warnings/errors, 790/790 tests, reference/product/format gates passed.
- N/A: host loading and live acceptance remain outside repository-static verification.
- Rollback: revert only this repository's `AGENTS.md` and this evidence file to the recorded before hash.
- Truth boundary: `repo_verified=passed`; `host_loaded=codex_fresh_prompt_verified`; `claude_loaded=not_run`; `live_accepted=not_run`.
