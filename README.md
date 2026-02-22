# Core.Network.Simulation

Simulation contracts and helper primitives for **Core.Network**.

This package provides interfaces and supporting types for describing how a networked simulation world works:
simulation ticks, rollback/resim (resimulation), snapshot creation/application, and simulation entity contracts.

It is transport-agnostic and does not include a concrete network transport adapter.

- [Core.Network (base)](https://github.com/Fur-Fighters-Frenzy/Core.Network)

> **Status:** WIP

---

## What’s included

- `ISimulationSystem`, `IRollbackSystem`, `ISimulationPostTickSync` for simulation step and rollback lifecycle hooks
- `ISnapshotSystem<TKind>`, `ISnapshotStore<TSnapshot>`, `Snapshots.ISnapshot` for snapshot production, storage, and apply
- `Snapshots.SimWorldSnapshotHub<TKind, TCodec>` for building and dispatching batched snapshot payloads by system kind
- `SimulationFrame` for tick/delta context shared across simulation APIs
- Entity contracts: `INetEntity`, `ISimEntity`, `IRollbackEntity<TState>`, `IUsesNetEntity`
- Per-tick buffers/utilities: `TickRingBuffer<T>`, `TickValueRing<T>`
- Presentation helpers for prediction vs resim state: `SimulationStateBus`, `EffectsPresenterBase`

---

## Notes

- This package focuses on contracts and shared runtime helpers for simulation orchestration.
- Concrete game/system snapshot formats are implemented by your `ISnapshotSystem<TKind>` implementations.
- The package depends on `Validosik.Core.Network` for shared network types/events used by simulation contracts and snapshot payload building.

---

# Part of the Core Project

This package is part of the **Core** project, which consists of multiple Unity packages.
See the full project here: [Core](https://github.com/Fur-Fighters-Frenzy/Core)

---