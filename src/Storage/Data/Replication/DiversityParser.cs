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
        if (parsed == null)
            throw new DiversityParserException($"Cannot parse this \"{_fieldDataType}\" expression of the \"{_fieldName}\" field: \"{_diversitySource}\"");
        AssertDataType(parsed);
        return parsed;
    }
    private void AssertDataType(IDiversity parsed)
    {
        if (_fieldDataType == parsed.DataType)
            return;
        if ((_fieldDataType == DataType.String || _fieldDataType == DataType.Text) &&
            (parsed.DataType == DataType.String || parsed.DataType == DataType.Text))
            return;
        throw new DiversityParserException($"Cannot use \"{parsed.DataType}\" for the \"{_fieldName}\" field because it's data type is \"{_fieldDataType}\".");
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
            throw new DiversityParserException($"Invalid maximum value in the integer range of the \"{_fieldName}\" field: \"{_diversitySource}\".");
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
            throw new DiversityParserException($"Invalid step value in the integer range of the \"{_fieldName}\" field: \"{_diversitySource}\".");

        var step = _lexer.IntegerValue;
        _lexer.NextToken();
        return step;
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
            throw new DiversityParserException($"Invalid maximum value in the date-time range of the \"{_fieldName}\" field: \"{_diversitySource}\".");
        var maxValue = _lexer.DateTimeValue;

        _lexer.NextToken();
        return new DateTimeDiversity { Type = _currentDiversityRangeType, Sequence = new DateTimeSequence { MinValue = minValue, MaxValue = maxValue } };
    }
    //<DateTimeStepExpr>			::= "STEP" <TimespanConstant>
    private TimeSpan? ParseDateTimeStepExpr()
    {
        if (_lexer.CurrentToken != DiversityLexer.Token.Step)
            return null;

        _lexer.NextToken();
        if (_lexer.CurrentToken != DiversityLexer.Token.Timespan)
            throw new DiversityParserException($"Invalid step value in the DateTime range of the \"{_fieldName}\" field: \"{_diversitySource}\".");

        var step = _lexer.TimeSpanValue;
        _lexer.NextToken();
        return step;
    }

    //<StringExpression>		::= <StringConstant>* | <Pattern> "," <IntegerRange>
    //<Pattern>					::= <StringConstant> "*" <StringConstant>
    private IDiversity? ParseStringExpression()
    {
        if (_lexer.CurrentToken != DiversityLexer.Token.Pattern)
            return ParseStringConstant();
        var pattern = _lexer.StringValue;
        _lexer.NextToken();
        var range = ParseBuiltInTopLevelExpression();
        if (range is not IntDiversity intRange || intRange.Type == DiversityType.Constant)
            throw new DiversityParserException(
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

    //<StringConstant>			::= any char except ',' '*' ':' | empty
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
