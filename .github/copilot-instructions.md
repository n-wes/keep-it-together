# Copilot Instructions — Keep It Together 🏰

## Project Overview

Keep It Together is an **idle castle defense game** for iOS & Android built with **Unity (C#)**. Players recruit and manage AI-driven NPCs who autonomously defend a castle from enemy waves. The game runs **fully offline** with no cloud dependencies — all AI/ML inference happens on-device.

## Tech Stack

- **Engine:** Unity 2022+ LTS
- **Language:** C#
- **ML Framework:** Unity ML-Agents Toolkit
- **ML Inference:** Unity Barracuda (on-device `.onnx` runtime)
- **Platforms:** iOS, Android
- **Version Control:** Git + GitHub

## Architecture

The codebase follows a layered architecture:

1. **UI Layer** — Recruitment, equipment, and management screens
2. **Game Manager** — Wave control, game state, save/load
3. **Idle Engine** — Offline progression using elapsed time deltas
4. **AI/ML Layer** — NPC behavior (ML-Agents), adaptive enemy director, procedural event generation
5. **Data/Persistence** — Local save data (PlayerPrefs / JSON / SQLite)

## Repository Structure

```
Assets/
├── Scripts/
│   ├── NPC/           # NPC behavior, personality, needs
│   ├── Enemy/         # Enemy types, wave system, director
│   ├── Castle/        # Castle state, defenses, upgrades
│   ├── ML/            # ML-Agents configs and training
│   ├── Events/        # Procedural event system
│   ├── Idle/          # Offline progression engine
│   └── UI/            # All UI controllers
├── Prefabs/
├── Scenes/
├── Art/
└── Audio/
ml-agents/             # Training configs and environments
Docs/                  # Design docs, GDD
```

## Coding Conventions

- Use **C# naming conventions**: `PascalCase` for public members and methods, `camelCase` for local variables, `_camelCase` for private fields.
- Prefer **composition over inheritance** for game systems.
- Use **ScriptableObjects** for data-driven configuration (NPC traits, enemy types, wave definitions, event templates).
- Keep MonoBehaviour scripts **lean** — delegate logic to plain C# classes where possible.
- Use `[SerializeField]` for inspector-exposed private fields instead of making fields public.

## AI / ML Guidelines

- NPC behavior is trained via **Unity ML-Agents** with personality traits influencing reward functions.
- Trained models are exported as `.onnx` files and run on-device via **Barracuda** — never call external APIs.
- The **adaptive enemy director** tracks player strategy and adjusts wave composition at runtime using local game state.
- Procedural events use **on-device generation** with local game context (NPC relationships, morale, etc.) — no cloud LLMs.

## Design Principles (Follow These)

1. **Autonomy over control** — NPCs act on their own; the player manages, not micromanages.
2. **Chaos is content** — Emergent failures should be funny and memorable, not frustrating.
3. **Personality is everything** — NPCs are characters with traits, relationships, and memories, not just stat blocks.
4. **Respect the player's time** — Idle progression must feel rewarding, not punishing for being away.
5. **AI should surprise** — Predictable AI means the ML isn't doing its job.

## Key Constraints

- **Zero cloud dependencies** — No backend servers, no API calls, no cloud costs. Everything runs on-device.
- **Offline-first** — Players never need internet to play.
- **Privacy-friendly** — No player data leaves the device.
- **Mobile-first** — Design for short sessions, touch input, and battery efficiency.
- **Ethical monetization** — No pay-to-win mechanics.

## When Generating Code

- All game logic must be **deterministic and reproducible** for the idle/offline calculation system.
- When implementing NPC behavior, consider the **personality trait system** — different traits should produce different outcomes.
- Enemy AI should **adapt to player patterns**, not use static difficulty curves.
- Idle progression calculations should use **elapsed time deltas**, not real-time clocks or server timestamps.
- Prefer **event-driven communication** (C# events, UnityEvents, or a message bus) over tight coupling between systems.
- Write code that is **testable offline** — mock ML model outputs when unit testing.

## Tone

The game's tone is **humorous and lighthearted**. Comments, variable names, and debug messages can reflect this — but keep production code clean and professional. Save the comedy for player-facing content.
