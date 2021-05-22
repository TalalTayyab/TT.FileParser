using NUnit.Framework;
using TT.FileParserFunction;

namespace FileLogicTest
{
    public class ParseLineUnitTests
    {
        [SetUp]
        public void Setup()
        {
            /*
             *  Pattern must be configuration and is a string that contain letters, numbers, ? and * symbols. 
                ? stands for 1 any character, * stands for 0 or many of any characters. 
                For instance, input 'abcd' matches pattern 'a*d' but input 'abcde' doesn't.
             * */
        }

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