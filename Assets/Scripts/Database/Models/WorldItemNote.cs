using System.Data;
using System.Linq;
using System.Collections.Generic;

namespace Database {
    public class WorldItemNote : IDatabaseModel {
        public int WorldItemId;
        public string Description;
        public int SourceId;
        public Source NoteSource;
        public bool Inconsistent;

        public IDatabaseModel Initialize(IDataReader reader) {
            WorldItemNote item = new WorldItemNote();
            
            item.WorldItemId = (int)reader["world_item_id"];
            item.Description = (string)reader["description"];
            item.SourceId = (int)reader["source_id"];
            item.NoteSource = Source.GetDocumentById(item.SourceId);
            item.Inconsistent = (byte)reader["inconsistent"] > 0;

            return item;
        }

        public static List<WorldItemNote> GetDocumentsByWorldItemId(long id) {
            var results = DiabloDatabase.Select<WorldItemNote>("world_item_notes", new string[]{"*"}, new Dictionary<string, object>(){{"world_item_id",id}});
            return results;
        }
    }
}