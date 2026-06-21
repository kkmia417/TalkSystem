using System.Linq;
using NUnit.Framework;

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
    }
}
