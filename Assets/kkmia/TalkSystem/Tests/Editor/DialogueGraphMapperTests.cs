using NUnit.Framework;
using kkmia.TalkSystem.Editor;

namespace kkmia.TalkSystem.Tests
{
    public sealed class DialogueGraphMapperTests
    {
        [Test]
        public void FromCsv_BuildsNextAndChoiceEdges()
        {
            var csv = "Id,Speaker,Text,NextId,EmotionKey,TriggerKey,ConditionKey,EventKey,Choices,AutoNextSeconds\n" +
                      "1,A,Start,2,,,,,Choice->3,\n" +
                      "2,A,Next,-1,,,,,,\n" +
                      "3,A,Choice,-1,,,,,,\n";

            var model = DialogueGraphMapper.FromCsv(csv);

            Assert.AreEqual(3, model.Nodes.Count);
            Assert.AreEqual(2, model.Edges.Count);
            Assert.IsFalse(model.Diagnostics.HasErrors);
        }

        [Test]
        public void ToCsv_RoundTripsEditableNodeFields()
        {
            var model = DialogueGraphMapper.FromCsv("Id,Speaker,Text,NextId\n1,A,Hello,-1\n");
            var node = model.Find(1);
            node.text = "Hello, graph";

            var csv = DialogueGraphMapper.ToCsv(model);
            var parsed = CsvLoader.ParseText<DialogueData>(csv);

            Assert.AreEqual("Hello, graph", parsed[1].Text);
        }
    }
}
