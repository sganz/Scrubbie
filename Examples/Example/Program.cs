using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Scrubbie;

namespace Example
{
    /// <summary>
    /// Simple helper class to create a couple of match'ers
    ///
    /// This has 2 potential usable match functions
    /// </summary>
    class Upper
    {
        // Make just the first letter of each match Upper
        public string FirstUpper(Match match)
        {
            string word = match.Value;
            return char.ToUpper(word[0]) + word.Substring(1);
        }

        // Make the entire match upper case
        public string AllUpper(Match match)
        {
            return match.Value.ToUpper();
        }
    }

    /// <summary>
    /// Simple Class that uses the Regex's Match Groups.
    /// </summary>
    class FlipFlopper
    {
        public string FlipFlop(Match match)
        {
            // match.Groups[0] is the original match, same as match.Value I think...

            // Take the first part of the match and flip it around with the second
            // Also flip the comparison sign around

            string firstWord = match.Groups[1].Value;       // First Match Group
            string sep = match.Groups[2].Value;             // Second Match Group
            string secondWord = match.Groups[3].Value;      // Third Match Group

            return secondWord + sep + firstWord;
        }
    }

    /// <summary>
    /// Simple replacer class. This is a bit more complicated as you
    /// need the replacement string to be scoped to the class so it
    /// can be replaced by the match method
    /// </summary>
    class Replacer
    {
        private readonly string _replacement;

        public Replacer(string replacement)
        {
            _replacement = replacement;
        }

        public string ReplaceMatch(Match match)
        {
            return _replacement;
        }
    }

    class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            // Map any character to any other character. The matchCarArray MUST be only
            // have unique characters. The replaceChar array will have the matching translated char.

            // The example below of accent chars, and their non-accented equiv
            // Both strings must be 1 to 1 mapping and size of strings. This was done as strings
            // to make it easier to deal with lots of characters.

            string matchChar =   "ŠŒŽšœžŸ¥µÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖØÙÚÛÜÝßàáâãäåæçèéêëìíîïðñòóôõöøùúûüýÿ¡¿";
            string replaceChar = "SOZsozYYuAAAAAAACEEEEIIIIDNOOOOOOUUUUYsaaaaaaaceeeeiiiionoooooouuuuyy  ";

            // Set up a dictionary, if ignore case, set the dict up with a new comparer
            // These words are mapped to any instances of other words. See comments
            // on how this works vs regx, basically each word from a sentence is passed
            // to the dictionary for translation. Current or past changes are not candidates
            // for any further changes

            StringComparer comparer = StringComparer.OrdinalIgnoreCase; // default is just Ordinal
            Dictionary<string, string> wordDictionary = new Dictionary<string, string>(comparer)
            {
                {"chevrolet", "Ford"},
                {"mAzDa", "BMW"},
                {"and and", "and"}  // will never match
            };

            // NOTE : Need `System.ValueTuple` package to do this style of init on v4.6 and below.

            // Regx list each item is executed in order of the list.
            // First element is the Regx match string (C# style) and the second
            // is the replacement string if the pattern matches. Matches can affect the entire
            // string, and each subsequent match can as well.

            List<(string, string)> regxList = new List<(string, string)>
            {   // Match, Replace
                ("BMW", "Fiat"),       // swaps 'BMW' (case dependent) with 'Fiat'
                (@"\s+", " "),         // multi whitespace to 1 space
                (@"^\s*|\s*$", "")     // trims leading/ending spaces
            };

            // Test sentence with odd characters, spaces and other things needing scrubbing

            string sentence = "¿¡Señor, the Chevrolet guys don't like     Dodge     guys, and and no one like MaZdA, Ola Senor?!    ";

            // Dump the orig string

            Console.WriteLine("The Sentence : >{0}<", sentence);

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

            string translated = st.Strip("[,]").MapChars().MapWords().RegxTranslate().Strip(@"Mr\.").ToString();

            // Should be something like the string below -
            // Magoo the Ford guys don#t like Mercedes guys and and no one like Fiat Ola Magoo?!

            Console.WriteLine("Translated   : >{0}<", translated);

            // reset the string with some emails
            st.Set("Hank@kimball.com is sending an email to haystack@calhoon.com");

            translated = st.RegxDefined("Email", "**Email Removed**").ToString();

            Console.WriteLine("Masked   : >{0}<", translated);

