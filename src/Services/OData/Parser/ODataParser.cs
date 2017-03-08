using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;

namespace SenseNet.Portal.OData.Parser
{
    internal class ODataParser
    {
        private ODataRequest req;
        private ODataLexer lexer;
        public ODataLexer Lexer { get { return lexer; } }
        private ExpressionBuilder builder;
        public ExpressionBuilder Builder { get { return builder; } }

        public ODataParser()
        {
            builder = new ExpressionBuilder(this);
        }

        internal void Parse(string filter, ODataRequest req)
        {
            this.req = req;
            if (filter != null) parseFilterExpr(filter);
        }
        private void parseFilterExpr(string filter)
        {
            this.lexer = new ODataLexer(filter);
            var expr = this.parseExpr();
            var token = this.lexer.Token;
            if (token != null && token.TokenType != TokenType.EOF)
                throw SyntaxError(String.Concat("Unexpected ", token.TokenType.ToString(), " in $filter: '", token.Value, "'."));
            var final = Expression.Lambda(expr, this.Builder.x);
            this.req.Filter = final;
        }

        /***************************************************     BNF    *****************************************************/

        internal class FunctionNames
        {
            public const string substringof = "substringof";
            public const string endswith = "endswith";
            public const string startswith = "startswith";
            public const string length = "length";
            public const string indexof = "indexof";
            public const string replace = "replace";
            public const string substring = "substring";
            public const string tolower = "tolower";
            public const string toupper = "toupper";
            public const string trim = "trim";
            public const string concat = "concat";

            public const string day = "day";
            public const string hour = "hour";
            public const string minute = "minute";
            public const string month = "month";
            public const string second = "second";
            public const string year = "year";

            public const string round = "round";
            public const string floor = "floor";
            public const string ceiling = "ceiling";

            public const string isof = "isof";
        }
        private static List<string> functionNames = new List<string>(new[]{
            // String
            FunctionNames.substringof, FunctionNames.endswith, FunctionNames.startswith, FunctionNames.length, FunctionNames.indexof, 
            FunctionNames.replace, FunctionNames.substring, FunctionNames.tolower, FunctionNames.toupper, FunctionNames.trim, FunctionNames.concat,
            // Date
            FunctionNames.day, FunctionNames.hour, FunctionNames.minute, FunctionNames.month, FunctionNames.second, FunctionNames.year,
            // Math
            FunctionNames.round, FunctionNames.floor, FunctionNames.ceiling,
            // Type
            FunctionNames.isof
        });

