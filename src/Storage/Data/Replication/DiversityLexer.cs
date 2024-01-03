using System;
using System.Linq;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

[Serializable]
public class DiversityParserException : Exception
{
    public DiversityParserException() { }
    public DiversityParserException(string message) : base(message) { }
    public DiversityParserException(string message, Exception inner) : base(message, inner) { }
    protected DiversityParserException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
internal class DiversityLexer
{
    public enum Token
    {
        To,
        Step,
        BuiltInGeneratorKeyword,
        GeneratorKeyword,
        Integer,
        DateTime,
        Timespan,
        Pattern,
        String,
        Unknown,
        Eof,
    }

    private static readonly char[] SplitChars = " \t\r\n".ToCharArray();
    private string _fieldName;
    private readonly string[] _words;
    private int _wordIndex = -1;

    public Token CurrentToken { get; private set; }
    public string StringValue { get; private set; }
    public int IntegerValue { get; private set; }
    public DateTime DateTimeValue { get; private set; }
    public TimeSpan TimeSpanValue { get; private set; }

    public DiversityLexer(string fieldName, string src)
    {
        _fieldName = fieldName;
        _words = src.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries);
    }

    public string? GetCurrentWord()
    {
        if (_wordIndex < 0)
            return null;
        if (_wordIndex >= _words.Length)
            return null;
        if (CurrentToken == Token.Eof)
            return null;
        return _words[_wordIndex];
    }
    public bool NextToken()
    {
        if (++_wordIndex >= _words.Length)
        {
            CurrentToken = Token.Eof;
            return false;
        }

        var word = _words[_wordIndex];
        if (IsReservedWord(word))
            return true;
        if (IsGeneratorKeyword(word))
            return true;
        if (IsPattern())
            return true;
        if (IsConstant(word))
            return true;

        CurrentToken = Token.Unknown;
        return true;
    }

    private bool IsReservedWord(string word)
    {
        if (word.Equals("TO", StringComparison.OrdinalIgnoreCase))
        {
            CurrentToken = Token.To;
            StringValue = "TO";
            return true;
        }
        if (word.Equals("STEP", StringComparison.OrdinalIgnoreCase))
        {
            CurrentToken = Token.Step;
            StringValue = "STEP";
            return true;
        }

        return false;
    }
    private bool IsGeneratorKeyword(string word)
    {
        if (word[word.Length - 1] != ':')
            return false;

        var src = word.Substring(0, word.Length - 1);

        if (src.Equals("RANDOM", StringComparison.OrdinalIgnoreCase))
        {
            CurrentToken = Token.BuiltInGeneratorKeyword;
            StringValue = "RANDOM";
            return true;
        }
        if (src.Equals("SEQUENCE", StringComparison.OrdinalIgnoreCase))
        {
            CurrentToken = Token.BuiltInGeneratorKeyword;
            StringValue = "SEQUENCE";
            return true;
        }
        if (src.All(c => char.IsLetterOrDigit(c) || c == '_'))
        {
            CurrentToken = Token.GeneratorKeyword;
            StringValue = src;
            return true;
        }

        throw new DiversityParserException(
            $"Invalid keyword in the \"{_fieldName}\" field: \"{word}\". The keyword can only contain letter, digit or '_' characters.");

    }
    private bool IsPattern()
    {
        for (int i = _wordIndex; i < _words.Length; i++)
        {
            if (_words[i].EndsWith(","))
            {
                CurrentToken = Token.Pattern;
                var value = string.Join(" ", _words.Skip(_wordIndex).Take(i - _wordIndex + 1));
                StringValue = value.Substring(0, value.Length - 1);
                _wordIndex = i;
                return true;
            }
        }

        return false;
    }
    private bool IsConstant(string word)
    {
        if (int.TryParse(word, out var intValue))
        {
            CurrentToken = Token.Integer;
            IntegerValue = intValue;
            return true;
        }

        if (word.Contains('.') && TimeSpan.TryParse(word, out var timeSpanValue))
        {
            CurrentToken = Token.Timespan;
            TimeSpanValue = timeSpanValue;
            return true;
        }

        if (word.Contains('-') && DateTime.TryParse(word, out var date))
        {
            if (_wordIndex < _words.Length - 1)
            {
                var secondWord = _words[_wordIndex + 1];
                // lookahead one word for parsing the optional time part
                if (secondWord.Contains(':') && DateTime.TryParse($"{word} {secondWord}", out var dateTime))
                {
                    CurrentToken = Token.DateTime;
                    DateTimeValue = dateTime;
                    _wordIndex++;
                    return true;
                }
            }
            CurrentToken = Token.DateTime;
            DateTimeValue = date;
            return true;
        }

        CurrentToken = Token.String;
        StringValue = word;
        return true;
    }
}