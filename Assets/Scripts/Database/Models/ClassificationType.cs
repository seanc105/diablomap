using System.Collections.Generic;
using System.Data;

namespace Database {
    public class ClassificationType : IDatabaseModel {
        public long Id;
        public string Label;

        public IDatabaseModel Initialize(IDataReader reader) {
            ClassificationType item = new ClassificationType();
            
            item.Id = (long)reader["id"];
            item.Label = (string)reader["label"];

            return item;
        }

        public static ClassificationType GetDocumentByLabel(string label) {
            var results = DiabloDatabase.Select<ClassificationType>("classification_types", new string[]{"*"}, new Dictionary<string, object>(){{"label",label}});
            return results[0];
        }
        public static ClassificationType GetDocumentById(int id) {
            var results = DiabloDatabase.Select<ClassificationType>("classification_types", new string[]{"*"}, new Dictionary<string, object>(){{"id",id}});
            return results[0];
        }
    }
}