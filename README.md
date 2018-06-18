# Scrubbie
[![license](https://img.shields.io/github/license/desktop/desktop.svg?style=flat-square)](https://github.com/desktop/desktop/blob/master/LICENSE)

C# Scrubbing Helper
Simple helper class for doing text scrubbing, generally Regex's behind the scenes. 

* Strip stings from other strings
* Replace by list of regexs
* Replace words by other words
* Translate characters from one set to another

# Easy To Use

``` c#
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
```

Todo: 
This is a .Net 2.0 Core build. Need to make it a package for nuget.org
More useful functionality, still basically a wrapper around regex stuff
Add constant regex patterns for things like space removal, trim, etc

# Examples
Check out the Examples project directory to see a general example of how it can be used. 

# Tests
The project has unit and integration tests. Look at the tests for some additional use patterns.

# License 
MIT
