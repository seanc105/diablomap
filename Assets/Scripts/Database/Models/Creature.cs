using System.Data;
using System.Collections.Generic;

namespace Database {
    public class Creature : IDatabaseModel {
        public long Id;
        public string Name;
        public int? ClassificationTypeId;
        public ClassificationType ClassificationType;
        public int? ParentSpeciesId;
        public Creature ParentSpecies;

        public IDatabaseModel Initialize(IDataReader reader) {
            Creature item = new Creature();
            
            item.Id = (long)reader["id"];
            item.Name = (string)reader["name"];
            if (reader["classification_type_id"] != System.DBNull.Value) {
                item.ClassificationTypeId = (int?)reader["classification_type_id"];
                item.ClassificationType = ClassificationType.GetDocumentById(item.ClassificationTypeId.Value);
            }
            if (reader["parent_species_id"] != System.DBNull.Value) {
                item.ParentSpeciesId = (int?)reader["parent_species_id"];
                item.ParentSpecies = Creature.GetDocumentById(item.ParentSpeciesId.Value);
            }

            return item;
        }

        public static Creature GetDocumentByName(string name) {
            var results = DiabloDatabase.Select<Creature>("creatures", new string[]{"*"}, new Dictionary<string, object>(){{"name",name}});
            return results[0];
        }
        
        public static List<Creature> GetDocumentsByParentSpeciesId(long id) {
            var results = DiabloDatabase.Select<Creature>("creatures", new string[]{"*"}, new Dictionary<string, object>(){{"parent_species_id",id}});
            return results;
        }

        public static Creature GetDocumentById(int id) {
            var results = DiabloDatabase.Select<Creature>("creatures", new string[]{"*"}, new Dictionary<string, object>(){{"id",id}});
            return results[0];
        }
    }
}