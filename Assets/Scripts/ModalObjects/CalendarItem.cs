using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("Scripts/ModalObjects/CalendarItem")]
public class CalendarItem : IInfoObject {
    private string _modalTextTemplate = 
        "<color=#FFF><b>Type:</b> <color=#0F0>{{type}}\n"+
        "<color=#FFF><b>Comes after</b>: <color=#00FFFF>{{previousCalendarItem}}\n"+
        "<color=#FFF><b>Comes before</b>: <color=#00FFFF>{{nextCalendarItem}}\n"+
        "<color=#FFF><b><u>Information</u></b>\n"+
        "<color=#BBB>{{facts}}\n"+
        "<color=#FFF><b><u>Inconsistent Facts</u></b>\n"+
        "<color=#F00>{{inconsistencies}}\n";
    
    public string ModalTextTemplate { 
        private set => _modalTextTemplate = value; 
        get => _modalTextTemplate;
    }

    private Database.CalendarItem _correspondingDatabaseItem;
    private List<Database.CalendarItemNote> _calendaritemNotes;

    private static List<CalendarItem> _calendaritemsList = new List<CalendarItem>();

    public CalendarItem(Database.CalendarItem respectiveDatabaseItem, List<Database.CalendarItemNote> notes) {
        _correspondingDatabaseItem = respectiveDatabaseItem;
        _calendaritemNotes = notes;
        _calendaritemsList.Add(this);
    }

    /// <summary>
    /// Updates the primary modal to display and show notes on this calendaritem
    /// </summary>
    /// <param name="calendaritemName">The name of the calendaritem to show</param>
    /// <param name="linked">Whether this was linked to or not</param>
    public static void ShowCalendarItemNotes(string calendaritemName, bool linked = false) {
        if (!ViewController.Instance.IsDragging) {
            CalendarItem respectiveCalendarItem = _calendaritemsList.FirstOrDefault<CalendarItem>(item => item._correspondingDatabaseItem.Name == calendaritemName);
            // If this is the first time calling this object's notes, we need to retrieve
            // the info from the DB and fill this object's modal text content correctly
            if (respectiveCalendarItem == null) {
                respectiveCalendarItem = GetDatabaseObject(calendaritemName);
                respectiveCalendarItem.FillModalTextContent();
            }
            ViewController.Instance.ShowModal(respectiveCalendarItem.ModalTextTemplate, respectiveCalendarItem._correspondingDatabaseItem.Name, linked);
        }
    }

    private static CalendarItem GetDatabaseObject(string calendaritemName) {
        Database.CalendarItem respectiveCalendarItem = Database.CalendarItem.GetDocumentByName(calendaritemName);
        return new CalendarItem(
            respectiveCalendarItem,
            Database.CalendarItemNote.GetDocumentsByCalendarItemId(respectiveCalendarItem.Id)
        );
    }

    public void FillModalTextContent() {
        string type = _correspondingDatabaseItem.ClassificationType.Label;
        string previousCalendarItem = "", nextCalendarItem = "";
        string facts = "", inconsistencies = "";

        try {
            var item = Database.CalendarItem.GetDocumentById(_correspondingDatabaseItem.Number-1);
            previousCalendarItem = $"<u><link=\"CalendarItem:{item.Name}\">{item.Name}</link></u>";
        } catch {
            previousCalendarItem = "N/A";
        }
        try {
            var item = Database.CalendarItem.GetDocumentById(_correspondingDatabaseItem.Number+1);
            nextCalendarItem = $"<u><link=\"CalendarItem:{item.Name}\">{item.Name}</link></u>";
        } catch {
            nextCalendarItem = "N/A";
        }

        foreach (Database.CalendarItemNote note in _calendaritemNotes) {
            if (note.Inconsistent) {
                inconsistencies += $" - {note.Description} [<u><link=\"SourceId:{note.SourceId}\">{note.SourceId}</link></u>]\n";
            } else {
                facts += $" - {note.Description}";
                if (note.SourceId > 0) { 
                    facts += $"[<u><link=\"SourceId:{note.SourceId}\">{note.SourceId}</link></u>]\n";
                } else {
                    facts += "\n";
                }
            }
        }

        facts = string.IsNullOrEmpty(facts)? "N/A\n" : facts;
        inconsistencies = string.IsNullOrEmpty(inconsistencies)? "N/A\n" : inconsistencies;
        
        ModalTextTemplate = ModalTextTemplate
            .Replace("{{type}}", type)
            .Replace("{{previousCalendarItem}}", previousCalendarItem)
            .Replace("{{nextCalendarItem}}", nextCalendarItem)
            .Replace("{{facts}}", facts)
            .Replace("{{inconsistencies}}", inconsistencies);
    }
}