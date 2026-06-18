Talk System

`kkmia.TalkSystem` は、CSVで管理された会話データを使って、シンプルかつ柔軟に会話演出を実現できる初心者向けライブラリです。【Unity】

- 会話データは CSV で定義
- トリガー（TriggerKey）や条件分岐（ConditionKey）に対応
- タイプライター風の演出付きでテキストを表示
- Undo / Redo 対応の CSV エディタ付き
- Manager / Presenter / View に分離された拡張しやすい設計


`kkmia.TalkSystem` is a CSV-driven dialogue system library for Unity.  
It provides a simple and extensible structure for branching conversations, and condition-based dialogue flow.

- CSV-based dialogue data with support for branching
- TriggerKey and ConditionKey filtering system
- Built-in typewriter effect (1 character at a time)
- Editable dialogue CSV via a custom Unity Editor tool
- Clear separation of logic (Manager, Presenter, View)

## Runtime extension points

TalkSystem keeps the existing CSV workflow, while adding extension points for larger projects.

- `IDialogueConditionEvaluator`: evaluates `ConditionKey`
- `IDialogueVariableResolver`: resolves text placeholders such as `{playerName}`
- `IDialogueTextResolver`: swaps inline text, localization keys, or external localization backends
- `IDialogueEventDispatcher`: reacts to `EventKey`
- `DialogueSaveData`: captures/restores current line, seen lines, choices, and trigger state

Optional CSV columns are supported after the original schema:

```csv
Id,Speaker,Text,NextId,EmotionKey,TriggerKey,ConditionKey,EventKey,Choices,AutoNextSeconds
1,Guide,"Hello, {playerName}",-1,,,has_met_guide,greet,"Yes->2|No->3",
```

`Choices` uses `Label->NextId` entries separated by `|`. A choice can add a condition with `?conditionKey`, for example `Buy->10?has_money|Leave->20`.

## Editor tools

Open these from Unity:

- `Tools/kkmia/Dialogue CSV Editor`
- `Tools/kkmia/Dialogue Validator`
- `Tools/kkmia/Dialogue Preview`

The CSV editor now round-trips quoted fields, commas, escaped quotes, and multiline text through the shared runtime CSV codec.

This software is released under the MIT License, see LICENSE.txt.
