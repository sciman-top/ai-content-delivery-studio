# Preset Governance Evidence Table

Last reviewed: 2026-06-02.

This document supports the preset governance design and implementation. It records which external and local evidence is allowed to shape executable presets, recommendation metadata, and provider capability warnings.

## Source Authority

| Source type | Authority | How it can affect the product |
| --- | --- | --- |
| Official provider docs | High | Defines real provider capabilities, parameters, limits, cost drivers, and unsupported settings. |
| Local code and tests | High | Defines current contracts, compatibility boundaries, and verified behavior. |
| Project-specific production sample | High | Defines known workflow risks and delivery requirements from real batch generation. |
| Mature community projects | Medium | Suggests workflow patterns, UI grouping, and failure modes. |
| Best-practice articles and skill catalogs | Medium to low | Suggests process structure and candidate dimensions. |
| GitHub Trending monthly | Low | Discovers emerging categories only; it does not define executable defaults. |

## Evidence Table

| Dimension | Candidate values or rule | Source | Source type | Confidence | Provider-specific | Fixed or flexible | Test gate |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `workflowMode` | `text-to-image`, `reference-image`, `edit`, `mask-edit`, `variation`, `background-plate` | OpenAI image generation guide | Official provider docs | High | Yes | Fixed executable ID | Provider capability tests and queue warnings |
| Output customization | `size`, `quality`, `format`, `compression`, `background` | OpenAI image generation guide | Official provider docs | High | Yes | Fixed recipe fields with provider mapping | `GenerationRecipe` and provider capability tests |
| Draft quality | Prefer low-cost draft quality before final quality | OpenAI image generation guide | Official provider docs | High | Yes | Constrained recommendation | Fake provider recommendation tests |
| Size constraints | Max edge, multiples of 16, aspect ratio limit, and total pixel limits must stay provider-validated | OpenAI image generation guide | Official provider docs | High | Yes | Provider adapter rule | Capability warning tests |
| Transparent background | Do not assume all image models support transparency | OpenAI image generation guide | Official provider docs | High | Yes | Capability warning | Provider-specific validation |
| Text rendering | Text-heavy output should use deterministic post-render composition | OpenAI image generation limitations and physics poster tool | Official plus local sample | High | Partly | Fixed `ImageTextPolicy` | Text-heavy preset invariant tests |
| Composition precision | Structured layouts should use post-render layout or review warnings | OpenAI image generation limitations | Official provider docs | High | Yes | Constrained recommendation | Rubric and capability warning tests |
| Vision review detail | Small text, rotated text, graphs, spatial reasoning, counting, and metadata are unreliable | OpenAI images and vision guide | Official provider docs | High | Yes | Review warning and rubric dimension | Structured review tests |
| Vision input cost | Image inputs count toward token usage | OpenAI images and vision guide | Official provider docs | High | Yes | Cost strategy warning | Cost estimate tests |
| Workflow graph inspiration | Workflows can be reproducible artifacts with queue visibility | ComfyUI docs and repository | Mature community project | Medium | No | Flexible architecture pattern | Workflow export/import plan |
| Parameter experiments | Prompt matrix and X/Y/Z-style parameter comparisons are useful advanced tools | AUTOMATIC1111 repository | Mature community project | Medium | No | Constrained advanced mode | `ParameterExperiment` tests |
| Creator workspace | Boards, gallery actions, prompt reuse, and remix flows improve iteration | InvokeAI repository and docs | Mature community project | Medium | No | UI workflow pattern | Gallery and prompt reuse tests |
| Provider-neutral pipelines | Swappable pipeline components and reproducible configs reduce provider lock-in | Hugging Face Diffusers repository | Mature community project | Medium | Partly | Architecture constraint | Provider contract tests |
| Review workflow | Review needs structured labels, decisions, comments, and audit history | Label Studio, CVAT, FiftyOne | Mature community projects | Medium | No | Review contract | `ReviewRubric` and delivery report tests |
| Skill structure | Skills can package repeatable agent workflows, but arbitrary skills require review | skills.sh docs | Skill catalog | Medium | No | Process guidance only | No direct executable defaults |
| Trend discovery | Trending projects can reveal emerging workflow categories | GitHub Trending monthly | Trend signal | Low | No | Discovery only | No direct catalog promotion |
| Delivery traceability | Prompt snapshots, JSON metadata, CSV manifests, dry-run mode, and human approval are essential | `D:\CODE\physicist_chinese_poster_batch_tool` | Local production sample | High | No | Fixed workflow requirement | Delivery package and fake provider tests |

## Preset Admission Rules

A new executable `ImageTypePreset` should not enter the catalog until it satisfies all of these checks:

| Check | Requirement |
| --- | --- |
| Use case | At least one concrete workflow example exists. |
| Delivery shape | Default aspect ratio, output format, and naming policy are defined. |
| Text policy | The preset explicitly chooses `ImageModelOnly`, `DeterministicPostRender`, or `Hybrid`. |
| Review | A review rubric template exists and matches the failure mode. |
| Capability | Provider-specific limitations are known or represented as warnings. |
| Cost | Draft and final quality expectations are clear. |
| Tests | Catalog invariants verify IDs, rubric links, text-heavy policy, and non-empty metadata. |

## Recommendation Rules

AI text planning may recommend values, but queue execution may only consume catalog-backed fields.

| Recommendation output | Rule |
| --- | --- |
| `imageTypePresetId` | Must match `ImageTypePresetCatalog`. |
| `reviewRubricTemplateId` | Must match `ReviewRubricTemplateCatalog`. |
| `qualityBand` | Product-level value; provider adapters translate it. |
| `styleIntent` | Free text; never treated as an executable provider parameter. |
| `capabilityWarnings` | Must remain visible through promotion and queue planning. |
| `nonExecutableSuggestions` | Useful for brainstorming, but not consumed by generation without user confirmation. |

## Reference Links

- OpenAI Image generation guide: https://developers.openai.com/api/docs/guides/image-generation
- OpenAI Images and vision guide: https://developers.openai.com/api/docs/guides/images-vision
- ComfyUI: https://github.com/comfyanonymous/ComfyUI
- ComfyUI documentation: https://docs.comfy.org/
- AUTOMATIC1111 Stable Diffusion WebUI: https://github.com/AUTOMATIC1111/stable-diffusion-webui
- InvokeAI: https://github.com/invoke-ai/InvokeAI
- InvokeAI documentation: https://invoke-ai.github.io/InvokeAI/
- Hugging Face Diffusers: https://github.com/huggingface/diffusers
- Label Studio: https://github.com/HumanSignal/label-studio
- CVAT: https://github.com/cvat-ai/cvat
- FiftyOne: https://github.com/voxel51/fiftyone
- skills.sh docs: https://skills.sh/docs
- GitHub Trending monthly: https://github.com/trending?since=monthly
