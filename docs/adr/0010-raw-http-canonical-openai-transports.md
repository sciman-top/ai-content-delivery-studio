# ADR 0010: Raw HTTP as the Canonical OpenAI Transport

## Status

Accepted. Supersedes the "SDK-first migration" direction of [ADR 0009](./0009-openai-dotnet-sdk-adoption.md).

## Context

ADR 0009 adopted the official OpenAI .NET SDK as the preferred transport for stable OpenAI surfaces, with runtime migration to follow after adapter-level parity slices. Since then the repository accumulated two parallel OpenAI stacks:

- The production seam: `ITextPlanningProvider`, `IImageGenerationProvider`, `IImageEditProvider`, and `IVisionReviewProvider` are registered in live mode through `OpenAiProviderFailoverFactory`, which builds the raw `HttpClient` adapters (`OpenAiTextPlanningProvider`, `OpenAiImageGenerationProvider`, `OpenAiImageEditProvider`, `OpenAiVisionReviewProvider`, `OpenAiScientificReviewProvider`). This path owns the OpenAI-compatible endpoint support, per-role configuration, failover chains, telemetry/provenance, and the `store: false` strict structured-output contracts.
- Unwired SDK candidates: an SDK text-planning provider, an official-SDK image-generation provider with its own transport/backend/factory chain, a cost guard, and DI registrations plus contract tests reachable only from tests. The env-gated live sample route even routed image generation through the SDK candidate instead of the production path, producing policy drift between the recorded direction and actual routing.

One SDK consumer is genuinely production-wired: `OpenAiScientificUnderstandingProvider` (with `OpenAiSdkClientFactory` and the Responses client/options support). It is an adopted surface, not a migration candidate.

## Decision

1. V1 production text planning, image generation, image edit, and vision/scientific review treat the existing raw `HttpClient` adapters as the only canonical implementations. They continue to own OpenAI-compatible endpoints, failover, telemetry/provenance, and `store: false` strict structured-output contracts.
2. Delete the unwired SDK candidates: the SDK text-planning provider, the official-SDK image-generation provider, its image transport/backend/factory chain, the cost guard, the DI registrations that exist only for them, and their dedicated tests.
3. The env-gated live sample route must exercise the actual production routes (failover factory) rather than deleted candidates.
4. Keep `OpenAiScientificUnderstandingProvider` and its minimal SDK support (`OpenAiSdkClientFactory`, Responses client/options) as the single adopted SDK surface.
5. The SDK may be reintroduced for a role only when all of the following hold, recorded in a new ADR: a second real production consumer exists; the SDK path can express the compatible-endpoint, failover, and telemetry/provenance contracts; an equivalent contract-level parity is proven without paid calls.

## Consequences

- OpenAI routing collapses into one deeper module: callers depend on the stable provider interfaces, and transport choice no longer leaks as a long-lived candidate branch.
- Provider routing policy, DI registration, and the live sample route all describe the same transport reality.
- Losing the SDK candidates' contract tests is accepted because the raw adapters retain equivalent contract coverage for the production routes.
- SDK telemetry gaps (`OPENAI001` experimental surface) no longer block any production role; only scientific understanding keeps that exposure deliberately.
