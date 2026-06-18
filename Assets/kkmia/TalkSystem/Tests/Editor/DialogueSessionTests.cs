using NUnit.Framework;

namespace kkmia.TalkSystem.Tests
{
    public sealed class DialogueSessionTests
    {
        [Test]
        public void Session_StartAdvanceEnd_TracksStateAndSeenLines()
        {
            var repo = new DialogueRepository(CsvLoader.ParseText<DialogueData>(
                "Id,Speaker,Text,NextId\n1,A,Hello,2\n2,A,End,-1\n").Values);
            var session = new DialogueSession(repo);

            Assert.IsTrue(session.Start(1));
            Assert.AreEqual(DialogueSessionState.ShowingLine, session.State);
            session.MarkLineReady();
            Assert.AreEqual(DialogueSessionState.WaitingForInput, session.State);

            Assert.IsTrue(session.Advance());
            Assert.AreEqual(2, session.CurrentData.Id);
            Assert.AreEqual(2, session.SeenLineIds.Count);

            Assert.IsFalse(session.Advance());
            Assert.AreEqual(DialogueSessionState.Ended, session.State);
        }

        [Test]
        public void Session_SelectChoice_AdvancesToSelectedTarget()
        {
            var csv = "Id,Speaker,Text,NextId,EmotionKey,TriggerKey,ConditionKey,EventKey,Choices\n" +
                      "1,A,Choose,-1,,,,,Left->2|Right->3\n" +
                      "2,A,Left,-1,,,,,\n" +
                      "3,A,Right,-1,,,,,\n";
            var repo = new DialogueRepository(CsvLoader.ParseText<DialogueData>(csv).Values);
            var session = new DialogueSession(repo);

            session.Start(1);
            session.MarkLineReady();
            Assert.AreEqual(DialogueSessionState.ChoicePending, session.State);

            Assert.IsTrue(session.SelectChoice(1));
            Assert.AreEqual(3, session.CurrentData.Id);
        }
    }
}
