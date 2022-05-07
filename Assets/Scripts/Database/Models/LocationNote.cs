using System.Data;
using System.Linq;
using System.Collections.Generic;

namespace Database {
    public class LocationNote : IDatabaseModel {
        public int LocationId;
        public string Description;
        public int SourceId;
        public Source NoteSource;
        public bool Inconsistent;

        public IDatabaseModel Initialize(IDataReader reader) {
            LocationNote item = new LocationNote();
            
            item.LocationId = (int)reader["location_id"];
            item.Description = (string)reader["description"];
            item.SourceId = (int)reader["source_id"];
            item.NoteSource = Source.GetDocumentById(item.SourceId);
            item.Inconsistent = (byte)reader["inconsistent"] > 0;

            return item;
        }

        public static List<LocationNote> GetDocumentsByLocationId(long id) {
            var results = DiabloDatabase.Select<LocationNote>("location_notes", new string[]{"*"}, new Dictionary<string, object>(){{"location_id",id}});
            return results;
        }
    }
}