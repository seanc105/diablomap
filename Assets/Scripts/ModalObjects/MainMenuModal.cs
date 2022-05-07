using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Scripts/ModalObjects/MainMenuModal")]
public class MainMenuModal : MonoBehaviour, IInfoObject {
    private string _modalTextTemplate = "";
    
    public string ModalTextTemplate { 
        private set => _modalTextTemplate = value; 
        get => _modalTextTemplate;
    }

    [Tooltip("The list of buttons for the primary menu")]
    public GameObject MenuButtonList;
    [Tooltip("The prefab for the buttons in the menu")]
    public PoolObject ButtonPrefab;

    private enum MenuSection {
        Main,
        Calendar,
        Characters,
        Classes,
        Creatures,
        Locations,
        MapLocations,
        Sources,
        TimelineEvents,
        WorldFacts,
        WorldItems,
        AboutMe,
        Exit,
        OpenViewModal
    }
    
    private Dictionary<MenuSection, Dictionary<string, MenuSection>> _menuSectionLists = new Dictionary<MenuSection, Dictionary<string, MenuSection>>();
    private Stack<MenuSection> _menuSectionStack = new Stack<MenuSection>();

    private float _menuButtonHeight;
    private RectTransform _buttonListRectTransform;
    private Button _backButton;
    private RectTransform _scrollViewRectTransform;
    private ScrollRect _scrollbarRect;
    private VerticalLayoutGroup _verticalLayoutGroup;

    private int _pageNumber = 1;
    private bool _initialized = false;
    private static readonly float ScrollPageLoadThreshold = 0.01f;

    // These are needed to force refresh the scrollview between menu transitions since Unity doesn't automatically redraw them
    // and ForceUpdateRectTransforms doesn't work
    private static readonly float PseudoEpsilon = 0.001f;

    private static readonly int EntriesPerPage = 50;
    private static int EpsilonDirection = -1;
    private TextMeshProUGUI _modalText;

    private static readonly MenuSection[] scrollableMenus = new MenuSection[] {
        MenuSection.Characters,
        MenuSection.Creatures,
        MenuSection.Locations,
        MenuSection.MapLocations,
        MenuSection.TimelineEvents,
        MenuSection.WorldItems,
        MenuSection.WorldFacts
    };

    private void Start() {
        _menuButtonHeight = ButtonPrefab.GetComponent<RectTransform>().rect.height;
        _buttonListRectTransform = MenuButtonList.GetComponent<RectTransform>();
        _backButton = GetComponentsInChildren<Button>().FirstOrDefault(item => item.name == "BackButton");
        _scrollViewRectTransform = GetComponentsInChildren<ScrollRect>().FirstOrDefault(item => item.name == "Scroll View").GetComponent<RectTransform>();
        _scrollbarRect = GetComponentInChildren<ScrollRect>();
        _modalText = GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(item => item.name == "ModalText");
        _verticalLayoutGroup = GetComponentsInChildren<VerticalLayoutGroup>().FirstOrDefault();
        ResetMenus();
        _initialized = true;
    }

    private void OnEnable() {
        if (_initialized) {
            ResetMenus();
        }
    }

    private void OnDisable() {
        _menuSectionStack.Clear();
        _pageNumber = 1;
        _scrollbarRect.verticalNormalizedPosition = 1.0f;
    }

    private void Update() {
        if (_menuSectionStack.Count > 0 && 
            _menuSectionLists[_menuSectionStack.Peek()].Count > 0) {
            if (scrollableMenus.Contains(_menuSectionStack.Peek()) &&
                _scrollbarRect.verticalNormalizedPosition <= ScrollPageLoadThreshold && 
                _pageNumber > 0)
            {
                    if (CreateMenu(_menuSectionStack.Peek(), ++_pageNumber)) {
                        DrawMenu(_menuSectionStack.Peek());
                        StartCoroutine(ResetScrollbar());
                    } else {
                        // _pageNumber goes to -1 when we've hit the final page of a multipaged menu
                        _pageNumber = -1;
                    }
            }
        }
    }

