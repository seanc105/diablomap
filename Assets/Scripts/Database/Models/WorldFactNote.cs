using System.Data;
using System.Linq;
using System.Collections.Generic;

namespace Database {
    public class WorldFactNote : IDatabaseModel {
        public int WorldFactId;
        public string Description;
        public int SourceId;
        public Source NoteSource;
        public bool Inconsistent;

        public IDatabaseModel Initialize(IDataReader reader) {
            WorldFactNote item = new WorldFactNote();
            
            item.WorldFactId = (int)reader["world_fact_id"];
            item.Description = (string)reader["description"];
            item.SourceId = (int)reader["source_id"];
            item.NoteSource = Source.GetDocumentById(item.SourceId);
            item.Inconsistent = (byte)reader["inconsistent"] > 0;

            return item;
        }

        public static List<WorldFactNote> GetDocumentsByWorldFactId(long id) {
            var results = DiabloDatabase.Select<WorldFactNote>("world_fact_notes", new string[]{"*"}, new Dictionary<string, object>(){{"world_fact_id",id}});
            return results;
        }
    }
}