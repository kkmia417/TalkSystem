# Runtime API

## Extension Points

- `IDialogueConditionEvaluator`
- `IDialogueVariableResolver`
- `IDialogueTextResolver`
- `IDialogueEventDispatcher`
- `IDialogueView`

## Save Data

Use `DialogueManager.CaptureState()` and `DialogueManager.RestoreState(saveData)` to integrate with your game's save system.

Talk System does not own persistence; it gives you serializable state to store wherever your project already stores save data.

## Async Loading And Large Projects

Use `IDialogueRepositoryLoader` when dialogue data is loaded after scene boot, from remote content, or from Addressables.

```csharp
DialogueManager.Instance.LoadRepository(
    new TextAssetDialogueRepositoryLoader(dialogueCsv));
```

For large games, split dialogue into multiple databases and merge them after loading:

```csharp
var loader = new CompositeDialogueRepositoryLoader(new IDialogueRepositoryLoader[]
{
    new TextAssetDialogueRepositoryLoader(commonCsv),
    new TextAssetDialogueRepositoryLoader(chapterCsv)
});

DialogueManager.Instance.LoadRepository(loader);
```

Addressables support is intentionally dependency-free in the runtime package. Load your Addressable `TextAsset` in project code, then pass it into `TextAssetDialogueRepositoryLoader`, or implement `IDialogueRepositoryLoader` directly for custom caching, patching, or remote delivery.

Missing data should call the loader `onError` callback with a clear message; `DialogueManager.ErrorRaised` can route that message into your telemetry or debug UI.
