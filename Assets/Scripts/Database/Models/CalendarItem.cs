using System.Data;
using System.Collections.Generic;

namespace Database {
    public class CalendarItem : IDatabaseModel {
        public long Id;
        public string Name;
        public int? ClassificationTypeId;
        public ClassificationType ClassificationType;
        public int Number;

        public IDatabaseModel Initialize(IDataReader reader) {
            CalendarItem item = new CalendarItem();
            
            item.Id = (long)reader["id"];
            item.Name = (string)reader["name"];
            if (reader["classification_type_id"] != System.DBNull.Value) {
                item.ClassificationTypeId = (int?)reader["classification_type_id"];
                item.ClassificationType = ClassificationType.GetDocumentById(item.ClassificationTypeId.Value);
            }
            item.Number = (int)reader["number"];

            return item;
        }

        public static CalendarItem GetDocumentByName(string name) {
            var results = DiabloDatabase.Select<CalendarItem>("calendar_items", new string[]{"*"}, new Dictionary<string, object>(){{"name",name}});
            return results[0];
        }
        
        public static CalendarItem GetDocumentById(int id) {
            var results = DiabloDatabase.Select<CalendarItem>("calendar_items", new string[]{"*"}, new Dictionary<string, object>(){{"id",id}});
            return results[0];
        }
    }
}