using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Tools.CommandLineArguments;
using SenseNet.Tools.SnInitialDataGenerator;

namespace SnInitialDataGenerator.Tests
{
    [TestClass]
    public class ArgumentsTests
    {
        private static readonly char VOL = Path.VolumeSeparatorChar;
        private static readonly char DIR = Path.DirectorySeparatorChar;
        private static readonly string ImportPath = $"A{VOL}{DIR}import";
        private static readonly string OutputPath = $"B{VOL}{DIR}output";

        [TestMethod]
        public void DataGen_Args_MostCommonUseCase()
        {
            var expectedDataFileName = $"C{VOL}{DIR}src{DIR}InitialDatabase.cs";
            var expectedDbClass = "Database1";
            var expectedDbNamespace = "DatabaseNamespace";
            var expectedIndexFileName = $"C{VOL}{DIR}src{DIR}InitialIndex.cs";
            var expectedIndexClass = "Index1";
            var args = new[]
            {
                "-I", ImportPath,
                "-DF", expectedDataFileName,
                "-DT", $"{expectedDbNamespace}.{expectedDbClass}",
                "-IF", expectedIndexFileName,
                "-IT", expectedIndexClass,
            };

            var arguments = new Arguments();
            try
            {
                var parser = ArgumentParser.Parse(args, arguments);
            }
            catch (ParsingException e)
            {
                Assert.Fail(e.FormattedMessage);
            }

            // ACTION
            arguments.PrepareProperties();

            // ASSERT
            var expectedOutputPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "output");
            Assert.AreEqual(expectedOutputPath, arguments.OutputPath);
            Assert.AreEqual(expectedDataFileName, arguments.DataFileName);
            Assert.AreEqual(expectedIndexFileName, arguments.IndexFileName);

            Assert.AreEqual(expectedDbNamespace, arguments.DatabaseNamespace);
            Assert.AreEqual(expectedDbClass, arguments.DatabaseClassName);
            Assert.AreEqual(expectedDbNamespace, arguments.IndexNamespace);
            Assert.AreEqual(expectedIndexClass, arguments.IndexClassName);
        }

        [TestMethod]
        public void DataGen_Args_EverythingIsDefault()
        {
            var args = new[]
            {
                "-IMPORT", ImportPath,
            };
            var arguments = new Arguments();
            try
            {
                var parser = ArgumentParser.Parse(args, arguments);
            }
            catch (ParsingException e)
            {
                Assert.Fail(e.FormattedMessage);
            }

            // ACTION
            arguments.PrepareProperties();

            // ASSERT
            var expectedOutputPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "output");
            var expectedDataFileName = Path.Combine(expectedOutputPath, Arguments.DefaultDataFileName);
            var expectedIndexFileName = Path.Combine(expectedOutputPath, Arguments.DefaultIndexFileName);

            Assert.AreEqual(expectedOutputPath, arguments.OutputPath);
            Assert.AreEqual(expectedDataFileName, arguments.DataFileName);
            Assert.AreEqual(expectedIndexFileName, arguments.IndexFileName);

