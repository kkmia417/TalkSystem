# CSV Schema

Required columns:

```csv
Id,Speaker,Text,NextId,EmotionKey,TriggerKey,ConditionKey
```

Optional columns:

```csv
EventKey,Choices,AutoNextSeconds
```

## Choices

Choices use `Label->NextId` entries separated by `|`.

```csv
1,Guide,"Choose a path",-1,,,,choice_shown,"Left->2|Right->3",
```

Conditional choices add `?conditionKey`.

```csv
1,Merchant,"Buy something?",-1,,,,shop_open,"Buy->10?has_money|Leave->20",
```

## CSV Escaping

Talk System supports quoted commas, escaped quotes, and quoted multiline text.
