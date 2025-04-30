using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace kkmia.TalkSystem
{
    public static class CsvLoader
    {
        /// <summary>
        /// CSVファイルを読み込んで辞書に変換します。
        /// </summary>
        /// <typeparam name="T">DialogueDataを継承した型</typeparam>
        /// <param name="csv">TextAsset 形式のCSVファイル</param>
        /// <returns>Idをキーとした辞書</returns>
        public static Dictionary<int, T> Parse<T>(TextAsset csv) where T : DialogueData, new()
        {
            if (csv == null)
            {
                Debug.LogError("CsvLoader: csvファイルが null です。");
                return new Dictionary<int, T>();
            }

            var dict = new Dictionary<int, T>();
            var lines = csv.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length <= 1)
            {
                Debug.LogWarning("CsvLoader: データ行が存在しません。");
                return dict;
            }

            for (int i = 1; i < lines.Length; i++) // 1行目はヘッダー行
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                var cols = line.Split(',');

                try
                {
                    var obj = new T
                    {
                        Id           = ParseInt(cols, 0),
                        Speaker      = GetSafe(cols, 1),
                        Text         = GetSafe(cols, 2),
                        NextId       = ParseInt(cols, 3),
                        EmotionKey   = GetSafe(cols, 4),
                        TriggerKey   = GetSafe(cols, 5),
                        ConditionKey = GetSafe(cols, 6)
                    };

                    if (!dict.ContainsKey(obj.Id))
                        dict[obj.Id] = obj;
                    else
                        Debug.LogWarning($"CsvLoader: 重複するIDが見つかりました ({obj.Id})。上書きされます。");
                }
                catch (Exception e)
                {
                    Debug.LogError($"CsvLoader: 行のパースに失敗しました（{i+1}行目）。内容: \"{line}\"\n例外: {e.Message}");
                }
            }

            return dict;
        }

        private static string GetSafe(string[] cols, int index)
        {
            return (index < cols.Length) ? cols[index] : null;
        }

        private static int ParseInt(string[] cols, int index)
        {
            if (index >= cols.Length || string.IsNullOrWhiteSpace(cols[index]))
                return -1;

            if (int.TryParse(cols[index], out int result))
                return result;

            throw new FormatException($"数値変換に失敗: {cols[index]}");
        }
    }
}
