# Calliope
Calliope is a procedural narrative assembly framework for Unity that enables dynamic, character-driven dialogue through fragment-based text composition and trait-weighted saliency selection. Named after the Greek Muse of epic poetry who inspired Homer's great works, Calliope gives voice to game characters by intelligently selecting and assembling dialogue based on personality traits, relationships, and narrative context.
The system addresses a gap between existing narrative middleware solutions: Yarn Spinner and Ink excel at branching dialogue but require explicit authoring of every path, while pure grammar-based generation produces text that can feel mechanical. Calliope occupies a middle ground—authored fragments with procedural assembly—enabling emergent character moments while maintaining authorial control over tone and content.

**Key Innovations**
- Fragment-Based Assembly: Dialogue is composed from modular pieces (openers, stances, closers) rather than complete sentences, enabling combinatorial variety from a smaller content corpus.
- Trait-Weighted Saliency: Character personality traits influence content selection probabilistically rather than as hard gates, creating nuanced character expression.
- Combined Casting Scores: Multi-character scenes cast roles using a weighted combination of trait affinity and relationship values, enabling emergent character dynamics.
- Professional Architecture: Built with enterprise software patterns (Repository, Strategy, Observer, Clean Architecture) for extensibility and testability.
