# ADR 0001: Independent Repo And Stack

Date: 2026-05-31

## Status

Accepted.

## Context

The existing physics poster project contains valuable production workflow experience, but it is domain-specific. Its tables, prompts, review criteria, and delivery structure are tied to physics educators and two specific series.

The new product should be a general-purpose Windows desktop application for image series planning, generation, review, iteration, and delivery.

## Decision

Create a new repository at `D:\CODE\ai-image-series-studio`.

Use WPF on .NET 10 for the MVP, with:

- WPF desktop shell.
- .NET Generic Host.
- MVVM pattern.
- SQLite via EF Core.
- Provider-neutral AI interfaces.
- Filesystem asset store.

## Rationale

Microsoft recommends WinUI for new modern Windows apps, but WPF is mature, actively maintained, strong for local data-heavy desktop tools, and easier to bootstrap for this Windows-first workflow. A clean `Core` and `Infrastructure` split keeps future WinUI migration possible.

The physics poster project becomes a sample import and validation case, not the product codebase.

## Consequences

- The new product is not coupled to physics-specific tables or prompt sections.
- The MVP can move quickly with mature WPF tooling.
- Future UI migration remains possible if the domain and application layers stay clean.
- The first implementation must prove the generic model with fake providers before any paid API calls.
