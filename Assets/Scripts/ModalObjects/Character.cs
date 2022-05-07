using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[AddComponentMenu("Scripts/ModalObjects/Character")]
public class Character : IInfoObject {
    private string _modalTextTemplate = 
        "<color=#FA0>[<u><link=\"Action:CharacterPath_{{characterId}}\">See Character Path!</link></u>]\n\n"+
        "<color=#FFF><b>Class:</b> <color=#0F0>{{class}}\n"+
        "<color=#FFF><b>Alternative Names</b>: <color=#00FFFF>{{altNames}}\n"+
        "<color=#FFF><b>Deceased</b>: <color=#00FFFF>{{deceased}}\n"+
        "<color=#FFF><b>Birth Year</b>: <color=#00FFFF>{{birthYear}}\n"+
        "<color=#FFF><b>Cause of Death</b>: <color=#00FFFF>{{causeOfDeath}}\n\n"+
        "<color=#FFF><b><u>Information</u></b>\n"+
        "<color=#BBB>{{facts}}\n"+
        "<color=#FFF><b><u>Inconsistent Facts</u></b>\n"+
        "<color=#F00>{{inconsistencies}}\n"+
        "<color=#FFF><b><u>Known Places Visited (est. year)</u></b>\n"+
        "<color=#BBB>{{placesVisited}}";
    
    public string ModalTextTemplate { 
        private set => _modalTextTemplate = value; 
        get => _modalTextTemplate;
    }

    private Database.Character _correspondingDatabaseItem;
    private List<Database.CharacterNote> _characterNotes;
    private List<Database.CharacterTraveledLocation> _locationsVisited;

    private static List<Character> _charactersList = new List<Character>();

    public Character(Database.Character respectiveDatabaseItem, List<Database.CharacterNote> notes, List<Database.CharacterTraveledLocation> locationsVisited) {
        _correspondingDatabaseItem = respectiveDatabaseItem;
        _characterNotes = notes;
        _locationsVisited = locationsVisited;
        _charactersList.Add(this);
    }

    /// <summary>
    /// Updates the primary modal to display and show notes on this character
    /// </summary>
    /// <param name="characterName">The name of the character to show</param>
    /// <param name="linked">Whether this was linked to or not</param>
    public static void ShowCharacterNotes(string characterName, bool linked = false) {
        if (!ViewController.Instance.IsDragging) {
            Character respectiveCharacter = _charactersList.FirstOrDefault<Character>(item => item._correspondingDatabaseItem.Name == characterName);
            // If this is the first time calling this object's notes, we need to retrieve
            // the info from the DB and fill this object's modal text content correctly
            if (respectiveCharacter == null) {
                respectiveCharacter = GetDatabaseObject(characterName);
                respectiveCharacter.FillModalTextContent();
            }
            ViewController.Instance.ShowModal(respectiveCharacter.ModalTextTemplate, respectiveCharacter._correspondingDatabaseItem.Name, linked);
        }
    }

    private static Character GetDatabaseObject(string characterName) {
        Database.Character respectiveCharacter = Database.Character.GetDocumentByName(characterName);
        return new Character(
            respectiveCharacter,
            Database.CharacterNote.GetDocumentsByCharacterId(respectiveCharacter.Id),
            Database.CharacterTraveledLocation.GetDocumentsByCharacterId(respectiveCharacter.Id)
        );
    }

    public void FillModalTextContent() {
        long characterId = _correspondingDatabaseItem.Id;
        string characterClass = _correspondingDatabaseItem.CharClass.Label;
        string altNames = _correspondingDatabaseItem.AltNames;
        string deceased = "N/A";
        if (_correspondingDatabaseItem.Deceased.HasValue) {
            if (_correspondingDatabaseItem.Deceased.Value == true) {
                deceased = "Yes";
            } else {
                deceased = "Unknown";
            }
        }
        string birthYear = "Unknown or N/A";
        if (_correspondingDatabaseItem.BirthYear.HasValue) {
            birthYear = _correspondingDatabaseItem.BirthYear.Value.ToString();
        }
        string causeOfDeath = _correspondingDatabaseItem.CauseOfDeath;
        string facts = "", inconsistencies = "";
        string placesVisited = "";

        foreach (Database.CharacterNote note in _characterNotes) {
            if (note.Inconsistent) {
                inconsistencies += $" - {note.Description} [<u><link=\"SourceId:{note.SourceId}\">{note.SourceId}</link></u>]\n";
            } else {
                facts += $" - {note.Description} [<u><link=\"SourceId:{note.SourceId}\">{note.SourceId}</link></u>]\n";
            }
        }

        foreach (Database.CharacterTraveledLocation traveledLocation in _locationsVisited) {
            placesVisited += $"<u><link=\"MapLocation:{traveledLocation.MapLocation.Name}\">{traveledLocation.MapLocation.Name}</link></u> ({(traveledLocation.YearOfTravel.HasValue? traveledLocation.YearOfTravel.Value.ToString() : "?")}) \n";
        }
        
        altNames = string.IsNullOrEmpty(altNames)? "N/A" : altNames;
        facts = string.IsNullOrEmpty(facts)? "N/A\n" : facts;
        inconsistencies = string.IsNullOrEmpty(inconsistencies)? "N/A\n" : inconsistencies;
        placesVisited = string.IsNullOrEmpty(placesVisited)? "N/A\n" : placesVisited;
        
        ModalTextTemplate = ModalTextTemplate
            .Replace("{{class}}", characterClass)
            .Replace("{{characterId}}", characterId.ToString())
            .Replace("{{altNames}}", altNames)
            .Replace("{{deceased}}", deceased)
            .Replace("{{birthYear}}", birthYear)
            .Replace("{{causeOfDeath}}", causeOfDeath)
            .Replace("{{facts}}", facts)
            .Replace("{{inconsistencies}}", inconsistencies)
            .Replace("{{placesVisited}}", placesVisited);
    }

    /// <summary>
    /// Draw the character's path on the map
    /// </summary>
    /// <param name="characterId">The character id to reference</param>
    /// <returns>A list of Vector3 positions the player traveled to, in order</returns>
    public static List<MapLocation> GetCharacterPath(int characterId) {
        Character respectiveCharacter = _charactersList.FirstOrDefault(c => c._correspondingDatabaseItem.Id == characterId);
        List<MapLocation> mapPositions = new List<MapLocation>();
        if (respectiveCharacter != null) {
            foreach (Database.CharacterTraveledLocation traveledLocation in respectiveCharacter._locationsVisited) {
                MapLocation respectiveMapObject;
                if (MapLocation.MapLocationIndex.TryGetValue(traveledLocation.MapLocation.Name, out respectiveMapObject)) {
                    mapPositions.Add(respectiveMapObject);
                }
            }
        }
        return mapPositions;
    }
}