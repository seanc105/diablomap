using System.Data;
using System.Collections.Generic;

namespace Database {
    public class TimelineEvent : IDatabaseModel {
        public long Id;
        public int Year;
        public int? AfterEventId;
        public string EventName;
        public int SourceId;
        public Source EventSource;


        public IDatabaseModel Initialize(IDataReader reader) {
            TimelineEvent item = new TimelineEvent();
            
            item.Id = (long)reader["id"];
            item.Year = (int)reader["year"];
            if (reader["after_event_id"] != System.DBNull.Value) {
                item.AfterEventId = (int?)reader["after_event_id"];
            }            
            item.EventName = (string)reader["event_name"];
            item.SourceId = (int)reader["source_id"];
            item.EventSource = Source.GetDocumentById(item.SourceId);

            return item;
        }

        public static TimelineEvent GetDocumentById(int id) {
            var results = DiabloDatabase.Select<TimelineEvent>("timeline_events", new string[]{"*"}, new Dictionary<string, object>(){{"id",id}});
            return results[0];
        }

        public static TimelineEvent GetDocumentByAfterEventId(int id) {
            var results = DiabloDatabase.Select<TimelineEvent>("timeline_events", new string[]{"*"}, new Dictionary<string, object>(){{"after_event_id",id}});
            return results[0];
        }
    }
}