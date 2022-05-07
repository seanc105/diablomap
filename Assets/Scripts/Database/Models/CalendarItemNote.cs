using System.Data;
using System.Linq;
using System.Collections.Generic;

namespace Database {
    public class CalendarItemNote : IDatabaseModel {
        public int CalendarItemId;
        public string Description;
        public int SourceId;
        public Source NoteSource;
        public bool Inconsistent;

        public IDatabaseModel Initialize(IDataReader reader) {
            CalendarItemNote item = new CalendarItemNote();
            
            item.CalendarItemId = (int)reader["calendar_item_id"];
            item.Description = (string)reader["description"];
            item.SourceId = (int)reader["source_id"];
            if (item.SourceId > 0) {
                item.NoteSource = Source.GetDocumentById(item.SourceId);
            }
            item.Inconsistent = (byte)reader["inconsistent"] > 0;

            return item;
        }

        public static List<CalendarItemNote> GetDocumentsByCalendarItemId(long id) {
            var results = DiabloDatabase.Select<CalendarItemNote>("calendar_item_notes", new string[]{"*"}, new Dictionary<string, object>(){{"calendar_item_id",id}});
            return results;
        }
    }
}