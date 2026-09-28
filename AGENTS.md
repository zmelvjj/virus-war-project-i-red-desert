# Unity / C# Working Rules

## Core

* Implement the smallest clear solution that satisfies the request.
* Follow existing project architecture and conventions before introducing new ones.
* Modify only files relevant to the task. Do not perform unrelated refactors or cleanup.
* Search narrowly: target symbol → direct dependencies/callers → expand only if blocked.
* Do not read generated/cache folders or large Unity YAML files unless required.

## Code Design

* Do not add excessive defensive programming for developer configuration errors.
* Required missing references should normally remain visible instead of being auto-found, created, replaced, or silently repaired.
* Do not spam `Debug.Log*` unless explicitly requested or genuinely needed for diagnosis.
* Do not connect systems through unnecessary serialized/direct references.
* Inspector references are mainly for configurable values, assets, prefabs, and intentional scene references.
* Shared systems should use the project's existing service/global access architecture.
* Do not hardcode values that are intended to be developer-configurable.
* Never overwrite Inspector-configured values at runtime unless explicitly required.
* Keep responsibilities separated, but do not create classes/interfaces/abstractions without a concrete reason.
* Prefer composition and clear dependency direction. Avoid circular dependencies.
* Reuse existing managers, pools, services, factories, utilities, and patterns before creating new ones.
* Avoid unnecessary work in hot paths such as repeated searches, allocations, component lookups, or resource loading.

## Unity

* Treat scenes, prefabs, and Inspector configuration as developer-owned content.
* Do not modify scenes/prefabs unless the task actually requires it.
* For simple component attachment or Inspector assignment, tell the developer what to assign instead of using editor automation.
* Do not enter Play Mode unless the developer explicitly requests it.
* Use Unity MCP only when actual Editor/scene manipulation is necessary.
* If MCP is unavailable, complete safe code work and leave required Editor steps to the developer.

## Validation

* Maximum 2 compile validation attempts per task.
* Normal flow: implement → compile → fix → compile once more.
* If compilation still fails, stop and report the remaining error instead of continuing the loop.

## Stop Rule

Stop when the requested behavior is implemented and reasonably validated.

Do not add extra safeguards, abstractions, tests, logs, documentation, refactors, or tooling merely to make the solution appear more complete.
