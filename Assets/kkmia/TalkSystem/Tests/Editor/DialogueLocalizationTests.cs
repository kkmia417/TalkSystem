using System.Collections.Generic;
using NUnit.Framework;

namespace kkmia.TalkSystem.Tests
{
    public sealed class DialogueLocalizationTests
    {
        private sealed class DictVariableResolver : IDialogueVariableResolver
        {
            private readonly Dictionary<string, string> _values;

            public DictVariableResolver(Dictionary<string, string> values)
            {
                _values = values;
            }

            public bool TryResolve(string variableName, DialogueData data, out string value)
            {
                return _values.TryGetValue(variableName, out value);
            }
        }

        private static DialogueData Line(int id, string text)
        {
            var csv = "Id,Speaker,Text,NextId\n" + id + ",A,\"" + text + "\",-1\n";
            return CsvLoader.ParseText<DialogueData>(csv)[id];
        }

        [Test]
        public void Resolve_ReturnsLanguageSpecificText()
        {
            var table = new DialogueTranslationTable();
            table.Add(1, "ja", "こんにちは");
            table.Add(1, "en", "Hello");
            var resolver = new LocalizedDialogueTextResolver(table, "ja");

            var data = Line(1, "default");
            Assert.AreEqual("こんにちは", resolver.Resolve(data, "ja", new EmptyDialogueVariableResolver()));
            Assert.AreEqual("Hello", resolver.Resolve(data, "en", new EmptyDialogueVariableResolver()));
        }

        [Test]
        public void Resolve_FallsBackToFallbackLanguageThenLineText()
        {
            var table = new DialogueTranslationTable();
            table.Add(1, "ja", "こんにちは");
            var resolver = new LocalizedDialogueTextResolver(table, "ja");

            var data = Line(1, "raw text");
            // en 未翻訳 -> フォールバック ja
            Assert.AreEqual("こんにちは", resolver.Resolve(data, "en", new EmptyDialogueVariableResolver()));

            // 翻訳が一切無い行 -> 行の素テキスト
            var untranslated = Line(2, "raw text");
            Assert.AreEqual("raw text", resolver.Resolve(untranslated, "en", new EmptyDialogueVariableResolver()));
        }

        [Test]
        public void Resolve_ExpandsVariablesInLocalizedText()
        {
            var table = new DialogueTranslationTable();
            table.Add(1, "en", "Hello {name}");
            var resolver = new LocalizedDialogueTextResolver(table, "en");
            var vars = new DictVariableResolver(new Dictionary<string, string> { { "name", "Bob" } });

            Assert.AreEqual("Hello Bob", resolver.Resolve(Line(1, "x"), "en", vars));
        }

        [Test]
        public void FromCsv_ParsesIdAndLanguageColumns()
        {
            var table = DialogueTranslationTable.FromCsv(
                "Id,ja,en\n1,こんにちは,Hello\n2,さようなら,Goodbye\n");

            string text;
            Assert.IsTrue(table.TryGet(1, "ja", out text));
            Assert.AreEqual("こんにちは", text);
            Assert.IsTrue(table.TryGet(2, "en", out text));
            Assert.AreEqual("Goodbye", text);
            Assert.IsFalse(table.TryGet(3, "ja", out text));
        }
    }
}
