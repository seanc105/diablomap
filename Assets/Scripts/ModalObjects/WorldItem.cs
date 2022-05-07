using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("Scripts/ModalObjects/WorldItem")]
public class WorldItem : IInfoObject {
    private string _modalTextTemplate = 
        "<color=#FFF><b>Type:</b> <color=#0F0>{{type}}\n"+
        "<color=#FFF><b>From Location</b>: <color=#00FFFF>{{fromLocation}}\n"+
        "<color=#FFF><b><u>Information</u></b>\n"+
        "<color=#BBB>{{facts}}\n"+
        "<color=#FFF><b><u>Inconsistent Facts</u></b>\n"+
        "<color=#F00>{{inconsistencies}}\n";
    
    public string ModalTextTemplate { 
        private set => _modalTextTemplate = value; 
        get => _modalTextTemplate;
    }

    private Database.WorldItem _correspondingDatabaseItem;
    private List<Database.WorldItemNote> _worldItemNotes;

    private static List<WorldItem> _worldItemsList = new List<WorldItem>();

    public WorldItem(Database.WorldItem respectiveDatabaseItem, List<Database.WorldItemNote> notes) {
        _correspondingDatabaseItem = respectiveDatabaseItem;
        _worldItemNotes = notes;
        _worldItemsList.Add(this);
    }

    /// <summary>
    /// Updates the primary modal to display and show notes on this worldItem
    /// </summary>
    /// <param name="worldItemName">The name of the worldItem to show</param>
    /// <param name="linked">Whether this was linked to or not</param>
    public static void ShowWorldItemNotes(string worldItemName, bool linked = false) {
        if (!ViewController.Instance.IsDragging) {
            WorldItem respectiveWorldItem = _worldItemsList.FirstOrDefault<WorldItem>(item => item._correspondingDatabaseItem.Name == worldItemName);
            // If this is the first time calling this object's notes, we need to retrieve
            // the info from the DB and fill this object's modal text content correctly
            if (respectiveWorldItem == null) {
                respectiveWorldItem = GetDatabaseObject(worldItemName);
                respectiveWorldItem.FillModalTextContent();
            }
            ViewController.Instance.ShowModal(respectiveWorldItem.ModalTextTemplate, respectiveWorldItem._correspondingDatabaseItem.Name, linked);
        }
    }

    private static WorldItem GetDatabaseObject(string worldItemName) {
        Database.WorldItem respectiveWorldItem = Database.WorldItem.GetDocumentByName(worldItemName);
        return new WorldItem(
            respectiveWorldItem,
            Database.WorldItemNote.GetDocumentsByWorldItemId(respectiveWorldItem.Id)
        );
    }

    public void FillModalTextContent() {
        string type = _correspondingDatabaseItem.ClassificationType.Label;
        string fromLocation = "";
        string facts = "", inconsistencies = "";


        if (_correspondingDatabaseItem.FromLocation != null) {
            fromLocation = _correspondingDatabaseItem.FromLocation;
        } else {
            fromLocation = "N/A";  
        } 

        foreach (Database.WorldItemNote note in _worldItemNotes) {
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
            .Replace("{{fromLocation}}", fromLocation)
            .Replace("{{facts}}", facts)
            .Replace("{{inconsistencies}}", inconsistencies);
    }
}