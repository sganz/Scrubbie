using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scrubbie;

namespace IntegrationTests
{
    [TestClass]
    public class ScrubbieIntegrationTests
    {
        // Delegate method for the Matcher

        public string LowerCaseMatcher(Match match)
        {
            return match.Value.ToLower();
        }

        [TestMethod]
        public void Predefined_CompactWhitespace_Compacted()
        {
            string sentence = "¿¡Señor, the Chevrolet guys don't like     Dodge     guys, and and no one like MaZdA, Ola Senor?!    ";
            string expectedSentance = "¿¡Señor, the Chevrolet guys don't like Dodge guys, and and no one like MaZdA, Ola Senor?! ";

            Scrub st = new Scrub(sentence);

            // Compact whitespaces to one space, note does not imply trim!
            // overides default empty string replace to replace with single space
            // note trailing space at end of string

            st.RegxDefined("WhitespaceCompact", " ");

            Assert.AreEqual(expectedSentance, st.ToString());
        }

        [TestMethod]
        public void Predefined_InvalidName_Untouched()
        {
            string sentence = "¿¡Señor, the Chevrolet guys don't like     Dodge     guys, and and no one like MaZdA, Ola Senor?!    ";

            Scrub st = new Scrub(sentence);

            // Invalid pre-defined patter, should throw

            Assert.ThrowsException<KeyNotFoundException>(() => st.RegxDefined("NotInTheListOfDefined"));
        }

        [TestMethod]
        public void IgnoreCase_RegxDefined_Matches()
        {
            string sentence = "wtf does RemoveWTF do? Is WtF Case SeNsItIvE?";
            string expectedSentance = "XXX does RemoveXXX do? Is XXX Case SeNsItIvE?";

            Scrub st = new Scrub(sentence);

            st.Set(sentence );
            st.RegxMatchesDefined.Add("RemoveWTF", @"(wtf)|(what the)\s+(hell|$hit)");
            st.RegxIgnoreCase().RegxDefined("RemoveWTF", "XXX");

            Assert.AreEqual(expectedSentance, st.ToString());
        }

        [TestMethod]
        public void MatchCase_RegxDefined_Matches()
        {
            string sentence = "wtf does RemoveWTF do? Is WtF Case SeNsItIvE?";
            string expectedSentance = "XXX does RemoveWTF do? Is WtF Case SeNsItIvE?";

            Scrub st = new Scrub(sentence);

            st.Set(sentence);
            st.RegxMatchesDefined.Add("RemoveWTF", @"(wtf)|(what the)\s+(hell|$hit)");
            st.RegxDefined("RemoveWTF", "XXX");

            Assert.AreEqual(expectedSentance, st.ToString());
        }

        [TestMethod]
        public void Translate_RegxTranslateDefault_Matches()
        {

            string sentence = "  Señor, the BMW guys don't like     Dodge     guys, and and no one like MaZdA.    ";
            string expectedSentance = "Señor, the Fiat guys don't like Dodge guys, and and no one like Daihatsu.";

            // Need `System.ValueTuple` package to do this style of init
            // on v4.6 and below

            List<(string, string)> regxList = new List<(string, string)>
            {   // Match, Replace
                ("BMW", "Fiat"),
                ("MaZdA", "Daihatsu"),
                (@"\s+", " "),         // multi whitespace to 1 space
                (@"^\s*|\s*$", "")     // trims leading/ending spaces
            };

            Scrub st = new Scrub(sentence);
            st.SetRegxTranslator(regxList);
            st.RegxTranslate();

            Assert.AreEqual(expectedSentance, st.ToString());
        }

        [TestMethod]
        public void Translate_RegxTranslateUserMatch_Matches()
        {
            // Test the user delegate overide match. Replace values don't do anything
            // in the regxList, just the match. Any match will be set to lower case. That is all.

            string sentence = "Señor, the BMW guys don't like Dodge guys, and and no one like MaZdA.";
            string expectedSentance = "Señor, the bmw guys don't like dodge guys, and and no one like mazda.";

            // Need `System.ValueTuple` package to do this style of init
            // on v4.6 and below

            List<(string, string)> regxList = new List<(string, string)>
            {   // Match, Replace Ignored since using LowerCaseMatche (user match delegate)
                ("BMW", ""),
                ("MaZdA", ""),
                ("Dodge", "")
            };

            Scrub st = new Scrub(sentence);
            st.SetRegxTranslator(regxList);
            st.RegxTranslate(LowerCaseMatcher);

            Assert.AreEqual(expectedSentance, st.ToString());
        }

        [TestMethod]
        public void TestAll()
        {
            // get most of the mapped accent chars, and their non-accented equiv
            // must be 1 to 1 mapping and size of arrays. Easier to do lots of chars this
            // way then with lists

            string matchChar = "ŠŒŽšœžŸ¥µÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖØÙÚÛÜÝßàáâãäåæçèéêëìíîïðñòóôõöøùúûüýÿ¡¿";
            string replaceChar = "SOZsozYYuAAAAAAACEEEEIIIIDNOOOOOOUUUUYsaaaaaaaceeeeiiiionoooooouuuuyy  ";

            // set up a dictionary, if ignore case, set the dict up with a new comparer

            StringComparer comparer = StringComparer.OrdinalIgnoreCase; // default is just Ordinal
            Dictionary<string, string> wordDictionary = new Dictionary<string, string>(comparer)
            {
                {"chevrolet", "Ford"},
                {"mAzDa", "BMW"},
                {"and and", "and"},  // will never match
            };

            // Need `System.ValueTuple` package to do this style of init
            // on v4.6 and below

            List<(string, string)> regxList = new List<(string, string)>
            {   // Match, Replace
                ("BMW", "Fiat"),
                (@"\s+", " "),         // multi whitespace to 1 space
                (@"^\s*|\s*$", "")     // trims leading/ending spaces
            };

            string sentence = "¿¡Señor, the Chevrolet guys don't like     Dodge     guys, and and no one like MaZdA, Ola Senor?!    ";
            string expectedSentance = "Magoo the Ford guys don#t like Mercedes guys and and no one like Fiat Ola Magoo?!";

            Scrub st = new Scrub(sentence);

            // Set dictionary up, case insensitive match

            st.SetStringTranslator(wordDictionary, true);

            // set up character translators

            st.SetCharTranslator(matchChar, replaceChar);

            // set up list of regx replaces

            st.SetRegxTranslator(regxList);

            // add a string translation after the fact

            st.StringTransDict.Add("dodge", "Mercedes");

            // add a Regx translation after the fact

            st.RegxTuples.Add(("Senor", "Mr.Magoo"));

            // add a chracter Translation after the fact

            st.CharTransDict.Add('\'', '#');

            // so all sorts of stuff!

            st.Strip("[,]").MapChars().MapWords().RegxTranslate().Strip(@"Mr\.");

            Assert.AreEqual(expectedSentance, st.ToString());
        }
    }
}
