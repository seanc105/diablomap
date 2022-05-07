using System.Data;
using System.Linq;
using System.Collections.Generic;

namespace Database {
    public class CharacterNote : IDatabaseModel {
        public int CharacterId;
        public string Description;
        public int SourceId;
        public Source NoteSource;
        public bool Inconsistent;

        public IDatabaseModel Initialize(IDataReader reader) {
            CharacterNote item = new CharacterNote();
            
            item.CharacterId = (int)reader["character_id"];
            item.Description = (string)reader["description"];
            item.SourceId = (int)reader["source_id"];
            item.NoteSource = Source.GetDocumentById(item.SourceId);
            item.Inconsistent = (byte)reader["inconsistent"] > 0;

            return item;
        }

        public static List<CharacterNote> GetDocumentsByCharacterId(long id) {
            var results = DiabloDatabase.Select<CharacterNote>("character_notes", new string[]{"*"}, new Dictionary<string, object>(){{"character_id",id}});
            return results;
        }
    }
}