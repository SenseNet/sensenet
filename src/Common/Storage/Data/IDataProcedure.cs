using System;
using System.Data.Common;
using System.Data;

namespace SenseNet.ContentRepository.Storage.Data
{
    /// <summary>
    /// Defines an interface that represents a database operation.
    /// </summary>
    public interface IDataProcedure : IDisposable
    {
        /// <summary>
        /// Gets or sets the CommandText interpretation mode (for example: Text or StoredProcedure etc.).
        /// </summary>
        CommandType CommandType { get; set; }
        /// <summary>
        /// Gets or sets the text command that will be executed on the database.
        /// </summary>
        string CommandText { get; set; }
        /// <summary>
        /// Gets the collection of the parameters.
        /// </summary>
        DbParameterCollection Parameters { get; }

        /// <summary>
        /// Executes the CommandText and builds an IDataReader.
        /// </summary>
        DbDataReader ExecuteReader();
        /// <summary>
        /// Executes the CommandText and builds an IDataReader using one of the CommandBehavior values.
        /// </summary>
        DbDataReader ExecuteReader(CommandBehavior behavior);
        /// <summary>
        /// Executes the CommandText and returns the first column of the first row in the resultset.
        /// Any other columns or rows are ignored.
        /// </summary>
        object ExecuteScalar();
        /// <summary>
        /// Executes the CommandText and returns the number of rows affected.
        /// </summary>
        /// <returns></returns>
        int ExecuteNonQuery();

        /// <summary>
        /// Returns a newly created, platform specific parameter object.
        /// </summary>
        /// <returns></returns>
        IDataParameter CreateParameter();
    }
}
