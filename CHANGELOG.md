# Changelog

All notable changes to Talk System are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses semantic versioning for package releases.

## [Unreleased]

### Added

- Stage presentation runtime: `DialogueStageState` (slot occupancy logic), `DialogueStageDirector` (applies a line's background/character directives), `IDialogueStageView` + `DialogueStageView` (UGUI rendering with fades), `BackgroundDatabase`, and `DialogueStageBinder` (auto-wires to `DialogueManager` events). The director is Unity-independent and unit-tested.
- Audio runtime: `DialogueAudioDirector` (applies a line's `Bgm`/`Se`/`Voice`), `IDialogueAudioPlayer` + `DialogueAudioPlayer` (BGM with looped fades, multi-shot SE, per-line voice), `AudioDatabase` (categorized BGM/SE/Voice clip lookup), and `DialogueAudioBinder` (auto-wires to `DialogueManager` events). The director is Unity-independent and unit-tested.
- Lip-sync: `DialogueLipSync` samples the voice `AudioSource` amplitude and drives a 0..1 mouth-openness value (event + optional open/closed sprite swap); signal processing lives in the pure, unit-tested `DialogueLipSyncMath`.
- Presentation (stage) CSV columns: `Background`, `Bgm`, `Se`, `Voice`, and `Characters`, matched by header name and backward compatible with existing CSVs.
- `DialogueMediaCue` parsing for `Background`/`Bgm` cells (`key#transition:duration`, plus `stop`/`none`/`hide`/`clear`).
- `DialogueStageDirective` parsing for the `Characters` column (`Character@slot:expression#animation`, character exit with `-`, full-stage clear with `*`).
- Validation for malformed stage directives, malformed/negative transition durations, and stage-directive characters/expressions against the character expression database.
- Graph editor and JSON import/export now round-trip the presentation columns without data loss.

## [0.2.0] - 2026-06-18

### Added

- UPM package metadata and release-ready repository files.
- Runtime, Editor, and Test assembly boundaries.
- Quoted-field safe CSV reader/writer shared by runtime and editor tooling.
- Dialogue validation reports for schema, duplicate IDs, broken links, unreachable rows, and cycles.
- Dialogue session state machine.
- Runtime extension points for conditions, variables, text resolution, and event dispatch.
- Choice branching, save data, editor preview, editor validation, and EditMode tests.

### Changed

- Dialogue presenter now uses explicit view binding and unbinding to prevent duplicate callbacks.
- Dialogue view supports optional choice buttons, configurable auto advance, and character expression database assets.

## [0.1.0] - 2026-06-18

### Added

- Initial CSV-driven Unity dialogue system.
- Dialogue manager, presenter, repository, view, typewriter effect, CSV editor, prefabs, demo scene, and sample CSV.
