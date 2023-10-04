using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Data.Replication;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.Services.Core.Operations;
using SenseNet.Tests.Core;

namespace SenseNet.ContentRepository.Tests;

[TestClass]
public class ReplicationTests : TestBase
{
    [TestMethod]
    public void Replication_Lexer_ReservedWords()
    {
        var lexer = new DiversityLexer("Field1", "To Step");
        lexer.NextToken();
        Assert.AreEqual(DiversityLexer.Token.To, lexer.CurrentToken);
        Assert.AreEqual("TO", lexer.StringValue);
        lexer.NextToken();
        Assert.AreEqual(DiversityLexer.Token.Step, lexer.CurrentToken);
        Assert.AreEqual("STEP", lexer.StringValue);
        Assert.IsFalse(lexer.NextToken());
    }
    [TestMethod]
    public void Replication_Lexer_GeneratorKeywords()
    {
        var lexer = new DiversityLexer("Field1", "sequence: Random: Custom_Keyword_5:");
        lexer.NextToken();
        Assert.AreEqual(DiversityLexer.Token.BuiltInGeneratorKeyword, lexer.CurrentToken);
        Assert.AreEqual("SEQUENCE", lexer.StringValue);
        lexer.NextToken();
        Assert.AreEqual(DiversityLexer.Token.BuiltInGeneratorKeyword, lexer.CurrentToken);
        Assert.AreEqual("RANDOM", lexer.StringValue);
        lexer.NextToken();
        Assert.AreEqual(DiversityLexer.Token.GeneratorKeyword, lexer.CurrentToken);
        Assert.AreEqual("Custom_Keyword_5", lexer.StringValue);
        Assert.IsFalse(lexer.NextToken());
    }
    [TestMethod]
    public void Replication_Lexer_Integer()
    {
        var lexer = new DiversityLexer("Field1", "42");
        lexer.NextToken();
        Assert.AreEqual(DiversityLexer.Token.Integer, lexer.CurrentToken);
        Assert.AreEqual(42, lexer.IntegerValue);
        Assert.IsFalse(lexer.NextToken());
    }
    [TestMethod]
    public void Replication_Lexer_Timespan()
    {
        var lexer = new DiversityLexer("Field1", "0.00:10:00");
        lexer.NextToken();
        Assert.AreEqual(DiversityLexer.Token.Timespan, lexer.CurrentToken);
        Assert.AreEqual(TimeSpan.FromMinutes(10.0), lexer.TimeSpanValue);
        Assert.IsFalse(lexer.NextToken());
    }
    [TestMethod]
    public void Replication_Lexer_DateTime()
    {
        //                               int  <--date-->   int    <--datetime-------->    int
        var lexer = new DiversityLexer("Field1", "000  2023-09-28   111    2023-09-28   03:44:19   222");
        lexer.NextToken(); // 000
        lexer.NextToken();
        Assert.AreEqual(DiversityLexer.Token.DateTime, lexer.CurrentToken);
        Assert.AreEqual(new DateTime(2023, 09, 28), lexer.DateTimeValue);
        lexer.NextToken(); // 111
        lexer.NextToken();
        Assert.AreEqual(DiversityLexer.Token.DateTime, lexer.CurrentToken);
        Assert.AreEqual(new DateTime(2023, 09, 28, 3, 44, 19), lexer.DateTimeValue);
        lexer.NextToken(); // 222
        Assert.IsFalse(lexer.NextToken());
    }
    [TestMethod]
    public void Replication_Lexer_Pattern1()
    {
        //                               <------pattern------>     <--datetime-------->    int
        var lexer = new DiversityLexer("Field1", "000  2023-09-28   111,    2023-09-28   03:44:19   222");
        lexer.NextToken();
        Assert.AreEqual(DiversityLexer.Token.Pattern, lexer.CurrentToken);
        Assert.AreEqual("000 2023-09-28 111", lexer.StringValue);
        lexer.NextToken();
        Assert.AreEqual(DiversityLexer.Token.DateTime, lexer.CurrentToken);
        Assert.AreEqual(new DateTime(2023, 09, 28, 3, 44, 19), lexer.DateTimeValue);
        lexer.NextToken(); // 222
        Assert.IsFalse(lexer.NextToken());
    }
    [TestMethod]
    public void Replication_Lexer_Pattern2()
    {
        //                                        <------pattern------>  int
        var lexer = new DiversityLexer("Field1", "Keyword: 000  2023-09-28   111, 222");
        lexer.NextToken();
        Assert.AreEqual(DiversityLexer.Token.GeneratorKeyword, lexer.CurrentToken);
        Assert.AreEqual("Keyword", lexer.StringValue);
        lexer.NextToken();
        Assert.AreEqual(DiversityLexer.Token.Pattern, lexer.CurrentToken);
        Assert.AreEqual("000 2023-09-28 111", lexer.StringValue);
        lexer.NextToken();
        Assert.AreEqual(DiversityLexer.Token.Integer, lexer.CurrentToken);
        Assert.AreEqual(222, lexer.IntegerValue);
        Assert.IsFalse(lexer.NextToken());
    }
    [TestMethod]
    public void Replication_Lexer_String()
    {
        var lexer = new DiversityLexer("Field1", "S000  222 S01");
        lexer.NextToken();
        Assert.AreEqual(DiversityLexer.Token.String, lexer.CurrentToken);
        Assert.AreEqual("S000", lexer.StringValue);
        lexer.NextToken();
        Assert.AreEqual(DiversityLexer.Token.Integer, lexer.CurrentToken);
        Assert.AreEqual(222, lexer.IntegerValue);
        lexer.NextToken();
        Assert.AreEqual(DiversityLexer.Token.String, lexer.CurrentToken);
        Assert.AreEqual("S01", lexer.StringValue);
        lexer.NextToken();
        Assert.IsFalse(lexer.NextToken());
    }
    [TestMethod]
    public void Replication_Lexer_Error_AdditionalGeneratorKeyword()
    {
        var lexer = new DiversityLexer("Field1", "Wrong!Keyword:");
        try
        {
            lexer.NextToken();
            Assert.Fail("ReplicationParserException was not thrown.");
        }
        catch (DiversityParserException e)
        {
            Assert.AreEqual("Invalid keyword in the \"Field1\" field: \"Wrong!Keyword:\". " +
                            "The keyword can only contain letter, digit or '_' characters.", e.Message);
        }
    }

