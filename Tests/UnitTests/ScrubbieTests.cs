using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scrubbie;

namespace UnitTests
{
    [TestClass]
    public class ScrubbieTests
    {
        [TestMethod]
        public void Create_Constructor_InstantiatesObjects()
        {
            Scrub st = new Scrub("");

            Assert.IsNotNull(st.CharTransDict);
            Assert.IsNotNull(st.RegxTuples);
            Assert.IsNotNull(st.StringTransDict);
            Assert.IsNotNull(st.RegxMatchesDefined);
        }

        [TestMethod]
        public void Create_Constructor_ExpectSameString()
        {
            string expect = "Randy Butternubs";
            Scrub st = new Scrub(expect);

            Assert.IsNotNull(st.CharTransDict);
            Assert.IsNotNull(st.RegxTuples);
            Assert.IsNotNull(st.StringTransDict);
            Assert.AreEqual(expect, st.ToString());
            Assert.IsNotNull(st.RegxMatchesDefined);
        }

        [TestMethod]
        public void Translate_EmptyEverything_ExpectSameString()
        {
            string expect = "Randy Butternubs";
            Scrub st = new Scrub(expect);

            st.Strip("").MapChars().MapWords().RegxTranslate().Strip("");

            Assert.IsNotNull(st.CharTransDict);
            Assert.IsNotNull(st.RegxTuples);
            Assert.IsNotNull(st.StringTransDict);
            Assert.AreEqual(expect, st.ToString());
        }

        [TestMethod]
        public void SetString_AddStringAfter_ExpectNewString()
        {
            string expect = "Randy Butternubs";
            Scrub st = new Scrub("Haystack Calhoon");

            st.Set(expect);
            st.Strip("").MapChars().MapWords().RegxTranslate().Strip("");

            Assert.IsNotNull(st.CharTransDict);
            Assert.IsNotNull(st.RegxTuples);
            Assert.IsNotNull(st.StringTransDict);
            Assert.AreEqual(expect, st.ToString());
        }

        [TestMethod]
        public void SetString_CharTransDict_Matches()
        {
            Scrub st = new Scrub("");

            string expectedMatchChar = "äåéöúûü•µ¿¡¬√ƒ≈∆«»… ÀÃÕŒœ–—“”‘’÷ÿŸ⁄€‹›ﬂ‡·‚„‰ÂÊÁËÈÍÎÏÌÓÔÒÚÛÙıˆ¯˘˙˚¸˝ˇ°ø";
            string expectedReplaceChar = "SOZsozYYuAAAAAAACEEEEIIIIDNOOOOOOUUUUYsaaaaaaaceeeeiiiionoooooouuuuyy  ";

            st.SetCharTranslator(expectedMatchChar, expectedReplaceChar);

            var match = new string(st.CharTransDict.Keys.ToArray());
            var replace = new string(st.CharTransDict.Values.ToArray());

            Assert.AreEqual(expectedMatchChar.Length, st.CharTransDict.Count);
            Assert.AreEqual(expectedMatchChar, match);
            Assert.AreEqual(expectedReplaceChar, replace);
        }

        [TestMethod]
        public void SetDict_CharTransDict_Matches()
        {
            Scrub st = new Scrub("");

            var expectedCharMap = new Dictionary<char,char>()
            {
                {'a', 'A'},
                {'b', 'B'},
                {'c', 'C'}
            };

            st.SetCharTranslator(expectedCharMap);

            CollectionAssert.AreEqual(expectedCharMap, st.CharTransDict);
        }

        [TestMethod]
        public void Set_StringTransDict_Matches()
        {
            Scrub st = new Scrub("");

            Dictionary<string, string> expectedDict = new Dictionary<string, string>()
            {
                {"Haystack", "Calhoon"},
                {"Randy", "Butternubs"}
            };

            st.SetStringTranslator(expectedDict);

            Assert.AreEqual(0, st.CharTransDict.Count);
            Assert.AreEqual(0, st.RegxTuples.Count);
            CollectionAssert.AreEqual(expectedDict, st.StringTransDict);
        }

