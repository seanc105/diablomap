using System.Data;
using System.Linq;
using System.Collections.Generic;

namespace Database {
    public class TimelineEventNote : IDatabaseModel {
        public int TimelineEventId;
        public string Description;
        public int SourceId;
        public Source NoteSource;
        public bool Inconsistent;

        public IDatabaseModel Initialize(IDataReader reader) {
            TimelineEventNote item = new TimelineEventNote();
            
            item.TimelineEventId = (int)reader["timeline_event_id"];
            item.Description = (string)reader["description"];
            item.SourceId = (int)reader["source_id"];
            if (item.SourceId > 0) {
                item.NoteSource = Source.GetDocumentById(item.SourceId);
            }
            item.Inconsistent = (byte)reader["inconsistent"] > 0;

            return item;
        }

        public static List<TimelineEventNote> GetDocumentsByTimelineEventId(long id) {
            var results = DiabloDatabase.Select<TimelineEventNote>("timeline_event_notes", new string[]{"*"}, new Dictionary<string, object>(){{"timeline_event_id",id}});
            return results;
        }
    }
}