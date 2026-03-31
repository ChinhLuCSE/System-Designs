---
name: system-design-interview
description: Draft interview-style system design answers with a fixed section order, concise tradeoff-driven reasoning, and selective scope control. Use when Codex needs to create or extend a system design writeup for products like URL shorteners, chat systems, feeds, storage systems, or similar interview prompts, especially when the answer must follow a required structure such as functional requirements, non-functional requirements, data model, API design, high-level design, and deep dives.
---

# System Design Interview

## Overview

Write clear system design answers that follow the required interview structure without adding unrequested sections.
Read [references/answer-structure.md](references/answer-structure.md) before drafting so the section order and authoring rules stay consistent.

## Workflow

1. Identify the system being designed and the exact sections the user requested.
2. Read [references/answer-structure.md](references/answer-structure.md).
3. Draft only the requested sections, preserving the canonical order from the reference even when only a subset is needed.
4. Keep each section interview-oriented: list concrete requirements, model the core entities, define key APIs, then explain the architecture and tradeoffs at the right depth.
5. Avoid adding summaries, diagrams, implementation details, or extra sections unless the user explicitly asks for them.

## Drafting Rules

- State assumptions when requirements are ambiguous.
- Prefer concise bullets for requirements and APIs, then short explanatory paragraphs for architecture and tradeoffs.
- Tie non-functional requirements to design choices such as partitioning, replication, caching, async processing, and consistency.
- Mention scalability, performance, distributed systems concerns, data management, caching, messaging, and architectural patterns where relevant, but do not force every topic into every answer.
- Use concrete entity names, request/response shapes, and component responsibilities instead of vague prose.
- If the user asks for one section only, do not preview or foreshadow the others unless needed for clarity.

## Output Pattern

Use headings that match the requested sections exactly when possible.
For a full design, default to this flow:

1. Functional Requirements
2. Non-Functional Requirements
3. Data Model
4. API Design
5. High-Level Design
6. Deep Dives

For a partial design, keep the same relative ordering and omit everything not requested.

## Examples

- "Design TinyURL with functional requirements, non-functional requirements, data model, API design, and high-level design."
- "Add only a Deep Dives section for notification fanout bottlenecks."
- "Draft the API Design and High-Level Design sections for Dropbox."
