using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SenseNet.Search.Querying.Parser
{
    internal class CqlLexer
    {
        internal const string StringTerminatorChars = "\":+-&|!(){}[]^~";

        private static class Keywords
        {
            public static Token ScanKeyword(string text)
            {
                switch (text)
                {
                    case Cql.Keyword.Or: return Token.Or;
                    case Cql.Keyword.And: return Token.And;
                    case Cql.Keyword.Not: return Token.Not;
                    case Cql.Keyword.To: return Token.To;
                    default: return Token.String;
                }
            }

            public static Token ScanControl(string text, LineInfo line)
            {
                if (text == Cql.Keyword.Select || text == Cql.Keyword.Skip || text == Cql.Keyword.Top || text == Cql.Keyword.Sort || text == Cql.Keyword.ReverseSort ||
                    text == Cql.Keyword.Autofilters || text == Cql.Keyword.Lifespan || text == Cql.Keyword.CountOnly || text == Cql.Keyword.Quick || text == Cql.Keyword.AllVersions)
                    return Token.ControlKeyword;

                throw new ParserException("Unknown control keyword: " + text, line);
            }
        }
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum Token
        {
            Eof,             // \0
            White,           // [ \t\r\n\f\v]
            Number,          // [0-9]+([.][0-9]+)?
            Apos,            // '
            Quot,            // "
            LParen,          // (
            RParen,          // )
            LBracket,        // [
            RBracket,        // ]
            LBrace,          // {
            RBrace,          // }
            Comma,           // ,
            Colon,           // :
            Plus,            // +
            Minus,           // -
            Star,            // *
            QuestionMark,    // ?
            Circ,            // ^
            Tilde,           // ~
            Not,             // !

            And,             // AND
            Or,              // OR
            To,              // TO

            LT,              // <
            LTE,             // <=
            GT,              // >
            GTE,             // >=
            NEQ,             // <>

            ControlKeyword,  // SELECT, SKIP, TOP, SORT, REVERSESORT, AUTOFILTERS, LIFESPAN, COUNTONLY, QUICK

            Field,           // String:
            String,
            WildcardString
        }
        public enum CharType
        {
            Letter,
            Digit,
            Wildcard,
            Sign,
            Escape,
            WhiteSpace,
            Eof
        }

        public string Source { get; }
        public int SourceIndex { get; private set; }
        public char CurrentChar { get; private set; }
        public CharType CurrentCharType { get; private set; }
        public Token CurrentToken { get; private set; }
        public int CurrentLine { get; private set; }
        public int CurrentColumn { get; private set; }
        public int LastLine { get; private set; }
        public int LastColumn { get; private set; }

        public double NumberValue { get; private set; }
        public string StringValue { get; private set; }
        public bool IsPhrase { get; private set; }

        public CqlLexer(string source)
        {
            Source = source;
            CurrentColumn = -1;
            NextChar();
            NextToken();
        }

        private bool NextChar()
        {
            if (SourceIndex < Source.Length)
            {
                if (CurrentChar == '\n')
                {
                    CurrentLine++;
                    CurrentColumn = -1;
                }
                CurrentChar = Source[SourceIndex++];
                CurrentColumn++;
                SetCharType();
                return true;
            }
            CurrentChar = '\0';
            CurrentCharType = CharType.Eof;
            return false;
        }
        private char PeekNextChar()
        {
            if (SourceIndex >= Source.Length)
                return '\0';
            return Source[SourceIndex];
        }

        private void SetCharType()
        {
            switch (CurrentChar)
            {
                case '\\':
                    CurrentCharType = CharType.Escape;
                    break;
                case '*':
                case '?':
                    CurrentCharType = CharType.Wildcard;
                    break;
                default:
                    if (Char.IsWhiteSpace(CurrentChar))
                    {
                        CurrentCharType = CharType.WhiteSpace;
                        return;
                    }
                    if (Char.IsDigit(CurrentChar))
                    {
                        CurrentCharType = CharType.Digit;
                        return;
                    }
                    if (Char.IsLetter(CurrentChar))
                    {
                        CurrentCharType = CharType.Letter;
                        return;
                    }
                    CurrentCharType = CharType.Sign;
                    break;
            }
        }
        public bool NextToken()
        {
            bool hasWildcard;
            bool field;

            SkipWhiteSpaces();
            SaveLineInfo();
            IsPhrase = false;
            switch (CurrentChar)
            {
                case '\0': CurrentToken = Token.Eof; StringValue = string.Empty; return false;

                case '(': CurrentToken = Token.LParen; StringValue = CurrentChar.ToString(); NextChar(); break;
                case ')': CurrentToken = Token.RParen; StringValue = CurrentChar.ToString(); NextChar(); break;
                case '[': CurrentToken = Token.LBracket; StringValue = CurrentChar.ToString(); NextChar(); break;
                case ']': CurrentToken = Token.RBracket; StringValue = CurrentChar.ToString(); NextChar(); break;
                case '{': CurrentToken = Token.LBrace; StringValue = CurrentChar.ToString(); NextChar(); break;
                case '}': CurrentToken = Token.RBrace; StringValue = CurrentChar.ToString(); NextChar(); break;
                case ',': CurrentToken = Token.Comma; StringValue = CurrentChar.ToString(); NextChar(); break;
                case ':': CurrentToken = Token.Colon; StringValue = CurrentChar.ToString(); NextChar(); break;
                case '+': CurrentToken = Token.Plus; StringValue = CurrentChar.ToString(); NextChar(); break;
                case '-': CurrentToken = Token.Minus; StringValue = CurrentChar.ToString(); NextChar(); break;
                case '^': CurrentToken = Token.Circ; StringValue = CurrentChar.ToString(); NextChar(); break;
                case '~': CurrentToken = Token.Tilde; StringValue = CurrentChar.ToString(); NextChar(); break;
                case '!': CurrentToken = Token.Not; StringValue = CurrentChar.ToString(); NextChar(); break;
                case '"':
                case '\'':
                    StringValue = ScanQuotedString(out hasWildcard, out field, out var isPhrase);
                    CurrentToken = hasWildcard ? Token.WildcardString : field ? Token.Field : Token.String;
                    IsPhrase = isPhrase;
                    break;

                case '&':
                    NextChar();
                    if (CurrentChar != '&')
                        throw new ParserException("Invalid operator: &", CreateLastLineInfo());
                    CurrentToken = Token.And;
                    StringValue = "&&";
                    NextChar();
                    SkipWhiteSpaces();
                    break;
                case '|':
                    NextChar();
                    if (CurrentChar != '|')
                        throw new ParserException("Invalid operator: |", CreateLastLineInfo());
                    CurrentToken = Token.Or;
                    StringValue = "||";
                    NextChar();
                    SkipWhiteSpaces();
                    break;
                case '<':
                    NextChar();
                    if (CurrentChar == '=')
                    {
                        CurrentToken = Token.LTE;
                        StringValue = "<=";
                        NextChar();
                        SkipWhiteSpaces();
                    }
                    else if (CurrentChar == '>')
                    {
                        CurrentToken = Token.NEQ;
                        StringValue = "<>";
                        NextChar();
                        SkipWhiteSpaces();
                    }
                    else
                    {
                        CurrentToken = Token.LT;
                        StringValue = "<";
                    }
                    break;
                case '>':
                    NextChar();
                    if (CurrentChar == '=')
                    {
                        CurrentToken = Token.GTE;
                        StringValue = ">=";
                        NextChar();
                        SkipWhiteSpaces();
                    }
                    else
                    {
                        CurrentToken = Token.GT;
                        StringValue = ">";
                    }
                    break;

                // -----------------------------------

                default:
                    if (CurrentCharType == CharType.Digit)
                    {
                        if (ScanNumber(out var numberValue, out var stringValue, out hasWildcard, out field))
                        {
                            CurrentToken = Token.Number;
                            StringValue = stringValue;
                            NumberValue = numberValue;
                        }
                        else
                        {
                            CurrentToken = hasWildcard ? Token.WildcardString : Token.String;
                            StringValue = stringValue;
                        }
                    }
                    else
                    {
                        StringValue = ScanNonQuotedString(out hasWildcard, out field, out var keyword);
                        if (keyword)
                            CurrentToken = Keywords.ScanControl(StringValue, CreateLastLineInfo());
                        else if (hasWildcard)
                            CurrentToken = Token.WildcardString;
                        else if (field)
                            CurrentToken = Token.Field;
                        else
                            CurrentToken = Keywords.ScanKeyword(StringValue);
                        SkipWhiteSpaces();
                    }
                    break;
            }
            return true;
        }

        private bool ScanComment()
        {
            if (CurrentChar != '/')
                return false;

            var c = PeekNextChar();
            if (c != '*' && c != '/')
                return false;

            if (c == '/')
            {
                while (true)
                {
                    NextChar();
                    if (CurrentChar == '\0')
                        return true;
                    if (CurrentChar == '\n')
                    {
                        NextChar();
                        return true;
                    }
                }
            }
            else
            {
                while (true)
                {
                    NextChar();
                    if (CurrentChar == '\0')
                        return true;
                    if (CurrentChar == '*' && PeekNextChar() == '/')
                    {
                        NextChar();
                        NextChar();
                        return true;
                    }
                }
            }
        }
        private bool ScanNumber(out double numberValue, out string stringValue, out bool hasWildcard, out bool field)
        {
            SaveLineInfo();
            var startIndex = SourceIndex - 1;
            var length = 0;
            hasWildcard = false;
            while (CurrentCharType == CharType.Digit)
            {
                NextChar();
                length++;
            }
            if (CurrentChar == '.')
            {
                NextChar();
                length++;
                while (CurrentCharType == CharType.Digit)
                {
                    NextChar();
                    length++;
                }
            }
            if (IsStringEndChar(CurrentChar))
            {
                stringValue = Source.Substring(startIndex, length);
                numberValue = Convert.ToDouble(stringValue, CultureInfo.InvariantCulture);
                if (CurrentChar == '.')
                {
                    NextChar();
                    field = true;
                }
                else
                {
                    field = false;
                }
                return true;
            }
            numberValue = 0.0;
            var s0 = Source.Substring(startIndex, length);
            var s1 = ScanNonQuotedString(out hasWildcard, out field, out _);
            stringValue = s0 + s1;

            return false;
        }
        private string ScanQuotedString(out bool hasWildcard, out bool field, out bool isPhrase)
        {
            SaveLineInfo();
            var stringDelimiter = CurrentChar;
            NextChar();
            hasWildcard = false;
            field = false;
            isPhrase = false;
            var outstr = new StringBuilder();
            while (CurrentChar != stringDelimiter)
            {
                hasWildcard |= CurrentCharType == CharType.Wildcard;
                isPhrase |= CurrentCharType == CharType.WhiteSpace;
                if (CurrentCharType == CharType.Escape)
                {
                    if (!NextChar())
                        throw new ParserException("Unclosed string", CreateLastLineInfo());
                    outstr.Append(CurrentChar);
                    if (!NextChar())
                        throw new ParserException("Unclosed string", CreateLastLineInfo());
                }
                else
                {
                    outstr.Append(CurrentChar);
                    if (!NextChar())
                        throw new ParserException("Unclosed string", CreateLastLineInfo());
                }
            }
            NextChar();
            if (CurrentChar == ':')
            {
                field = true;
                NextChar();
            }
            return outstr.ToString();
        }
        private string ScanNonQuotedString(out bool hasWildcard, out bool field, out bool keyword)
        {
            SaveLineInfo();
            hasWildcard = false;
            field = false;
            var outstr = new StringBuilder();
            keyword = false;

            if (CurrentChar == '.')
            {
                keyword = true;
                outstr.Append(CurrentChar);
                NextChar();

                while (Char.IsLetter(CurrentChar))
                {
                    outstr.Append(CurrentChar);
                    NextChar();
                }
            }
            else
            {
                while (true)
                {
                    hasWildcard |= CurrentCharType == CharType.Wildcard;
                    if (CurrentCharType == CharType.Escape)
                    {
                        NextChar();
                        outstr.Append(CurrentChar);
                        NextChar();
                    }
                    else if (!IsStringEndChar(CurrentChar))
                    {
                        outstr.Append(CurrentChar);
                        NextChar();
                    }
                    else
                    {
                        break;
                    }
                }
            }
            if (CurrentChar == ':')
            {
                field = !keyword;
            }
            else if (CurrentChar == '<' || CurrentChar == '>')
            {
                field = !keyword;
            }
            return outstr.ToString();
        }
        private bool IsStringEndChar(char c)
        {
            return (StringTerminatorChars.Contains(c)) || (CurrentCharType == CharType.WhiteSpace) || (CurrentCharType == CharType.Eof);
        }

        private void SkipWhiteSpaces()
        {
            while (true)
            {
                if (CurrentCharType == CharType.WhiteSpace)
                    NextChar();
                else
                {
                    SaveLineInfo();
                    if (!ScanComment())
                    {
                        RestoreLineInfo();
                        return;
                    }
                }
            }
        }

        private void SaveLineInfo()
        {
            LastLine = CurrentLine;
            LastColumn = CurrentColumn;
        }
        private void RestoreLineInfo()
        {
            CurrentLine = LastLine;
            CurrentColumn = LastColumn;
        }
        internal LineInfo CreateLastLineInfo()
        {
            return new LineInfo(LastLine, LastColumn);
        }
    }
}
