# Keep It Together 🏰

> *An idle castle defense game where you recruit and manage AI-driven NPCs to defend your castle from endless hordes — while trying to keep everyone alive and sane.*

**Platforms:** iOS & Android (Unity, C#)

---

## 🎮 Concept

You are the **King of a crumbling castle**. Hordes of enemies approach in waves. Your only hope? Recruiting a ragtag band of NPCs — each with their own personality, quirks, needs, and (questionable) combat skills.

The twist: **you don't directly control combat.** Your defenders fight, eat, sleep, panic, argue, and occasionally do something heroic — all on their own. Your job is to **manage, recruit, equip, and keep morale up** so they don't fall apart before the enemy does.

---

## 🎯 Scope

### Core Pillars

| Pillar | Description |
|--------|-------------|
| **Idle Castle Defense** | Enemies attack in waves automatically. Defenders fight on their own based on AI behavior. Progress continues while offline. |
| **NPC Management** | Recruit defenders, assign roles, manage needs (hunger, morale, rest). Neglected NPCs desert, break down, or rebel. |
| **Emergent AI Personalities** | Each NPC develops traits over time — brave, cowardly, vengeful, loyal. They form relationships, hold grudges, and surprise you. |
| **Adaptive Enemy AI** | Enemy waves learn from your defensive patterns and adapt tactics. No single strategy works forever. |
| **Procedural Events** | AI-generated scenarios, dilemmas, and events keep each playthrough unique. |

### What This Game IS

- An **idle/management** game — the player makes strategic decisions, not tactical ones
- A **story generator** — emergent NPC behavior creates memorable, shareable moments
- A **castle defense** — waves of enemies with escalating difficulty
- **Humorous in tone** — NPCs complain, things go wrong, chaos is part of the fun
- **Cross-platform mobile** — designed for short sessions and offline progression

### What This Game is NOT

- Not a real-time strategy game (no direct unit control in combat)
- Not a hardcore survival sim (approachable, funny, not punishing)
- Not a PvP game (single-player focus for v1)
- Not a 3D AAA experience (2D or 2.5D art style, charm over fidelity)

---

## 🧠 ML / AI Systems

### 1. NPC Behavior (ML-Agents / Reinforcement Learning)
- NPCs trained via Unity ML-Agents to handle combat, self-preservation, and social behavior
- Personality traits influence reward functions (brave NPCs get rewarded for charging in, cowards for staying alive)
- NPCs learn from in-game experiences, not just pre-programmed behavior trees

### 2. Adaptive Enemy Director
- Tracks player's defensive composition and strategies
- Adjusts wave composition, timing, and tactics to counter the player
- Ensures the game stays challenging without being unfair (rubber-banding difficulty)

### 3. Procedural Event Generation
- Context-aware event system that considers current NPC relationships, morale, and game state
- Generates dilemmas: "Two of your best fighters are feuding. Intervene or let them sort it out?"
- Can use lightweight on-device LLM or rule-based system with ML-ranked outcomes

---

## 🏗️ Architecture (High-Level)

```
┌─────────────────────────────────────────────┐
│                  GAME CLIENT                │
│               (Unity / C#)                  │
├─────────────┬───────────┬───────────────────┤
│  UI Layer   │ Game Mgr  │   Idle Engine     │
│  (Recruit,  │ (Waves,   │   (Offline calc,  │
│   Equip,    │  State,   │    time-based     │
│   Manage)   │  Save)    │    progression)   │
├─────────────┴───────────┴───────────────────┤
│              AI / ML Layer                  │
│  ┌──────────┐ ┌──────────┐ ┌─────────────┐ │
│  │ NPC Mind │ │ Enemy    │ │ Event       │ │
│  │ (ML-     │ │ Director │ │ Generator   │ │
│  │ Agents)  │ │ (Adapt)  │ │ (Procedural)│ │
│  └──────────┘ └──────────┘ └─────────────┘ │
├─────────────────────────────────────────────┤
│           Data / Persistence                │
│  (Local save, offline progress, NPC state)  │
└─────────────────────────────────────────────┘
```

---

## 🗺️ Development Phases

### Phase 1: Foundation
- Unity project setup with mobile build targets (iOS/Android)
- Basic castle scene with walls, gate, and defender positions
- Simple NPC spawning and stat system (HP, hunger, morale)
- Basic wave spawner with dummy enemies
- Core idle loop (time-based progression)

### Phase 2: NPC Intelligence
- ML-Agents integration for NPC combat behavior
- Personality trait system (brave, cowardly, lazy, loyal, etc.)
- Basic needs system (hunger, rest, morale decay)
- NPC recruitment mechanic

### Phase 3: Enemy AI & Adaptation
- Enemy type variety (melee, ranged, siege, flying)
- Adaptive director that tracks player strategy
- Escalating wave difficulty with meta-progression

### Phase 4: Emergent Storytelling
- Relationship system between NPCs (friends, rivals, crushes)
- Procedural event/dilemma generator
- NPC memory system (remembers betrayals, heroic moments)
- Notification system for key events while idle

### Phase 5: Polish & Ship
- Art style and animations
- Sound design and music
- Tutorial and onboarding
- Monetization design (ethical — no pay-to-win)
- Beta testing and platform submission

---

## 🛠️ Tech Stack

| Component | Technology |
|-----------|-----------|
| Engine | Unity 2022+ LTS |
| Language | C# |
| ML Framework | Unity ML-Agents Toolkit |
| Platforms | iOS, Android |
| Version Control | Git + GitHub |
| CI/CD | GitHub Actions (build pipelines) |
| Art Style | 2D pixel art or hand-drawn (TBD) |

---

## 📂 Repository Structure (Planned)

```
keep-it-together/
├── Assets/
│   ├── Scripts/
│   │   ├── NPC/           # NPC behavior, personality, needs
│   │   ├── Enemy/         # Enemy types, wave system, director
│   │   ├── Castle/        # Castle state, defenses, upgrades
│   │   ├── ML/            # ML-Agents configs and training
│   │   ├── Events/        # Procedural event system
│   │   ├── Idle/          # Offline progression engine
│   │   └── UI/            # All UI controllers
│   ├── Prefabs/
│   ├── Scenes/
│   ├── Art/
│   └── Audio/
├── ml-agents/             # Training configs and environments
├── Docs/                  # Design docs, GDD
├── .github/workflows/     # CI/CD
└── README.md
```

---

## 💡 Design Principles

1. **Autonomy over control** — The fun is watching your NPCs act on their own, not micromanaging them
2. **Chaos is content** — When things go wrong, it should be funny and memorable, not frustrating
3. **Personality is everything** — Players should care about their NPCs as characters, not just stats
4. **Respect the player's time** — Idle progression should feel rewarding, not punishing for being away
5. **AI should surprise** — If the player can predict everything, the ML isn't doing its job

---

## 📄 License

TBD

---

*Keep it together. Or don't. The castle's on fire anyway.* 🔥
