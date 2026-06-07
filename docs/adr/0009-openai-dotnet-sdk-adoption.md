# ADR 0009: OpenAI .NET SDK Adoption Boundary

## Status

Accepted.

## Context

The V1 provider path already separates text planning, image generation, and vision review roles, uses fake providers by default, and locks provider routing defaults in code. The open question is whether the real OpenAI adapters should move from hand-authored `HttpClient` calls to the official OpenAI .NET SDK.

Official OpenAI documentation states that the Responses API is recommended for new projects, while storage must be explicitly disabled with `store: false` when remote retention is not desired. The same migration guidance also confirms that Structured Outputs use `text.format` on the Responses API and that `previous_response_id` is an explicit state-management choice, not a default local-history replacement.

The locally vendored `openai-dotnet` reference shows the official `OpenAI` NuGet package, generated from OpenAI's OpenAPI specification with Microsoft, with feature clients including `ResponsesClient` and `ImageClient`. The API surface exposes `ResponsesClient.CreateResponseAsync`, `CreateResponseOptions.StoredOutputEnabled`, `ResponseTextFormat.CreateJsonSchemaFormat`, `ImageClient.GenerateImageAsync`, and custom endpoint configuration. Its observability notes still mark SDK telemetry as experimental and incomplete, so replacing the current provider telemetry wholesale would be premature.

## Decision

Adopt the official OpenAI .NET SDK as the preferred transport for stable OpenAI surfaces, but migrate existing runtime adapters only after an adapter-level parity slice proves that the SDK path preserves current product contracts.

For V1, the current raw `HttpClient` provider implementation remains acceptable because it is already covered by contract tests for:

- role-scoped secret retrieval and provider option validation
- custom base URI support for OpenAI-compatible endpoints
- resilient named `HttpClient` registration
- request ID, token usage, latency, cost, and redacted error telemetry
- Responses `store: false` and strict structured output request shape
- Images API base64 extraction and artifact sidecar metadata

New or migrated OpenAI implementation should prefer the SDK when all of these are true:

- the needed SDK method and option are present in the vendored API surface
- the SDK request can express the repository's routing policy without hidden defaults
- tests can assert equivalent contract-level records without real paid calls
- telemetry gaps are either filled by the existing provider instrumentation or recorded as an explicit waiver

Raw `HttpClient` remains the fallback only for:

- unsupported or lagging SDK surfaces
- experimental streaming, image state, or tool paths not yet verified in the SDK
- OpenAI-compatible endpoints that need transport behavior the SDK cannot express safely
- telemetry/provenance gaps where product evidence would otherwise regress

## Consequences

- The task "Evaluate and adopt the official OpenAI .NET SDK where stable" is complete at the policy and architecture level.
- Runtime SDK migration remains a separate implementation slice and must be behavior-preserving.
- The first recommended migration target is a non-streaming Responses planning or review adapter, because it can exercise `ResponsesClient`, `StoredOutputEnabled = false`, and strict `ResponseTextFormat` without changing the user-visible golden path.
- Image generation should migrate only after base64 output extraction, revised prompt provenance, generated artifact sidecars, and error redaction are proven equivalent.
- Existing fake-first and opt-in real-call gates remain unchanged. Real paid API calls still require explicit user approval.

## Verification

- Official docs checked through OpenAI Developer Docs MCP:
  - `https://developers.openai.com/api/docs/guides/migrate-to-responses`
  - `https://developers.openai.com/api/docs/libraries`
- Local reference checked:
  - `D:\CODE\external\ai-content-delivery-studio-references\01-openai\openai-dotnet\README.md`
  - `D:\CODE\external\ai-content-delivery-studio-references\01-openai\openai-dotnet\docs\Observability.md`
  - `D:\CODE\external\ai-content-delivery-studio-references\01-openai\openai-dotnet\api\OpenAI.net10.0.cs`

## Rollback

If the SDK surface regresses or cannot preserve the current provider contract, keep the existing raw `HttpClient` adapters and mark the affected SDK migration task as deferred with evidence. This ADR can be superseded by a later ADR when the SDK surface and telemetry coverage are stable enough to make SDK transport mandatory.
