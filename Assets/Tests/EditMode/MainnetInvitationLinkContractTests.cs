using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class MainnetInvitationLinkContractTests
    {
        [Test]
        public void ExtractsAndNormalizesExactlyOneCanonicalCode()
        {
            string code;
            Assert.That(MainnetInvitationLinkContract.TryExtractCode(
                "lightseeker://invite?code=lsi1-01234567-89abcdef-01234567-89abcdef",
                out code), Is.True);
            Assert.That(code, Is.EqualTo("LSI1-01234567-89ABCDEF-01234567-89ABCDEF"));
        }

        [TestCase("https://invite?code=LSI1-01234567-89ABCDEF-01234567-89ABCDEF")]
        [TestCase("lightseeker://other?code=LSI1-01234567-89ABCDEF-01234567-89ABCDEF")]
        [TestCase("lightseeker://invite?code=LSI1-01234567-89ABCDEG-01234567-89ABCDEF")]
        [TestCase("lightseeker://invite?code=LSI1-01234567-89ABCDEF-01234567-89ABCDEF#leak")]
        [TestCase("lightseeker://invite?code=LSI1-01234567-89ABCDEF-01234567-89ABCDEF&code=LSI1-01234567-89ABCDEF-01234567-89ABCDEF")]
        [TestCase("lightseeker://invite?code=LSI1-01234567-89ABCDEF-01234567-89ABCDEF&wave=a")]
        [TestCase("lightseeker://invite/forward?code=LSI1-01234567-89ABCDEF-01234567-89ABCDEF")]
        [TestCase("lightseeker://invite:1234?code=LSI1-01234567-89ABCDEF-01234567-89ABCDEF")]
        public void RejectsNonCanonicalOrAmbiguousLinks(string value)
        {
            string code;
            Assert.That(MainnetInvitationLinkContract.TryExtractCode(value, out code), Is.False);
            Assert.That(code, Is.Null);
        }
    }
}
