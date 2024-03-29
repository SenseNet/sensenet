﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage.Data.MsSqlClient;
using SenseNet.Diagnostics;
using SenseNet.Tools;

namespace SenseNet.Packaging.Steps
{
    /// <summary>
    /// Executes the provided step block in a loop while a database query returns a positive value.
    /// Useful for dividing a long-running database script into multiple blocks to avoid
    /// exceeding the database command timeout.
    /// </summary>
    public class WhileDatabaseValue : While
    {
        /// <summary>
        /// Gets or sets a database query that represents the while loop's condition.
        /// </summary>
        public string Query { get; set; }

        protected override bool EvaluateCondition(ExecutionContext context)
        {
            if (string.IsNullOrEmpty(Query))
                throw new PackagingException(PackagingExceptionType.InvalidParameter);

            return ExecuteSql(Query, context.ConnectionStrings.Repository);
        }

        internal bool ExecuteSql(string script, string connectionString)
        {
            using var op = SnTrace.Database.StartOperation("WhileDatabaseValue: " +
                $"ExecuteSql: {script.ToTrace()}");

            //TODO: [DIREF] get options from DI through constructor
            using var ctx = new MsSqlDataContext(connectionString, DataOptions.GetLegacyConfiguration(),
                GetService<IRetrier>(), CancellationToken.None);
            try
            {
                var result = ctx.ExecuteScalarAsync(script).GetAwaiter().GetResult();

                if (result == null || Convert.IsDBNull(result))
                {
                    op.Successful = true;
                    return false;
                }
                op.Successful = true;
                return ConvertToBool(result);
            }
            catch (Exception ex)
            {
                throw new PackagingException("Error during SQL script execution. " + ex);
            }
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        private static bool ConvertToBool(object result)
        {
            switch (result)
            {
                case bool b:
                    return b;
                case byte b1:
                    return b1 != 0;
                case decimal @decimal:
                    return @decimal != 0;
                case double d:
                    return d != 0;
                case float f:
                    return f != 0;
                case int i:
                    return i != 0;
                case long l:
                    return l != 0;
                case sbyte @sbyte:
                    return @sbyte != 0;
                case short s:
                    return s != 0;
                case uint u:
                    return u != 0;
                case ulong @ulong:
                    return @ulong != 0;
                case ushort @ushort:
                    return @ushort != 0;
                case string s1:
                    return !string.IsNullOrEmpty(s1);
                default:
                    return false;
            }
        }
    }
}