        private Expression parseExpr()
        {
            // bnf: Expr               : OrExpr;
            return this.parseOrExpr();
        }
        private Expression parseOrExpr()
        {
            // bnf: OrExpr             : AndExpr | OrExpr "or" AndExpr;
            var expr = this.parseAndExpr();
            if (this.lexer.Token.Value == "or")
            {
                this.lexer.NextToken();
                var right = this.parseOrExpr();
                if (right == null)
                    throw SyntaxError("Expected: Expr.");
                expr = this.builder.BuildSimpleBinary(expr, right, "or");
            }
            return expr;
        }
        private Expression parseAndExpr()
        {
            // bnf: AndExpr            : EqualityExpr | AndExpr "and" EqualityExpr;
            var expr = this.parseEqualityExpr();
            if (this.lexer.Token.Value == "and")
            {
                this.lexer.NextToken();
                var right = this.parseAndExpr();
                if (right == null)
                    throw SyntaxError("Expected: Expr.");
                expr = this.builder.BuildSimpleBinary(expr, right, "and");
            }
            return expr;
        }
        private Expression parseEqualityExpr()
        {
            // bnf: EqualityExpr       : RelationalExpr | EqualityExpr "eq" RelationalExpr | EqualityExpr "ne" RelationalExpr;
            var expr = this.parseRelationalExpr();
            var token = this.lexer.Token;
            if (token.Value == "eq" || token.Value == "ne")
            {
                this.lexer.NextToken();
                var right = this.parseEqualityExpr();
                if (right == null)
                    throw SyntaxError("Expected: Expr.");
                expr = this.builder.BuildSimpleBinary(expr, right, token.Value);
            }
            return expr;
        }
        private Expression parseRelationalExpr()
        {
            // bnf: RelationalExpr     : AdditiveExpr | RelationalExpr ( "lt" | "gt" | "le" | "ge" ) AdditiveExpr;
            var expr = this.parseAdditiveExpr();
            var token = this.lexer.Token;
            if (token.Value == "lt" || token.Value == "gt" || token.Value == "le" || token.Value == "ge")
            {
                this.lexer.NextToken();
                var right = this.parseRelationalExpr();
                if (right == null)
                    throw SyntaxError("Expected: Expr.");
                expr = this.builder.BuildSimpleBinary(expr, right, token.Value);
            }
            return expr;
        }
        private Expression parseAdditiveExpr()
        {
            // bnf: AdditiveExpr       : MultiplicativeExpr | AdditiveExpr "add" MultiplicativeExpr | AdditiveExpr "sub" MultiplicativeExpr;
            var expr = this.parseMultiplicativeExpr();
            var token = this.lexer.Token;
            if (token.Value == "add" || token.Value == "sub")
            {
                this.lexer.NextToken();
                var right = this.parseAdditiveExpr();
                if (right == null)
                    throw SyntaxError("Expected: Expr.");
                expr = this.builder.BuildSimpleBinary(expr, right, token.Value);
            }
            return expr;
        }
        private Expression parseMultiplicativeExpr()
        {
            // bnf: MultiplicativeExpr : UnaryExpr | MultiplicativeExpr "mul" UnaryExpr | MultiplicativeExpr "div"  UnaryExpr | MultiplicativeExpr "mod" UnaryExpr;
            var expr = this.parseUnaryExpr();
            var token = this.lexer.Token;
            if (token.Value == "mul" || token.Value == "div" || token.Value == "mod")
            {
                this.lexer.NextToken();
                var right = this.parseMultiplicativeExpr();
                if (right == null)
                    throw SyntaxError("Expected: Expr.");
                expr = this.builder.BuildSimpleBinary(expr, right, token.Value);
            }
            return expr;
        }
        private Expression parseUnaryExpr()
        {
            // bnf: UnaryExpr          : PrimaryExpr | "-" PrimaryExpr | "not" PrimaryExpr;
            var expr = this.parsePrimaryExpr();
            if (expr != null)
                return expr;
            var token = this.lexer.Token;
            var c = this.lexer.Peek();
            if (token.Value == Strings.MINUS && !Char.IsNumber(c))
            {
                this.lexer.NextToken();
                expr = this.parseUnaryExpr();
                if (expr == null)
                    throw SyntaxError("Expected: Expr.");
                return this.builder.BuildUnary(expr, "minus");
            }
            if (token.Value == Strings.PLUS && !Char.IsNumber(c))
            {
                this.lexer.NextToken();
                expr = this.parseUnaryExpr();
                if (expr == null)
                    throw SyntaxError("Expected: Expr.");
                return this.builder.BuildUnary(expr, "plus");
            }
            if (token.Value == "not")
            {
                this.lexer.NextToken();
                expr = this.parseUnaryExpr();
                if (expr == null)
                    throw SyntaxError("Expected: Expr.");
                return this.builder.BuildUnary(expr, "not");
            }
            return null;
        }
        private Expression parsePrimaryExpr()
        {
            // bnf: PrimaryExpr        : NullLiteral | ParenExpr | LiteralExpr | FunctionCall | MemberPath
            Expression expr;
            if ((expr = this.parseNULLLiteral()) != null) return expr;
            if ((expr = this.parseParenExpr()) != null) return expr;
            if ((expr = this.parseLiteralExpr()) != null) return expr;
            if ((expr = this.parseFunctionCall()) != null) return expr;
            if ((expr = this.parseMemberPath()) != null) return expr;
            return null;
        }
        private Expression parseNULLLiteral()
        {
            // bnf: NullLiteral        : NULL | null
            var v = this.lexer.Token.Value;
            if (v == "NULL" || v == "null")
            {
                this.lexer.NextToken();
                return this.builder.BuildConstant(null);
            }
            return null;
        }
        private Expression parseParenExpr()
        {
            // bnf: ParenExpr          : "(" Expr ")"
            if (this.lexer.Token.Value != Strings.LPAREN)
                return null;
            this.lexer.NextToken();
            var expr = this.parseExpr();
            if (this.lexer.Token.Value != Strings.RPAREN)
                throw SyntaxError("Expected ')'.");
            this.lexer.NextToken();
            return expr;
        }
        private Expression parseLiteralExpr()
        {
            // bnf: LiteralExpr        : PointLiteral | DatetimeLiteral | StringLiteral | BoolLiteral | NumberLiteral
            Expression expr;
            if ((expr = this.parsePointLiteral()) != null) return expr;
            if ((expr = this.parseDatetimeLiteral()) != null) return expr;
            if ((expr = this.parseStringLiteral()) != null) return expr;
            if ((expr = this.parseBoolLiteral()) != null) return expr;
            if ((expr = this.parseNumberLiteral()) != null) return expr;
            return null;
        }
        private Expression parsePointLiteral()
        {
            // bnf: PointLiteral       : "POINT" "(" [ sign ] DIGITS [ "." DIGITS ] WSP  [ sign ] DIGITS [ "." DIGITS ] [ ","  [ sign ] DIGITS [ "." DIGITS ] ] ")"
            // example: POINT(12.3 45)
            //          POINT(12.3 -4.56)
            //          POINT(12.3 -4.56 789)
            Expression n1 = null;
            Expression n2 = null;
            Expression n3 = null;
            if (lexer.Token.Value != "POINT")
                return null;
            this.lexer.NextToken();
            var token = this.lexer.Token;
            if (token.Value != "(")
                throw SyntaxError("Invalid POINT format. Expected: '('");
            this.lexer.NextToken();
            n1 = parseNumberLiteral();
            n2 = parseNumberLiteral();
            if (this.lexer.Token.Value != ")")
                n3 = parseNumberLiteral();
            if (this.lexer.Token.Value != ")")
                throw SyntaxError("Invalid POINT format. Expected: ')'");
            this.lexer.NextToken();
            return this.builder.BuildPointConstant(n1, n2, n3);
            throw new Exception("##");
        }
        private Expression parseDatetimeLiteral()
        {
            // bnf: DatetimeLiteral    : "datetime" "'" DIGITS "-" DIGITS "-" DIGITS [ "T" DIGITS ":" DIGITS ":" DIGITS [ "." DIGITS ] [ ( SIGN ) DIGITS ":" DIGTS ] [ "Z" ]
            // example: datetime'2010-07-15'
            //         datetime'2010-07-15T16:19:54Z'.
            //         datetime'2011-06-07T13:18:25.0348565-07:00'
            if (this.lexer.Token.Value != "datetime")
                return null;
            this.lexer.NextToken();
            var token = this.lexer.Token;
            if (token.TokenType != TokenType.String)
                throw SyntaxError("Invalid date format.");
            DateTime d;
            try
            {
                d = DateTime.Parse(token.Value);
            }
            catch (Exception e)
            {
                throw SyntaxError("Invalid date format.", e);
            }
            this.lexer.NextToken();
            return this.builder.BuildConstant(d);
        }
        private Expression parseStringLiteral()
        {
            // bnf: StringLiteral      : STRING
            if (this.lexer.Token.TokenType != TokenType.String)
                return null;
            var v = this.lexer.Token.Value;
            this.lexer.NextToken();
            return this.builder.BuildConstant(v);
        }
        private Expression parseBoolLiteral()
        {
            // bnf: BoolLiteral        : "true" | "false" | "0" | "1"
            var v = this.lexer.Token.Value;
            if (v == "true"/*||v=="1"*/)
            {
                this.lexer.NextToken();
                return this.builder.BuildConstant(true);
            }
            if (v == "false"/*||v=="0"*/)
            {
                this.lexer.NextToken();
                return this.builder.BuildConstant(false);
            }
            return null;
        }
        private Expression parseNumberLiteral()
        {
            // bnf: NumberLiteral      : [ Sign ] 1*DIGIT [ "." 1*DIGIT ] [ "E" [ Sign ] 1*DIGIT ] [ "M" | "m" ] |  // double
            // bnf:                      [ Sign ] 1*DIGIT [ "." 1*DIGIT ] [ "f" ]                                |  // single
            // bnf:                      [ Sign ] 1*DIGIT [ "L" ]                                                   // long
            string sign1, sign2, digits1, digits2, digits3;
            var v = "";

            var token = this.lexer.Token;
            if (token.TokenType != TokenType.Digits && token.Value != "-" && token.Value != "+")
                return null;
            if (token.Value == "-" || token.Value == "+")
            {
                var c = lexer.Peek();
                if (!Char.IsNumber(c))
                    return null;

                sign1 = this.parseSign();
                v += sign1;
            }

            digits1 = this.lexer.Token.Value;
            v += digits1;
            var isInteger = true;
            this.lexer.NextToken();
            token = this.lexer.Token;
            if (token.Value == "L")
            {
                this.lexer.NextToken();
                return this.builder.BuildConstant(Int64.Parse(v));
            }
            if (token.Value == Strings.DOT)
            {
                isInteger = false;
                this.lexer.NextToken();
                token = this.lexer.Token;
                if (token.TokenType != TokenType.Digits)
                    throw SyntaxError("Expected DIGITS (after '.').");
                digits2 = token.Value;
                v += "." + digits2;
                this.lexer.NextToken();
                token = this.lexer.Token;
            }
            if (token.Value == "f")
            {
                this.lexer.NextToken();
                return this.builder.BuildConstant(Single.Parse(v, System.Globalization.CultureInfo.InvariantCulture));
            }
            if (token.Value == "e")
            {
                isInteger = false;
                this.lexer.NextToken();
                sign2 = this.parseSign();
                if (sign2 == null)
                    sign2 = "";
                token = this.lexer.Token;
                if (token.TokenType != TokenType.Digits)
                    throw SyntaxError("Expected DIGITS (after Sign).");
                digits3 = token.Value;
                v += "e" + sign2 + digits3;
                this.lexer.NextToken();
                token = this.lexer.Token;
            }
            if (token.Value == "m" || token.Value == "M")
            {
                this.lexer.NextToken();
                return this.builder.BuildConstant(Decimal.Parse(v, System.Globalization.CultureInfo.InvariantCulture));
            }
            if (token.Value == "d" || token.Value == "D")
            {
                this.lexer.NextToken();
                return this.builder.BuildConstant(Double.Parse(v, System.Globalization.CultureInfo.InvariantCulture));
            }
            if (isInteger)
            {
                var n = Int32.Parse(v);
                return this.builder.BuildConstant(n);
            }
            var d = double.Parse(v, System.Globalization.CultureInfo.InvariantCulture);
            return this.builder.BuildConstant(d);
        }
        private string parseSign()
        { // returns "+", "-" or null
            // bnf: Sign          : "+" | "-"
            var token = this.lexer.Token;
            if (token.Value != Strings.PLUS && token.Value != Strings.MINUS)
                return null;
            this.lexer.NextToken();
            return token.Value;
        }
        private Expression parseFunctionCall()
        {
            // bnf: FunctionCall       : FunctionName "(" [ Arguments ] ")";
            var fnName = this.parseFunctionName();
            if (fnName == null)
                return null;
            var token = this.lexer.Token;
            if (token.Value != Strings.LPAREN)
                throw SyntaxError("Expected '('.");
            this.lexer.NextToken();
            token = this.lexer.Token;
            var args = new Expression[0];
            if (token.Value != Strings.RPAREN)
            {
                args = this.parseArguments();
                token = this.lexer.Token;
            }
            if (token.Value != Strings.RPAREN)
                throw SyntaxError("Expected ')'.");
            this.lexer.NextToken();

            if (fnName == "isof")
            {
                var newArgs = new List<Expression>();
                newArgs.Add(this.builder.x);
                newArgs.AddRange(args);
                args = newArgs.ToArray();
            }

            return this.builder.BuildGlobalCall(fnName, args);
        }
        private string parseFunctionName()
        {
            // bnf: FunctionName       : "startswith" | "endswith" | ...;
            var token = this.lexer.Token;
            if (token.TokenType != TokenType.Word)
                return null;
            var name = token.Value;
            var lowerName = name.ToLower();
            var i = functionNames.IndexOf(lowerName);
            if (i < 0)
                return null;
            this.lexer.NextToken();
            return lowerName;
        }
        private Expression[] parseArguments()
        {
            // bnf: Arguments          : Argument | Arguments "," Argument;
            var args = new List<Expression>();
            while (true)
            {
                var expr = this.parseArgument();
                if (expr == null)
                    throw SyntaxError("Expected: expression");
                args.Add(expr);
                if (this.lexer.Token.Value != Strings.COMMA)
                    break;
                this.lexer.NextToken();
            }
            return args.ToArray();
        }
        private Expression parseArgument()
        {
            // bnf: Argument           : Expr;
            return this.parseExpr();
        }
        private Expression parseMemberPath()
        {
            // bnf: MemberPath         : [ Namespace "/" ] *(NavigationProperty "/") Field
            // bnf: Namespace          : NAME *("." NAME)
            // bnf: NavigationProperty : NAME
            // bnf: Field              : NAME
            // short: MemberPath         : [ Name *("." Name) "/" ] *(Name "/") Name
            if (this.lexer.Token.Value == "not")
                return null;
            var name = this.parseName();
            var token = this.lexer.Token;
            if (name == null)
                return null;

            var member = name;

            var hasDot = false;
            while (token.Value == Strings.DOT)
            {
                member += ".";
                hasDot = true;
                this.lexer.NextToken();
                name = this.parseName();
                token = this.lexer.Token;
                if (name == null)
                    throw SyntaxError("Expected: name after dot ('.').");
                member += name;
            }
            var steps = new List<string>();
            steps.Add(member);

            var hasSlash = false;
            while (token.Value == Strings.SLASH)
            {
                member += "/";
                hasSlash = true;
                this.lexer.NextToken();
                name = this.parseName();
                token = this.lexer.Token;
                if (name == null)
                    throw SyntaxError("Expected: name.");
                member += name;
                steps.Add(name);
            }
            if (hasDot && !hasSlash)
                throw SyntaxError("Expected: / after namespace.");
            if (this.lexer.Token.Value != Strings.LPAREN)
                return this.builder.BuildMemberPath(steps);
            // parse instance method
            this.lexer.NextToken();
            if (this.lexer.Token.Value != Strings.RPAREN)
                throw SyntaxError("Expected: right parenthesis: ')'.");
            this.lexer.NextToken();
            return this.builder.BuildMemberPath(steps);
        }
        private string parseName()
        {
            // bnf: Name               : (WORD | UNDERSCORE) *(WORD | UNDERSCORE | DIGIT)
            // this and getNextNamePart methods are not a clean parser function
            var token = this.lexer.Token;
            if (token.TokenType != TokenType.Word && token.Value != Strings.UNDERSCORE)
                return null;
            var name = token.ToString() + this.getNextNamePart();
            this.lexer.NextToken();
            return name;
        }
        private string getNextNamePart()
        {
            var part = "";
            var c = this.lexer.CurrentChar;
            var ct = this.lexer.CurrentCharType;
            while (ct == CharType.Digit || ct == CharType.Alpha || c == Chars.UNDERSCORE)
            {
                part += c;
                this.lexer.NextChar();
                c = this.lexer.CurrentChar;
                ct = this.lexer.CurrentCharType;
            }
            return part;
        }

        // =====================================================================================================================

        private Exception SyntaxError(string reason, Exception e = null)
        {
            return SyntaxError(this.lexer, reason, e);
        }
        internal static Exception SyntaxError(ODataLexer lexer, string reason, Exception e = null)
        {
            var src = lexer.Src;
            var token = lexer.Token;

            var lineInfo = token.Column >= src.Length
                ? " Source:" + src + ">>>>EOF<<<<"
                : " Source:" + src.Substring(0, token.Column) + ">>>>" + src.Substring(token.Column, 1) + "<<<<" + src.Substring(token.Column + 1);

            var msg = String.Concat("Syntax error at line ", token.Line, ", char ", token.Column, ". ", reason, lineInfo);

            return new ODataParserException(msg, token.Line, token.Column, e);
        }

    }
}
