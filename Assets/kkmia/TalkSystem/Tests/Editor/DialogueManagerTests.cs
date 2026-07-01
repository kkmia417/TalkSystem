using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace kkmia.TalkSystem.Tests
{
    public sealed class DialogueManagerTests
    {
        private const string Csv = "Id,Speaker,Text,NextId\n1,A,Hello,2\n2,A,End,-1\n";
        private const string ChoiceCsv = "Id,Speaker,Text,NextId,EmotionKey,TriggerKey,ConditionKey,EventKey,Choices\n" +
                                         "1,A,Choose,-1,,,,,Go->2|Stay->-1\n" +
                                         "2,A,End,-1,,,,,\n";

        [Test]
        public void Manager_NaturalEnd_HidesViewAndRaisesEndedOnce()
        {
            var view = CreateView("DialogueView");
            var manager = CreateManager(view);
            var endedCount = 0;

            try
            {
                manager.DialogueEnded += _ => endedCount++;

                manager.StartDialogue(1);
                view.RequestNext();
                view.RequestNext();

                Assert.AreEqual(DialogueSessionState.Ended, manager.State);
                Assert.IsFalse(view.gameObject.activeSelf);
                Assert.AreEqual(1, endedCount);
            }
            finally
            {
                Destroy(manager);
                Destroy(view);
            }
        }

        [Test]
        public void Manager_SetView_PreservesSessionAndMovesInputToNewView()
        {
            var firstView = CreateView("FirstDialogueView");
            var secondView = CreateView("SecondDialogueView");
            var manager = CreateManager(firstView);

            try
            {
                manager.StartDialogue(1);
                Assert.AreEqual(1, manager.History.Count);

                manager.SetView(secondView);
                Assert.AreEqual(1, manager.History.Count);

                firstView.RequestNext();
                Assert.AreEqual(1, manager.History.Count);

                secondView.RequestNext();
                Assert.AreEqual(2, manager.History.Count);
                Assert.AreEqual(DialogueSessionState.WaitingForInput, manager.State);
            }
            finally
            {
                Destroy(manager);
                Destroy(firstView);
                Destroy(secondView);
            }
        }

        [Test]
        public void Manager_ExposesCurrentChoiceCount()
        {
            var view = CreateView("ChoiceDialogueView");
            var manager = CreateManager(view, ChoiceCsv);

            try
            {
                manager.StartDialogue(1);

                Assert.AreEqual(DialogueSessionState.ChoicePending, manager.State);
                Assert.AreEqual(2, manager.CurrentChoiceCount);
                Assert.IsTrue(manager.HasCurrentChoices);
            }
            finally
            {
                Destroy(manager);
                Destroy(view);
            }
        }

        private static DialogueManager CreateManager(DialogueView view)
        {
            return CreateManager(view, Csv);
        }

        private static DialogueManager CreateManager(DialogueView view, string csv)
        {
            // EditMode テストでは Awake が自動で呼ばれないため、フィールド設定後に明示的に起動する。
            var gameObject = new GameObject("DialogueManager");
            var manager = gameObject.AddComponent<DialogueManager>();
            SetPrivateField(manager, "csvFile", new TextAsset(csv));
            SetPrivateField(manager, "view", view);
            Invoke(manager, "Awake");
            return manager;
        }

        private static void Invoke(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(target, null);
        }

        private static DialogueView CreateView(string name)
        {
            return new GameObject(name).AddComponent<DialogueView>();
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            field.SetValue(target, value);
        }

        private static void Destroy(Component component)
        {
            if (component != null)
                Object.DestroyImmediate(component.gameObject);
        }
    }
}
