using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Scrubbie
{
    /// <summary>
    /// Main Class where all the magic happens.
    /// </summary>
    public class Scrub
    {
        const int DefaultRegxCacheSize = 16;
        private const double DefaultTkoSeconds = 1.0d;

        public Dictionary<string, string> StringTransDict { private set; get; }
        public List<(string, string)> RegxTuples { private set; get; }
        public Dictionary<char, char> CharTransDict { private set; get; }
        private string _translatedStr;
        private TimeSpan _tkoSeconds;
        public RegexOptions RegxOptions { set; get; }

        /// <summary>
        /// Sets the MatchTimeout value for all regx calls
        /// </summary>
        public double TkoSeconds
        {
            set => _tkoSeconds = value <= 0.0 ? TimeSpan.FromSeconds(DefaultTkoSeconds) : TimeSpan.FromSeconds(value);
            get => _tkoSeconds.TotalSeconds;
        }

        /// <summary>
        /// Set the cache size for statically called regx calls used within
        /// </summary>
        public int CacheSize
        {
            set => Regex.CacheSize = value < 0 ? DefaultRegxCacheSize : value;
            get => Regex.CacheSize;
        }

        /// <summary>
        /// Default regx dictionay of helpers that do common things. Basically
        /// a dictionay of tuples that  have the Match Pattern. Under user
        /// control so can be updated at runtime
        /// </summary>
        public Dictionary<string, string> RegxMatchesDefined { private set; get; } = new Dictionary<string, string>()
        {
            { "WhitespaceCompact" , @"\s+"},    // Match multi-white space, used to compact white space
            { "WhitespaceEnds", @"^\s*|\s*$" }, // Match begin and end whitespace
            { "WhitespaceBegin", @"^\s*" },     // Match whitespace in front
            { "WhitespaceEnd", @"\s*$" },       // Match whitespace in End
            { "SingleEmailMask", @"(?<=.{2}).(?=[^@]*?@)" },  // masks a string with single email, confused by extra @ like abc***@crap.com
            { "Email", @"[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)"},   // fair regex, better than most I found
            { "NonAscii", @"[^\x00-\x7F]+\ *(?:[^\x00-\x7F]| )*" }, // removes all non-Ascii
            { "TagsSimple" , @"\<[^\>]*\>" },     // strip tags, simple version matches anything inside `<>`
            { "ScriptTags" , @"<script[^>]*>[\s\S]*?</script>" },       // matches script tag, use before Tags Simple if stripping html
            { "ENNumber", @"[+-]?([0-9]+([.][0-9]*)?|[.][0-9]+)"},      // format with a decimal as a period, no commas for 1000's
            { "EUNumber", @"[+-]?([0-9]+([,][0-9]*)?|[,][0-9]+)"},      // format with a decimal as a comma, no period for 1000's
            { "UniNumber", @"[+-]?([0-9]+([.,][0-9]*)?|[,.][0-9]+)"},   // picks up numbers with either comma, period in either place. May not be valid numbers
        };

        /// <summary>
        /// Constructor for the Scrubbies class. It set up default state
        /// for any needed variable and the initial string for which we want to scrub.
        /// </summary>
        /// <param name="origString">A string with each character to map</param>
        /// <exception cref="ArgumentNullException">Throws on null arg</exception>
        public Scrub(string origString)
        {
            _translatedStr = origString ?? throw new ArgumentNullException(nameof(origString));

            StringTransDict = new Dictionary<string, string>();
            RegxTuples = new List<(string, string)>();
            CharTransDict = new Dictionary<char, char>();

            // set OUR default regx compiled cache size

            CacheSize = DefaultRegxCacheSize;

            // set local time out (TKO) for all regx's

            _tkoSeconds = TimeSpan.FromSeconds(DefaultTkoSeconds);

            // set to match case (not not ignore it)

            RegxOptions = RegexOptions.None;
        }

        /// <summary>
        /// Set the string translation up. Basically accepts a dictionary and a case flag for
        /// comparison. If the incoming dictionary is null, and empty one will be created.
        /// </summary>
        /// <param name="translateMap">Dictionay of words the map to each other</param>
        /// <param name="ignoreCase">True ignore case on dictionary match, False (default)
        /// case sensitive match</param>
        public void SetStringTranslator(Dictionary<string, string> translateMap = null, bool ignoreCase = false)
        {
            // set up the comparer for the dictionary

            StringComparer comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

            // create dict based on params

            StringTransDict = translateMap == null ? new Dictionary<string, string>(comparer) : new Dictionary<string, string>(translateMap, comparer);
        }

        /// <summary>
        /// Build a character translation dict. This works for any chars that are
        /// representable as a char in a string. The one to one mapping of the chars
        /// will effect a translation of chars from the inputMap to the matching offset
        /// char in the outputMap. The internal dictionary of this map is created as a
        /// result of these 2 strings. This can only replace 1 char with 1 char.
        /// </summary>
        /// <param name="inputMap">A string with each character to map</param>
        /// <param name="outputMap">The Output Character as a result </param>
        /// <exception cref="ArgumentException">If both strings are not the same length</exception>
        public void SetCharTranslator(string inputMap, string outputMap)
        {
            if (inputMap.Length != outputMap.Length)
                throw new ArgumentException("Invalid Length of Parameter Strings, they must be equal length");

            for (int i = 0; i < inputMap.Length; i++)
                CharTransDict[inputMap[i]] = outputMap[i];
        }

        /// <summary>
        /// Sets a character translation dict. This works for any chars that are
        /// representable as a char in a string. The one to one mapping of the chars
        /// will effect a translation of chars from the inputMap to the matching offset
        /// char in the outputMap. Use the string based Set for easy mapping of large
        /// amounts of characters.
        /// </summary>
        /// <param name="translateMap">A Dictionary with char to char mapping</param>
        public void SetCharTranslator(Dictionary<char, char> translateMap = null)
        {
            CharTransDict = translateMap == null ? new Dictionary<char, char >() : new Dictionary<char, char>(translateMap);
        }

        /// <summary>
        /// Sets up the List of regx match and replace list. The Item1 must be the Regx
        /// that will be match (C# style) and the Item2 element will be what's replaced.
        /// If the passed in list is null will create an empty list
        /// </summary>
        /// <param name="regxTuplesList">List of regx and replacement strings</param>
        public void SetRegxTranslator(List<(string, string)> regxTuplesList = null)
        {
            RegxTuples = regxTuplesList == null ? new List<(string, string)>() : new List<(string, string)>(regxTuplesList);
        }

        /// <summary>
        /// Sets the Regx pattern matcher to Ignore case. This can
        /// be used prior to any regx call. It does not affect any Map
        /// function as those typically require the dictionay to be
        /// setup prior. So be warned this is ONLY for REGX's not MAP
        /// </summary>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public Scrub RegxIgnoreCase(bool ignoreCase = true)
        {
            if (ignoreCase)
            {
                // add flag
                RegxOptions |= RegexOptions.IgnoreCase;
            }
            else
            {
                // remove flag (Think bitwise)
                RegxOptions &= ~RegexOptions.IgnoreCase;
            }

            return this;
        }

        /// <summary>
        /// Translates given string based on the the characters in the dictionary. If character is
        /// not in the dictionay, it is pass thru untouched. Size of string is not changed.
        /// </summary>
        /// <returns>Scrub</returns>
        public Scrub MapChars()
        {
            // create a new stringbuild of the same size
            char[] chars = _translatedStr.ToCharArray();

            for (int i = 0; i < _translatedStr.Length; i++)
            {
                if (CharTransDict.ContainsKey(chars[i]))
                    chars[i] = CharTransDict[chars[i]];
            }

            _translatedStr = new string(chars);

            return this;
        }

        /// <summary>
        /// Does a regx strip on the working sting. The passed in
        /// expression is a C# Regx style pattern match. This is designed
        /// to be more of an on-the-fly regx. Will Regex will compile and cache for
        /// static calls like this.
        /// </summary>
        /// <param name="matchRegx"></param>
        /// <returns>Scrub</returns>
        public Scrub Strip(string matchRegx)
        {
            // Call static replace method, strip and save

            _translatedStr = Regex.Replace(_translatedStr, matchRegx, String.Empty, RegxOptions, _tkoSeconds);

            return this;
        }

        /// <summary>
        /// Translates given string based on the dictionary. If character is
        /// not in the dictionay, it is pass thru untouched. Internally
        /// used helper.
        /// </summary>
        /// <param name="origStr"></param>
        /// <returns>string</returns>
        private string Map(string origStr)
        {
            if (String.IsNullOrEmpty(origStr))
            {
                return String.Empty;
            }

            // simple in this case, just get the value from the map

            return StringTransDict.ContainsKey(origStr) ? StringTransDict[origStr] : origStr;
        }

        /// <summary>
        /// Translates a phrase that has each word separated by the 'pattern' string.
        /// Will swap the words in the dictionary if it matches, otherwise just pass
        /// it as it was if no match. This adhears to the matching rules set when
        /// the dictionary was created. Generally the active string should be
        /// clean and have sane word seperators like single space, comma, etc.
        /// This map will process one time against each word, and once a word is translated
        /// it will be be a candidate for further translation.
        /// </summary>
        /// <param name="splitString">Will split the string on this string</param>
        /// <returns>Scrub</returns>
        public Scrub MapWords(string splitString = " ")
        {
            if (String.IsNullOrEmpty(_translatedStr) || String.IsNullOrEmpty(splitString))
            {
                _translatedStr = String.Empty;
                return this;
            }

            // Convert to an array of strings which split can use

            string[] patternArray = { splitString };
            string[] elements = _translatedStr.Split(patternArray, StringSplitOptions.None);

            // rebuild string, adding back in each mapped word and split seperator

            StringBuilder sb = new StringBuilder();

            foreach (string element in elements)
            {
                // This is subject to any sb issues with nulls, etc

                sb.Append(Map(element));
                sb.Append(splitString);
            }

            string cleanStr = sb.ToString();

            // check for empty, bounce since nothing to return

            if (cleanStr.Length == 0)
            {
                _translatedStr = String.Empty;
                return this;
            }

            // remove trailing splitString from end of string.

            _translatedStr = cleanStr.Substring(0, cleanStr.Length - splitString.Length);

            return this;
        }

        /// <summary>
        /// This will allow a list of regx's and match replacements (string, string) tuple
        /// to form a regx pattern and replacement string. Item1 is the pattern, Item2 is the
        /// replacement string on any matches. This is similar to the MapWords() but
        /// based on regx's AND at each new regx match pattern it will be reapplied to any
        /// previously applied matches that may have been replaced.
        /// </summary>
        /// <returns>Scrub</returns>
        public Scrub RegxTranslate()
        {
            // for each regx replace in the tuple list do a regx replace
            // with the regx as Item1 and the replace as Item2. Note regex will
            // throw if null set for most anything.

            foreach ((string, string) regxTuple in RegxTuples)
            {
                // static will compile and cache the regx for each one

                _translatedStr = Regex.Replace(_translatedStr, regxTuple.Item1, regxTuple.Item2, RegxOptions, _tkoSeconds);
            }

            return this;
        }

        ///  <summary>
        ///  This will a predefined Regx match and replacement value to be used by it's name.
        ///  This comes from the RegxDefinedTuples dictionary.
        ///
        ///  Currently if the name is not found in the dictionary it just ignores it and returns
        ///  the current state.
        ///  </summary>
        /// <param name="preDefined">String Name of the predefined match pattern</param>
        /// <param name="replacement">String of the data to replace matches, default is to empty string (strip)</param>
        /// <returns>Scrub</returns>
        public Scrub RegxDefined(string preDefined, string replacement = "")
        {
            // Throw with nicer message if invalid dict key

            if (!RegxMatchesDefined.ContainsKey(preDefined))
            {
                throw new KeyNotFoundException("Invalid Defined Regx Specified : `" + preDefined + "` Does not exist, sorry!");
            }

            // static will compile and cache the regx for each one

            _translatedStr = Regex.Replace(_translatedStr, RegxMatchesDefined[preDefined], replacement, RegxOptions, _tkoSeconds);

            return this;
        }

        /// <summary>
        /// Set the current working string
        /// </summary>
        /// <param name="workingStr"></param>
        /// <returns>Scrub</returns>
        /// <exception cref="ArgumentNullException">Throws on null arg</exception>
        public Scrub Set(string workingStr)
        {
            _translatedStr = workingStr ?? throw new ArgumentNullException(nameof(workingStr));

            return this;
        }

        /// <summary>
        /// Return as a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _translatedStr;
        }
    }
}

