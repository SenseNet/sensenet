using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Search.Querying.Parser;
using SenseNet.Tests;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Testing;

namespace SenseNet.Search.Tests
{
    [TestClass]
    public class CqlLexerTests : TestBase
    {
        [DebuggerDisplay("{Token}:{Value}")]
        private class TokenChecker
        {
            public CqlLexer.Token Token { get; set; }
            public string Value { get; set; }
        }

        private IEnumerable<TokenChecker> GetTokens(string source)
        {
            var lexer = new CqlLexer(source);
            var tokens = new List<TokenChecker>();
            do
            {
                //tokens.Add(new TokenChecker { Token = lexer.CurrentToken, Value = lexer.StringValue });
                tokens.Add(new TokenChecker { Token = lexer.CurrentToken, Value = lexer.StringValue });
            }
            while (lexer.NextToken());

            return tokens;
        }
        private string CheckTokensAndEof(IEnumerable<TokenChecker> tokensToCheck, IEnumerable<TokenChecker> expectedTokens)
        {
            if (tokensToCheck.Count() != expectedTokens.Count())
                return string.Format("Token counts are not equal. Expected: {0}, Current: {1}", expectedTokens.Count(), tokensToCheck.Count());

            for (int i = 0; i < tokensToCheck.Count(); i++)
            {
                if (tokensToCheck.ElementAt(i).Token != expectedTokens.ElementAt(i).Token)
                    return string.Format("Tokens are not equal on position {0}. Expected: {1}, Current: {2}", i, expectedTokens.ElementAt(i).Token, tokensToCheck.ElementAt(i).Token);
                if (tokensToCheck.ElementAt(i).Value != expectedTokens.ElementAt(i).Value)
                    return string.Format("Values are not equal on position {0}. Expected: {1}, Current: {2}", i, expectedTokens.ElementAt(i).Value, tokensToCheck.ElementAt(i).Value);
            }
            return null;
        }


        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_Keywords()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.ControlKeyword, Value = ".AUTOFILTERS" },
                new TokenChecker { Token = CqlLexer.Token.Colon, Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.String,  Value = "OFF" },
                new TokenChecker { Token = CqlLexer.Token.ControlKeyword, Value = ".SKIP" },
                new TokenChecker { Token = CqlLexer.Token.Colon, Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.Number,  Value = "100" },
                new TokenChecker { Token = CqlLexer.Token.ControlKeyword, Value = ".TOP" },
                new TokenChecker { Token = CqlLexer.Token.Colon, Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.Number,  Value = "20" },
                new TokenChecker { Token = CqlLexer.Token.ControlKeyword,  Value = ".SORT" },
                new TokenChecker { Token = CqlLexer.Token.Colon, Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.String,  Value = "FieldName1" },
                new TokenChecker { Token = CqlLexer.Token.ControlKeyword,  Value = ".REVERSESORT" },
                new TokenChecker { Token = CqlLexer.Token.Colon, Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.String,  Value = "FieldName2" },
                new TokenChecker { Token = CqlLexer.Token.ControlKeyword,  Value = ".SORT" },
                new TokenChecker { Token = CqlLexer.Token.Colon, Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.String,  Value = "FieldName3" },
            };
            var tokens = GetTokens(".AUTOFILTERS:OFF .SKIP:100 .TOP:20 .SORT:FieldName1 .REVERSESORT:FieldName2 .SORT:FieldName3");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_UnknownKeywords1()
        {
            try
            {
                var tokens = GetTokens("  :OFF . . : .:  ");
                Assert.Fail("Expected Parserexception was not thrown.");
            }
            catch (ParserException e)
            {
                Assert.AreEqual(0, e.LineInfo.Line);
                Assert.AreEqual(7, e.LineInfo.Column);
            }
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_UnknownKeywords2()
        {
            try
            {
                var tokens = GetTokens("  ..AUTOFILTERS::OFF . . : .:  ");
                Assert.Fail("Expected Parserexception was not thrown.");
            }
            catch (ParserException e)
            {
                Assert.AreEqual(0, e.LineInfo.Line);
                Assert.AreEqual(2, e.LineInfo.Column);
            }
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_FieldLimiters()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.Field, Value = "Field" },
                new TokenChecker { Token = CqlLexer.Token.Colon,  Value = ":" },
                new TokenChecker { Token = CqlLexer.Token.String,  Value = "value" },

                new TokenChecker { Token = CqlLexer.Token.Field, Value = "Field" },
                new TokenChecker { Token = CqlLexer.Token.Colon,  Value = ":" },
                new TokenChecker { Token = CqlLexer.Token.GT,  Value = ">" },
                new TokenChecker { Token = CqlLexer.Token.String,  Value = "value" },

                new TokenChecker { Token = CqlLexer.Token.Field, Value = "Field" },
                new TokenChecker { Token = CqlLexer.Token.Colon,  Value = ":" },
                new TokenChecker { Token = CqlLexer.Token.LT,  Value = "<" },
                new TokenChecker { Token = CqlLexer.Token.String,  Value = "value" },

                new TokenChecker { Token = CqlLexer.Token.Field, Value = "Field" },
                new TokenChecker { Token = CqlLexer.Token.Colon,  Value = ":" },
                new TokenChecker { Token = CqlLexer.Token.GTE,  Value = ">=" },
                new TokenChecker { Token = CqlLexer.Token.String,  Value = "value" },

                new TokenChecker { Token = CqlLexer.Token.Field, Value = "Field" },
                new TokenChecker { Token = CqlLexer.Token.Colon,  Value = ":" },
                new TokenChecker { Token = CqlLexer.Token.LTE,  Value = "<=" },
                new TokenChecker { Token = CqlLexer.Token.String,  Value = "value" },

                new TokenChecker { Token = CqlLexer.Token.Field, Value = "Field" },
                new TokenChecker { Token = CqlLexer.Token.Colon,  Value = ":" },
                new TokenChecker { Token = CqlLexer.Token.NEQ,  Value = "<>" },
                new TokenChecker { Token = CqlLexer.Token.String,  Value = "value" },
            };
            var tokens = GetTokens(" Field:value Field:>value Field:<value Field:>=value Field:<=value Field:<>value ");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_FieldBadLimiters()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.Field, Value = "Field" },
                new TokenChecker { Token = CqlLexer.Token.Colon,  Value = ":" },
                new TokenChecker { Token = CqlLexer.Token.String,  Value = "value" },

                new TokenChecker { Token = CqlLexer.Token.String, Value = "Field>value" },
                new TokenChecker { Token = CqlLexer.Token.String, Value = "Field<value" },
                new TokenChecker { Token = CqlLexer.Token.String, Value = "Field>=value" },
                new TokenChecker { Token = CqlLexer.Token.String, Value = "Field<=value" },
                new TokenChecker { Token = CqlLexer.Token.String, Value = "Field<>value" },
            };
            var tokens = GetTokens(" Field:value Field>value Field<value Field>=value Field<=value Field<>value ");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_FieldListFieldPrefix()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.Field, Value = "#Field1" },
                new TokenChecker { Token = CqlLexer.Token.Colon,  Value = ":" },
                new TokenChecker { Token = CqlLexer.Token.String,  Value = "value" },
                new TokenChecker { Token = CqlLexer.Token.Field, Value = "#Field2" },
                new TokenChecker { Token = CqlLexer.Token.Colon,  Value = ":" },
                new TokenChecker { Token = CqlLexer.Token.String,  Value = "value" },
            };
            var tokens = GetTokens("#Field1:value #Field2:value");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }

        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_FieldGrouping()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.Field, Value = "title" },
                new TokenChecker { Token = CqlLexer.Token.Colon, Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.LParen,  Value = "(" },
                new TokenChecker { Token = CqlLexer.Token.Plus, Value = "+" },
                new TokenChecker { Token = CqlLexer.Token.String, Value= "return" },
                new TokenChecker { Token = CqlLexer.Token.Plus,  Value = "+" },
                new TokenChecker { Token = CqlLexer.Token.String, Value = "pink panther" },
                new TokenChecker { Token = CqlLexer.Token.RParen, Value= ")" },
            };
            var tokens = GetTokens("title:(+return +\"pink panther\")");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }

        //--
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_TextAndNumber()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.String, Value = "text" },
                new TokenChecker { Token = CqlLexer.Token.Number, Value = "9" },
                new TokenChecker { Token = CqlLexer.Token.String, Value = "text" },
                new TokenChecker { Token = CqlLexer.Token.Number, Value = "12.34" },
                new TokenChecker { Token = CqlLexer.Token.ControlKeyword, Value = ".SKIP" },
                new TokenChecker { Token = CqlLexer.Token.String, Value = "12aa" },
                new TokenChecker { Token = CqlLexer.Token.String, Value = "a12" },
                new TokenChecker { Token = CqlLexer.Token.String, Value = "12.aa" },
                new TokenChecker { Token = CqlLexer.Token.String, Value = "aa12." },
                new TokenChecker { Token = CqlLexer.Token.String, Value = "12.34aa" },
                new TokenChecker { Token = CqlLexer.Token.Minus,  Value = "-" },
                new TokenChecker { Token = CqlLexer.Token.Number, Value = "12" },
                new TokenChecker { Token = CqlLexer.Token.Minus,  Value = "-" },
                new TokenChecker { Token = CqlLexer.Token.Number, Value = "12.34" },
                new TokenChecker { Token = CqlLexer.Token.Minus,  Value = "-" },
                new TokenChecker { Token = CqlLexer.Token.ControlKeyword, Value = ".TOP" }
            };
            var tokens = GetTokens("text 9 text 12.34 .SKIP 12aa a12 12.aa aa12. 12.34aa -12 -12.34 -.TOP");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_AndOrNot()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.String, Value = "text" },
                new TokenChecker { Token = CqlLexer.Token.And,    Value = "AND" },
                new TokenChecker { Token = CqlLexer.Token.String, Value = "and" },
                new TokenChecker { Token = CqlLexer.Token.Or,     Value = "OR" },
                new TokenChecker { Token = CqlLexer.Token.String, Value = "or" },
                new TokenChecker { Token = CqlLexer.Token.Not,    Value = "NOT" },
                new TokenChecker { Token = CqlLexer.Token.String, Value = "not" },
                new TokenChecker { Token = CqlLexer.Token.Not,    Value = "!" },
                new TokenChecker { Token = CqlLexer.Token.And,    Value = "&&" },
                new TokenChecker { Token = CqlLexer.Token.Or,     Value = "||" },
                new TokenChecker { Token = CqlLexer.Token.String, Value = "text" }
            };
            var tokens = GetTokens("text AND and OR or NOT not ! && || text");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_StringWithInnerApos()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.Field,  Value = "fieldname" },
                new TokenChecker { Token = CqlLexer.Token.Colon, Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.String, Value = "text text 'text' text" }
            };
            var tokens = GetTokens("fieldname:\"text text 'text' text\"");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_Numbers()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.Field,  Value= "NumberField" },
                new TokenChecker { Token = CqlLexer.Token.Colon, Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.Number, Value= "45678" },
                new TokenChecker { Token = CqlLexer.Token.Field,  Value= "NumberField" },
                new TokenChecker { Token = CqlLexer.Token.Colon, Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.Number, Value= "456.78" },
                new TokenChecker { Token = CqlLexer.Token.Minus,  Value= "-" },
                new TokenChecker { Token = CqlLexer.Token.Field,  Value= "NumberField" },
                new TokenChecker { Token = CqlLexer.Token.Colon, Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.Minus,  Value= "-" },
                new TokenChecker { Token = CqlLexer.Token.Number, Value= "78.456" }
            };
            var tokens = GetTokens("NumberField:45678 NumberField:456.78 -NumberField:-78.456");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_Path()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.Field,  Value= "Ancestor" },
                new TokenChecker { Token = CqlLexer.Token.Colon, Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.String, Value= "/Root/System" }
            };
            var tokens = GetTokens("Ancestor:/Root/System");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_TwoPaths()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.Plus, Value= "+" },
                new TokenChecker { Token = CqlLexer.Token.Field, Value= "Ancestor" },
                new TokenChecker { Token = CqlLexer.Token.Colon, Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.String, Value= "/Root/System" },
                new TokenChecker { Token = CqlLexer.Token.Minus, Value= "-" },
                new TokenChecker { Token = CqlLexer.Token.Field, Value= "Path" },
                new TokenChecker { Token = CqlLexer.Token.Colon, Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.String, Value= "/Root/System" }
            };
            var tokens = GetTokens("+Ancestor:/Root/System -Path:/Root/System");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_Groups()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.Field,  Value= "Field1" },
                new TokenChecker { Token = CqlLexer.Token.Colon, Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.LParen, Value= "(" },
                new TokenChecker { Token = CqlLexer.Token.Plus,   Value= "+" },
                new TokenChecker { Token = CqlLexer.Token.String, Value= "aaa" },
                new TokenChecker { Token = CqlLexer.Token.Plus,   Value= "+" },
                new TokenChecker { Token = CqlLexer.Token.LParen, Value= "(" },
                new TokenChecker { Token = CqlLexer.Token.String, Value= "bbb" },
                new TokenChecker { Token = CqlLexer.Token.String, Value= "ccc" },
                new TokenChecker { Token = CqlLexer.Token.RParen, Value= ")" },
                new TokenChecker { Token = CqlLexer.Token.String, Value= "ddd" },
                new TokenChecker { Token = CqlLexer.Token.Minus,  Value= "-" },
                new TokenChecker { Token = CqlLexer.Token.String, Value= "eee" },
                new TokenChecker { Token = CqlLexer.Token.RParen, Value= ")" },
            };
            var tokens = GetTokens("Field1:(+aaa +(bbb ccc) ddd -eee)");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_Range_Brackets()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.Field,         Value= "Number" },
                new TokenChecker { Token = CqlLexer.Token.Colon,         Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.LBracket,      Value= "[" },
                new TokenChecker { Token = CqlLexer.Token.Number,        Value= "1234" },
                new TokenChecker { Token = CqlLexer.Token.To,            Value= "TO" },
                new TokenChecker { Token = CqlLexer.Token.Number,        Value= "5678" },
                new TokenChecker { Token = CqlLexer.Token.RBracket,      Value= "]" },
            };
            var tokens = GetTokens("Number:[1234 TO 5678]");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_Range_Braces()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.Field,         Value= "Number" },
                new TokenChecker { Token = CqlLexer.Token.Colon,         Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.LBrace,        Value= "{" },
                new TokenChecker { Token = CqlLexer.Token.Number,        Value= "1234" },
                new TokenChecker { Token = CqlLexer.Token.To,            Value= "TO" },
                new TokenChecker { Token = CqlLexer.Token.Number,        Value= "5678" },
                new TokenChecker { Token = CqlLexer.Token.RBrace,        Value= "}" },
            };
            var tokens = GetTokens("Number:{1234 TO 5678}");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_Wildcards()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.Plus,            Value= "+" },
                new TokenChecker { Token = CqlLexer.Token.WildcardString,  Value= "startswith*" },
                new TokenChecker { Token = CqlLexer.Token.Minus,           Value= "-" },
                new TokenChecker { Token = CqlLexer.Token.WildcardString,  Value= "*endswith" },
                new TokenChecker { Token = CqlLexer.Token.WildcardString,  Value= "*contains*" },
                new TokenChecker { Token = CqlLexer.Token.Plus,            Value= "+" },
                new TokenChecker { Token = CqlLexer.Token.WildcardString,  Value= "starts*ends" },
                new TokenChecker { Token = CqlLexer.Token.Minus,           Value= "-" },
                new TokenChecker { Token = CqlLexer.Token.WildcardString,  Value= "startswith?" },
                new TokenChecker { Token = CqlLexer.Token.WildcardString,  Value= "?endswith" },
                new TokenChecker { Token = CqlLexer.Token.Plus,            Value= "+" },
                new TokenChecker { Token = CqlLexer.Token.WildcardString,  Value= "?contains?" },
                new TokenChecker { Token = CqlLexer.Token.Minus,           Value= "-" },
                new TokenChecker { Token = CqlLexer.Token.WildcardString,  Value= "starts?ends" },
            };
            var tokens = GetTokens("+startswith* -*endswith *contains* +starts*ends -startswith? ?endswith +?contains? -starts?ends");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_FieldName()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.Field, Value = "contains" },
                new TokenChecker { Token = CqlLexer.Token.Colon, Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.String, Value = "colon" }
            };
            var tokens = GetTokens("contains:colon");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_FieldNameInQuotedString()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.String, Value = "contains:colon" }
            };
            var tokens = GetTokens("\"contains:colon\"");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }

        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_QuotedStringAndEscapes()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.String, Value = "contains:colon" }
            };
            var tokens = GetTokens("\"contains\\:colon\"");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }
        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_NonQuotedStringAndEscapes()
        {
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.String, Value = "contains:colon" }
            };
            var tokens = GetTokens("contains\\:colon");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }

        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_X()
        {
            //dump: BooleanQuery(Clause(Occur(), TermQuery(Term(F:a))), Clause(Occur(), BooleanQuery(Clause(Occur(+), TermQuery(Term(G:b))), Clause(Occur(-), TermQuery(Term(F:d))))))
            var expectedTokens = new TokenChecker[]
            {
                new TokenChecker { Token = CqlLexer.Token.Field,  Value= "F" },
                new TokenChecker { Token = CqlLexer.Token.Colon, Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.LParen, Value= "(" },
                new TokenChecker { Token = CqlLexer.Token.String, Value= "a" },
                new TokenChecker { Token = CqlLexer.Token.LParen, Value= "(" },
                new TokenChecker { Token = CqlLexer.Token.Plus,   Value= "+" },
                new TokenChecker { Token = CqlLexer.Token.Field,  Value= "G" },
                new TokenChecker { Token = CqlLexer.Token.Colon, Value= ":" },
                new TokenChecker { Token = CqlLexer.Token.String, Value= "b" },
                new TokenChecker { Token = CqlLexer.Token.Minus,  Value= "-" },
                new TokenChecker { Token = CqlLexer.Token.String, Value= "d" },
                new TokenChecker { Token = CqlLexer.Token.RParen, Value= ")" },
                new TokenChecker { Token = CqlLexer.Token.RParen, Value= ")" }
            };
            var tokens = GetTokens("F:(a (+G:b -d))");
            var msg = CheckTokensAndEof(tokens, expectedTokens);
            Assert.IsNull(msg, msg);
        }

        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_CharTypeDoesNotThrow()
        {
            var s = new String(Enumerable.Range(1, 256 - 32).Select(i => (char)i).ToArray());
            var lexer = new CqlLexer(s);
            var lexerAcc = new ObjectAccessor(lexer);
            var thrown = false;
            try
            {
                while ((bool)lexerAcc.Invoke("NextChar")) ;
            }
            catch (Exception) // not logged, not rethrown
            {
                thrown = true;
            }
            Assert.IsFalse(thrown);
        }

        [TestMethod, TestCategory("IR")]
        public void SnQuery_Lexer_Comments()
        {
            TokenChecker t;

            var tokens = GetTokens(@"+Id:>042
/*Block comment* / **/ +TypeIs:WebContent // one line comment
-Name:'// /* this is not a comment */' .TOP:3
").ToArray();
            Assert.AreEqual(16, tokens.Length);
            t = tokens[0]; Assert.AreEqual(CqlLexer.Token.Plus, t.Token); Assert.AreEqual("+", t.Value);
            t = tokens[1]; Assert.AreEqual(CqlLexer.Token.Field, t.Token); Assert.AreEqual("Id", t.Value);
            t = tokens[2]; Assert.AreEqual(CqlLexer.Token.Colon, t.Token); Assert.AreEqual(":", t.Value);
            t = tokens[3]; Assert.AreEqual(CqlLexer.Token.GT, t.Token); Assert.AreEqual(">", t.Value);
            t = tokens[4]; Assert.AreEqual(CqlLexer.Token.Number, t.Token); Assert.AreEqual("042", t.Value);
            t = tokens[5]; Assert.AreEqual(CqlLexer.Token.Plus, t.Token); Assert.AreEqual("+", t.Value);
            t = tokens[6]; Assert.AreEqual(CqlLexer.Token.Field, t.Token); Assert.AreEqual("TypeIs", t.Value);
            t = tokens[7]; Assert.AreEqual(CqlLexer.Token.Colon, t.Token); Assert.AreEqual(":", t.Value);
            t = tokens[8]; Assert.AreEqual(CqlLexer.Token.String, t.Token); Assert.AreEqual("WebContent", t.Value);
            t = tokens[9]; Assert.AreEqual(CqlLexer.Token.Minus, t.Token); Assert.AreEqual("-", t.Value);
            t = tokens[10]; Assert.AreEqual(CqlLexer.Token.Field, t.Token); Assert.AreEqual("Name", t.Value);
            t = tokens[11]; Assert.AreEqual(CqlLexer.Token.Colon, t.Token); Assert.AreEqual(":", t.Value);
            t = tokens[12]; Assert.AreEqual(CqlLexer.Token.WildcardString, t.Token); Assert.AreEqual("// /* this is not a comment */", t.Value);
            t = tokens[13]; Assert.AreEqual(CqlLexer.Token.ControlKeyword, t.Token); Assert.AreEqual(".TOP", t.Value);
            t = tokens[14]; Assert.AreEqual(CqlLexer.Token.Colon, t.Token); Assert.AreEqual(":", t.Value);
            t = tokens[15]; Assert.AreEqual(CqlLexer.Token.Number, t.Token); Assert.AreEqual("3", t.Value);

            tokens = GetTokens(@"+Id:>042 /// one line comment
+TypeIs:WebContent /unparsed comment
/* unterminated comment
").ToArray();
            Assert.AreEqual(11, tokens.Length);
            t = tokens[0]; Assert.AreEqual(CqlLexer.Token.Plus, t.Token); Assert.AreEqual("+", t.Value);
            t = tokens[1]; Assert.AreEqual(CqlLexer.Token.Field, t.Token); Assert.AreEqual("Id", t.Value);
            t = tokens[2]; Assert.AreEqual(CqlLexer.Token.Colon, t.Token); Assert.AreEqual(":", t.Value);
            t = tokens[3]; Assert.AreEqual(CqlLexer.Token.GT, t.Token); Assert.AreEqual(">", t.Value);
            t = tokens[4]; Assert.AreEqual(CqlLexer.Token.Number, t.Token); Assert.AreEqual("042", t.Value);
            t = tokens[5]; Assert.AreEqual(CqlLexer.Token.Plus, t.Token); Assert.AreEqual("+", t.Value);
            t = tokens[6]; Assert.AreEqual(CqlLexer.Token.Field, t.Token); Assert.AreEqual("TypeIs", t.Value);
            t = tokens[7]; Assert.AreEqual(CqlLexer.Token.Colon, t.Token); Assert.AreEqual(":", t.Value);
            t = tokens[8]; Assert.AreEqual(CqlLexer.Token.String, t.Token); Assert.AreEqual("WebContent", t.Value);
            t = tokens[9]; Assert.AreEqual(CqlLexer.Token.String, t.Token); Assert.AreEqual("/unparsed", t.Value);
            t = tokens[10]; Assert.AreEqual(CqlLexer.Token.String, t.Token); Assert.AreEqual("comment", t.Value);
        }
    }
}