        [TestMethod]
        public void Set_RegxTuples_Matches()
        {
            Scrub st = new Scrub("");

            List<(string, string)> expectedList = new List<(string, string)>()
            {
                ("Haystack", "Calhoon"),
                ("Randy", "Butternubs")
            };

            st.SetRegxTranslator(expectedList);

            Assert.AreEqual(0, st.CharTransDict.Count);
            Assert.AreEqual(0, st.StringTransDict.Count);
            CollectionAssert.AreEqual(expectedList, st.RegxTuples);
        }
        [TestMethod]
        public void TestAll()
        {
            // get most of the mapped accent chars, and their non-accented equiv
            // must be 1 to 1 mapping and size of arrays. Easier to do lots of chars this
            // way then with lists

            string matchChar = "äåéöúûü•µ¿¡¬√ƒ≈∆«»… ÀÃÕŒœ–—“”‘’÷ÿŸ⁄€‹›ﬂ‡·‚„‰ÂÊÁËÈÍÎÏÌÓÔÒÚÛÙıˆ¯˘˙˚¸˝ˇ°ø";
            string replaceChar = "SOZsozYYuAAAAAAACEEEEIIIIDNOOOOOOUUUUYsaaaaaaaceeeeiiiionoooooouuuuyy  ";

            // set up a dictionary, if ignore case, set the dict up with a new comparer

            StringComparer comparer = StringComparer.OrdinalIgnoreCase; // default is just Ordinal
            Dictionary<string, string> wordDictionary = new Dictionary<string, string>(comparer)
            {
                {"chevrolet", "Ford"},
                {"mAzDa", "BMW"},
                {"and and", "and"}  // will never match
            };

            // Need `System.ValueTuple` package to do this style of init
            // on v4.6 and below

            List<(string, string)> regxList = new List<(string, string)>
            {   // Match, Replace
                ("BMW", "Fiat"),
                (@"\s+", " "),         // multi whitespace to 1 space
                (@"^\s*|\s*$", "")     // trims leading/ending spaces
            };

            string expect = "Randy Butternubs";
            Scrub st = new Scrub(expect);

            // Set dictionary up, case insensitive match

            st.SetStringTranslator(wordDictionary, true);

            // set up character translators

            st.SetCharTranslator(matchChar, replaceChar);

            // set up list of regx replaces

            st.SetRegxTranslator(regxList);

            st.SetStringTranslator();
            st.SetRegxTranslator();
            st.SetCharTranslator();

            Assert.AreEqual(0, st.CharTransDict.Count);
            Assert.AreEqual(0, st.StringTransDict.Count);
            Assert.AreEqual(0, st.RegxTuples.Count);
        }

        [TestMethod]
        public void SetRegxCache_CacheCount_Matches()
        {
            Scrub st = new Scrub("");

            // set and set again
            st.CacheSize = 1;
            int expectedSize = 39;
            st.CacheSize = expectedSize;

            Assert.AreEqual(expectedSize, st.CacheSize);
        }

        [TestMethod]
        public void SetRegxTimeOut_MatchTimeOut_Matches()
        {
            Scrub st = new Scrub("");

            // set and set again
            st.TkoSeconds = 1.25;
            double expectedTKO = 3.76;
            st.TkoSeconds = expectedTKO;

            Assert.AreEqual(expectedTKO, st.TkoSeconds);
        }

        [TestMethod]
        public void SetRegxOptions_SetIgnoreCaseTrue_Matches()
        {
            Scrub st = new Scrub("");
            RegexOptions expected = RegexOptions.IgnoreCase;

            // set case and check
            st.RegxIgnoreCase(true);

            Assert.AreEqual(expected, st.RegxOptions & RegexOptions.IgnoreCase);
        }

        [TestMethod]
        public void SetRegxOptions_SetIgnoreCaseFalse_Matches()
        {
            Scrub st = new Scrub("");
            RegexOptions expected = st.RegxOptions & ~RegexOptions.IgnoreCase;

            // set case and check
            st.RegxIgnoreCase(false);

            Assert.AreEqual(expected, st.RegxOptions & ~RegexOptions.IgnoreCase);
        }
    }
}
