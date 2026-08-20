# Copilot Instructions

## Project Guidelines
- In WinUiTemplate, IProgramData and ProgramData are the central places for feature toggle flags (e.g. EnableBackups, EnableSingleInstance) with a ToDo comment convention for values that need to be set by the developer.
- Prefer implementing UI state transitions with view-model bindings and commands, minimizing code-behind events and named-control manipulation.

## Localization Guidelines
- Setting names in localization Resource files should always use title case.