using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
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
        catch (ReplicationParserException e)
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
        Assert.AreEqual(0, diversity.Step);
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
        Assert.AreEqual(0, diversity.Step);
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
        Assert.AreEqual(0, diversity.Step);
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
        catch (ReplicationParserException e)
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
        catch (ReplicationParserException e)
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
        Assert.AreEqual(TimeSpan.Zero, diversity.Sequence.Step);
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
        Assert.AreEqual(TimeSpan.Zero, diversity.Sequence.Step);
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
        Assert.AreEqual(TimeSpan.Zero, diversity.Sequence.Step);
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
        Assert.AreEqual(TimeSpan.Zero, diversity.Sequence.Step);
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
        catch (ReplicationParserException e)
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
        catch (ReplicationParserException e)
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
        Assert.AreEqual(0, diversity.Sequence.Step);
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
        Assert.AreEqual(0, diversity.Sequence.Step);
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
        Assert.AreEqual(0, diversity.Sequence.Step);
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
        catch (ReplicationParserException e)
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
        catch (ReplicationParserException e)
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
        catch (ReplicationParserException e)
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
        catch (ReplicationParserException e)
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
        catch (ReplicationParserException e)
        {
            Assert.AreEqual("Invalid step value in the integer range of the \"Name\" field: \"Prefix*, 42 TO 508 STEP three\".", e.Message);
        }
    }

}