using System.Data;
using System.Collections.Generic;

namespace Database {
    public class Location : IDatabaseModel {
        public long Id;
        public string Name;
        public int? ClassificationTypeId;
        public ClassificationType ClassificationType;
        public int? MapLocationId;
        public MapLocation MapLocation;

        public IDatabaseModel Initialize(IDataReader reader) {
            Location item = new Location();
            
            item.Id = (long)reader["id"];
            item.Name = (string)reader["name"];
            if (reader["classification_type_id"] != System.DBNull.Value) {
                item.ClassificationTypeId = (int?)reader["classification_type_id"];
                item.ClassificationType = ClassificationType.GetDocumentById(item.ClassificationTypeId.Value);
            }
            if (reader["map_location_id"] != System.DBNull.Value) {
                item.MapLocationId = (int?)reader["map_location_id"];
                item.MapLocation = MapLocation.GetDocumentById(item.MapLocationId.Value);
            }

            return item;
        }

        public static Location GetDocumentByName(string name) {
            var results = DiabloDatabase.Select<Location>("locations", new string[]{"*"}, new Dictionary<string, object>(){{"name",name}});
            return results[0];
        }
        
        public static List<Location> GetDocumentsByMapLocationId(long id) {
            var results = DiabloDatabase.Select<Location>("locations", new string[]{"*"}, new Dictionary<string, object>(){{"map_location_id",id}});
            return results;
        }

        public static Location GetDocumentById(int id) {
            var results = DiabloDatabase.Select<Location>("locations", new string[]{"*"}, new Dictionary<string, object>(){{"id",id}});
            return results[0];
        }
    }
}