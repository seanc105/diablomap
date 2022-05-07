using System.Collections.Generic;
using System.Data;

namespace Database {
    public class CharacterClass : IDatabaseModel {
        public long Id;
        public string Label;

        public IDatabaseModel Initialize(IDataReader reader) {
            CharacterClass item = new CharacterClass();
            
            item.Id = (long)reader["id"];
            item.Label = (string)reader["label"];

            return item;
        }
        
        public static CharacterClass GetDocumentById(int id) {
            var results = DiabloDatabase.Select<CharacterClass>("character_classes", new string[]{"*"}, new Dictionary<string, object>(){{"id",id}});
            return results[0];
        }
    }
}