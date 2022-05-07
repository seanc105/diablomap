using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using TMPro;

[AddComponentMenu("Scripts/ModalObjects/MapLocation")]
public class MapLocation : MonoBehaviour, IInfoObject {
    [Tooltip("Should never lerp in and out of showing")]
    public bool AlwaysDisplayTitle;

    [Tooltip("If this is a region location that has a city with the same name")]
    public bool RegionHasCityWithSameName;

    private string _modalTextTemplate = 
        "<color=#FFF><b>Type:</b> <color=#0F0>{{type}}\n"+
        "<color=#FFF><b>Location</b>: <color=#00FFFF>{{location}} [{{sourceId}}]\n\n"+
        "<color=#FFF><b><u>Information</u></b>\n"+
        "<color=#BBB>{{facts}}\n"+
        "<color=#FFF><b><u>Inconsistent Facts</u></b>\n"+
        "<color=#F00>{{inconsistencies}}\n"+
        "<color=#FFF><b><u>Places of Interest</u></b>\n"+
        "<color=#BBB>{{places_of_interest}}\n"+
        "<color=#FFF><b><u>Who's been here</u></b>\n"+
        "<color=#BBB>{{visitors}}\n"+
        "<color=#FFF><b><u>Who's from here or was first seen here</u></b>\n"+
        "<color=#BBB>{{residents}}";
    
    public string ModalTextTemplate { 
        private set => _modalTextTemplate = value; 
        get => _modalTextTemplate;
    }

    private RectTransform _rectTransform;
    public RectTransform LocationRectTransform {
        get => _rectTransform;
    }

    private Button _button;
    private TextMeshProUGUI _label;

    private Color _currentColor, _destinationColor;
    private float _alphaLerpTime = 0.0f;
    private bool _lerpingUp = false;

    private static readonly float _alphaLerpDuration = 0.5f;

    // Used to get the map locations on the screen, for character traveling stuff: <name,object>
    public static Dictionary<string, MapLocation> MapLocationIndex = new Dictionary<string, MapLocation>();

    private Database.MapLocation _correspondingDatabaseItem;
    private List<Database.MapLocationNote> _mapLocationNotes;
    private List<Database.CharacterTraveledLocation> _visitors;
    private List<Database.CharacterTraveledLocation> _residents;
    private List<Database.Location> _placesOfInterest;

    private static List<MapLocation> _mapLocationsList = new List<MapLocation>();

    private void Start() {
        _label = GetComponentInChildren<TextMeshProUGUI>();
        _label.text = name.Trim();
        _button = GetComponent<Button>();
        _rectTransform = GetComponent<RectTransform>();

        if (AlwaysDisplayTitle) {
            ShowMapLocationName();
        } else {
            _label.color = new Color(_label.color.r, _label.color.g, _label.color.b, 0.0f);
        }
        if (!RegionHasCityWithSameName) {
            MapLocationIndex.Add(name, this);
        }
        _mapLocationsList.Add(this);
    }

    /// <summary>
    /// Updates the primary modal to display and show notes on this Map Location
    /// </summary>
    /// <param name="linked">Whether this was linked to or not</param>
    public static void ShowMapLocationNotes(string itemName, bool linked = false) {
        if (!ViewController.Instance.IsDragging) {
            MapLocation respectiveMapLocation = _mapLocationsList.FirstOrDefault<MapLocation>(item => item.name == itemName);
            // If this is the first time calling this object's notes, we need to retrieve
            // the info from the DB and fill this object's modal text content correctly
            if (respectiveMapLocation._correspondingDatabaseItem == null) {
                respectiveMapLocation = GetDatabaseObject(itemName);
                respectiveMapLocation.FillModalTextContent();
            }
            ViewController.Instance.ShowModal(respectiveMapLocation.ModalTextTemplate, respectiveMapLocation._correspondingDatabaseItem.Name, linked);
        }
    }

    /// <summary>
    /// Updates the primary modal to display and show notes on this Map Location
    /// </summary>
    /// <param name="linked">Whether this was linked to or not</param>
    public void ShowMapLocationNotes(bool linked = false) {
        if (!ViewController.Instance.IsDragging) {
            // If this is the first time calling this object's notes, we need to retrieve
            // the info from the DB and fill this object's modal text content correctly
            if (_correspondingDatabaseItem == null) {
                GetDatabaseObject(this.name);
                FillModalTextContent();
            }
            ViewController.Instance.ShowModal(ModalTextTemplate, _correspondingDatabaseItem.Name, linked);
        }
    }

