using Mono.Data.Sqlite;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace Database {
    public class DiabloDatabase
    {
        public static DiabloDatabase Instance;

        private static readonly string connectionString = $"URI=file:{UnityEngine.Application.dataPath}/Database/diablomap.db";
        private static IDbConnection dbConnection;


        private DiabloDatabase() {}

        ~DiabloDatabase() {
            if (dbConnection.State == ConnectionState.Open) {
                dbConnection.Close();
            }
        }

        private static void InitializeDatabase() {
            Instance = new DiabloDatabase();
            dbConnection = (IDbConnection) new SqliteConnection(connectionString);
            dbConnection.Open();
        }

        public static List<T> GetAllItems<T>(string tableName, int limit = 0) where T : IDatabaseModel {
            List<T> items = DiabloDatabase.Select<T>(tableName, new string[]{"*"});
            return items.GetRange(0, limit == 0 || limit > items.Count? items.Count : limit);
        }

        struct ResponseBodyStruct<T> {
            public List<T> Items;

            public ResponseBodyStruct(List<T> items) {
                this.Items = items;
            }
        }

        public static List<T> Select<T>(string tableName, string[] columns, Dictionary<string, object> whereClauses = null, List<object> groupBys = null) where T : IDatabaseModel {
            if (Instance == null) {
                InitializeDatabase();
            }
            if (columns.Length == 0) {
                return null;
            }

            List<IDatabaseModel> results = new List<IDatabaseModel>();
            List<IDbDataParameter> parameters = new List<IDbDataParameter>();
            IDbCommand command = dbConnection.CreateCommand();
            string selectColumns = "";
            if (columns.Length == 1 && columns[0] == "*") {
                selectColumns = "*";
            } else {
                foreach(string columnName in columns) {
                    selectColumns += $"@{columnName},";
                    IDbDataParameter parameter = command.CreateParameter();
                    parameter.ParameterName = $"@{columnName}";
                    parameter.Value = columnName;
                    parameters.Add(parameter);
                }
                selectColumns = selectColumns.Substring(0, selectColumns.Length - ",".Length);
            }
            string statement = $"SELECT {selectColumns} FROM {tableName}";
            if (whereClauses != null && whereClauses.Count > 0) {
                statement += " WHERE";
                foreach (KeyValuePair<string, object> keyValue in whereClauses) {
                    IDbDataParameter whereParam = command.CreateParameter();
                    whereParam.ParameterName = $"@{keyValue.Key}";
                    whereParam.Value = keyValue.Value;
                    parameters.Add(whereParam);
                    statement += $" {keyValue.Key} = @{keyValue.Key} AND";
                }
                statement = statement.Substring(0, statement.Length - " AND".Length);
            }

            if (groupBys != null && groupBys.Count > 0) {
                statement += " GROUP BY";
                foreach (object item in groupBys) {
                    statement += $" {item} AND";
                }
                statement = statement.Substring(0, statement.Length - " AND".Length);
            }
            command.CommandText = statement;
            parameters.ForEach(p => command.Parameters.Add(p));
            IDataReader reader = command.ExecuteReader();

            while (reader.Read()) {
                T item = System.Activator.CreateInstance<T>();
                results.Add(item.Initialize(reader));
            }

            // Need to run an explicit convert all on objects
            return results.ConvertAll<T>(new System.Converter<IDatabaseModel, T>((item) => (T)item));
        }
    }
}
