# Cloud-First Provider Hardening Plan

## Goal

Make the real-provider execution path robust, observable, and cloud-first without changing the product into a local-model launcher.

## Milestone 1: Secrets And Provider Wiring

- Add a production secret-store abstraction backed by Windows Credential Locker or DPAPI.
- Keep environment variables as a development or smoke-test fallback.
- Verify that secrets never appear in logs, manifests, or diagnostics packages.

## Milestone 2: Resilient Provider HTTP

- Register named provider clients through `IHttpClientFactory`.
- Add `Microsoft.Extensions.Http.Resilience` policies with explicit timeout, retry, and circuit-breaker behavior.
- Disable unsafe retries where request semantics are not idempotent.

## Milestone 3: Provider Provenance And Telemetry

- Capture request IDs, model IDs, latency, token usage, and cost estimates.
- Add OpenTelemetry traces and metrics for provider calls, queue execution, and delivery export.
- Keep the first local developer experience simple through OTLP-compatible tooling.

## Milestone 4: Responses API Workflow Upgrade

- Add stateful multi-turn image workflow support where Responses API is a better fit than a direct image endpoint.
- Preserve revised prompts and prior response linkage as durable provenance.
- Add partial-image streaming support only when it improves the review or workbench experience.

## Milestone 5: Optional Remote Workflow Boundary

- Keep local model runtimes optional.
- Define an adapter boundary for remote workflow engines or managed services.
- Do not make remote workflow graphs a prerequisite for standard project execution.

## Verification

- `dotnet build`
- `dotnet test`
- `dotnet format --verify-no-changes`

For documentation-only planning slices, record the plan and evidence first. Real provider execution remains opt-in and fake providers remain the default development gate.
