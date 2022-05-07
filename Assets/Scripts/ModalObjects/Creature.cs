using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("Scripts/ModalObjects/Creature")]
public class Creature : IInfoObject {
    private string _modalTextTemplate = 
        "<color=#FFF><b>Type:</b> <color=#0F0>{{type}}\n"+
        "<color=#FFF><b>Parent Species</b>: <color=#00FFFF>{{parentSpecies}}\n"+
        "<color=#FFF><b><u>Information</u></b>\n"+
        "<color=#BBB>{{facts}}\n"+
        "<color=#FFF><b><u>Inconsistent Facts</u></b>\n"+
        "<color=#F00>{{inconsistencies}}\n";
    
    public string ModalTextTemplate { 
        private set => _modalTextTemplate = value; 
        get => _modalTextTemplate;
    }

    private Database.Creature _correspondingDatabaseItem;
    private List<Database.CreatureNote> _creatureNotes;

    private static List<Creature> _creaturesList = new List<Creature>();

    public Creature(Database.Creature respectiveDatabaseItem, List<Database.CreatureNote> notes) {
        _correspondingDatabaseItem = respectiveDatabaseItem;
        _creatureNotes = notes;
        _creaturesList.Add(this);
    }

    /// <summary>
    /// Updates the primary modal to display and show notes on this creature
    /// </summary>
    /// <param name="creatureName">The name of the creature to show</param>
    /// <param name="linked">Whether this was linked to or not</param>
    public static void ShowCreatureNotes(string creatureName, bool linked = false) {
        if (!ViewController.Instance.IsDragging) {
            Creature respectiveCreature = _creaturesList.FirstOrDefault<Creature>(item => item._correspondingDatabaseItem.Name == creatureName);
            // If this is the first time calling this object's notes, we need to retrieve
            // the info from the DB and fill this object's modal text content correctly
            if (respectiveCreature == null) {
                respectiveCreature = GetDatabaseObject(creatureName);
                respectiveCreature.FillModalTextContent();
            }
            ViewController.Instance.ShowModal(respectiveCreature.ModalTextTemplate, respectiveCreature._correspondingDatabaseItem.Name, linked);
        }
    }

    private static Creature GetDatabaseObject(string creatureName) {
        Database.Creature respectiveCreature = Database.Creature.GetDocumentByName(creatureName);
        return new Creature(
            respectiveCreature,
            Database.CreatureNote.GetDocumentsByCreatureId(respectiveCreature.Id)
        );
    }

    public void FillModalTextContent() {
        string type = _correspondingDatabaseItem.ClassificationType.Label;
        string parentSpecies = "";
        string facts = "", inconsistencies = "";

        if (_correspondingDatabaseItem.ParentSpecies != null) {
            parentSpecies = $"<u><link=\"Creature:{_correspondingDatabaseItem.ParentSpecies.Name}\">{_correspondingDatabaseItem.ParentSpecies.Name}</link></u>";
        } else {
            parentSpecies = "N/A";
        }

        foreach (Database.CreatureNote note in _creatureNotes) {
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
            .Replace("{{parentSpecies}}", parentSpecies)
            .Replace("{{facts}}", facts)
            .Replace("{{inconsistencies}}", inconsistencies);
    }
}