            st.Set("　前に来た時は北側からで、当時の光景はいまでも思い出せる。 Even now I remember the scene I saw approaching the city from the north. 　青竜山脈から流れる川が湖へと流れこむ様、湖の中央には純白のホ");
            translated = st.RegxDefined("NonAscii", string.Empty).ToString();

            Console.WriteLine("To all ASCII : >{0}<", translated);

            // reset the string with some emails
            st.Set(@"<h1>Title</h1><script>var a=1; \\comment</script> Not In Script Tags");

            translated = st.RegxDefined("ScriptTags", string.Empty).RegxDefined("TagsSimple", string.Empty).ToString();

            Console.WriteLine("Strip Script and Tags   : >{0}<", translated);

            // reset and set up a predefined match pattern and set regx case sensitivity
            st.Set("wtf does RemoveWTF do? Is WtF Case SeNsItIvE?");
            st.RegxMatchesDefined.Add("RemoveWTF", @"(wtf)|(what the)\s+(hell|$hit)");

            translated = st.RegxIgnoreCase().RegxDefined("RemoveWTF", "XXX").ToString();
            Console.WriteLine("New Pre-defined Match   : >{0}<", translated);

            // same as sluggify

            st.Set("Excursion    Front Brake Pad Replacement");
            translated = st.RegxDefined("WhitespaceCompact", "-").ToString().ToLower();
            Console.WriteLine("Sluggify   : >{0}<", translated);

            // strip hyphens

            st.Set(translated);
            translated = st.RegxDefined("Un-Hypen", " ").ToString();
            Console.WriteLine("Un-Sluggify   : >{0}<", translated);

            //
            // Now try some custom Regex Matchers
            //

            // All in one with lambda function

            st.Set("I want the first letter of each word capitalized");
            translated = st.TestEvaluator(@"\w+", m => m.Value[0].ToString().ToUpper() + m.Value.Substring(1)).ToString();

            Console.WriteLine("Lambda First Letter To Upper : >{0}<", translated);

            // WordReplacer method is STATIC so just pass it along to do the work

            st.Set("I want the first letter of each word capitalized, aGain");
            translated = st.TestEvaluator(@"\w+", StaticWordFirstUpperCaser).ToString();

            Console.WriteLine("Static Method First Letter to Upper : >{0}<", translated);

            // Now do some non-static classes various match helpers

            // Upper Case First

            var upperCaseStuff = new Upper();
            MatchEvaluator myCaseClassEvaluator = upperCaseStuff.FirstUpper;

            st.Set("From another static class we can get stuff for the match function and do stuff");
            translated = st.TestEvaluator(@"\w+", myCaseClassEvaluator).ToString();

            Console.WriteLine("Custom Matcher Class First to Upper   : >{0}<", translated);

            myCaseClassEvaluator = upperCaseStuff.AllUpper;
            st.Set("From another static class we can get stuff for the match function and do stuff");
            translated = st.TestEvaluator(@"\w+", myCaseClassEvaluator).ToString();

            Console.WriteLine("Custom Matcher All Upper   : >{0}<", translated);

            var flipFlopStuff = new FlipFlopper();
            var myFlipFlopEvaluator = new MatchEvaluator(flipFlopStuff.FlipFlop);

            // Now a Flip Flopper that looks at Regex Match Groups
            //
            // ([a-z0-9\-]+)(\.)([a-z0-9\-]+) Match stuff with a perior in the middle like -
            //    "First.Second"

            st.Set("First.Second");
            translated = st.TestEvaluator(@"([a-z0-9\-]+)(\.)([a-z0-9\-]+)", myFlipFlopEvaluator).ToString();

            Console.WriteLine("Custom Matcher Flip Floper   : {0}", translated);

            // Now a bit tricky, since we need to set up a parameter that the matcher
            // can see, we need to set it to an internal class var so it can be used.
            // Replace 'chevy' match with 'Chevrolet'

            var replaceStuff = new Replacer("Chevrolet");
            MatchEvaluator myReplacementClassEvaluator = replaceStuff.ReplaceMatch;

            st.Set("The repacement for a chevy should be the full name, not short.");
            translated = st.TestEvaluator(@"chevy", myReplacementClassEvaluator).ToString();

            Console.WriteLine("Custom Matcher Replacer     : {0}", translated);
        }

        // Static matcher method defined

        private static string StaticWordFirstUpperCaser(Match match)
        {
            string word = match.Value;
            return char.ToUpper(word[0]) + word.Substring(1);
        }
    }
}