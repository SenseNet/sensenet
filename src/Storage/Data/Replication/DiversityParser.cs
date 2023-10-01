using System;
using System.Collections.Generic;
using SenseNet.ContentRepository.Storage.Schema;

// ReSharper disable once CheckNamespace
namespace SenseNet.ContentRepository.Storage.Data.Replication;

public class DiversityParser
{
    private readonly string _diversitySource;
    private readonly string _fieldName;
    private readonly DataType _fieldDataType;
    private readonly DiversityLexer _lexer;

    public DiversityParser(string fieldName, DataType fieldDataType, string diversitySource)
    {
        _fieldName = fieldName;
        _fieldDataType = fieldDataType;
        _diversitySource = diversitySource;
        _lexer = new DiversityLexer(fieldName, diversitySource);
    }

    public IDiversity Parse()
    {
        _lexer.NextToken();
        return ParseTopLevelExpression();
    }

    //<TopLevelExpression>		    ::= <AdditionalExpression> | <BuiltInTopLevelExpression>
    private IDiversity ParseTopLevelExpression()
    {
        var parsed = ParseAdditionalExpression() ?? ParseBuiltInTopLevelExpression();
        if (parsed != null)
            return parsed;
        throw new ReplicationParserException($"Cannot parse this \"{_fieldDataType}\" expression of the \"{_fieldName}\" field: \"{_diversitySource}\"");
    }

    //<AdditionalExpression>		::= <GeneratorKeyword> ":" | <AdditionalParameters>*
    private IDiversity? ParseAdditionalExpression()
    {
        return null; //UNDONE:xxxxReplication: ParseAdditionalExpression is not implemented
    }

    //<BuiltInTopLevelExpression>   ::= <BuiltInGeneratorKeyword>? <BuiltInExpression>
    private DiversityType _currentDiversityRangeType = DiversityType.Sequence;
    private IDiversity? ParseBuiltInTopLevelExpression()
    {
        _currentDiversityRangeType = ParseBuiltInGeneratorKeyword();
        return ParseBuiltInExpression();
    }
    //<BuiltInGeneratorKeyword>	::= "RANDOM" | "SEQUENCE"
    private DiversityType ParseBuiltInGeneratorKeyword()
    {
        if (_lexer.CurrentToken != DiversityLexer.Token.BuiltInGeneratorKeyword)
            return DiversityType.Sequence;
        var type = _lexer.StringValue == "RANDOM" ? DiversityType.Random : DiversityType.Sequence;
        _lexer.NextToken();
        return type;
    }
    //<BuiltInExpression>			::= <IntExpression> | <DateTimeExpression> | <StringExpression>
    private IDiversity? ParseBuiltInExpression()
    {
        return ParseIntExpression() ?? ParseDateTimeExpression() ?? ParseStringExpression();
    }

    //<IntExpression>				::= <IntConstant> | <IntegerRange>
    private IDiversity? ParseIntExpression()
    {
        if (_lexer.CurrentToken != DiversityLexer.Token.Integer)
            return null;
        var minValue = _lexer.IntegerValue;
        _lexer.NextToken();
        return ParseIntegerRange(minValue) ??
               new IntDiversity { Type = DiversityType.Constant, MinValue = minValue };
    }
    //<IntegerRange>			::= <SimpleIntRange> | <SimpleIntRange> <IntStepExpr>
    private IDiversity? ParseIntegerRange(int minValue)
    {
        var intDiversity = ParseSimpleIntRange(minValue);
        if (intDiversity == null)
            return null;

        var step = ParseIntStepExpr();
        if (step != null)
            intDiversity.Step = step.Value;

        return intDiversity;
    }
    //<SimpleIntRange>			::= <IntConstant> "TO" <IntConstant> 
    private IntDiversity? ParseSimpleIntRange(int minValue)
    {
        if (_lexer.CurrentToken != DiversityLexer.Token.To)
            return null;

        _lexer.NextToken();
        if (_lexer.CurrentToken != DiversityLexer.Token.Integer)
            throw new ReplicationParserException($"Invalid maximum value in the integer range of the \"{_fieldName}\" field: \"{_diversitySource}\".");
        var maxValue = _lexer.IntegerValue;

        _lexer.NextToken();
        return new IntDiversity { Type = _currentDiversityRangeType, MinValue = minValue, MaxValue = maxValue };
    }
    //<IntStepExpr>				::= "STEP" <IntConstant>
    private int? ParseIntStepExpr()
    {
        if (_lexer.CurrentToken != DiversityLexer.Token.Step)
            return null;

        _lexer.NextToken();
        if (_lexer.CurrentToken != DiversityLexer.Token.Integer)
            throw new ReplicationParserException($"Invalid step value in the integer range of the \"{_fieldName}\" field: \"{_diversitySource}\".");

        _lexer.NextToken();
        return _lexer.IntegerValue;
    }


    //<DateTimeExpression>		    ::= <DateTimeConstant> | <DateTimeRange>
    private IDiversity? ParseDateTimeExpression()
    {
        if (_lexer.CurrentToken != DiversityLexer.Token.DateTime)
            return null;
        var minValue = _lexer.DateTimeValue;
        _lexer.NextToken();
        return ParseDateTimeRange(minValue)
               ?? new DateTimeDiversity
               { Type = DiversityType.Constant, Sequence = new DateTimeSequence { MinValue = minValue } };
    }

