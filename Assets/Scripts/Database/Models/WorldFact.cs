using System.Data;
using System.Collections.Generic;

namespace Database {
    public class WorldFact : IDatabaseModel {
        public long Id;
        public string Name;
        public int? ClassificationTypeId;
        public ClassificationType ClassificationType;

        public IDatabaseModel Initialize(IDataReader reader) {
            WorldFact item = new WorldFact();
            
            item.Id = (long)reader["id"];
            item.Name = (string)reader["name"];
            if (reader["classification_type_id"] != System.DBNull.Value) {
                item.ClassificationTypeId = (int?)reader["classification_type_id"];
                item.ClassificationType = ClassificationType.GetDocumentById(item.ClassificationTypeId.Value);
            }

            return item;
        }

        public static WorldFact GetDocumentByName(string name) {
            var results = DiabloDatabase.Select<WorldFact>("world_facts", new string[]{"*"}, new Dictionary<string, object>(){{"name",name}});
            return results[0];
        }

        public static WorldFact GetDocumentById(int id) {
            var results = DiabloDatabase.Select<WorldFact>("world_facts", new string[]{"*"}, new Dictionary<string, object>(){{"id",id}});
            return results[0];
        }
    }
}