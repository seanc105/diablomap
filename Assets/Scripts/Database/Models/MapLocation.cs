using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Database {
    public class MapLocation : IDatabaseModel {
        public long Id;
        public int? FirstActiveYear;
        public int? LastActiveYear;
        public string Name;
        public int ClassificationTypeId;
        public ClassificationType ClassificationType;
        public string FromLocationDescription;
        public int? FromLocationSourceId;
        public Source FromLocationSource;

        private static long _regionId = ClassificationType.GetDocumentByLabel("Region").Id;

        public IDatabaseModel Initialize(IDataReader reader) {
            MapLocation item = new MapLocation();
            
            item.Id = (long)reader["id"];
            if (reader["first_active_year"] != System.DBNull.Value) {
                item.FirstActiveYear = (int?)reader["first_active_year"];
            }
            if (reader["last_active_year"] != System.DBNull.Value) {
                item.LastActiveYear = (int?)reader["last_active_year"];
            }
            item.Name = (string)reader["name"];
            item.ClassificationTypeId = (int)reader["classification_type_id"];
            item.ClassificationType = ClassificationType.GetDocumentById(item.ClassificationTypeId);
            item.FromLocationDescription = (string)reader["from_location_description"];
            if (reader["from_location_source_id"] != System.DBNull.Value) {
                item.FromLocationSourceId = (int)reader["from_location_source_id"];
                item.FromLocationSource = Source.GetDocumentById(item.FromLocationSourceId.Value);
            }

            return item;
        }

        public static MapLocation GetDocumentByName(string name, bool regionWithCityName = false) {
            var results = DiabloDatabase.Select<MapLocation>("map_locations", new string[]{"*"}, new Dictionary<string, object>(){{"name",name}});
            if (regionWithCityName) {
                return results.FirstOrDefault<MapLocation>(
                    item => item.Name == name &&
                    item.ClassificationTypeId == _regionId
                );
            }
            return results[0];
        }
        public static MapLocation GetDocumentById(int id) {
            var results = DiabloDatabase.Select<MapLocation>("map_locations", new string[]{"*"}, new Dictionary<string, object>(){{"id",id}});
            return results[0];
        }
    }
}