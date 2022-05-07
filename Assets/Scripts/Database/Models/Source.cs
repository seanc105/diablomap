using System.Collections.Generic;
using System.Data;

namespace Database {
    public class Source : IDatabaseModel {
        public long Id;
        public string Name;

        public IDatabaseModel Initialize(IDataReader reader) {
            Source item = new Source();
            
            item.Id = (long)reader["id"];
            item.Name = (string)reader["name"];

            return item;
        }

        public static Source GetDocumentById(int id) {
            var results = DiabloDatabase.Select<Source>("sources", new string[]{"*"}, new Dictionary<string, object>(){{"id",id}});
            return results[0];
        }
    }
}