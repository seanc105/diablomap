using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("Scripts/ModalObjects/WorldFact")]
public class WorldFact : IInfoObject {
    private string _modalTextTemplate = 
        "<color=#FFF><b>Type:</b> <color=#0F0>{{type}}\n"+
        "<color=#FFF><b><u>Information</u></b>\n"+
        "<color=#BBB>{{facts}}\n"+
        "<color=#FFF><b><u>Inconsistent Facts</u></b>\n"+
        "<color=#F00>{{inconsistencies}}\n";
    
    public string ModalTextTemplate { 
        private set => _modalTextTemplate = value; 
        get => _modalTextTemplate;
    }

    private Database.WorldFact _correspondingDatabaseItem;
    private List<Database.WorldFactNote> _worldFactNotes;

    private static List<WorldFact> _worldFactsList = new List<WorldFact>();

    public WorldFact(Database.WorldFact respectiveDatabaseItem, List<Database.WorldFactNote> notes) {
        _correspondingDatabaseItem = respectiveDatabaseItem;
        _worldFactNotes = notes;
        _worldFactsList.Add(this);
    }

    /// <summary>
    /// Updates the primary modal to display and show notes on this worldFact
    /// </summary>
    /// <param name="worldFactName">The name of the worldFact to show</param>
    /// <param name="linked">Whether this was linked to or not</param>
    public static void ShowWorldFactNotes(string worldFactName, bool linked = false) {
        if (!ViewController.Instance.IsDragging) {
            WorldFact respectiveWorldFact = _worldFactsList.FirstOrDefault<WorldFact>(item => item._correspondingDatabaseItem.Name == worldFactName);
            // If this is the first time calling this object's notes, we need to retrieve
            // the info from the DB and fill this object's modal text content correctly
            if (respectiveWorldFact == null) {
                respectiveWorldFact = GetDatabaseObject(worldFactName);
                respectiveWorldFact.FillModalTextContent();
            }
            ViewController.Instance.ShowModal(respectiveWorldFact.ModalTextTemplate, respectiveWorldFact._correspondingDatabaseItem.Name, linked);
        }
    }

    private static WorldFact GetDatabaseObject(string worldFactName) {
        Database.WorldFact respectiveWorldFact = Database.WorldFact.GetDocumentByName(worldFactName);
        return new WorldFact(
            respectiveWorldFact,
            Database.WorldFactNote.GetDocumentsByWorldFactId(respectiveWorldFact.Id)
        );
    }

    public void FillModalTextContent() {
        string type = _correspondingDatabaseItem.ClassificationType.Label;
        string facts = "", inconsistencies = "";


        foreach (Database.WorldFactNote note in _worldFactNotes) {
            if (note.Inconsistent) {
                inconsistencies += $" - {note.Description} [<u><link=\"SourceId:{note.SourceId}\">{note.SourceId}</link></u>]\n";
            } else {
                facts += $" - {note.Description} [<u><link=\"SourceId:{note.SourceId}\">{note.SourceId}</link></u>]\n";
            }
        }

        facts = string.IsNullOrEmpty(facts)? "N/A\n" : facts;
        inconsistencies = string.IsNullOrEmpty(inconsistencies)? "N/A\n" : inconsistencies;
        
        ModalTextTemplate = ModalTextTemplate
            .Replace("{{type}}", type)
            .Replace("{{facts}}", facts)
            .Replace("{{inconsistencies}}", inconsistencies);
    }
}