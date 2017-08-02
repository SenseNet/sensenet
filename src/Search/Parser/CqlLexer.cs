using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Globalization;

namespace SenseNet.Search.Parser
{
    internal class CqlLexer
    {
        internal const string STRINGTERMINATORCHARS = "\":+-&|!(){}[]^~";

        public static class Keywords
        {
            public const string Or = "OR";
            public const string And = "AND";
            public const string Not = "NOT";
            public const string To = "TO";

            public static Token ScanKeyword(string text)
            {
                switch (text)
                {
                    case Or: return Token.Or;
                    case And: return Token.And;
                    case Not: return Token.Not;
                    case To: return Token.To;
                    default: return Token.String;
                }
            }

            public const string Select = ".SELECT";
            public const string Skip = ".SKIP";
            public const string Top = ".TOP";
            public const string Sort = ".SORT";
            public const string ReverseSort = ".REVERSESORT";
            public const string Autofilters = ".AUTOFILTERS";
            public const string Lifespan = ".LIFESPAN";
            public const string CountOnly = ".COUNTONLY";
            public const string Quick = ".QUICK";

            public static Token ScanControl(string text, LineInfo line)
            {
                if (text == Select || text == Skip || text == Top || text == Sort || text == ReverseSort ||
                    text == Autofilters || text == Lifespan || text == CountOnly || text == Quick)
                    return Token.ControlKeyword;
                throw new ParserException("Unknown control keyword: " + text, line);
            }

            public const string On = "ON";
            public const string Off = "OFF";

        }
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

        public string Source { get; private set; }
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
            this.Source = source;
            this.CurrentColumn = -1;
            NextChar();
            NextToken();
        }

        private bool NextChar()
        {
            if (this.SourceIndex < this.Source.Length)
            {
                if (this.CurrentChar == '\n')
                {
                    this.CurrentLine++;
                    this.CurrentColumn = -1;
                }
                this.CurrentChar = this.Source[this.SourceIndex++];
                this.CurrentColumn++;
                SetCharType();
                return true;
            }
            this.CurrentChar = '\0';
            this.CurrentCharType = CharType.Eof;
            return false;
        }
        private char PeekNextChar()
        {
            if (this.SourceIndex >= this.Source.Length)
                return '\0';
            return this.Source[SourceIndex];
        }

