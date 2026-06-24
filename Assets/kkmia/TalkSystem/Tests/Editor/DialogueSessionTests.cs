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
        public void Session_RevisitsConditionFailingNode_ContinuesWithoutEnding()
        {
            // 2 は条件不成立で常にスキップされ、3 から再び 2 へ戻る構造。
            // _skipGuard がセッション全体で共有されていると、2 回目の再訪で
            // 会話が誤って終了してしまう（regression: issue #37）。
            var csv = "Id,Speaker,Text,NextId,EmotionKey,TriggerKey,ConditionKey,EventKey,Choices\n" +
                      "1,A,Hello,2,,,,,\n" +
                      "2,A,Gated,3,,,gate,,\n" +
                      "3,A,Middle,2,,,,,\n";
            var repo = new DialogueRepository(CsvLoader.ParseText<DialogueData>(csv).Values);
            var session = new DialogueSession(repo)
            {
                ConditionEvaluator = new BlockKeyConditionEvaluator("gate")
            };

            Assert.IsTrue(session.Start(1));

            // 1 回目: 2 をスキップして 3 へ。
            Assert.IsTrue(session.Advance());
            Assert.AreEqual(3, session.CurrentData.Id);

            // 2 回目: 再び 2 をスキップして 3 へ戻る。終了してはならない。
            Assert.IsTrue(session.Advance());
            Assert.AreEqual(3, session.CurrentData.Id);
            Assert.AreNotEqual(DialogueSessionState.Ended, session.State);
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

        [Test]
        public void Session_Restore_PreservesHistoryAndSavedState()
        {
            var repo = new DialogueRepository(CsvLoader.ParseText<DialogueData>(
                "Id,Speaker,Text,NextId\n1,A,Hello,2\n2,A,End,-1\n").Values);

            var source = new DialogueSession(repo);
            source.Start(1);
            source.RecordDisplayedLine(source.CurrentData);
            source.MarkLineReady();
            Assert.AreEqual(DialogueSessionState.WaitingForInput, source.State);

            var save = source.Capture();
            Assert.AreEqual(1, save.History.Count);
            Assert.AreEqual(DialogueSessionState.WaitingForInput, save.State);

            var restored = new DialogueSession(repo);
            Assert.IsTrue(restored.Restore(save));

            Assert.AreEqual(1, restored.CurrentData.Id, "保存時の現在行が復元される");
            Assert.AreEqual(1, restored.History.Count, "復元で履歴が増えない");
            Assert.AreEqual(DialogueSessionState.WaitingForInput, restored.State,
                "保存時の State が ShowingLine に潰されず維持される");
        }

        [Test]
        public void Session_StartAgain_ResetsHistorySeenAndChoiceHistory()
        {
            var csv = "Id,Speaker,Text,NextId,EmotionKey,TriggerKey,ConditionKey,EventKey,Choices\n" +
                      "1,A,Choose,-1,,,,,Left->2|Right->3\n" +
                      "2,A,Left,-1,,,,,\n" +
                      "3,A,Right,-1,,,,,\n";
            var repo = new DialogueRepository(CsvLoader.ParseText<DialogueData>(csv).Values);
            var session = new DialogueSession(repo);

            // 1 回目の会話: 行を表示・既読化し、選択肢を選ぶ。
            session.Start(1);
            session.RecordDisplayedLine(session.CurrentData);
            session.MarkLineReady();
            Assert.IsTrue(session.SelectChoice(0)); // 1 -> 2
            session.RecordDisplayedLine(session.CurrentData);

            Assert.AreEqual(1, session.ChoiceHistory.Count);
            Assert.AreEqual(2, session.History.Count);
            Assert.AreEqual(2, session.SeenLineIds.Count);

            // 2 回目の Start で前回会話の状態が残ってはならない。
            session.Start(1);
            session.RecordDisplayedLine(session.CurrentData);

            Assert.AreEqual(0, session.ChoiceHistory.Count, "選択履歴は新会話で空から始まる");
            Assert.AreEqual(1, session.History.Count, "履歴は新会話の表示行だけを含む");
            CollectionAssert.AreEqual(new[] { 1 }, session.SeenLineIds, "既読は新会話の表示済み行だけ");
        }

        private sealed class BlockKeyConditionEvaluator : IDialogueConditionEvaluator
        {
            private readonly string _blockedKey;

            public BlockKeyConditionEvaluator(string blockedKey)
            {
                _blockedKey = blockedKey;
            }

            public bool Evaluate(string conditionKey, DialogueData data)
            {
                return conditionKey != _blockedKey;
            }
        }
    }
}
