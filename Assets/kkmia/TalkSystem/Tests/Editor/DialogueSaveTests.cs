using System.Collections.Generic;
using NUnit.Framework;

namespace kkmia.TalkSystem.Tests
{
    public sealed class DialogueSaveTests
    {
        private sealed class MemoryStorage : IDialogueSaveStorage
        {
            public readonly Dictionary<int, DialogueSaveSlot> Slots = new Dictionary<int, DialogueSaveSlot>();
            public readonly Dictionary<int, byte[]> Thumbs = new Dictionary<int, byte[]>();

            public bool TryLoad(int slot, out DialogueSaveSlot data)
            {
                return Slots.TryGetValue(slot, out data);
            }

            public void Save(DialogueSaveSlot slot)
            {
                Slots[slot.SlotIndex] = slot;
            }

            public void Delete(int slot)
            {
                Slots.Remove(slot);
                Thumbs.Remove(slot);
            }

            public bool Exists(int slot)
            {
                return Slots.ContainsKey(slot);
            }

            public IEnumerable<int> ListSlots()
            {
                return new List<int>(Slots.Keys);
            }

            public byte[] LoadThumbnail(int slot)
            {
                byte[] bytes;
                return Thumbs.TryGetValue(slot, out bytes) ? bytes : null;
            }

            public void SaveThumbnail(int slot, byte[] pngBytes)
            {
                Thumbs[slot] = pngBytes;
            }
        }

        private sealed class FakeContributor : IDialogueSaveContributor
        {
            public int CaptureCount;
            public int RestoreCount;
            public string Restored;

            public void Capture(DialogueSaveData data)
            {
                CaptureCount++;
                data.SetExtra("stage", "Alice@left");
            }

            public void Restore(DialogueSaveData data)
            {
                RestoreCount++;
                data.TryGetExtra("stage", out Restored);
            }
        }

        [Test]
        public void ExtraState_SetOverwritesAndGetReturnsValue()
        {
            var data = new DialogueSaveData();
            data.SetExtra("bgm", "theme");
            data.SetExtra("bgm", "battle");

            string value;
            Assert.IsTrue(data.TryGetExtra("bgm", out value));
            Assert.AreEqual("battle", value);
            Assert.AreEqual(1, data.ExtraState.Count);
            Assert.IsFalse(data.TryGetExtra("missing", out _));
        }

        [Test]
        public void Service_Save_RunsContributorsAndPersistsMetadata()
        {
            var storage = new MemoryStorage();
            var contributor = new FakeContributor();
            var service = new DialogueSaveService(storage, new[] { contributor });

            var slot = service.Save(2, new DialogueSaveData { CurrentDialogueId = 7 }, "Chapter 1", true, 1700000000);

            Assert.AreEqual(1, contributor.CaptureCount);
            Assert.AreEqual(2, slot.SlotIndex);
            Assert.AreEqual("Chapter 1", slot.Title);
            Assert.IsTrue(slot.IsAutosave);
            Assert.IsTrue(storage.Exists(2));

            string stage;
            Assert.IsTrue(storage.Slots[2].Data.TryGetExtra("stage", out stage));
            Assert.AreEqual("Alice@left", stage);
        }

        [Test]
        public void Service_LoadThenApplyRestore_RoundTripsAndRestoresContributors()
        {
            var storage = new MemoryStorage();
            var contributor = new FakeContributor();
            var service = new DialogueSaveService(storage, new[] { contributor });
            service.Save(1, new DialogueSaveData { CurrentDialogueId = 9 }, "t", false, 0);

            var loaded = service.Load(1);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(9, loaded.Data.CurrentDialogueId);

            service.ApplyRestore(loaded.Data);
            Assert.AreEqual(1, contributor.RestoreCount);
            Assert.AreEqual("Alice@left", contributor.Restored);
        }

        [Test]
        public void Service_ListSlots_ReturnsSorted()
        {
            var storage = new MemoryStorage();
            var service = new DialogueSaveService(storage);
            service.Save(3, new DialogueSaveData(), "c", false, 0);
            service.Save(1, new DialogueSaveData(), "a", false, 0);
            service.Save(2, new DialogueSaveData(), "b", false, 0);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, service.ListSlots());
        }

        [Test]
        public void Service_Delete_RemovesSlot()
        {
            var storage = new MemoryStorage();
            var service = new DialogueSaveService(storage);
            service.Save(1, new DialogueSaveData(), "a", false, 0);

            service.Delete(1);

            Assert.IsFalse(service.Exists(1));
            Assert.IsNull(service.Load(1));
        }
    }
}
