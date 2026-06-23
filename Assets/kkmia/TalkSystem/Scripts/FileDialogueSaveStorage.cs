using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace kkmia.TalkSystem
{
    /// <summary>
    /// <see cref="IDialogueSaveStorage"/> のファイル実装。スロットを JSON、サムネイルを PNG として
    /// 既定では <c>Application.persistentDataPath</c> 配下に保存する。
    /// </summary>
    public sealed class FileDialogueSaveStorage : IDialogueSaveStorage
    {
        private const string SlotPrefix = "slot_";
        private readonly string _directory;

        public FileDialogueSaveStorage(string directory = null)
        {
            _directory = string.IsNullOrEmpty(directory)
                ? Path.Combine(Application.persistentDataPath, "dialogue_saves")
                : directory;
        }

        public bool TryLoad(int slot, out DialogueSaveSlot data)
        {
            data = null;
            var path = SlotPath(slot);
            if (!File.Exists(path)) return false;

            try
            {
                var json = File.ReadAllText(path);
                data = JsonUtility.FromJson<DialogueSaveSlot>(json);
                return data != null;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[FileDialogueSaveStorage] スロット " + slot + " の読み込みに失敗しました: " + e.Message);
                return false;
            }
        }

        public void Save(DialogueSaveSlot slot)
        {
            if (slot == null) return;
            EnsureDirectory();

            try
            {
                File.WriteAllText(SlotPath(slot.SlotIndex), JsonUtility.ToJson(slot));
            }
            catch (System.Exception e)
            {
                Debug.LogError("[FileDialogueSaveStorage] スロット " + slot.SlotIndex + " の保存に失敗しました: " + e.Message);
            }
        }

        public void Delete(int slot)
        {
            TryDelete(SlotPath(slot));
            TryDelete(ThumbnailPath(slot));
        }

        public bool Exists(int slot)
        {
            return File.Exists(SlotPath(slot));
        }

        public IEnumerable<int> ListSlots()
        {
            var result = new List<int>();
            if (!Directory.Exists(_directory)) return result;

            foreach (var file in Directory.GetFiles(_directory, SlotPrefix + "*.json"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                int index;
                if (int.TryParse(name.Substring(SlotPrefix.Length), out index))
                    result.Add(index);
            }

            result.Sort();
            return result;
        }

        public byte[] LoadThumbnail(int slot)
        {
            var path = ThumbnailPath(slot);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        public void SaveThumbnail(int slot, byte[] pngBytes)
        {
            if (pngBytes == null || pngBytes.Length == 0) return;
            EnsureDirectory();

            try
            {
                File.WriteAllBytes(ThumbnailPath(slot), pngBytes);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[FileDialogueSaveStorage] サムネイル " + slot + " の保存に失敗しました: " + e.Message);
            }
        }

        private void EnsureDirectory()
        {
            if (!Directory.Exists(_directory))
                Directory.CreateDirectory(_directory);
        }

        private string SlotPath(int slot)
        {
            return Path.Combine(_directory, SlotPrefix + slot + ".json");
        }

        private string ThumbnailPath(int slot)
        {
            return Path.Combine(_directory, SlotPrefix + slot + ".png");
        }

        private static void TryDelete(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
