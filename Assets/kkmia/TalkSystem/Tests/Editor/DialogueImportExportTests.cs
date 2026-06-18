using NUnit.Framework;
using kkmia.TalkSystem.Editor;

namespace kkmia.TalkSystem.Tests
{
    public sealed class DialogueImportExportTests
    {
        [Test]
        public void CsvToJsonToCsv_PreservesDialogueText()
        {
            var csv = "Id,Speaker,Text,NextId\n1,A,\"Hello, JSON\",-1\n";

            var json = DialogueImportExportUtility.CsvToJson(csv);
            var roundTrip = DialogueImportExportUtility.JsonToCsv(json);
            var parsed = CsvLoader.ParseText<DialogueData>(roundTrip);

            Assert.AreEqual("Hello, JSON", parsed[1].Text);
        }

        [Test]
        public void YarnLikeToCsv_ImportsSpeakerLine()
        {
            var csv = DialogueImportExportUtility.YarnLikeToCsv("Guide: Hello from text");
            var parsed = CsvLoader.ParseText<DialogueData>(csv);

            Assert.AreEqual("Guide", parsed[1].Speaker);
            Assert.AreEqual("Hello from text", parsed[1].Text);
        }
    }
}