    /// <summary>
    /// Creates a menu of the given type and page number
    /// </summary>
    /// <param name="menuSection">The respective section</param>
    /// <param name="pageNumber">The page number to start from. -1 means don't render ANY, just create the submenu dictionary</param>
    /// <returns></returns>
    private bool CreateMenu(MenuSection menuSection, int pageNumber = 1) {
        if (!_menuSectionLists.ContainsKey(menuSection)) {
            _menuSectionLists.Add(menuSection, new Dictionary<string, MenuSection>());
        }

        // This implies to not add items yet
        if (pageNumber == -1) {
            return false;
        }

        Dictionary<string, MenuSection> menuItemDictionary = _menuSectionLists[menuSection];
        System.Func<Dictionary<string, MenuSection>, List<Database.IDatabaseModel>, bool> createFunc;
        List<Database.IDatabaseModel> newItems;

        switch (menuSection) {
            case MenuSection.AboutMe:
                RenderAboutMe();
                return false;
            case MenuSection.Calendar:
                newItems = Database.DiabloDatabase.GetAllItems<Database.CalendarItem>("calendar_items", EntriesPerPage * pageNumber).ToList<Database.IDatabaseModel>();
                createFunc = CreateCalendarMenu;
                break;
            case MenuSection.Characters:
                newItems = Database.DiabloDatabase.GetAllItems<Database.Character>("characters", EntriesPerPage * pageNumber).ToList<Database.IDatabaseModel>();
                createFunc = CreateCharacterMenu;
                break;
            case MenuSection.Classes:
                newItems = Database.DiabloDatabase.GetAllItems<Database.CharacterClass>("character_classes", EntriesPerPage * pageNumber).ToList<Database.IDatabaseModel>();
                createFunc = CreateCharacterClassMenu;
                break;
            case MenuSection.Creatures:
                newItems = Database.DiabloDatabase.GetAllItems<Database.Creature>("creatures", EntriesPerPage * pageNumber).ToList<Database.IDatabaseModel>();
                createFunc = CreateCreatureMenu;
                break;
            case MenuSection.Locations:
                newItems = Database.DiabloDatabase.GetAllItems<Database.Location>("locations", EntriesPerPage * pageNumber).ToList<Database.IDatabaseModel>();
                createFunc = CreateLocationMenu;
                break;
            case MenuSection.MapLocations:
                newItems = Database.DiabloDatabase.GetAllItems<Database.MapLocation>("map_locations", EntriesPerPage * pageNumber).ToList<Database.IDatabaseModel>();
                createFunc = CreateMapLocationMenu;
                break;
            case MenuSection.Sources:
                newItems = Database.DiabloDatabase.GetAllItems<Database.Source>("sources", EntriesPerPage * pageNumber).ToList<Database.IDatabaseModel>();
                createFunc = CreateSourceMenu;
                break;
            case MenuSection.TimelineEvents:
                newItems = Database.DiabloDatabase.GetAllItems<Database.TimelineEvent>("timeline_events", EntriesPerPage * pageNumber).ToList<Database.IDatabaseModel>();
                createFunc = CreateTimelineEventMenu;
                break;
            case MenuSection.WorldItems:
                newItems = Database.DiabloDatabase.GetAllItems<Database.WorldItem>("world_items", EntriesPerPage * pageNumber).ToList<Database.IDatabaseModel>();
                createFunc = CreateWorldItemMenu;
                break;
            case MenuSection.WorldFacts:
                newItems = Database.DiabloDatabase.GetAllItems<Database.WorldFact>("world_facts", EntriesPerPage * pageNumber).ToList<Database.IDatabaseModel>();
                createFunc = CreateWorldFactMenu;
                break;
            default:
                return false;
        }

        if (newItems.Count == menuItemDictionary.Count) {
            return false;
        }

        createFunc(menuItemDictionary, newItems);

        // Very final page will need to be handled w/ this odd offset
        if (EntriesPerPage * pageNumber - EntriesPerPage > newItems.Count) {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Creates the calendar menu
    /// </summary>
    /// <param name="menuItemDictionary">The current or new item dictionary for the menu</param>
    /// <param name="newItems">The full list of items for the respective menu</param>
    /// <returns>True if the menu updated, false otherwise</returns>
    private bool CreateCalendarMenu(Dictionary<string, MenuSection> menuItemDictionary, List<Database.IDatabaseModel> newItems) {
        newItems.ForEach(item => {
            Database.CalendarItem calendarItem = item as Database.CalendarItem;
            if (!menuItemDictionary.Keys.Contains(calendarItem.Name)) { 
                menuItemDictionary.Add(calendarItem.Name, MenuSection.OpenViewModal);
            }
        });

        return true;
    }

    /// <summary>
    /// Creates the character menu
    /// </summary>
    /// <param name="menuItemDictionary">The current or new item dictionary for the menu</param>
    /// <param name="newItems">The full list of items for the respective menu</param>
    /// <returns>True if the menu updated, false otherwise</returns>
    private bool CreateCharacterMenu(Dictionary<string, MenuSection> menuItemDictionary, List<Database.IDatabaseModel> newItems) {
        newItems.ForEach(item => {
            Database.Character character = item as Database.Character;
            if (!menuItemDictionary.Keys.Contains(character.Name)) { 
                menuItemDictionary.Add(character.Name, MenuSection.OpenViewModal);
            }
        });

        return true;
    }

    /// <summary>
    /// Creates the character class menu
    /// </summary>
    /// <param name="menuItemDictionary">The current or new item dictionary for the menu</param>
    /// <param name="newItems">The full list of items for the respective menu</param>
    /// <returns>True if the menu updated, false otherwise</returns>
    private bool CreateCharacterClassMenu(Dictionary<string, MenuSection> menuItemDictionary, List<Database.IDatabaseModel> newItems) {
        ModalTextTemplate = "<color=#0F0><b><u>NOTE</u></b>\n"+
                             "<color=#FFF>This is simply a reference list to the classes defined in characters. To view information about ones that have details, go to the World Facts menu.\n\n";

        newItems.ForEach(item => {
            Database.CharacterClass characterClass = item as Database.CharacterClass;
            ModalTextTemplate += $"{characterClass.Label}\n";
        });

        FillModalTextContent();
        return true;
    }

    /// <summary>
    /// Creates the creature menu
    /// </summary>
    /// <param name="menuItemDictionary">The current or new item dictionary for the menu</param>
    /// <param name="newItems">The full list of items for the respective menu</param>
    /// <returns>True if the menu updated, false otherwise</returns>
    private bool CreateCreatureMenu(Dictionary<string, MenuSection> menuItemDictionary, List<Database.IDatabaseModel> newItems) {
        newItems.ForEach(item => {
            Database.Creature creature = item as Database.Creature;
            if (!menuItemDictionary.Keys.Contains(creature.Name)) { 
                menuItemDictionary.Add(creature.Name, MenuSection.OpenViewModal);
            }
        });

        return true;
    }

    /// <summary>
    /// Creates the location menu
    /// </summary>
    /// <param name="menuItemDictionary">The current or new item dictionary for the menu</param>
    /// <param name="newItems">The full list of items for the respective menu</param>
    /// <returns>True if the menu updated, false otherwise</returns>
    private bool CreateLocationMenu(Dictionary<string, MenuSection> menuItemDictionary, List<Database.IDatabaseModel> newItems) {
        newItems.ForEach(item => {
            Database.Location location = item as Database.Location;
            if (!menuItemDictionary.Keys.Contains(location.Name)) { 
                menuItemDictionary.Add(location.Name, MenuSection.OpenViewModal);
            }
        });

        return true;
    }

    /// <summary>
    /// Creates the map location menu
    /// </summary>
    /// <param name="menuItemDictionary">The current or new item dictionary for the menu</param>
    /// <param name="newItems">The full list of items for the respective menu</param>
    /// <returns>True if the menu updated, false otherwise</returns>
    private bool CreateMapLocationMenu(Dictionary<string, MenuSection> menuItemDictionary, List<Database.IDatabaseModel> newItems) {
        newItems.ForEach(item => {
            Database.MapLocation location = item as Database.MapLocation;
            if (!menuItemDictionary.Keys.Contains(location.Name)) { 
                menuItemDictionary.Add(location.Name, MenuSection.OpenViewModal);
            }
        });

        return true;
    }

    /// <summary>
    /// Creates the source class menu
    /// </summary>
    /// <param name="menuItemDictionary">The current or new item dictionary for the menu</param>
    /// <param name="newItems">The full list of items for the respective menu</param>
    /// <returns>True if the menu updated, false otherwise</returns>
    private bool CreateSourceMenu(Dictionary<string, MenuSection> menuItemDictionary, List<Database.IDatabaseModel> newItems) {
        ModalTextTemplate = "<color=#0F0><b><u>NOTE</u></b>\n"+
                             "<color=#FFF>This is simply a reference list to the sources used on this app. You can see these by their number in brackets on information.\n\n";

        newItems.ForEach(item => {
            Database.Source source = item as Database.Source;
            ModalTextTemplate += $"{source.Name}\n";
        });

        FillModalTextContent();
        return true;
    }

    /// <summary>
    /// Creates the timeline event menu
    /// </summary>
    /// <param name="menuItemDictionary">The current or new item dictionary for the menu</param>
    /// <param name="newItems">The full list of items for the respective menu</param>
    /// <returns>True if the menu updated, false otherwise</returns>
    private bool CreateTimelineEventMenu(Dictionary<string, MenuSection> menuItemDictionary, List<Database.IDatabaseModel> newItems) {
        newItems.ForEach(item => {
            Database.TimelineEvent timelineEvent = item as Database.TimelineEvent;
            string eventLabel = $"{timelineEvent.Id.ToString()}: {timelineEvent.Year.ToString()}";
            if (!menuItemDictionary.Keys.Contains(eventLabel)) { 
                menuItemDictionary.Add(eventLabel, MenuSection.OpenViewModal);
            }
        });

        return true;
    }

    /// <summary>
    /// Creates the world fact menu
    /// </summary>
    /// <param name="menuItemDictionary">The current or new item dictionary for the menu</param>
    /// <param name="newItems">The full list of items for the respective menu</param>
    /// <returns>True if the menu updated, false otherwise</returns>
    private bool CreateWorldFactMenu(Dictionary<string, MenuSection> menuItemDictionary, List<Database.IDatabaseModel> newItems) {
        newItems.ForEach(item => {
            Database.WorldFact worldFact = item as Database.WorldFact;
            if (!menuItemDictionary.Keys.Contains(worldFact.Name)) { 
                menuItemDictionary.Add(worldFact.Name, MenuSection.OpenViewModal);
            }
        });

        return true;
    }

    /// <summary>
    /// Creates the world item menu
    /// </summary>
    /// <param name="menuItemDictionary">The current or new item dictionary for the menu</param>
    /// <param name="newItems">The full list of items for the respective menu</param>
    /// <returns>True if the menu updated, false otherwise</returns>
    private bool CreateWorldItemMenu(Dictionary<string, MenuSection> menuItemDictionary, List<Database.IDatabaseModel> newItems) {
        newItems.ForEach(item => {
            Database.WorldItem worldItem = item as Database.WorldItem;
            if (!menuItemDictionary.Keys.Contains(worldItem.Name)) { 
                menuItemDictionary.Add(worldItem.Name, MenuSection.OpenViewModal);
            }
        });

        return true;
    }

    private void ResetMenus() {
        // Keep this disabled until we need it!
        _modalText.gameObject.SetActive(false);

        _menuSectionLists.Clear();
        _menuSectionLists.Add(MenuSection.Main, new Dictionary<string, MenuSection>(){
            {"Calendar", MenuSection.Calendar}, {"Characters", MenuSection.Characters}, {"Classes", MenuSection.Classes}, 
            {"Creatures", MenuSection.Creatures}, {"Locations", MenuSection.Locations}, {"Map Locations", MenuSection.MapLocations}, 
            {"Sources", MenuSection.Sources}, {"Timeline Events", MenuSection.TimelineEvents}, {"World Facts", MenuSection.WorldFacts}, 
            {"World Items", MenuSection.WorldItems}, {"About Me", MenuSection.AboutMe}, {"Exit App", MenuSection.Exit}
        });

        CreateMenu(MenuSection.Calendar, -1);
        CreateMenu(MenuSection.Characters, -1);
        CreateMenu(MenuSection.Classes, -1);
        CreateMenu(MenuSection.Creatures, -1);
        CreateMenu(MenuSection.Locations, -1);
        CreateMenu(MenuSection.Locations, -1);
        CreateMenu(MenuSection.MapLocations, -1);
        CreateMenu(MenuSection.Sources, -1);
        CreateMenu(MenuSection.TimelineEvents, -1);
        CreateMenu(MenuSection.WorldFacts, -1);
        CreateMenu(MenuSection.WorldItems, -1);

        PushMenu(MenuSection.Main);
    }
 
    private System.Collections.IEnumerator ResetScrollbar() {
        yield return new WaitForEndOfFrame();
        _scrollbarRect.verticalNormalizedPosition = 1.0f / (float)_pageNumber;
    }

    public void ClickMenuButton(TMP_Text callingButton) {
        MenuSection buttonSection = _menuSectionLists[_menuSectionStack.Peek()][callingButton.text];
        // If a button to another section was clicked, push that to the stack
        if (buttonSection != MenuSection.OpenViewModal) {
            if (buttonSection == MenuSection.Exit) {
                Application.Quit(0);
            } else {
                _scrollbarRect.verticalNormalizedPosition = 1.0f;
                // Create the submenu by defining its first page
                CreateMenu(buttonSection);
                PushMenu(buttonSection);
            }

            return;
        }

        // If a button to a view modal activation was clicked, we open the modal w/ the requested information
        switch (_menuSectionStack.Peek()) {
            case MenuSection.Main:
                break;
            case MenuSection.Calendar:
                CalendarItem.ShowCalendarItemNotes(callingButton.text, ViewController.Instance.Modal.activeSelf);
                break;
            case MenuSection.Characters:
                Character.ShowCharacterNotes(callingButton.text, ViewController.Instance.Modal.activeSelf);
                break;
            case MenuSection.Creatures:
                Creature.ShowCreatureNotes(callingButton.text, ViewController.Instance.Modal.activeSelf);
                break;
            case MenuSection.Locations:
                Location.ShowLocationNotes(callingButton.text, ViewController.Instance.Modal.activeSelf);
                break;
            case MenuSection.MapLocations:
                MapLocation.ShowMapLocationNotes(callingButton.text, ViewController.Instance.Modal.activeSelf);
                break;
            case MenuSection.TimelineEvents:
                TimelineEvent.ShowTimelineEventNotes(callingButton.text, ViewController.Instance.Modal.activeSelf);
                break;
            case MenuSection.WorldFacts:
                WorldFact.ShowWorldFactNotes(callingButton.text, ViewController.Instance.Modal.activeSelf);
                break;
            case MenuSection.WorldItems:
                WorldItem.ShowWorldItemNotes(callingButton.text, ViewController.Instance.Modal.activeSelf);
                break;
            default:
                break;
        }
    }

    private void PushMenu(MenuSection menuSection) {
        if (!_menuSectionLists.ContainsKey(menuSection)) {
            return;
        }
        
        DrawMenu(menuSection);

        _menuSectionStack.Push(menuSection);
    }

    private void DrawMenu(MenuSection menuSection) {

        var buttons = MenuButtonList.GetComponentsInChildren<PoolObject>();
        foreach (PoolObject button in buttons) {
            button.DisablePoolObject();
        }
        _buttonListRectTransform.sizeDelta = new Vector2(_buttonListRectTransform.rect.width, 0);
        

        int number = 0; // Can't do a for loop on a dictionary, so we'll watch the height this way
        foreach (KeyValuePair<string, MenuSection> buttonSection in _menuSectionLists[menuSection]) {
            GameObject buttonObject = ObjectPoolManager.Instance.RetrieveNewlyActiveObject(ButtonPrefab, Vector3.zero, Quaternion.identity);
            buttonObject.transform.parent = MenuButtonList.transform;
            buttonObject.transform.localPosition = new Vector3(_buttonListRectTransform.rect.x, number*-100, 0);
            buttonObject.transform.localScale = Vector3.one;

            Button button = buttonObject.GetComponent<Button>();
            TMP_Text buttonText = buttonObject.GetComponentInChildren<TMP_Text>();

            buttonText.text = buttonSection.Key;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ViewController.Instance.MainMenuModal.ClickMenuButton(buttonText));

            number++;
        }
        
        _buttonListRectTransform.sizeDelta = new Vector2(_buttonListRectTransform.rect.width, number*100);
        _scrollViewRectTransform.sizeDelta = new Vector2(_scrollViewRectTransform.sizeDelta.x, _scrollViewRectTransform.sizeDelta.y+((EpsilonDirection*=-1)*PseudoEpsilon));
    }

    public void GoBack() {
        if (_menuSectionStack.Count > 1) {
            _modalText.gameObject.SetActive(false);
            _verticalLayoutGroup.childControlHeight = false;
            _menuSectionStack.Pop();
            _scrollbarRect.verticalNormalizedPosition = 1.0f;
            DrawMenu(_menuSectionStack.Peek());
        }
    }

    public void Close() {
        _verticalLayoutGroup.childControlHeight = false;
        gameObject.SetActive(false);
    }

    private void RenderAboutMe() {
        ModalTextTemplate = "<color=#0F0><b><u>About Me</u></b>\n"+
                             "<color=#FFF>Who am I? Well, I'm a major Diablo fan - if that wasn't obvious already. I have actually been playing Diablo since the first game came out. My friend and I would stay up real late at night playing Diablo I on Playstation I. We didn't have a memory card, and playing the game on a Playstation without a memory card is a guaranteed permanent game over... But yes, good times.\n\n"+
                             "After Diablo I, I played Diablo II for many years straight. I basically became obsessed with the game to the point where I was reading the books by Richard A Knaak in high school. I found the lore in the books very interesting and then started to focus around the game lore a lot more heavily. Since then, I've basically fallen in love with the lore of the entire universe that Diablo takes place in.\n\n"+
                             "Then Diablo III came out and I started up again. But at that point, I actually had a huge collection of Diablo-related stuff. Unfortunately, I ended up needing some finances and sold my entire collection to a friend (who I have not spoken to in many years - I don't know if he still has the collection).\n\n"+
                             "I played some Diablo III shortly after its release, but quickly grew uninterested. The lore and plot holes that were left in place (many call them 'retconned', I call them 'flaws' or 'inconsistencies') actually grew me distant from the Diablo Universe for a long period of time. But then Diablo IV was announced and sparked my interest once more. Being in a much more financially stable place, I decided to start up my collection again and re-read all of the lore in the Diablo Universe. This time, I decided I wanted to record all my findings and readings, and then create a large map that people could use to reference locations that weren't on the map, places characters traveled, etc.\n\n"+
                             "And that, is what led to this application you see before you now.\n\n"+
                             "<color=#0F0><b><u>About this App</u></b>\n"+
                             "<color=#FFF>The point of this app, as mentioned above, is to introduce a visual aspect to the game world. It's also made to show locations that the existing map doesn't have. The next two key parts to this app is to show where characters throughout the lore history have traveled (or at least places they're mentioned/known to have traveled).\n\n"+
                             "You may notice that several locations don't match perfectly with some of the existing maps, perhaps the latest. This is either because 1.) The maps being released have been inconsistent through their history (Westmarch, for example, has moved several times) or 2.) The descriptions in the original games/books were different than later on.\n\n"+
                             "My thought process on how I decided to draw out this map:\n"+
                             " 1.) Pre-Diablo III lore took precedence at all times, unless there were existing plot holes\n"+
                             " 2.) If there were plot holes in several locations, I took what made most logical sense\n"+
                             " 3.) I left notes in characters, locations, etc. with a section called 'Inconsistencies'. As mentioned above, some may call these 'retconned'. I'm not changing these labels.\n\n"+
                             "Additional notes:\n"+
                             " 1.) Everything presented here is prior to Diablo Immortal at this time. There are a few mentions of Diablo IV things here and there, but not many.\n"+
                             " 2.) I apparently love typos. You will probably find a few.\n\n"+
                             "Finally:\n"+
                             " - I do not work for Blizzard\n"+
                             " - The Diablo Map is artwork owned by Blizzard, not me\n"+
                             " - Modals contain artwork owned by Blizzard, not me";


        FillModalTextContent();
    }

    public void FillModalTextContent() {
        _verticalLayoutGroup.childControlHeight = true;
        _modalText.text = ModalTextTemplate;
        _modalText.gameObject.SetActive(true);
    }

}
