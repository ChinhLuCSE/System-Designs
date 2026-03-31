# Canonical Section Order

Always preserve this order when the requested output includes these sections:

1. Functional Requirements
2. Non-Functional Requirements
3. Data Model
4. API Design
5. High-Level Design
6. Deep Dives

When the user asks for only some sections, include only those sections but keep their relative order from the list above.

# Core Design Themes

Use these themes when they materially improve the answer:

- Scalability and performance
- Distributed systems tradeoffs
- Databases and data management
- Caching
- Communication and messaging
- Architectural patterns

# Authoring Rules

- Include only sections or design content the user explicitly requested.
- Do not add extra sections, summaries, diagrams, or implementation details unless the user asks for them.
- If the user asks for more content later, add it in a separate step instead of silently expanding scope.
- Prefer concise, interview-ready writing over tutorial-style explanation.
