using System.Data;
using System.Collections.Generic;

namespace Database {
    public class WorldItem : IDatabaseModel {
        public long Id;
        public string Name;
        public int? ClassificationTypeId;
        public ClassificationType ClassificationType;
        public string FromLocation;

        public IDatabaseModel Initialize(IDataReader reader) {
            WorldItem item = new WorldItem();
            
            item.Id = (long)reader["id"];
            item.Name = (string)reader["name"];
            if (reader["classification_type_id"] != System.DBNull.Value) {
                item.ClassificationTypeId = (int?)reader["classification_type_id"];
                item.ClassificationType = ClassificationType.GetDocumentById(item.ClassificationTypeId.Value);
            }
            if (reader["from_location"] != System.DBNull.Value) {
                item.FromLocation = (string)reader["from_location"];
            }

            return item;
        }

        public static WorldItem GetDocumentByName(string name) {
            var results = DiabloDatabase.Select<WorldItem>("world_items", new string[]{"*"}, new Dictionary<string, object>(){{"name",name}});
            return results[0];
        }

        public static WorldItem GetDocumentById(int id) {
            var results = DiabloDatabase.Select<WorldItem>("world_items", new string[]{"*"}, new Dictionary<string, object>(){{"id",id}});
            return results[0];
        }
    }
}