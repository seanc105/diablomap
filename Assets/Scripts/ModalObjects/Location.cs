using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("Scripts/ModalObjects/Location")]
public class Location : IInfoObject {
    private string _modalTextTemplate = 
        "<color=#FFF><b>Type:</b> <color=#0F0>{{type}}\n"+
        "<color=#FFF><b>Map Location</b>: <color=#00FFFF>{{mapLocation}}\n"+
        "<color=#FFF><b><u>Information</u></b>\n"+
        "<color=#BBB>{{facts}}\n"+
        "<color=#FFF><b><u>Inconsistent Facts</u></b>\n"+
        "<color=#F00>{{inconsistencies}}\n";
    
    public string ModalTextTemplate { 
        private set => _modalTextTemplate = value; 
        get => _modalTextTemplate;
    }

    private Database.Location _correspondingDatabaseItem;
    private List<Database.LocationNote> _locationNotes;

    private static List<Location> _locationsList = new List<Location>();

    public Location(Database.Location respectiveDatabaseItem, List<Database.LocationNote> notes) {
        _correspondingDatabaseItem = respectiveDatabaseItem;
        _locationNotes = notes;
        _locationsList.Add(this);
    }

    /// <summary>
    /// Updates the primary modal to display and show notes on this location
    /// </summary>
    /// <param name="locationName">The name of the location to show</param>
    /// <param name="linked">Whether this was linked to or not</param>
    public static void ShowLocationNotes(string locationName, bool linked = false) {
        if (!ViewController.Instance.IsDragging) {
            Location respectiveLocation = _locationsList.FirstOrDefault<Location>(item => item._correspondingDatabaseItem.Name == locationName);
            // If this is the first time calling this object's notes, we need to retrieve
            // the info from the DB and fill this object's modal text content correctly
            if (respectiveLocation == null) {
                respectiveLocation = GetDatabaseObject(locationName);
                respectiveLocation.FillModalTextContent();
            }
            ViewController.Instance.ShowModal(respectiveLocation.ModalTextTemplate, respectiveLocation._correspondingDatabaseItem.Name, linked);
        }
    }

    private static Location GetDatabaseObject(string locationName) {
        Database.Location respectiveLocation = Database.Location.GetDocumentByName(locationName);
        return new Location(
            respectiveLocation,
            Database.LocationNote.GetDocumentsByLocationId(respectiveLocation.Id)
        );
    }

    public void FillModalTextContent() {
        string type = _correspondingDatabaseItem.ClassificationType.Label;
        string mapLocation = "";
        long locationId = _correspondingDatabaseItem.Id;
        string facts = "", inconsistencies = "";


        if (_correspondingDatabaseItem.MapLocation != null) {
            mapLocation = $"<u><link=\"MapLocation:{_correspondingDatabaseItem.MapLocation.Name}\">{_correspondingDatabaseItem.MapLocation.Name}</link></u>";
        } else {
            mapLocation = "N/A";  
        } 

        foreach (Database.LocationNote note in _locationNotes) {
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
            .Replace("{{mapLocation}}", mapLocation)
            .Replace("{{locationId}}", locationId.ToString())
            .Replace("{{facts}}", facts)
            .Replace("{{inconsistencies}}", inconsistencies);
    }
}