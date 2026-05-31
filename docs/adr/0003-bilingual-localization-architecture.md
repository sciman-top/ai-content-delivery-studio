# ADR 0003: Bilingual Localization Architecture

Date: 2026-06-01

## Status

Accepted.

## Context

The product must support both Chinese and English. This affects the desktop UI, validation messages, logs shown to users, prompt templates, review reports, delivery manifests, and exported documentation.

Retrofitting localization after the WPF shell and workflow screens grow would be expensive because text would be scattered across XAML, view models, delivery writers, and provider prompts.

## Decision

Support two first-class UI languages from the early MVP:

- `zh-CN`
- `en-US`

Use a typed localization service with stable resource keys. The first implementation may keep the catalog in code or `.resx` resources, but callers must use keys rather than hard-coded visible text.

Language preference is stored as:

- `System`: follow current OS UI culture when it maps to a supported language.
- `Chinese`: force `zh-CN`.
- `English`: force `en-US`.

The WPF shell can rebuild localized labels through view-model property change notifications. Early MVP language switching may be in-process for shell text; provider prompts and export text must read the selected language at execution time.

## Rationale

Stable keys make UI, export, and provider prompt localization testable. They also keep provider contracts language-neutral: prompts may be localized, but domain entities and protocol fields stay in English.

Keeping `zh-CN` and `en-US` first-class avoids treating Chinese as a later translation pack. This matches the primary user workflow while preserving English for broader distribution and generated package interoperability.

## Alternatives Considered

### Hard-coded Chinese UI first, translate later

- Pros: fastest first screen.
- Cons: expensive to unwind; export/report language becomes inconsistent.
- Rejected: violates the product requirement that both languages are selectable.

### Full runtime theme-like resource dictionary switching only

- Pros: common WPF pattern for visible XAML text.
- Cons: does not cover provider prompts, generated reports, logs, and manifest labels by itself.
- Rejected as the only mechanism; resource dictionaries can still be used by the shell later.

### Machine translation for UI and reports

- Pros: low authoring effort.
- Cons: unstable wording for professional controls, safety messages, and delivery artifacts.
- Rejected for core product strings; AI may assist draft translations, but committed strings are reviewed resources.

## Consequences

- New user-facing text must go through localization keys.
- Tests should cover language fallback and representative Chinese/English strings.
- Delivery writers and prompt builders must accept an effective language or localization service.
- Logs and protocol identifiers remain English internally, but user-visible summaries can be localized.
- Future plugin/provider UI must declare localizable display names and descriptions.
