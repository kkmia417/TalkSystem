using System.Collections.Generic;
using NUnit.Framework;

namespace kkmia.TalkSystem.Tests
{
    public sealed class DialogueAudioTests
    {
        private sealed class RecordingAudioPlayer : IDialogueAudioPlayer
        {
            public readonly List<string> Calls = new List<string>();

            public void PlayBgm(string bgmKey, bool stop, string transition, float duration)
            {
                Calls.Add(stop ? "bgm:stop:" + transition + ":" + duration : "bgm:" + bgmKey + ":" + transition + ":" + duration);
            }

            public void PlaySe(string seKey)
            {
                Calls.Add("se:" + seKey);
            }

            public void PlayVoice(string voiceKey)
            {
                Calls.Add("voice:" + voiceKey);
            }

            public void StopVoice()
            {
                Calls.Add("stopVoice");
            }

            public void StopAll()
            {
                Calls.Add("stopAll");
            }
        }

        private static DialogueData BuildRow(string bgm, string se, string voice)
        {
            var csv = "Id,Speaker,Text,NextId,Bgm,Se,Voice\n" +
                      "1,A,Hi,-1," + Quote(bgm) + "," + Quote(se) + "," + Quote(voice) + "\n";
            return CsvLoader.ParseText<DialogueData>(csv)[1];
        }

        private static string Quote(string value)
        {
            value = value ?? string.Empty;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        [Test]
        public void Director_PlaysBgmSeAndVoiceInOrder()
        {
            var player = new RecordingAudioPlayer();
            var director = new DialogueAudioDirector(player);

            director.Apply(BuildRow("theme#fade:2", "door|step", "line_001"));

            CollectionAssert.AreEqual(
                new[] { "bgm:theme:fade:2", "se:door", "se:step", "voice:line_001" },
                player.Calls);
        }

        [Test]
        public void Director_BgmStopKeyword_StopsBgm()
        {
            var player = new RecordingAudioPlayer();
            var director = new DialogueAudioDirector(player);

            director.Apply(BuildRow("stop", string.Empty, string.Empty));

            Assert.AreEqual(1, player.Calls.Count);
            Assert.AreEqual("bgm:stop::0", player.Calls[0]);
        }

        [Test]
        public void Director_NoAudioFields_DoesNothing()
        {
            var player = new RecordingAudioPlayer();
            var director = new DialogueAudioDirector(player);

            director.Apply(BuildRow(string.Empty, string.Empty, string.Empty));

            Assert.IsEmpty(player.Calls);
        }

        [Test]
        public void Director_StopAll_DelegatesToPlayer()
        {
            var player = new RecordingAudioPlayer();
            var director = new DialogueAudioDirector(player);

            director.StopAll();

            CollectionAssert.AreEqual(new[] { "stopAll" }, player.Calls);
        }

        [Test]
        public void LipSync_Rms_ComputesRootMeanSquare()
        {
            var samples = new[] { 0.5f, -0.5f, 0.5f, -0.5f };

            var rms = DialogueLipSyncMath.Rms(samples, samples.Length);

            Assert.AreEqual(0.5f, rms, 1e-5f);
        }

        [Test]
        public void LipSync_Openness_RespectsThresholdAndClamp()
        {
            // しきい値未満は 0。
            Assert.AreEqual(0f, DialogueLipSyncMath.Openness(0.01f, 0.02f, 12f));
            // しきい値超過分 * 感度、上限 1 にクランプ。
            Assert.AreEqual(1f, DialogueLipSyncMath.Openness(0.5f, 0.02f, 12f));
            // 中間値。
            Assert.AreEqual((0.07f - 0.02f) * 5f, DialogueLipSyncMath.Openness(0.07f, 0.02f, 5f), 1e-5f);
        }
    }
}
