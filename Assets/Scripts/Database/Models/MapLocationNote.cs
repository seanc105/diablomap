using System.Data;
using System.Linq;
using System.Collections.Generic;

namespace Database {
    public class MapLocationNote : IDatabaseModel {
        public int MapLocationId;
        public string Description;
        public int SourceId;
        public Source NoteSource;
        public bool Inconsistent;

        public IDatabaseModel Initialize(IDataReader reader) {
            MapLocationNote item = new MapLocationNote();
            
            item.MapLocationId = (int)reader["map_location_id"];
            item.Description = (string)reader["description"];
            item.SourceId = (int)reader["source_id"];
            item.NoteSource = Source.GetDocumentById(item.SourceId);
            item.Inconsistent = (byte)reader["inconsistent"] > 0;

            return item;
        }

        public static List<MapLocationNote> GetDocumentsByMapLocationId(long id) {
            var results = DiabloDatabase.Select<MapLocationNote>("map_location_notes", new string[]{"*"}, new Dictionary<string, object>(){{"map_location_id",id}});
            return results;
        }
    }
}