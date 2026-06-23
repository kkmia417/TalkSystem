using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace kkmia.TalkSystem.Tests
{
    public sealed class DialoguePresenterTests
    {
        [Test]
        public void Presenter_RebindAndDispose_UnsubscribesOldView()
        {
            var repo = CreateRepository();
            var first = new FakeDialogueView();
            var second = new FakeDialogueView();
            var presenter = new DialoguePresenter(repo, first);

            Assert.AreEqual(1, first.NextSubscriberCount);

            presenter.BindView(second);
            Assert.AreEqual(0, first.NextSubscriberCount);
            Assert.AreEqual(1, second.NextSubscriberCount);

            presenter.Dispose();
            Assert.AreEqual(0, second.NextSubscriberCount);
        }

        [Test]
        public void Presenter_NextRequested_AdvancesOnlyOnce()
        {
            var repo = CreateRepository();
            var view = new FakeDialogueView();
            var presenter = new DialoguePresenter(repo, view);

            presenter.Start(1);
            view.RaiseNext();

            Assert.AreEqual(2, presenter.CurrentData.Id);
        }

        [Test]
        public void Presenter_FinalNext_ClearsViewAndRaisesEndedOnce()
        {
            var repo = CreateRepository();
            var view = new FakeDialogueView();
            var presenter = new DialoguePresenter(repo, view);
            var endedCount = 0;
            presenter.DialogueEnded += _ => endedCount++;

            presenter.Start(1);
            view.RaiseNext();
            view.RaiseNext();
            presenter.End();

            Assert.AreEqual(DialogueSessionState.Ended, presenter.State);
            Assert.AreEqual(1, view.ClearCount);
            Assert.AreEqual(1, endedCount);
        }

        [Test]
        public void Presenter_RestoreState_DoesNotReplayHistoryLineEventsOrDispatcher()
        {
            var repo = CreateRepositoryWithEvent();
            var view = new FakeDialogueView();
            var presenter = new DialoguePresenter(repo, view);
            var lineStartedCount = 0;
            var eventDispatchCount = 0;
            presenter.LineStarted += _ => lineStartedCount++;
            presenter.SetEventDispatcher(new CountingEventDispatcher(() => eventDispatchCount++));

            presenter.Start(1);
            var saveData = presenter.CaptureState();
            var historyCount = presenter.Session.History.Count;

            Assert.IsTrue(presenter.RestoreState(saveData));

            Assert.AreEqual(historyCount, presenter.Session.History.Count);
            Assert.AreEqual(1, lineStartedCount);
            Assert.AreEqual(1, eventDispatchCount);
            Assert.AreEqual(saveData.State, presenter.State);
        }

        [Test]
        public void Presenter_RebindRefresh_PreservesSessionAndMovesInputToNewView()
        {
            var repo = CreateRepository();
            var first = new FakeDialogueView();
            var second = new FakeDialogueView();
            var presenter = new DialoguePresenter(repo, first);

            presenter.Start(1);
            var historyCount = presenter.Session.History.Count;

            presenter.BindView(second);
            presenter.RefreshView();
            first.RaiseNext();

            Assert.AreEqual(1, presenter.CurrentData.Id);
            Assert.AreEqual(historyCount, presenter.Session.History.Count);

            second.RaiseNext();

            Assert.AreEqual(2, presenter.CurrentData.Id);
        }

        private static DialogueRepository CreateRepository()
        {
            return new DialogueRepository(CsvLoader.ParseText<DialogueData>(
                "Id,Speaker,Text,NextId\n1,A,Hello,2\n2,A,End,-1\n").Values);
        }

        private static DialogueRepository CreateRepositoryWithEvent()
        {
            return new DialogueRepository(CsvLoader.ParseText<DialogueData>(
                "Id,Speaker,Text,NextId,EmotionKey,TriggerKey,ConditionKey,EventKey\n1,A,Hello,-1,,,,line_event\n").Values);
        }

        private sealed class CountingEventDispatcher : IDialogueEventDispatcher
        {
            private readonly Action _onDispatch;

            public CountingEventDispatcher(Action onDispatch)
            {
                _onDispatch = onDispatch;
            }

            public void Dispatch(DialogueEventContext context)
            {
                if (_onDispatch != null)
                    _onDispatch();
            }
        }

        private sealed class FakeDialogueView : IDialogueView
        {
            private Action _nextRequested;
            private Action<int> _choiceSelected;

            public int NextSubscriberCount { get; private set; }
            public int ChoiceSubscriberCount { get; private set; }
            public int ClearCount { get; private set; }
            public bool IsTyping { get; private set; }

            public event Action NextRequested
            {
                add
                {
                    _nextRequested += value;
                    NextSubscriberCount++;
                }
                remove
                {
                    _nextRequested -= value;
                    NextSubscriberCount--;
                }
            }

            public event Action<int> ChoiceSelected
            {
                add
                {
                    _choiceSelected += value;
                    ChoiceSubscriberCount++;
                }
                remove
                {
                    _choiceSelected -= value;
                    ChoiceSubscriberCount--;
                }
            }

            public void Show(DialogueData data, IReadOnlyList<DialogueChoice> choices, Action onComplete)
            {
                IsTyping = false;
                if (onComplete != null)
                    onComplete();
            }

            public void CompleteTyping()
            {
                IsTyping = false;
            }

            public void Clear()
            {
                ClearCount++;
            }

            public void ForceStop()
            {
            }

            public void SetTypewriterSpeed(float newInterval)
            {
            }

            public void RaiseNext()
            {
                if (_nextRequested != null)
                    _nextRequested();
            }

            public void RaiseChoice(int index)
            {
                if (_choiceSelected != null)
                    _choiceSelected(index);
            }
        }
    }
}
