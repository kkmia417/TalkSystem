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
    }
}