    public void ShowMapLocationName() {
        _alphaLerpTime = 0.0f;
        _currentColor = _label.color;
        _destinationColor = new Color(_label.color.r, _label.color.g, _label.color.b, 1.0f);
        _lerpingUp = true;
        StopCoroutine("LerpColor");
        StartCoroutine("LerpColor");
    }

    public void HideMapLocationName() {
        if (!AlwaysDisplayTitle && !ViewController.Instance.ShowAllLocationNames) {
            _alphaLerpTime = 0.0f;
            _currentColor = _label.color;
            _destinationColor = new Color(_label.color.r, _label.color.g, _label.color.b, 0.0f);
            _lerpingUp = false;
            StopCoroutine("LerpColor");
            StartCoroutine("LerpColor");
        }
    }

    private IEnumerator LerpColor() {
        while (_label.color != _destinationColor) {
            _label.color = Color.Lerp(_currentColor, _destinationColor, _alphaLerpTime / _alphaLerpDuration);
            _alphaLerpTime += Time.deltaTime;
            yield return null;
        }        
        _alphaLerpTime = 0.0f;
    }

    private static MapLocation GetDatabaseObject(string itemName) {
        MapLocation respectiveMapLocation = _mapLocationsList.FirstOrDefault(item => item.name == itemName);
        respectiveMapLocation._correspondingDatabaseItem = Database.MapLocation.GetDocumentByName(respectiveMapLocation.name, respectiveMapLocation.RegionHasCityWithSameName);
        respectiveMapLocation._mapLocationNotes = Database.MapLocationNote.GetDocumentsByMapLocationId(respectiveMapLocation._correspondingDatabaseItem.Id);
        respectiveMapLocation._visitors = Database.CharacterTraveledLocation.GetDocumentsByMapLocationId(respectiveMapLocation._correspondingDatabaseItem.Id);
        respectiveMapLocation._residents = Database.CharacterTraveledLocation.GetFirstTraveledCharactersByMapLocationId(respectiveMapLocation._correspondingDatabaseItem.Id);
        respectiveMapLocation._placesOfInterest = Database.Location.GetDocumentsByMapLocationId(respectiveMapLocation._correspondingDatabaseItem.Id);
        return respectiveMapLocation;
    }

    public void FillModalTextContent() {

        string type = _correspondingDatabaseItem.ClassificationType.Label;
        string location = _correspondingDatabaseItem.FromLocationDescription;
        int sourceId = _correspondingDatabaseItem.FromLocationSourceId ?? 0;

        string facts = "", inconsistencies = "";
        string visitors = "", residents = "", placesOfInterest = "";

        foreach (Database.MapLocationNote note in _mapLocationNotes) {
            if (note.Inconsistent) {
                inconsistencies += $" - {note.Description} [<u><link=\"SourceId:{note.SourceId}\">{note.SourceId}</link></u>]\n";
            } else {
                facts += $" - {note.Description} [<u><link=\"SourceId:{note.SourceId}\">{note.SourceId}</link></u>]\n";
            }
        }

        foreach (Database.CharacterTraveledLocation traveledLocation in _visitors) {
            visitors += $"<u><link=\"Character:{traveledLocation.Character.Name}\">{traveledLocation.Character.Name}</link></u>\n";
        }
        foreach (Database.CharacterTraveledLocation traveledLocation in _residents) {
            residents += $"<u><link=\"Character:{traveledLocation.Character.Name}\">{traveledLocation.Character.Name}</link></u>\n";
        }
        foreach (Database.Location placeOfInterest in _placesOfInterest) {
            placesOfInterest += $"<u><link=\"Location:{placeOfInterest.Name}\">{placeOfInterest.Name}</link></u>\n";
        }
        
        facts = string.IsNullOrEmpty(facts)? "N/A\n" : facts;
        inconsistencies = string.IsNullOrEmpty(inconsistencies)? "N/A\n" : inconsistencies;
        visitors = string.IsNullOrEmpty(visitors)? "N/A\n" : visitors;
        residents = string.IsNullOrEmpty(residents)? "N/A\n" : residents;
        placesOfInterest = string.IsNullOrEmpty(placesOfInterest)? "N/A\n" : placesOfInterest;

        ModalTextTemplate = ModalTextTemplate
            .Replace("{{type}}", type)
            .Replace("{{location}}", location)
            .Replace("{{sourceId}}", sourceId > 0 ? $"<u><link=\"SourceId:{sourceId.ToString()}\">{sourceId.ToString()}</link></u>" : "?")
            .Replace("{{facts}}", facts)
            .Replace("{{inconsistencies}}", inconsistencies)
            .Replace("{{places_of_interest}}", placesOfInterest)
            .Replace("{{visitors}}", visitors)
            .Replace("{{residents}}", residents);
    }
}
