# Editor Tools

Open tools from the Unity menu:

- `Tools/kkmia/Dialogue CSV Editor`
- `Tools/kkmia/Dialogue Validator`
- `Tools/kkmia/Dialogue Preview`
- `Tools/kkmia/Dialogue Graph Editor`

The Graph Editor is intended for visual authoring. The CSV Editor remains available for table-style editing.

## Dialogue Preview

`Tools/kkmia/Dialogue Preview` can simulate a CSV without entering Play Mode. Assign the scenario CSV, choose a start ID or `TriggerKey`, and optionally assign a translation CSV plus language/fallback keys.

The preview window exposes condition toggles discovered from `ConditionKey` and choice conditions, and variable text fields discovered from `{variable}` placeholders. The current line shows resolved text, raw text, active choices, hidden choices, `EventKey`, background/audio/voice cues, and character stage directives.

Assign a `DialogueValidationProfile` to surface missing asset and localization warnings while previewing. The window intentionally reports keys and warnings only; project-specific gameplay, gallery UI, and asset loading remain in game code.

## Graph Round Trip

The Graph Editor reads and writes `DialogueSchema.FullHeaders`, including choices, auto-advance, presentation cues, and progress keys. Use this path when authors need visual editing without losing runtime or localization-relevant CSV fields.
