# CSV Schema

Required columns:

```csv
Id,Speaker,Text,NextId,EmotionKey,TriggerKey,ConditionKey
```

Optional columns:

```csv
EventKey,Choices,AutoNextSeconds
```

Optional presentation (stage) columns:

```csv
Background,Bgm,Se,Voice,Characters
```

All columns are matched by header name, so any subset can be added in any order; older CSVs without these columns keep working.

## Choices

Choices use `Label->NextId` entries separated by `|`.

```csv
1,Guide,"Choose a path",-1,,,,choice_shown,"Left->2|Right->3",
```

Conditional choices add `?conditionKey`.

```csv
1,Merchant,"Buy something?",-1,,,,shop_open,"Buy->10?has_money|Leave->20",
```

## Presentation Columns

These columns describe per-line staging. The runtime parses and validates them; the visual/audio playback is applied by the presentation layer (added in later milestones).

### Background and Bgm

Both use the same cue syntax: `key`, `key#transition`, or `key#transition:duration`.

```csv
1,Guide,"A forest clearing.",-1,,,,,,forest#crossfade:1.0,theme#fade:2,,,
```

Use `stop`, `none`, `hide`, or `clear` to clear the layer (e.g. `Bgm = stop`).

### Se

One or more sound-effect keys separated by `|`:

```csv
Se = door|footstep
```

### Voice

A single voice-clip key played for that line (also drives lip-sync later):

```csv
Voice = alice_line_001
```

### Characters

Stage directives separated by `|`. Each entry is `Character@slot:expression#animation`, where `@slot`, `:expression`, and `#animation` are optional.

```csv
Characters = "Alice@left:smile#fadein|Bob@right:angry"
```

- Exit a character with a leading `-`: `-Alice`
- Clear the whole stage with `*`
- Known slots: `left`, `center`, `right` (custom slot names are allowed)

## CSV Escaping

Talk System supports quoted commas, escaped quotes, and quoted multiline text.
