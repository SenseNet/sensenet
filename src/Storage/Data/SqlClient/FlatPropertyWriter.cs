using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SenseNet.ContentRepository.Storage.Schema;
using System.Data.SqlTypes;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    [Obsolete("##", true)]
    internal class FlatPropertyWriter
    {
        private int _versionId;
        private Dictionary<int, Dictionary<string, SqlParameter>> _values = new Dictionary<int, Dictionary<string, SqlParameter>>(); // page, property, value

        public FlatPropertyWriter(int versionId)
        {
            _versionId = versionId;
        }

        private static List<int> GetExistingFlatPages(int versionId)
        {
            List<int> pages = new List<int>();
            SqlProcedure cmd = null;
            SqlDataReader reader = null;
            try
            {
                cmd = new SqlProcedure { CommandText = "proc_FlatProperties_GetExistingPages" };
                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                reader = cmd.ExecuteReader();

                int pageIndex = reader.GetOrdinal("Page");
                while (reader.Read())
                {
                    pages.Add(reader.GetInt32(pageIndex));
                }
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();

                cmd.Dispose();
            }
            return pages;
        }

        // ================================================================================ Add Property

        public void WriteStringProperty(string value, PropertyType type)
        {
            WriteFlatProperty(
                type.Mapping,
                SqlProvider.StringPageSize,
                SqlProvider.StringMappingPrefix,
                SqlDbType.NVarChar,
                SqlProvider.StringDataTypeSize,
                value);
        }
        public void WriteIntProperty(int value, PropertyType type)
        {
            WriteFlatProperty(
                type.Mapping,
                SqlProvider.IntPageSize,
                SqlProvider.IntMappingPrefix,
                SqlDbType.Int,
                0,
                value);
        }
        public void WriteDateTimeProperty(DateTime value, PropertyType type)
        {

            // provider-level hack to handle the "System.Data.SqlTypes.SqlDateTime.MinValue > System.DateTime.Minvalue
            object valueInDb;
            if ((value > SqlDateTime.MinValue.Value) && (value < SqlDateTime.MaxValue.Value))
                valueInDb = value;
            else
                valueInDb = DBNull.Value;

            WriteFlatProperty(
                type.Mapping,
                SqlProvider.DateTimePageSize,
                SqlProvider.DateTimeMappingPrefix,
                SqlDbType.DateTime,
                0,
                valueInDb);
        }
        public void WriteCurrencyProperty(decimal value, PropertyType type)
        {
            WriteFlatProperty(
                type.Mapping,
                SqlProvider.CurrencyPageSize,
                SqlProvider.CurrencyMappingPrefix,
                SqlDbType.Money,
                0,
                value);
        }
        private void WriteFlatProperty(int totalIndex, int pageSize, string mappingPrefix, SqlDbType dataType, int dataSize, object value)
        {
            int page = totalIndex / pageSize;
            int index = totalIndex - (page * pageSize);
            string column = string.Concat(mappingPrefix, index + 1);

            Dictionary<string, SqlParameter> valuePage;
            if (!_values.TryGetValue(page, out valuePage))
                _values.Add(page, valuePage = new Dictionary<string, SqlParameter>());

            SqlParameter param;
            if (dataSize > 0)
                param = new SqlParameter("@" + column, dataType, dataSize);
            else
                param = new SqlParameter("@" + column, dataType);
            param.Value = value ?? DBNull.Value;

            valuePage.Add(column, param);
        }

        // ================================================================================ Execute

        public void Execute()
        {
            var existingPages = GetExistingFlatPages(_versionId);
            foreach (var page in _values.Keys)
            {
                if (existingPages.Contains(page))
                    UpdatePage(_versionId, page, _values[page]);
                else
                    InsertPage(_versionId, page, _values[page]);
            }
        }
        private void InsertPage(int versionId, int page, Dictionary<string, SqlParameter> values)
        {
            // INSERT INTO FlatProperties  (VersionId,  Page,  nvarchar_1,  nvarchar_2)
            //                     VALUES (@VersionId, @Page, @nvarchar_1, @nvarchar_2)

            var columnNames = values.Keys.Select(c => String.Format("\t{0}", c)).ToArray();
            var columnString = String.Join(", " + Environment.NewLine, columnNames);
            var paramNames = values.Keys.Select(c => String.Format("\t@{0}", c)).ToArray();
            var paramString = String.Join(", " + Environment.NewLine, paramNames);

            var script = new StringBuilder();
            script.AppendLine("INSERT INTO FlatProperties");
            script.AppendLine("(");
            script.AppendLine("\tVersionId, Page,");
            script.AppendLine(columnString);
            script.AppendLine(")");
            script.AppendLine("VALUES");
            script.AppendLine("(");
            script.AppendLine("\t@VersionId, @Page,");
            script.AppendLine(paramString);
            script.AppendLine(")");

            ExecuteScript(script.ToString(), versionId, page, values.Values);
        }
        private void UpdatePage(int versionId, int page, Dictionary<string, SqlParameter> values)
        {
            // UPDATE FlatProperties SET
            //    nvarchar_1 = @nvarchar_1,
            //    nvarchar_2 = @nvarchar_2
            // WHERE VersionId = @VersionId AND Page = @Page

            var setLines = values.Keys.Select(c => String.Format("\t{0} = @{0}", c)).ToArray();
            var setString = String.Join(", " + Environment.NewLine, setLines);
            var script = new StringBuilder();
            script.AppendLine("UPDATE FlatProperties SET");
            script.AppendLine(setString);
            script.AppendLine("WHERE VersionId = @VersionId AND Page = @Page");

            ExecuteScript(script.ToString(), versionId, page, values.Values);
        }
        private void ExecuteScript(string script, int versionId, int page, IEnumerable<SqlParameter> @params)
        {
            using (var cmd = new SqlProcedure { CommandText = script })
            {
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = versionId;
                cmd.Parameters.Add("@Page", SqlDbType.Int).Value = page;
                cmd.Parameters.AddRange(@params.ToArray());
                cmd.ExecuteNonQuery();
            }
        }
    }
}