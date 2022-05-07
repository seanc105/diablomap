using System.Data;
using System.Collections.Generic;

namespace Database {
    public class CharacterTraveledLocation : IDatabaseModel {
        public int CharacterId;
        public Character Character;
        public int MapLocationId;
        public MapLocation MapLocation;
        public int OrderNum;
        public int? YearOfTravel;

        public IDatabaseModel Initialize(IDataReader reader) {
            CharacterTraveledLocation item = new CharacterTraveledLocation();
            
            item.CharacterId = (int)reader["character_id"];
            item.Character = Character.GetDocumentById(item.CharacterId);
            item.MapLocationId = (int)reader["map_location_id"];
            item.MapLocation = MapLocation.GetDocumentById(item.MapLocationId);
            item.OrderNum = (int)reader["order_num"];

            if (reader["year_of_travel"] != System.DBNull.Value) {
                item.YearOfTravel = (int?)reader["year_of_travel"];
            }

            return item;
        }

        public static List<CharacterTraveledLocation> GetDocumentsByCharacterId(long id) {
            var results = DiabloDatabase.Select<CharacterTraveledLocation>("character_traveled_locations", new string[]{"*"}, new Dictionary<string, object>(){{"character_id",id}});
            return results;
        }

        public static List<CharacterTraveledLocation> GetDocumentsByMapLocationId(long id) {
            var results = DiabloDatabase.Select<CharacterTraveledLocation>(
                "character_traveled_locations", 
                new string[]{"*"}, 
                new Dictionary<string, object>(){{"map_location_id",id}},
                new List<object>(){{"character_id"}}
            );
            return results;
        }

        public static List<CharacterTraveledLocation> GetDocumentsByYearOfTravel(int year) {
            var results = DiabloDatabase.Select<CharacterTraveledLocation>("character_traveled_locations", new string[]{"*"}, new Dictionary<string, object>(){{"year_of_travel",year}});
            return results;
        }

        public static List<CharacterTraveledLocation> GetFirstTraveledCharactersByMapLocationId(long id) {
            var results = DiabloDatabase.Select<CharacterTraveledLocation>("character_traveled_locations", new string[]{"*"}, new Dictionary<string, object>(){{"map_location_id",id}, {"order_num", 1}});
            return results;
        }
    }
}