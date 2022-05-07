using System.Data;
using System.Linq;
using System.Collections.Generic;

namespace Database {
    public class CreatureNote : IDatabaseModel {
        public int CreatureId;
        public string Description;
        public int SourceId;
        public Source NoteSource;
        public bool Inconsistent;

        public IDatabaseModel Initialize(IDataReader reader) {
            CreatureNote item = new CreatureNote();
            
            item.CreatureId = (int)reader["creature_id"];
            item.Description = (string)reader["description"];
            item.SourceId = (int)reader["source_id"];
            if (item.SourceId > 0) {
                item.NoteSource = Source.GetDocumentById(item.SourceId);
            }
            item.Inconsistent = (byte)reader["inconsistent"] > 0;

            return item;
        }

        public static List<CreatureNote> GetDocumentsByCreatureId(long id) {
            var results = DiabloDatabase.Select<CreatureNote>("creature_notes", new string[]{"*"}, new Dictionary<string, object>(){{"creature_id",id}});
            return results;
        }
    }
}