            Assert.AreEqual(Arguments.DefaultNamespace, arguments.DatabaseNamespace);
            Assert.AreEqual(Arguments.DefaultDatabaseName, arguments.DatabaseClassName);
            Assert.AreEqual(Arguments.DefaultNamespace, arguments.IndexNamespace);
            Assert.AreEqual(Arguments.DefaultIndexName, arguments.IndexClassName);
        }
        [TestMethod]
        public void DataGen_Args_CustomOutput()
        {
            var expectedOutputPath = OutputPath;
            var args = new[]
            {
                "-IMPORT", ImportPath,
                "-OUTPUT", expectedOutputPath
            };

            var arguments = new Arguments();
            try
            {
                var parser = ArgumentParser.Parse(args, arguments);
            }
            catch (ParsingException e)
            {
                Assert.Fail(e.FormattedMessage);
            }

            // ACTION
            arguments.PrepareProperties();

            // ASSERT
            var expectedDataFileName = Path.Combine(expectedOutputPath, Arguments.DefaultDataFileName);
            var expectedIndexFileName = Path.Combine(expectedOutputPath, Arguments.DefaultIndexFileName);

            Assert.AreEqual(expectedOutputPath, arguments.OutputPath);
            Assert.AreEqual(expectedDataFileName, arguments.DataFileName);
            Assert.AreEqual(expectedIndexFileName, arguments.IndexFileName);

            Assert.AreEqual(Arguments.DefaultNamespace, arguments.DatabaseNamespace);
            Assert.AreEqual(Arguments.DefaultDatabaseName, arguments.DatabaseClassName);
            Assert.AreEqual(Arguments.DefaultNamespace, arguments.IndexNamespace);
            Assert.AreEqual(Arguments.DefaultIndexName, arguments.IndexClassName);
        }
        [TestMethod]
        public void DataGen_Args_CustomDataFile()
        {
            var expectedDataFile = "DataBase1.cs";
            var expectedDataClassName = "DataBase1";
            var args = new[]
            {
                "-I", ImportPath, "-O", OutputPath,
                "-DF", expectedDataFile
            };

            var arguments = new Arguments();
            try
            {
                var parser = ArgumentParser.Parse(args, arguments);
            }
            catch (ParsingException e)
            {
                Assert.Fail(e.FormattedMessage);
            }

            // ACTION
            arguments.PrepareProperties();

            // ASSERT
            var expectedDataFileName = Path.Combine(OutputPath, expectedDataFile);
            var expectedIndexFileName = Path.Combine(OutputPath, Arguments.DefaultIndexFileName);

            Assert.AreEqual(OutputPath, arguments.OutputPath);
            Assert.AreEqual(expectedDataFileName, arguments.DataFileName);
            Assert.AreEqual(expectedIndexFileName, arguments.IndexFileName);

            Assert.AreEqual(Arguments.DefaultNamespace, arguments.DatabaseNamespace);
            Assert.AreEqual(expectedDataClassName, arguments.DatabaseClassName);
            Assert.AreEqual(Arguments.DefaultNamespace, arguments.IndexNamespace);
            Assert.AreEqual(Arguments.DefaultIndexName, arguments.IndexClassName);
        }
        [TestMethod]
        public void DataGen_Args_CustomIndexFile()
        {
            var expectedIndexFile = "Index1.cs";
            var expectedIndexClassName = "Index1";
            var args = new[]
            {
                "-I", ImportPath, "-O", OutputPath,
                "-IF", expectedIndexFile
            };

            var arguments = new Arguments();
            try
            {
                var parser = ArgumentParser.Parse(args, arguments);
            }
            catch (ParsingException e)
            {
                Assert.Fail(e.FormattedMessage);
            }

            // ACTION
            arguments.PrepareProperties();

            // ASSERT
            var expectedDataFileName = Path.Combine(OutputPath, Arguments.DefaultDataFileName);
            var expectedIndexFileName = Path.Combine(OutputPath, expectedIndexFile);

            Assert.AreEqual(OutputPath, arguments.OutputPath);
            Assert.AreEqual(expectedDataFileName, arguments.DataFileName);
            Assert.AreEqual(expectedIndexFileName, arguments.IndexFileName);

            Assert.AreEqual(Arguments.DefaultNamespace, arguments.DatabaseNamespace);
            Assert.AreEqual(Arguments.DefaultDatabaseName, arguments.DatabaseClassName);
            Assert.AreEqual(Arguments.DefaultNamespace, arguments.IndexNamespace);
            Assert.AreEqual(expectedIndexClassName, arguments.IndexClassName);
        }
        [TestMethod]
        public void DataGen_Args_CustomDb_Name()
        {
            var expectedDbClass = "Database1";
            var args = new[]
            {
                "-I", ImportPath, "-O", OutputPath,
                "-DT", expectedDbClass
            };

            var arguments = new Arguments();
            try
            {
                var parser = ArgumentParser.Parse(args, arguments);
            }
            catch (ParsingException e)
            {
                Assert.Fail(e.FormattedMessage);
            }

            // ACTION
            arguments.PrepareProperties();

            // ASSERT
            var expectedDataFileName = Path.Combine(OutputPath, Arguments.DefaultDataFileName);
            var expectedIndexFileName = Path.Combine(OutputPath, Arguments.DefaultIndexFileName);

            Assert.AreEqual(OutputPath, arguments.OutputPath);
            Assert.AreEqual(expectedDataFileName, arguments.DataFileName);
            Assert.AreEqual(expectedIndexFileName, arguments.IndexFileName);

            Assert.AreEqual(Arguments.DefaultNamespace, arguments.DatabaseNamespace);
            Assert.AreEqual(expectedDbClass, arguments.DatabaseClassName);
            Assert.AreEqual(Arguments.DefaultNamespace, arguments.IndexNamespace);
            Assert.AreEqual(Arguments.DefaultIndexName, arguments.IndexClassName);
        }
        [TestMethod]
        public void DataGen_Args_CustomIndex_Name()
        {
            var expectedIndexClass = "Index1";
            var args = new[]
            {
                "-I", ImportPath, "-O", OutputPath,
                "-IT", expectedIndexClass
            };

            var arguments = new Arguments();
            try
            {
                var parser = ArgumentParser.Parse(args, arguments);
            }
            catch (ParsingException e)
            {
                Assert.Fail(e.FormattedMessage);
            }

            // ACTION
            arguments.PrepareProperties();

            // ASSERT
            var expectedDataFileName = Path.Combine(OutputPath, Arguments.DefaultDataFileName);
            var expectedIndexFileName = Path.Combine(OutputPath, Arguments.DefaultIndexFileName);

            Assert.AreEqual(OutputPath, arguments.OutputPath);
            Assert.AreEqual(expectedDataFileName, arguments.DataFileName);
            Assert.AreEqual(expectedIndexFileName, arguments.IndexFileName);

            Assert.AreEqual(Arguments.DefaultNamespace, arguments.DatabaseNamespace);
            Assert.AreEqual(Arguments.DefaultDatabaseName, arguments.DatabaseClassName);
            Assert.AreEqual(Arguments.DefaultNamespace, arguments.IndexNamespace);
            Assert.AreEqual(expectedIndexClass, arguments.IndexClassName);
        }
        [TestMethod]
        public void DataGen_Args_CustomDb_FullName()
        {
            var expectedDbClass = "Database1";
            var expectedDbNamespace = "DatabaseNamespace";
            var args = new[]
            {
                "-I", ImportPath, "-O", OutputPath,
                "-DT", $"{expectedDbNamespace}.{expectedDbClass}"
            };

            var arguments = new Arguments();
            try
            {
                var parser = ArgumentParser.Parse(args, arguments);
            }
            catch (ParsingException e)
            {
                Assert.Fail(e.FormattedMessage);
            }

            // ACTION
            arguments.PrepareProperties();

            // ASSERT
            var expectedDataFileName = Path.Combine(OutputPath, Arguments.DefaultDataFileName);
            var expectedIndexFileName = Path.Combine(OutputPath, Arguments.DefaultIndexFileName);

            Assert.AreEqual(OutputPath, arguments.OutputPath);
            Assert.AreEqual(expectedDataFileName, arguments.DataFileName);
            Assert.AreEqual(expectedIndexFileName, arguments.IndexFileName);

            Assert.AreEqual(expectedDbNamespace, arguments.DatabaseNamespace);
            Assert.AreEqual(expectedDbClass, arguments.DatabaseClassName);
            Assert.AreEqual(expectedDbNamespace, arguments.IndexNamespace);
            Assert.AreEqual(Arguments.DefaultIndexName, arguments.IndexClassName);
        }
        [TestMethod]
        public void DataGen_Args_CustomIndex_FullName()
        {
            var expectedIndexClass = "Index1";
            var expectedIndexNamespace = "IndexNamespace";
            var args = new[]
            {
                "-I", ImportPath, "-O", OutputPath,
                "-IT", $"{expectedIndexNamespace}.{expectedIndexClass}"
            };

            var arguments = new Arguments();
            try
            {
                var parser = ArgumentParser.Parse(args, arguments);
            }
            catch (ParsingException e)
            {
                Assert.Fail(e.FormattedMessage);
            }

            // ACTION
            arguments.PrepareProperties();

            // ASSERT
            var expectedDataFileName = Path.Combine(OutputPath, Arguments.DefaultDataFileName);
            var expectedIndexFileName = Path.Combine(OutputPath, Arguments.DefaultIndexFileName);

            Assert.AreEqual(OutputPath, arguments.OutputPath);
            Assert.AreEqual(expectedDataFileName, arguments.DataFileName);
            Assert.AreEqual(expectedIndexFileName, arguments.IndexFileName);

            Assert.AreEqual(expectedIndexNamespace, arguments.DatabaseNamespace);
            Assert.AreEqual(Arguments.DefaultDatabaseName, arguments.DatabaseClassName);
            Assert.AreEqual(expectedIndexNamespace, arguments.IndexNamespace);
            Assert.AreEqual(expectedIndexClass, arguments.IndexClassName);
        }
        [TestMethod]
        public void DataGen_Args_CustomDb_FullName_CustomIndex_Name()
        {
            var expectedDbClass = "Database1";
            var expectedDbNamespace = "DatabaseNamespace";
            var expectedIndexClass = "Index1";
            var args = new[]
            {
                "-I", ImportPath, "-O", OutputPath,
                "-DT", $"{expectedDbNamespace}.{expectedDbClass}",
                "-IT", expectedIndexClass
            };

            var arguments = new Arguments();
            try
            {
                var parser = ArgumentParser.Parse(args, arguments);
            }
            catch (ParsingException e)
            {
                Assert.Fail(e.FormattedMessage);
            }

            // ACTION
            arguments.PrepareProperties();

            // ASSERT
            var expectedDataFileName = Path.Combine(OutputPath, Arguments.DefaultDataFileName);
            var expectedIndexFileName = Path.Combine(OutputPath, Arguments.DefaultIndexFileName);

            Assert.AreEqual(OutputPath, arguments.OutputPath);
            Assert.AreEqual(expectedDataFileName, arguments.DataFileName);
            Assert.AreEqual(expectedIndexFileName, arguments.IndexFileName);

            Assert.AreEqual(expectedDbNamespace, arguments.DatabaseNamespace);
            Assert.AreEqual(expectedDbClass, arguments.DatabaseClassName);
            Assert.AreEqual(expectedDbNamespace, arguments.IndexNamespace);
            Assert.AreEqual(expectedIndexClass, arguments.IndexClassName);
        }
        [TestMethod]
        public void DataGen_Args_EverythingIsCustomized()
        {
            var expectedDataFileName = $"C{VOL}{DIR}src{DIR}InitialDatabase.cs";
            var expectedDbClass = "Database1";
            var expectedDbNamespace = "DatabaseNamespace";
            var expectedIndexFileName = $"C{VOL}{DIR}src{DIR}InitialIndex.cs";
            var expectedIndexClass = "Index1";
            var expectedIndexNamespace = "IndexNamespace";
            var args = new[]
            {
                "-I", ImportPath, "-O", OutputPath,
                "-DF", expectedDataFileName,
                "-DT", $"{expectedDbNamespace}.{expectedDbClass}",
                "-IF", expectedIndexFileName,
                "-IT", $"{expectedIndexNamespace}.{expectedIndexClass}",
            };

            var arguments = new Arguments();
            try
            {
                var parser = ArgumentParser.Parse(args, arguments);
            }
            catch (ParsingException e)
            {
                Assert.Fail(e.FormattedMessage);
            }

            // ACTION
            arguments.PrepareProperties();

            // ASSERT
            Assert.AreEqual(OutputPath, arguments.OutputPath);
            Assert.AreEqual(expectedDataFileName, arguments.DataFileName);
            Assert.AreEqual(expectedIndexFileName, arguments.IndexFileName);

            Assert.AreEqual(expectedDbNamespace, arguments.DatabaseNamespace);
            Assert.AreEqual(expectedDbClass, arguments.DatabaseClassName);
            Assert.AreEqual(expectedIndexNamespace, arguments.IndexNamespace);
            Assert.AreEqual(expectedIndexClass, arguments.IndexClassName);
        }

        [TestMethod]
        public void DataGen_Args_ImportPathTrailing()
        {
            var expectedPath = $"A{VOL}{DIR}import{DIR}";
            var args = new[]
            {
                "-IMPORT", expectedPath
            };
            var arguments = new Arguments();
            try
            {
                var parser = ArgumentParser.Parse(args, arguments);
            }
            catch (ParsingException e)
            {
                Assert.Fail(e.FormattedMessage);
            }

            // ACTION
            arguments.PrepareProperties();

            // ASSERT
            Assert.AreEqual(expectedPath, arguments.ImportPath);
        }

        [TestMethod]
        public void DataGen_Args_Skipped()
        {
            var args = new[]
            {
                "-IMPORT", ImportPath, "-S", "aaa\\bbb,Aaa\\Ccc,bbb"
            };
            var arguments = new Arguments();
            try
            {
                var parser = ArgumentParser.Parse(args, arguments);
            }
            catch (ParsingException e)
            {
                Assert.Fail(e.FormattedMessage);
            }

            // ACTION
            arguments.PrepareProperties();

            // ASSERT
            Assert.AreEqual(3, arguments.SkippedPathArray.Length);
            Assert.AreEqual("/Root/aaa/bbb", arguments.SkippedPathArray[0]);
            Assert.AreEqual("/Root/Aaa/Ccc", arguments.SkippedPathArray[1]);
            Assert.AreEqual("/Root/bbb", arguments.SkippedPathArray[2]);
        }
        [TestMethod]
        public void DataGen_Args_Skipped_Trailing()
        {
            var args = new[]
            {
                "-IMPORT", ImportPath, "-S", "aaa,bbb,bbb\\"
            };
            var arguments = new Arguments();
            try
            {
                var parser = ArgumentParser.Parse(args, arguments);
            }
            catch (ParsingException e)
            {
                Assert.Fail(e.FormattedMessage);
            }

            // ACTION
            arguments.PrepareProperties();

            // ASSERT
            Assert.AreEqual(3, arguments.SkippedPathArray.Length);
            Assert.AreEqual("/Root/aaa", arguments.SkippedPathArray[0]);
            Assert.AreEqual("/Root/bbb", arguments.SkippedPathArray[1]);
            Assert.AreEqual("/Root/bbb/", arguments.SkippedPathArray[2]);
        }
        [TestMethod]
        public void DataGen_Args_Skipped_EmptyPath()
        {
            var args = new[]
            {
                "-IMPORT", ImportPath, "-S", "aaa,"
            };
            var arguments = new Arguments();
            try
            {
                var parser = ArgumentParser.Parse(args, arguments);
            }
            catch (ParsingException e)
            {
                Assert.Fail(e.FormattedMessage);
            }

            // ACTION
            arguments.PrepareProperties();

            // ASSERT
            Assert.AreEqual(2, arguments.SkippedPathArray.Length);
            Assert.AreEqual("/Root/aaa", arguments.SkippedPathArray[0]);
            Assert.AreEqual("/Root", arguments.SkippedPathArray[1]);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DataGen_Args_Skipped_OneSlash()
        {
            var args = new[]
            {
                "-IMPORT", ImportPath, "-S", "aaa,,\\"
            };
            var arguments = new Arguments();
            try
            {
                var parser = ArgumentParser.Parse(args, arguments);
            }
            catch (ParsingException e)
            {
                Assert.Fail(e.FormattedMessage);
            }

            // ACTION
            arguments.PrepareProperties();

            // ASSERT
            Assert.AreEqual(3, arguments.SkippedPathArray.Length);
            Assert.AreEqual("/Root/aaa", arguments.SkippedPathArray[0]);
            Assert.AreEqual("/Root", arguments.SkippedPathArray[1]);
            Assert.AreEqual("/Root/", arguments.SkippedPathArray[2]);
        }
    }
}
