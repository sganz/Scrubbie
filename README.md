# Scrubbie
[![license](https://img.shields.io/github/license/desktop/desktop.svg?style=flat-square)](https://github.com/desktop/desktop/blob/master/LICENSE)

![alt text](https://user-images.githubusercontent.com/5179047/41923201-b27b9b1c-791b-11e8-98dd-fd7fb15f122a.png)

# C# Text Scrubbing
Simple helper class for doing text scrubbing, cleaning, and formatting. 
Generally Regex's behind the scenes, with a few other dictionary mappings to 
help things move along. Access to a few of the Regex's special features such 
as maximum execution time and compiled cache size are controllable as well.

* Strip strings from other strings
* Replace by list of regexs
* Replace words by other words
* Translate characters from one set to another
* Pre-Defined list of useful Regex's (runtime expandable)
* Source on Github

# Very Easy To Use

``` c#
// Map any character to any other character. The `matchChar` array MUST only
// have unique characters. The `replaceChar` array will have the matching translated char.

// The example below of accent chars, and their non-accented equiv
// Both strings must be 1 to 1 mapping and size of strings. This was done as strings
// to make it easier to deal with lots of characters. Can also add directly to the CharTransDict
// if you want instead of a set of strings.

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

// Dump the original string

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

// ** Test Pre-Defined Regex Patterns **

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
```

# Todo
More useful functionality, still basically a wrapper around regex stuff
Add constant regex patterns for things like space removal, trim, etc.
Currently Core 2.0 build.

# Examples
Check out the Examples project directory on Github to see a general example of how it can be used. 

# Tests
The project has unit and integration tests. Also look at the tests for some additional use patterns.

# Your Suggestion
Help with some ideas, code fixes are welcome. Use Github for opening request, bugs, etc.

# License 
MIT
