using SenseNet.ContentRepository.Storage.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.ContentRepository.Storage.Data.SqlClient
{
    public class TextPropertyWriter
    {
        private int _versionId;
        private Dictionary<int, string> _values = new Dictionary<int,string>();

        public TextPropertyWriter(int versionId)
        {
            _versionId = versionId;
        }

        internal void Write(string value, PropertyType propertyType, bool isLoaded)
        {
            System.Diagnostics.Debug.Assert(isLoaded, "Text property must be loaded.");
            _values[propertyType.Id] = value;
        }

        internal void Execute()
        {
            if (_values.Count == 0)
                return;

            SqlProcedure cmd;
            try
            {
                var values = _values.ToArray();
                using (cmd = CreateDeleteCommand(values))
                    cmd.ExecuteNonQuery();

                using (cmd = CreateInsertCommand(values))
                    if (cmd != null)
                        cmd.ExecuteNonQuery();
            }
            catch(Exception e)
            {
                throw e;
            }
        }



        private SqlProcedure CreateDeleteCommand(KeyValuePair<int, string>[] values)
        {
            var sql = String.Format(DELETESQL, String.Join(", ", Enumerable.Range(0, _values.Count).Select(x => PROPERTYTYPE_PARAMPREFIX + x)));
            var cmd = new SqlProcedure { CommandText = sql, CommandType = CommandType.Text };

            cmd.Parameters.Add("@VersionId", SqlDbType.Int).Value = _versionId;

            for (int i = 0; i < values.Length; i++)
                cmd.Parameters.Add(PROPERTYTYPE_PARAMPREFIX + i, SqlDbType.Int).Value = values[i].Key;

            return cmd;
        }

        private SqlProcedure CreateInsertCommand(KeyValuePair<int, string>[] values)
        {
            var sql = new StringBuilder();
            var parameters = new List<SqlParameter>();

            var paramIndex = 0;
            foreach (var item in _values)
            {
                var value = item.Value;
                if (value == null)
                    continue;

                string tableName;
                if (!DataProvider.Current.IsCacheableText(value))
                {
                    tableName = "TextPropertiesNText";
                    parameters.Add(new SqlParameter(VALUE_PARAMPREFIX + paramIndex, SqlDbType.NText) { Value = value });
                }
                else
                {
                    tableName = "TextPropertiesNVarchar";
                    parameters.Add(new SqlParameter(VALUE_PARAMPREFIX + paramIndex, SqlDbType.NVarChar, SqlProvider.TextAlternationSizeLimit) { Value = value });
                }
                sql.AppendFormat(INSERTSQL, tableName, PROPERTYTYPE_PARAMPREFIX + paramIndex, VALUE_PARAMPREFIX + paramIndex).AppendLine();
                parameters.Add(new SqlParameter(PROPERTYTYPE_PARAMPREFIX + paramIndex, SqlDbType.Int) { Value = item.Key });
                paramIndex++;
            }

            if (sql.Length == 0)
                return null;

            parameters.Add(new SqlParameter("@VersionId", SqlDbType.Int) { Value = _versionId });

            var cmd = new SqlProcedure { CommandText = sql.ToString(), CommandType = CommandType.Text };
            cmd.Parameters.AddRange(parameters.ToArray());

            return cmd;
        }



        private const string PROPERTYTYPE_PARAMPREFIX = "@Prop";
        private const string VALUE_PARAMPREFIX = "@Val";

        private const string DELETESQL = @"
DELETE FROM TextPropertiesNText    WHERE (VersionId = @VersionId) AND (PropertyTypeId IN ({0}))
DELETE FROM TextPropertiesNVarchar WHERE (VersionId = @VersionId) AND (PropertyTypeId IN ({0}))
";
        private const string INSERTSQL = @"INSERT INTO {0} (VersionId, PropertyTypeId, Value) VALUES (@VersionId, {1}, {2})";
    }
}