        private void SetCharType()
        {
            switch (this.CurrentChar)
            {
                case '\\':
                    CurrentCharType = CharType.Escape;
                    break;
                case '*':
                case '?':
                    CurrentCharType = CharType.Wildcard;
                    break;
                default:
                    if (Char.IsWhiteSpace(this.CurrentChar))
                    {
                        CurrentCharType = CharType.WhiteSpace;
                        return;
                    }
                    if (Char.IsDigit(this.CurrentChar))
                    {
                        CurrentCharType = CharType.Digit;
                        return;
                    }
                    if (Char.IsLetter(this.CurrentChar))
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
            bool isPhrase;
            bool field;
            bool keyword;

            SkipWhiteSpaces();
            SaveLineInfo();
            this.IsPhrase = false;
            switch (this.CurrentChar)
            {
                case '\0': this.CurrentToken = Token.Eof; this.StringValue = string.Empty; return false;

                case '(': this.CurrentToken = Token.LParen; this.StringValue = this.CurrentChar.ToString(); NextChar(); break;
                case ')': this.CurrentToken = Token.RParen; this.StringValue = this.CurrentChar.ToString(); NextChar(); break;
                case '[': this.CurrentToken = Token.LBracket; this.StringValue = this.CurrentChar.ToString(); NextChar(); break;
                case ']': this.CurrentToken = Token.RBracket; this.StringValue = this.CurrentChar.ToString(); NextChar(); break;
                case '{': this.CurrentToken = Token.LBrace; this.StringValue = this.CurrentChar.ToString(); NextChar(); break;
                case '}': this.CurrentToken = Token.RBrace; this.StringValue = this.CurrentChar.ToString(); NextChar(); break;
                case ',': this.CurrentToken = Token.Comma; this.StringValue = this.CurrentChar.ToString(); NextChar(); break;
                case ':': this.CurrentToken = Token.Colon; this.StringValue = this.CurrentChar.ToString(); NextChar(); break;
                case '+': this.CurrentToken = Token.Plus; this.StringValue = this.CurrentChar.ToString(); NextChar(); break;
                case '-': this.CurrentToken = Token.Minus; this.StringValue = this.CurrentChar.ToString(); NextChar(); break;
                case '^': this.CurrentToken = Token.Circ; this.StringValue = this.CurrentChar.ToString(); NextChar(); break;
                case '~': this.CurrentToken = Token.Tilde; this.StringValue = this.CurrentChar.ToString(); NextChar(); break;
                case '!': this.CurrentToken = Token.Not; this.StringValue = this.CurrentChar.ToString(); NextChar(); break;
                case '"':
                case '\'':
                    this.StringValue = this.ScanQuotedString(out hasWildcard, out field, out isPhrase);
                    this.CurrentToken = hasWildcard ? Token.WildcardString : field ? Token.Field : Token.String;
                    this.IsPhrase = isPhrase;
                    break;

                case '&':
                    this.NextChar();
                    if (this.CurrentChar != '&')
                        throw new ParserException("Invalid operator: &", CreateLastLineInfo());
                    this.CurrentToken = Token.And;
                    this.StringValue = "&&";
                    this.NextChar();
                    this.SkipWhiteSpaces();
                    break;
                case '|':
                    this.NextChar();
                    if (this.CurrentChar != '|')
                        throw new ParserException("Invalid operator: |", CreateLastLineInfo());
                    this.CurrentToken = Token.Or;
                    this.StringValue = "||";
                    this.NextChar();
                    this.SkipWhiteSpaces();
                    break;
                case '<':
                    this.NextChar();
                    if (this.CurrentChar == '=')
                    {
                        this.CurrentToken = Token.LTE;
                        this.StringValue = "<=";
                        NextChar();
                        this.SkipWhiteSpaces();
                    }
                    else if (this.CurrentChar == '>')
                    {
                        this.CurrentToken = Token.NEQ;
                        this.StringValue = "<>";
                        NextChar();
                        this.SkipWhiteSpaces();
                    }
                    else
                    {
                        this.CurrentToken = Token.LT;
                        this.StringValue = "<";
                    }
                    break;
                case '>':
                    this.NextChar();
                    if (this.CurrentChar == '=')
                    {
                        this.CurrentToken = Token.GTE;
                        this.StringValue = ">=";
                        NextChar();
                        this.SkipWhiteSpaces();
                    }
                    else
                    {
                        this.CurrentToken = Token.GT;
                        this.StringValue = ">";
                    }
                    break;

                // -----------------------------------

                default:
                    if (this.CurrentCharType == CharType.Digit)
                    {
                        double numberValue;
                        string stringValue;
                        if (this.ScanNumber(out numberValue, out stringValue, out hasWildcard, out field))
                        {
                            this.CurrentToken = Token.Number;
                            this.StringValue = stringValue;
                            this.NumberValue = numberValue;
                        }
                        else
                        {
                            this.CurrentToken = hasWildcard ? Token.WildcardString : Token.String;
                            this.StringValue = stringValue;
                        }
                    }
                    else
                    {
                        this.StringValue = this.ScanNonQuotedString(out hasWildcard, out field, out keyword);
                        if (keyword)
                            this.CurrentToken = Keywords.ScanControl(this.StringValue, CreateLastLineInfo());
                        else if (hasWildcard)
                            this.CurrentToken = Token.WildcardString;
                        else if (field)
                            this.CurrentToken = Token.Field;
                        else
                            this.CurrentToken = Keywords.ScanKeyword(this.StringValue);
                        this.SkipWhiteSpaces();
                    }
                    break;
            }
            return true;
        }

        private bool ScanComment()
        {
            if (this.CurrentChar != '/')
                return false;

            var c = PeekNextChar();
            if (c != '*' && c != '/')
                return false;

            if (c == '/')
            {
                while (true)
                {
                    NextChar();
                    if (this.CurrentChar == '\0')
                        return true;
                    if (this.CurrentChar == '\n')
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
                    if (this.CurrentChar == '\0')
                        return true;
                    if (this.CurrentChar == '*' && PeekNextChar() == '/')
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
            var startIndex = this.SourceIndex - 1;
            var length = 0;
            hasWildcard = false;
            while (this.CurrentCharType == CharType.Digit)
            {
                NextChar();
                length++;
            }
            if (this.CurrentChar == '.')
            {
                NextChar();
                length++;
                while (this.CurrentCharType == CharType.Digit)
                {
                    NextChar();
                    length++;
                }
            }
            if (IsStringEndChar(this.CurrentChar))
            {
                stringValue = this.Source.Substring(startIndex, length);
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
            var s0 = this.Source.Substring(startIndex, length);
            bool keyword;
            var s1 = ScanNonQuotedString(out hasWildcard, out field, out keyword);
            stringValue = s0 + s1;

            return false;
        }
        private string ScanQuotedString(out bool hasWildcard, out bool field, out bool isPhrase)
        {
            SaveLineInfo();
            char stringDelimiter = this.CurrentChar;
            NextChar();
            hasWildcard = false;
            field = false;
            isPhrase = false;
            var outstr = new StringBuilder();
            while (this.CurrentChar != stringDelimiter)
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

            if (this.CurrentChar == '.')
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
                    else if (!IsStringEndChar(this.CurrentChar))
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
            return (STRINGTERMINATORCHARS.Contains(c)) || (CurrentCharType == CharType.WhiteSpace) || (CurrentCharType == CharType.Eof);
        }

        private void SkipWhiteSpaces()
        {
            while (true)
            {
                if (this.CurrentCharType == CharType.WhiteSpace)
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