    /* ========================================================================================= INTEGER PARSER */

    [TestMethod]
    public void Replication_Parser_IntConstant()
    {
        var parser = new DiversityParser("Index", DataType.Int, "42");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as IntDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Constant, diversity.Type);
        Assert.AreEqual(42, diversity.MinValue);
    }
    [TestMethod]
    public void Replication_Parser_IntRange()
    {
        var parser = new DiversityParser("Index", DataType.Int, "42 TO 52");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as IntDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Sequence, diversity.Type);
        Assert.AreEqual(42, diversity.MinValue);
        Assert.AreEqual(52, diversity.MaxValue);
        Assert.AreEqual(1, diversity.Step);
    }
    [TestMethod]
    public void Replication_Parser_IntRangeWithStep()
    {
        var parser = new DiversityParser("Index", DataType.Int, "42 TO 532 STEP 7");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as IntDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Sequence, diversity.Type);
        Assert.AreEqual(42, diversity.MinValue);
        Assert.AreEqual(532, diversity.MaxValue);
        Assert.AreEqual(7, diversity.Step);
    }
    [TestMethod]
    public void Replication_Parser_IntSequence()
    {
        var parser = new DiversityParser("Index", DataType.Int, "Sequence: 42 TO 52");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as IntDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Sequence, diversity.Type);
        Assert.AreEqual(42, diversity.MinValue);
        Assert.AreEqual(52, diversity.MaxValue);
        Assert.AreEqual(1, diversity.Step);
    }
    [TestMethod]
    public void Replication_Parser_IntRandom()
    {
        var parser = new DiversityParser("Index", DataType.Int, "Random: 42 TO 52");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as IntDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Random, diversity.Type);
        Assert.AreEqual(42, diversity.MinValue);
        Assert.AreEqual(52, diversity.MaxValue);
        Assert.AreEqual(1, diversity.Step);
    }
    [TestMethod]
    public void Replication_Parser_Error_IntRange()
    {
        var parser = new DiversityParser("Index", DataType.Int, "42 TO FiftyFour");

        // ACT
        try
        {
            parser.Parse();
            Assert.Fail("ReplicationParserException was not thrown.");
        }
        catch (DiversityParserException e)
        {
            Assert.AreEqual("Invalid maximum value in the integer range of the \"Index\" field: \"42 TO FiftyFour\".", e.Message);
        }
    }
    [TestMethod]
    public void Replication_Parser_Error_IntStep()
    {
        var parser = new DiversityParser("Index", DataType.Int, "42 TO 508 STEP three");

        try
        {
            // ACT
            parser.Parse();
            Assert.Fail("ReplicationParserException was not thrown.");
        }
        catch (DiversityParserException e)
        {
            Assert.AreEqual("Invalid step value in the integer range of the \"Index\" field: \"42 TO 508 STEP three\".", e.Message);
        }
    }

    /* ========================================================================================= DATETIME PARSER */

    [TestMethod]
    public void Replication_Parser_DateTimeConstant1()
    {
        var parser = new DiversityParser("CreationDate", DataType.DateTime, "2023-09-28");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as DateTimeDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Constant, diversity.Type);
        Assert.AreEqual(new DateTime(2023, 09, 28), diversity.Sequence.MinValue);
    }
    [TestMethod]
    public void Replication_Parser_DateTimeConstant2()
    {
        var parser = new DiversityParser("CreationDate", DataType.DateTime, "2023-09-28 18:20:32");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as DateTimeDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Constant, diversity.Type);
        Assert.AreEqual(new DateTime(2023, 09, 28, 18, 20, 32), diversity.Sequence.MinValue);
    }
    [TestMethod]
    public void Replication_Parser_DateTimeRange1()
    {
        var parser = new DiversityParser("CreationDate", DataType.DateTime, "2022-09-28 TO 2023-09-28");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as DateTimeDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Sequence, diversity.Type);
        Assert.AreEqual(new DateTime(2022, 09, 28), diversity.Sequence.MinValue);
        Assert.AreEqual(new DateTime(2023, 09, 28), diversity.Sequence.MaxValue);
        Assert.AreEqual(TimeSpan.FromSeconds(1), diversity.Sequence.Step);
    }
    [TestMethod]
    public void Replication_Parser_DateTimeRange2()
    {
        var parser = new DiversityParser("CreationDate", DataType.DateTime, "2022-09-28 18:20:32 TO 2023-09-28");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as DateTimeDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Sequence, diversity.Type);
        Assert.AreEqual(new DateTime(2022, 09, 28, 18, 20, 32), diversity.Sequence.MinValue);
        Assert.AreEqual(new DateTime(2023, 09, 28), diversity.Sequence.MaxValue);
        Assert.AreEqual(TimeSpan.FromSeconds(1), diversity.Sequence.Step);
    }
    [TestMethod]
    public void Replication_Parser_DateTimeRange3()
    {
        var parser = new DiversityParser("CreationDate", DataType.DateTime, "2022-09-28 TO 2023-09-28 18:20:32");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as DateTimeDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Sequence, diversity.Type);
        Assert.AreEqual(new DateTime(2022, 09, 28), diversity.Sequence.MinValue);
        Assert.AreEqual(new DateTime(2023, 09, 28, 18, 20, 32), diversity.Sequence.MaxValue);
        Assert.AreEqual(TimeSpan.FromSeconds(1), diversity.Sequence.Step);
    }
    [TestMethod]
    public void Replication_Parser_DateTimeRange4()
    {
        var parser = new DiversityParser("CreationDate", DataType.DateTime, "2022-09-28 18:20:32 TO 2023-09-28 18:20:32");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as DateTimeDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Sequence, diversity.Type);
        Assert.AreEqual(new DateTime(2022, 09, 28, 18, 20, 32), diversity.Sequence.MinValue);
        Assert.AreEqual(new DateTime(2023, 09, 28, 18, 20, 32), diversity.Sequence.MaxValue);
        Assert.AreEqual(TimeSpan.FromSeconds(1), diversity.Sequence.Step);
    }
    [TestMethod]
    public void Replication_Parser_DateTimeRange1Step()
    {
        var parser = new DiversityParser("CreationDate", DataType.DateTime, "2022-09-28 TO 2023-09-28 STEP 0.00:10:00");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as DateTimeDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Sequence, diversity.Type);
        Assert.AreEqual(new DateTime(2022, 09, 28), diversity.Sequence.MinValue);
        Assert.AreEqual(new DateTime(2023, 09, 28), diversity.Sequence.MaxValue);
        Assert.AreEqual(TimeSpan.FromMinutes(10), diversity.Sequence.Step);
    }
    [TestMethod]
    public void Replication_Parser_DateTimeRange2Step()
    {
        var parser = new DiversityParser("CreationDate", DataType.DateTime, "2022-09-28 18:20:32 TO 2023-09-28 STEP 12.00:00:00");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as DateTimeDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Sequence, diversity.Type);
        Assert.AreEqual(new DateTime(2022, 09, 28, 18, 20, 32), diversity.Sequence.MinValue);
        Assert.AreEqual(new DateTime(2023, 09, 28), diversity.Sequence.MaxValue);
        Assert.AreEqual(TimeSpan.FromDays(12), diversity.Sequence.Step);
    }
    [TestMethod]
    public void Replication_Parser_DateTimeRange3Step()
    {
        var parser = new DiversityParser("CreationDate", DataType.DateTime, "2022-09-28 TO 2023-09-28 18:20:32 STEP 0.00:00:32");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as DateTimeDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Sequence, diversity.Type);
        Assert.AreEqual(new DateTime(2022, 09, 28), diversity.Sequence.MinValue);
        Assert.AreEqual(new DateTime(2023, 09, 28, 18, 20, 32), diversity.Sequence.MaxValue);
        Assert.AreEqual(TimeSpan.FromSeconds(32), diversity.Sequence.Step);
    }
    [TestMethod]
    public void Replication_Parser_DateTimeRange4Step()
    {
        var parser = new DiversityParser("StartDate", DataType.DateTime,
            "2023-01-01 12:00:05 TO 2023-01-01 12:50:00 STEP 0.00:05:00");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as DateTimeDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Sequence, diversity.Type);
        Assert.AreEqual(new DateTime(2023, 1, 1, 12, 0, 5), diversity.Sequence.MinValue);
        Assert.AreEqual(new DateTime(2023, 1, 1, 12, 50, 00), diversity.Sequence.MaxValue);
        Assert.AreEqual(TimeSpan.FromMinutes(5), diversity.Sequence.Step);
    }
    [TestMethod]
    public void Replication_Parser_DateTimeSequence()
    {
        var parser = new DiversityParser("CreationDate", DataType.DateTime, "2022-09-28 18:20:32 TO 2023-09-28 18:20:32 STEP 0.02:00:32");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as DateTimeDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Sequence, diversity.Type);
        Assert.AreEqual(new DateTime(2022, 09, 28, 18, 20, 32), diversity.Sequence.MinValue);
        Assert.AreEqual(new DateTime(2023, 09, 28, 18, 20, 32), diversity.Sequence.MaxValue);
        Assert.AreEqual(TimeSpan.FromHours(2) + TimeSpan.FromSeconds(32), diversity.Sequence.Step);
    }
    [TestMethod]
    public void Replication_Parser_DateTimeRandom()
    {
        var parser = new DiversityParser("CreationDate", DataType.DateTime, "Random: 2022-09-28 TO 2023-09-28");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as DateTimeDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Random, diversity.Type);
        Assert.AreEqual(new DateTime(2022, 09, 28), diversity.Sequence.MinValue);
        Assert.AreEqual(new DateTime(2023, 09, 28), diversity.Sequence.MaxValue);
    }
    [TestMethod]
    public void Replication_Parser_Error_DateTimeRandom()
    {
        var parser = new DiversityParser("CreationDate", DataType.DateTime, "Random: 2022-09-28 TO 2023-09+28");

        try
        {
            // ACT
            parser.Parse();
            Assert.Fail("ReplicationParserException was not thrown.");
        }
        catch (DiversityParserException e)
        {
            Assert.AreEqual("Invalid maximum value in the date-time range of the \"CreationDate\" field: \"Random: 2022-09-28 TO 2023-09+28\".", e.Message);
        }
    }
    [TestMethod]
    public void Replication_Parser_Error_DateTimeStep()
    {
        var parser = new DiversityParser("CreationDate", DataType.DateTime,
            "2022-09-28 TO 2023-09-28 STEP 10:00");

        try
        {
            // ACT
            parser.Parse();
            Assert.Fail("ReplicationParserException was not thrown.");
        }
        catch (DiversityParserException e)
        {
            Assert.AreEqual("Invalid step value in the DateTime range of the \"CreationDate\" field: \"2022-09-28 TO 2023-09-28 STEP 10:00\".", e.Message);
        }
    }

    /* ========================================================================================= STRING PARSER */

    [TestMethod]
    public void Replication_Parser_StringConstant()
    {
        var parser = new DiversityParser("Name", DataType.String,
            "String1");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as StringDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Constant, diversity.Type);
        Assert.AreEqual("String1", diversity.Pattern);
    }
    [TestMethod]
    public void Replication_Parser_StringConstantMoreWords()
    {
        var parser = new DiversityParser("Name", DataType.String,
            "Word1 word2 word3 ");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as StringDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Constant, diversity.Type);
        Assert.AreEqual("Word1 word2 word3", diversity.Pattern);
    }
    [TestMethod]
    public void Replication_Parser_StringPattern_WithoutTrailingSpace()
    {
        var parser = new DiversityParser("Name", DataType.String, "Prefix*,FortyTwo TO 54");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as StringDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual(DiversityType.Constant, diversity.Type);
        Assert.AreEqual("Prefix*,FortyTwo TO 54", diversity.Pattern);
    }
    [TestMethod]
    public void Replication_Parser_StringRange()
    {
        var parser = new DiversityParser("Name", DataType.String, "Prefix*, 42 TO 52");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as StringDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual("Prefix*", diversity.Pattern);
        Assert.AreEqual(DiversityType.Sequence, diversity.Type);
        Assert.AreEqual(42, diversity.Sequence.MinValue);
        Assert.AreEqual(52, diversity.Sequence.MaxValue);
        Assert.AreEqual(1, diversity.Sequence.Step);
    }
    [TestMethod]
    public void Replication_Parser_StringRangeWithStep()
    {
        var parser = new DiversityParser("Name", DataType.String, "Prefix*, 42 TO 532 STEP 7");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as StringDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual("Prefix*", diversity.Pattern);
        Assert.AreEqual(DiversityType.Sequence, diversity.Type);
        Assert.AreEqual(42, diversity.Sequence.MinValue);
        Assert.AreEqual(532, diversity.Sequence.MaxValue);
        Assert.AreEqual(7, diversity.Sequence.Step);
    }
    [TestMethod]
    public void Replication_Parser_StringSequence()
    {
        var parser = new DiversityParser("Name", DataType.String, "Prefix*, Sequence: 42 TO 52");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as StringDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual("Prefix*", diversity.Pattern);
        Assert.AreEqual(DiversityType.Sequence, diversity.Type);
        Assert.AreEqual(42, diversity.Sequence.MinValue);
        Assert.AreEqual(52, diversity.Sequence.MaxValue);
        Assert.AreEqual(1, diversity.Sequence.Step);
    }
    [TestMethod]
    public void Replication_Parser_StringRandom()
    {
        var parser = new DiversityParser("Name", DataType.String, "Prefix*, Random: 42 TO 52");

        // ACT
        var parsed = parser.Parse();

        // ASSERT
        var diversity = parsed as StringDiversity;
        Assert.IsNotNull(diversity);
        Assert.AreEqual("Prefix*", diversity.Pattern);
        Assert.AreEqual(DiversityType.Random, diversity.Type);
        Assert.AreEqual(42, diversity.Sequence.MinValue);
        Assert.AreEqual(52, diversity.Sequence.MaxValue);
        Assert.AreEqual(1, diversity.Sequence.Step);
    }
    [TestMethod]
    public void Replication_Parser_Error_StringMissingRange1()
    {
        var parser = new DiversityParser("Name", DataType.String, "Word,");

        // ACT
        try
        {
            parser.Parse();
            Assert.Fail("ReplicationParserException was not thrown.");
        }
        catch (DiversityParserException e)
        {
            Assert.AreEqual("Invalid integer range in the string expression of the \"Name\" field: \"Word,\".", e.Message);
        }
    }
    [TestMethod]
    public void Replication_Parser_Error_StringMissingRange2()
    {
        var parser = new DiversityParser("Name", DataType.String, "Word word last-word,");

        // ACT
        try
        {
            parser.Parse();
            Assert.Fail("ReplicationParserException was not thrown.");
        }
        catch (DiversityParserException e)
        {
            Assert.AreEqual("Invalid integer range in the string expression of the \"Name\" field: \"Word word last-word,\".", e.Message);
        }
    }
    [TestMethod]
    public void Replication_Parser_Error_StringRangeMin()
    {
        var parser = new DiversityParser("Name", DataType.String, "Prefix*, FortyTwo TO 54");

        // ACT
        try
        {
            parser.Parse();
            Assert.Fail("ReplicationParserException was not thrown.");
        }
        catch (DiversityParserException e)
        {
            Assert.AreEqual("Invalid integer range in the string expression of the \"Name\" field: \"Prefix*, FortyTwo TO 54\".", e.Message);
        }
    }
    [TestMethod]
    public void Replication_Parser_Error_StringRangeMax()
    {
        var parser = new DiversityParser("Name", DataType.String, "Prefix*, 42 TO FiftyFour");

        // ACT
        try
        {
            parser.Parse();
            Assert.Fail("ReplicationParserException was not thrown.");
        }
        catch (DiversityParserException e)
        {
            Assert.AreEqual("Invalid maximum value in the integer range of the \"Name\" field: \"Prefix*, 42 TO FiftyFour\".", e.Message);
        }
    }
    [TestMethod]
    public void Replication_Parser_Error_StringRangeStep()
    {
        var parser = new DiversityParser("Name", DataType.String, "Prefix*, 42 TO 508 STEP three");

        try
        {
            // ACT
            parser.Parse();
            Assert.Fail("ReplicationParserException was not thrown.");
        }
        catch (DiversityParserException e)
        {
            Assert.AreEqual("Invalid step value in the integer range of the \"Name\" field: \"Prefix*, 42 TO 508 STEP three\".", e.Message);
        }
    }

    /* ========================================================================================= DATA-TYPE ERRORS */

    [TestMethod]
    public void Replication_Parser_DataType_Integer_StringSequence()
    {
        var parser = new DiversityParser("Index", DataType.Int, "Prefix*, Sequence: 42 TO 52");

        try
        {
            // ACT
            var _ = parser.Parse();

            // ASSERT
            Assert.Fail("DiversityParserException was not thrown.");
        }
        catch (DiversityParserException e)
        {
            Assert.AreEqual("Cannot use \"String\" for the \"Index\" field because it's data type is \"Int\".", e.Message);
        }
    }
    [TestMethod]
    public void Replication_Parser_DataType_Integer_DateTimeSequence()
    {
        var parser = new DiversityParser("Index", DataType.Int, "2022-09-28 TO 2023-09-28");

        try
        {
            // ACT
            var _ = parser.Parse();

            // ASSERT
            Assert.Fail("DiversityParserException was not thrown.");
        }
        catch (DiversityParserException e)
        {
            Assert.AreEqual("Cannot use \"DateTime\" for the \"Index\" field because it's data type is \"Int\".", e.Message);
        }
    }
    [TestMethod]
    public void Replication_Parser_DataType_DateTime_StringSequence()
    {
        var parser = new DiversityParser("CreationDate", DataType.DateTime, "Prefix*, Sequence: 42 TO 52");

        try
        {
            // ACT
            var _ = parser.Parse();

            // ASSERT
            Assert.Fail("DiversityParserException was not thrown.");
        }
        catch (DiversityParserException e)
        {
            Assert.AreEqual("Cannot use \"String\" for the \"CreationDate\" field because it's data type is \"DateTime\".", e.Message);
        }
    }
    [TestMethod]
    public void Replication_Parser_DataType_DateTime_IntSequence()
    {
        var parser = new DiversityParser("CreationDate", DataType.DateTime, "42 TO 52");

        try
        {
            // ACT
            var _ = parser.Parse();

            // ASSERT
            Assert.Fail("DiversityParserException was not thrown.");
        }
        catch (DiversityParserException e)
        {
            Assert.AreEqual("Cannot use \"Int\" for the \"CreationDate\" field because it's data type is \"DateTime\".", e.Message);
        }
    }
    [TestMethod]
    public void Replication_Parser_DataType_String_DateTimeSequence()
    {
        var parser = new DiversityParser("DisplayName", DataType.String, "2022-09-28 TO 2023-09-28");

        try
        {
            // ACT
            var _ = parser.Parse();

            // ASSERT
            Assert.Fail("DiversityParserException was not thrown.");
        }
        catch (DiversityParserException e)
        {
            Assert.AreEqual("Cannot use \"DateTime\" for the \"DisplayName\" field because it's data type is \"String\".", e.Message);
        }
    }
    [TestMethod]
    public void Replication_Parser_DataType_String_IntSequence()
    {
        var parser = new DiversityParser("DisplayName", DataType.String, "42 TO 52");

        try
        {
            // ACT
            var _ = parser.Parse();

            // ASSERT
            Assert.Fail("DiversityParserException was not thrown.");
        }
        catch (DiversityParserException e)
        {
            Assert.AreEqual("Cannot use \"Int\" for the \"DisplayName\" field because it's data type is \"String\".", e.Message);
        }
    }

    /* ========================================================================================= ODATA API */

    private class TestReplicationService : IReplicationService
    {
        public Node Source { get; private set; }
        public Node Target { get; private set; }
        public ReplicationDescriptor ReplicationDescriptor { get; private set; }
        public System.Threading.Tasks.Task ReplicateNodeAsync(Node source, Node target, ReplicationDescriptor replicationDescriptor,
            CancellationToken cancel)
        {
            Source = source;
            Target = target;
            ReplicationDescriptor = replicationDescriptor;
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }

    [TestMethod]
    public async System.Threading.Tasks.Task Replication_OData_Happy_Path()
    {
        await Test2(services =>
        {
            services.AddSingleton<IReplicationService, TestReplicationService>();
        }, async () =>
        {
            var httpContext = new DefaultHttpContext { RequestServices = Providers.Instance.Services };

            var workspace = await Node.LoadNodeAsync("/Root/Content", CancellationToken.None);
            var source = new Folder(workspace) { Name = "ReplicationSource" };
            await source.SaveAsync(CancellationToken.None);
            var target = new Folder(workspace) { Name = "ReplicationTarget" };
            await target.SaveAsync(CancellationToken.None);

            // ACT
            await ContentGenerationOperations.ReplicateAsync(Content.Load(source.Id), httpContext, target.Id.ToString(),
                new ReplicationDescriptor
                {
                    Fields = new Dictionary<string, string>
                    {
                        {"Name", "Replicated-*, 1 to 10"},
                        {"Index", "random: 1 to 9 step 2"}
                    }
                });

            // ASSERT
            var replicator = Providers.Instance.Services.GetRequiredService<IReplicationService>() as TestReplicationService;
            Assert.IsNotNull(replicator);
            Assert.AreEqual("/Root/Content/ReplicationSource", replicator.Source.Path);
            Assert.AreEqual("/Root/Content/ReplicationTarget", replicator.Target.Path);
            var diversity = replicator.ReplicationDescriptor.Diversity;
            Assert.IsNotNull(diversity);
            Assert.AreEqual(2, diversity.Count);

            var diversityItems = diversity.ToArray();
            Assert.AreEqual("Name", diversityItems[0].Key);
            var nameDiversity = diversityItems[0].Value as StringDiversity;
            Assert.IsNotNull(nameDiversity);
            Assert.AreEqual("Replicated-*", nameDiversity.Pattern);
            Assert.AreEqual(DiversityType.Sequence, nameDiversity.Type);
            Assert.AreEqual(1, nameDiversity.Sequence.MinValue);
            Assert.AreEqual(10, nameDiversity.Sequence.MaxValue);
            Assert.AreEqual(1, nameDiversity.Sequence.Step);

            Assert.AreEqual("Index", diversityItems[1].Key);
            var indexDiversity = diversityItems[1].Value as IntDiversity;
            Assert.IsNotNull(indexDiversity);
            Assert.AreEqual(DiversityType.Random, indexDiversity.Type);
            Assert.AreEqual(1, indexDiversity.MinValue);
            Assert.AreEqual(9, indexDiversity.MaxValue);
            Assert.AreEqual(2, indexDiversity.Step);

        }).ConfigureAwait(false);
    }
    [TestMethod]
    public async System.Threading.Tasks.Task Replication_OData_Errors()
    {
        await Test2(services =>
        {
            services.AddSingleton<IReplicationService, TestReplicationService>();
        }, async () =>
        {
            var httpContext = new DefaultHttpContext {RequestServices = Providers.Instance.Services};

            var workspace = await Node.LoadNodeAsync("/Root/Content", CancellationToken.None);
            var source = new Folder(workspace) { Name = "ReplicationSource" };
            await source.SaveAsync(CancellationToken.None);
            var target = new Folder(workspace) { Name = "ReplicationTarget" };
            await target.SaveAsync(CancellationToken.None);

            // ACT
            string[] errors = null;
            try
            {
                await ContentGenerationOperations.ReplicateAsync(Content.Load(source.Id), httpContext, target.Id.ToString(),
                    new ReplicationDescriptor
                    {
                        Fields = new Dictionary<string, string>
                        {
                            {"Name2", "Replicated-*, 1 to 10"},
                            {"Index2", "random: 1 to 9 step 2"},
                            {"StartDate", "1 to 2"},
                            {"ModificationDate", "1 to end"}
                        }
                    });
                Assert.Fail("AggregateException was not thrown.");
            }
            catch (AggregateException ae)
            {
                errors = ae.InnerExceptions.Select(e => e.Message).ToArray();
            }

            // ASSERT
            Assert.AreEqual(4, errors.Length);
            Assert.AreEqual("Type \"Folder\" does not have a field named \"Name2\".", errors[0]);
            Assert.AreEqual("Type \"Folder\" does not have a field named \"Index2\".", errors[1]);
            Assert.AreEqual("Type \"Folder\" does not have a field named \"StartDate\".", errors[2]);
            Assert.AreEqual("Invalid maximum value in the integer range of the \"ModificationDate\" field: \"1 to end\".", errors[3]);

        }).ConfigureAwait(false);
    }
}