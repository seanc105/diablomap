using System.Data;
using System.Collections.Generic;

namespace Database {
    public class Character_OLD : IDatabaseModel {
        public long Id;
        public string Name;
        public string AltNames;
        public int CharacterClassId;
        public CharacterClass CharClass;
        public bool? Deceased; // true = Yes, false = Unknown, null = Not Applicable -- unknown could mean they might be deceased, but it was never clarified or if they ran from a disaster, etc.
        public int? BirthYear; // null = unknown or N/A
        public string CauseOfDeath;


        public IDatabaseModel Initialize(IDataReader reader) {
            Character item = new Character();
            
            item.Id = (long)reader["id"];
            item.Name = (string)reader["name"];
            if (reader["alt_names"] != System.DBNull.Value) {
                item.AltNames = (string)reader["alt_names"];
            }
            item.CharacterClassId = (int)reader["character_class_id"];
            item.CharClass = CharacterClass.GetDocumentById(item.CharacterClassId);
            // UnityEngine.Debug.Log(item.Name);
            if (reader["deceased"] != System.DBNull.Value) {
                item.Deceased = (byte?)reader["deceased"] > 0;
            }
            if (reader["birth_year"] != System.DBNull.Value) {
                item.BirthYear = (int?)reader["birth_year"];
            }
            if (reader["cause_of_death"] != System.DBNull.Value) {
                item.CauseOfDeath = (string)reader["cause_of_death"];
            }

            return item;
        }
        
        public static Character GetDocumentById(int id) {
            var results = DiabloDatabase.Select<Character>("characters", new string[]{"*"}, new Dictionary<string, object>(){{"id",id}});
            return results[0];
        }
        
        public static Character GetDocumentByName(string name) {
            var results = DiabloDatabase.Select<Character>("characters", new string[]{"*"}, new Dictionary<string, object>(){{"name",name}});
            return results[0];
        }

        public static List<Character> GetDocumentsByCharacterClassId(int id) {
            return DiabloDatabase.Select<Character>("characters", new string[]{"*"}, new Dictionary<string, object>(){{"id",id}});
        }
    }
}