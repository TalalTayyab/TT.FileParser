using NUnit.Framework;
using TT.FileParserFunction;

namespace FileLogicTest.Unit
{
    public class ParseLineUnitTests
    {
        [Test]
        [TestCase("this is a test with abcd", "a*d", true)]
        [TestCase("this is a test with Abcd", "a*d", true)]
        [TestCase("this is a test with abcdef", "a?c*f", true)]
        [TestCase("this is a test with abcf", "a?c*f", true)]
        [TestCase("this is a test with acdef", "a?c*f", false)]
        [TestCase("this is a test with abcd", "a?d", false)]
        [TestCase("this is a test with abcde", "a*d", false)]
        public void GivenInputWhenMatchedReturnsResult(string input, string pattern, bool expected)
        {
            var parseLine = new ParseLine();

            var result = parseLine.IsMatch(input, pattern);

            Assert.AreEqual(result, expected);
        }
    }
}