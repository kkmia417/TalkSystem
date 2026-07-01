using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace kkmia.TalkSystem.Tests
{
    public sealed class DialogueValidatorTests
    {
        [Test]
        public void ValidateCsv_ReportsMissingNextIdAndDuplicateId()
        {
            var csv = "Id,Speaker,Text,NextId\n1,A,Hello,99\n1,A,Duplicate,-1\n";

            var report = DialogueValidator.ValidateCsv(csv, new[] { 1 });

            Assert.IsTrue(report.Messages.Any(m => m.FieldName == DialogueSchema.Id && m.Severity == DialogueValidationSeverity.Error));
            Assert.IsTrue(report.Messages.Any(m => m.FieldName == DialogueSchema.NextId && m.Severity == DialogueValidationSeverity.Error));
        }

        [Test]
        public void ValidateCsv_ReportsUnreachableRows()
        {
            var csv = "Id,Speaker,Text,NextId\n1,A,Hello,-1\n2,A,Unused,-1\n";

            var report = DialogueValidator.ValidateCsv(csv, new[] { 1 });

            Assert.IsTrue(report.Messages.Any(m => m.Message.Contains("unreachable")));
        }

        [Test]
        public void ValidateCsv_DetectsCycle()
        {
            var csv = "Id,Speaker,Text,NextId\n1,A,Hello,2\n2,A,Loop,1\n";

            var report = DialogueValidator.ValidateCsv(csv, new[] { 1 });

            Assert.IsTrue(report.Messages.Any(m =>
                m.Severity == DialogueValidationSeverity.Info && m.Message.Contains("Cycle detected")));
        }

        [Test]
        public void ValidateCsv_DoesNotReportCycleForAcyclicGraph()
        {
            var csv = "Id,Speaker,Text,NextId\n1,A,Hello,2\n2,A,End,-1\n";

            var report = DialogueValidator.ValidateCsv(csv, new[] { 1 });

            Assert.IsFalse(report.Messages.Any(m => m.Message.Contains("Cycle detected")));
        }

        [Test]
        public void ValidateCsv_WarnsOnMalformedChoiceEntries()
        {
            var csv = "Id,Speaker,Text,NextId,EmotionKey,TriggerKey,ConditionKey,EventKey,Choices\n" +
                      "1,A,Choose,-1,,,,,Good->2|BrokenEntry|AlsoBad->xyz\n" +
                      "2,A,Target,-1,,,,,\n";

            var report = DialogueValidator.ValidateCsv(csv, new[] { 1 });

            var warnings = report.Messages
                .Where(m => m.FieldName == DialogueSchema.Choices &&
                            m.Severity == DialogueValidationSeverity.Warning &&
                            m.Message.Contains("could not be parsed"))
                .ToList();

            Assert.AreEqual(2, warnings.Count);
        }

        [Test]
        public void ValidateCsv_WithProfileReportsMissingPresentationAssets()
        {
            var csv = "Id,Speaker,Text,NextId,EmotionKey,TriggerKey,ConditionKey,EventKey,Choices,AutoNextSeconds,Background,Bgm,Se,Voice,Characters\n" +
                      "1,A,Hello,-1,,,,,,,-missing_bg,missing_bgm,missing_se|missing_se2,missing_voice,\n";
            var profile = ScriptableObject.CreateInstance<DialogueValidationProfile>();
            profile.BackgroundDatabase = ScriptableObject.CreateInstance<BackgroundDatabase>();
            profile.AudioDatabase = ScriptableObject.CreateInstance<AudioDatabase>();
            profile.MissingReferenceSeverity = DialogueValidationSeverity.Error;

            var report = DialogueValidator.ValidateCsv(csv, new[] { 1 }, profile);

            Assert.IsTrue(report.Messages.Any(m => m.FieldName == DialogueSchema.Background && m.Severity == DialogueValidationSeverity.Error));
            Assert.IsTrue(report.Messages.Any(m => m.FieldName == DialogueSchema.Bgm && m.Severity == DialogueValidationSeverity.Error));
            Assert.AreEqual(2, report.Messages.Count(m => m.FieldName == DialogueSchema.Se && m.Severity == DialogueValidationSeverity.Error));
            Assert.IsTrue(report.Messages.Any(m => m.FieldName == DialogueSchema.Voice && m.Severity == DialogueValidationSeverity.Error));
        }

        [Test]
        public void ValidateCsv_WithProfileAcceptsConfiguredPresentationAssets()
        {
            var csv = "Id,Speaker,Text,NextId,EmotionKey,TriggerKey,ConditionKey,EventKey,Choices,AutoNextSeconds,Background,Bgm,Se,Voice,Characters\n" +
                      "1,A,Hello,-1,,,,,,,bg_day,bgm_theme,se_click,voice_001,\n";
            var profile = ScriptableObject.CreateInstance<DialogueValidationProfile>();
            profile.BackgroundDatabase = CreateBackgroundDatabase("bg_day");
            profile.AudioDatabase = CreateAudioDatabase("bgm_theme", "se_click", "voice_001");
            profile.MissingReferenceSeverity = DialogueValidationSeverity.Error;

            var report = DialogueValidator.ValidateCsv(csv, new[] { 1 }, profile);

            Assert.IsFalse(report.Messages.Any(m =>
                (m.FieldName == DialogueSchema.Background ||
                 m.FieldName == DialogueSchema.Bgm ||
                 m.FieldName == DialogueSchema.Se ||
                 m.FieldName == DialogueSchema.Voice) &&
                m.Severity == DialogueValidationSeverity.Error));
        }

        [Test]
        public void ValidateCsv_WithProfileReportsUnknownCatalogKeysAndVariables()
        {
            var csv = "Id,Speaker,Text,NextId,EmotionKey,TriggerKey,ConditionKey,EventKey,Choices\n" +
                      "1,A,Hello {missingName},-1,,,missing_condition,missing_event,Choice->-1 ?missing_choice_condition\n";
            var profile = ScriptableObject.CreateInstance<DialogueValidationProfile>();
            profile.EventKeyCatalog = CreateCatalog("known_event");
            profile.ConditionKeyCatalog = CreateCatalog("known_condition");
            profile.VariableCatalog = CreateCatalog("knownName");

            var report = DialogueValidator.ValidateCsv(csv, new[] { 1 }, profile);

            Assert.IsTrue(report.Messages.Any(m => m.FieldName == DialogueSchema.EventKey && m.Message.Contains("missing_event")));
            Assert.IsTrue(report.Messages.Any(m => m.FieldName == DialogueSchema.ConditionKey && m.Message.Contains("missing_condition")));
            Assert.IsTrue(report.Messages.Any(m => m.FieldName == DialogueSchema.Choices && m.Message.Contains("missing_choice_condition")));
            Assert.IsTrue(report.Messages.Any(m => m.FieldName == "Variable" && m.Message.Contains("missingName")));
        }

        private static BackgroundDatabase CreateBackgroundDatabase(string key)
        {
            var database = ScriptableObject.CreateInstance<BackgroundDatabase>();
            var texture = new Texture2D(1, 1);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero);

            var serialized = new SerializedObject(database);
            var backgrounds = serialized.FindProperty("backgrounds");
            backgrounds.arraySize = 1;
            var entry = backgrounds.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("backgroundKey").stringValue = key;
            entry.FindPropertyRelative("sprite").objectReferenceValue = sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return database;
        }

        private static AudioDatabase CreateAudioDatabase(string bgmKey, string seKey, string voiceKey)
        {
            var database = ScriptableObject.CreateInstance<AudioDatabase>();
            SetAudioEntry(database, "bgm", bgmKey);
            SetAudioEntry(database, "se", seKey);
            SetAudioEntry(database, "voice", voiceKey);
            return database;
        }

        private static void SetAudioEntry(AudioDatabase database, string fieldName, string key)
        {
            var clip = AudioClip.Create(key, 1, 1, 44100, false);
            var serialized = new SerializedObject(database);
            var entries = serialized.FindProperty(fieldName);
            entries.arraySize = 1;
            var entry = entries.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("audioKey").stringValue = key;
            entry.FindPropertyRelative("clip").objectReferenceValue = clip;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static DialogueKeyCatalog CreateCatalog(params string[] keys)
        {
            var catalog = ScriptableObject.CreateInstance<DialogueKeyCatalog>();
            catalog.SetKeys(keys);
            return catalog;
        }
    }
}