    //<DateTimeRange>				::= <SimpleDateTimeRange> | <SimpleDateTimeRange> <DateTimeStepExpr>
    private IDiversity? ParseDateTimeRange(DateTime minValue)
    {
        var dateTimeDiversity = ParseSimpleDateTimeRange(minValue);
        if (dateTimeDiversity == null)
            return null;

        var step = ParseDateTimeStepExpr();
        if (step != null)
            dateTimeDiversity.Sequence.Step = step.Value;

        return dateTimeDiversity;
    }
    //<SimpleDateTimeRange>		    ::= <DateTimeConstant> "TO" <DateTimeConstant> 
    private DateTimeDiversity? ParseSimpleDateTimeRange(DateTime minValue)
    {
        if (_lexer.CurrentToken != DiversityLexer.Token.To)
            return null;

        _lexer.NextToken();
        if (_lexer.CurrentToken != DiversityLexer.Token.DateTime)
            throw new ReplicationParserException($"Invalid maximum value in the date-time range of the \"{_fieldName}\" field: \"{_diversitySource}\".");
        var maxValue = _lexer.DateTimeValue;

        _lexer.NextToken();
        return new DateTimeDiversity { Type = _currentDiversityRangeType, Sequence = new DateTimeSequence { MinValue = minValue, MaxValue = maxValue, Step = TimeSpan.Zero } };
    }
    //<DateTimeStepExpr>			::= "STEP" <TimespanConstant>
    private TimeSpan? ParseDateTimeStepExpr()
    {
        if (_lexer.CurrentToken != DiversityLexer.Token.Step)
            return null;

        _lexer.NextToken();
        if (_lexer.CurrentToken != DiversityLexer.Token.Timespan)
            throw new ReplicationParserException($"Invalid step value in the DateTime range of the \"{_fieldName}\" field: \"{_diversitySource}\".");

        _lexer.NextToken();
        return _lexer.TimeSpanValue;
    }

    //<StringExpression>		::= <StringConstant>* | <Pattern> "," <IntegerRange>
    //<Pattern>					::= <StringConstant> "*" <StringConstant>
    //<StringConstant>			::= any char except ',' '*' ':' | empty
    private IDiversity? ParseStringExpression()
    {
        if (_lexer.CurrentToken != DiversityLexer.Token.Pattern)
            return ParseStringConstant();
        var pattern = _lexer.StringValue;
        _lexer.NextToken();
        var range = ParseBuiltInTopLevelExpression();
        if (range is not IntDiversity intRange || intRange.Type == DiversityType.Constant)
            throw new ReplicationParserException(
                $"Invalid integer range in the string expression of the \"{_fieldName}\" field: \"{_diversitySource}\".");
        return new StringDiversity
        {
            Pattern = pattern,
            Type = intRange.Type,
            Sequence = new Sequence
            {
                MinValue = intRange.MinValue,
                MaxValue = intRange.MaxValue,
                Step = intRange.Step
            }
        };
    }

    private IDiversity? ParseStringConstant()
    {
        if (_lexer.CurrentToken != DiversityLexer.Token.String)
            return null;

        var words = new List<string>();
        while (_lexer.CurrentToken != DiversityLexer.Token.Eof)
        {
            words.Add(_lexer.GetCurrentWord() ?? string.Empty);
            _lexer.NextToken();
        }

        return new StringDiversity { Pattern = string.Join(" ", words), Type = DiversityType.Constant };
    }
}
//<TopLevelExpression>		::= <AdditionalExpression> | <BuiltInTopLevelExpression>
//<AdditionalExpression>		::= <GeneratorKeyword> ":" | <AdditionalParameters>*
//<BuiltInTopLevelExpression> ::= <BuiltInGeneratorKeyword>? <BuiltInExpression>
//<BuiltInExpression>			::= <IntExpression> | <DateTimeExpression> | <StringExpression>

//<IntExpression>				::= <IntConstant> | <IntegerRange>
//<IntegerRange>				::= <SimpleIntRange> | <SimpleIntRange> <IntStepExpr>
//<SimpleIntRange>			::= <IntConstant> "TO" <IntConstant> 
//<IntStepExpr>				::= "STEP" <IntConstant>
//<IntConstant>				::= ('0'..'9')*

//<DateTimeExpression>		::= <DateTimeConstant> | <DateTimeRange>
//<DateTimeRange>				::= <SimpleDateTimeRange> | <SimpleDateTimeRange> <DateTimeStepExpr>
//<SimpleDateTimeRange>		::= <DateTimeConstant> "TO" <DateTimeConstant> 
//<DateTimeStepExpr>			::= "STEP" <TimespanConstant>
//<DateTimeConstant>			::= "yyyy-MM-dd" | "yyyy-MM-dd HH:mm:ss"
//<TimespanConstant>			::= "days.HH:mm:ss"

//<StringExpression>		::= <StringConstant>* | <Pattern> "," <IntegerRange>
//<Pattern>					::= <StringConstant> "*" <StringConstant>
//<StringConstant>			::= any char except ',' '*' ':' | empty

//<GeneratorKeyword>			::=	[a-zA-Z0-9_] ; well known word in a list except: <builtInGeneratorKeyword>
//<BuiltInGeneratorKeyword>	::= "RANDOM" | "SEQUENCE"

/*
Request:
POST https://example.com/odata.svc/root/test('mySource')/Replicate
body:
{
	"target": 4564,
	"options": {
		MaxCount = 1000000,
		MaxItemsPerFolder = 40,
		MaxFoldersPerFolder = 100,
		FirstFolderIndex = 1,
		Fields = {
			"Name": "Event-*, 1 TO 9999999999,",
			"Mérőóra": "mérőóra: 12 length 3 decimal"
			"DisplayName": "DICIONARY: 'Lorem', WORDS 10 TO 900",
		}
	}
}
*/
