using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("Scripts/ModalObjects/TimelineEvent")]
public class TimelineEvent : IInfoObject {
    private string _modalTextTemplate = 
        "<color=#FFF><b>Year:</b> <color=#0F0>{{year}}\n"+
        "<color=#FFF><b>Previous Event:</b> <color=#0F0>{{afterEventName}}\n"+
        "<color=#FFF><b>Event Name:</b> <color=#0F0>{{eventName}}  [<u><link=\"SourceId:{{sourceId}}\">{{sourceId}}</link></u>]\n"+
        "<color=#FFF><b><u>Information</u></b>\n"+
        "<color=#BBB>{{facts}}\n"+
        "<color=#FFF><b><u>Inconsistent Facts</u></b>\n"+
        "<color=#F00>{{inconsistencies}}\n";
    
    public string ModalTextTemplate { 
        private set => _modalTextTemplate = value; 
        get => _modalTextTemplate;
    }

    private Database.TimelineEvent _correspondingDatabaseItem;
    private List<Database.TimelineEventNote> _worldFactNotes;

    private static List<TimelineEvent> _worldFactsList = new List<TimelineEvent>();

    public TimelineEvent(Database.TimelineEvent respectiveDatabaseItem, List<Database.TimelineEventNote> notes) {
        _correspondingDatabaseItem = respectiveDatabaseItem;
        _worldFactNotes = notes;
        _worldFactsList.Add(this);
    }

    /// <summary>
    /// Updates the primary modal to display and show notes on this worldFact
    /// </summary>
    /// <param name="idYearLabel">The label for the event, in the format of: "id: Year" (i.e. "30: 1017")</param>
    /// <param name="linked">Whether this was linked to or not</param>
    public static void ShowTimelineEventNotes(string idYearLabel, bool linked = false) {
        if (!ViewController.Instance.IsDragging) {
            string[] splitLabel = idYearLabel.Split(':');
            int idResult;
            if (splitLabel.Length == 2 && int.TryParse(splitLabel[0], out idResult)) {
                TimelineEvent respectiveTimelineEvent = _worldFactsList.FirstOrDefault<TimelineEvent>(item => item._correspondingDatabaseItem.Id == idResult);
                // If this is the first time calling this object's notes, we need to retrieve
                // the info from the DB and fill this object's modal text content correctly
                if (respectiveTimelineEvent == null) {
                    respectiveTimelineEvent = GetDatabaseObject(idResult);
                    respectiveTimelineEvent.FillModalTextContent();
                }
                ViewController.Instance.ShowModal(respectiveTimelineEvent.ModalTextTemplate, respectiveTimelineEvent._correspondingDatabaseItem.EventName, linked);
            } else {
                throw new System.ArgumentException("idYearLabel must follow the format: \"id: year\"");
            }
        }
    }

    private static TimelineEvent GetDatabaseObject(int id) {
        return new TimelineEvent(
            Database.TimelineEvent.GetDocumentById(id),
            Database.TimelineEventNote.GetDocumentsByTimelineEventId(id)
        );
    }

    public void FillModalTextContent() {
        int year = _correspondingDatabaseItem.Year;
        string afterEventName = "";
        string facts = "", inconsistencies = "";

        string eventName = _correspondingDatabaseItem.EventName;

        foreach (Database.TimelineEventNote note in _worldFactNotes) {
            if (note.Inconsistent) {
                inconsistencies += $" - {note.Description} [<u><link=\"SourceId:{note.SourceId}\">{note.SourceId}</link></u>]\n";
            } else {
                facts += $" - {note.Description} " + (note.NoteSource == null? "\n" : $"[<u><link=\"SourceId:{note.SourceId}\">{note.SourceId}</link></u>]\n");
            }
        }

        if (_correspondingDatabaseItem.AfterEventId != null) {
            Database.TimelineEvent respectiveEvent = Database.TimelineEvent.GetDocumentById(_correspondingDatabaseItem.AfterEventId.Value);
            afterEventName = respectiveEvent.EventName;
        } else {
            afterEventName = "N/A";
        }

        facts = string.IsNullOrEmpty(facts)? "N/A\n" : facts;
        inconsistencies = string.IsNullOrEmpty(inconsistencies)? "N/A\n" : inconsistencies;
        
        ModalTextTemplate = ModalTextTemplate
            .Replace("{{year}}", year.ToString())
            .Replace("{{afterEventName}}", afterEventName)
            .Replace("{{eventName}}", eventName)
            .Replace("{{facts}}", facts)
            .Replace("{{sourceId}}", _correspondingDatabaseItem.SourceId.ToString())
            .Replace("{{inconsistencies}}", inconsistencies);
    }
}