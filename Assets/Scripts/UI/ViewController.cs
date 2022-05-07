using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class CharacterPathConnection {
    public MapLocation NodeMapStartLocation { set; get; }
    public MapLocation NodeMapEndLocation { set; get; }
    public float CurrentLerpTime { set; get; }
    public LineRenderer ConnectionRenderer { set; get; }
    public CharacterPathConnection(MapLocation startLocation, MapLocation endLocation, float currentLerpTime, LineRenderer renderer) {
        NodeMapStartLocation = startLocation;
        NodeMapEndLocation = endLocation;
        CurrentLerpTime = currentLerpTime;
        ConnectionRenderer = renderer;
        ConnectionRenderer.SetPosition(0, NodeMapStartLocation.LocationRectTransform.position);
        ConnectionRenderer.SetPosition(1, NodeMapStartLocation.LocationRectTransform.position);
    }
}

[AddComponentMenu("Scripts/ViewController")]
public class ViewController : MonoBehaviour {
    public struct ModalStackItem {
        public string Text { private set; get; }
        public string Title { private set; get; }
        public ModalStackItem(string text, string title) {
            Text = text;
            Title = title;
        }
    }

    #region Unity Fields

    [Header("Reference Objects")]
    #region
    [Tooltip("The text displaying the zoom factor")]
    public UnityEngine.UI.Text ZoomFactorText;

    [Tooltip("The main map view Canvas to scale")]
    public UnityEngine.UI.CanvasScaler ViewCanvasScalar;
    public UnityEngine.UI.CanvasScaler HudCanvasScalar;

    [Tooltip("The main map view RectTransform")]
    public UnityEngine.RectTransform MapRectTransform;

    [Tooltip("The modal for the Main Menu")]
    public MainMenuModal MainMenuModal;

    [Tooltip("The map image itself")]
    public GameObject Map;

    [Tooltip("The modal used for information")]
    public GameObject Modal;

    [Tooltip("The popover tooltip to use for source info and such")]
    public GameObject PopoverTooltip;
    [Tooltip("The line rendering object to render character paths. Should be a prefab (going to instantiate per point)")]
    public LineRenderer CharacterPathLine;

    #endregion

    [Header("Zoom Fields")]
    #region
    [Tooltip("The maximum zoom factor the user can zoom into the map at any resolution")]
    [Range(1.0f, 5.0f)]
    public float MaxZoomFactor = 3.0f;

    [Tooltip("How much the zoom factor changes when zooming in or out")]
    [Range(0.01f, 1.0f)]
    public float ZoomFactorChangeAmount = 0.25f;
    #endregion

    [Header("Drag/Drop Fields")]
    #region     
    [Tooltip("What % of the screen should the mouse move before it should be considered dragging to move the map?")]
    [Range(0.001f, 1.0f)]
    public float DistanceForDrag = 0.03f;
    #endregion

    [Header("Miscellaneous Fields")]
    #region
    [Tooltip("This is the maximum resolution width the image can be drawn at.")]
    public int MaxResolutionWidth = 3840;

    [Tooltip("This is the maximum resolution height the image can be drawn at.")]
    public int MaxResolutionHeight = 2160;

    [Tooltip("How far to the left/right the modal should move to on the X when clicked")]
    public int ModalXPaddingAmount = 550;
    [Tooltip("How long it takes in between line renders for a character path")]
    public float LineRenderingTime = 2.0f;
    #endregion

    #endregion

    public static ViewController Instance;

    public bool IsDragging { private set; get; }
    public bool ShowAllLocationNames { private set; get; }

    #region Fields
    // Scaling fields
    private float _minimumScaleFactor;
    private float _zoomFactor = 1.0f; // What the user sees on the UI
    private float _minimumZoomFactor = 1.0f;

    // Drag/Drop fields
    private Vector3 _startingDragPosition;
    private Vector3 _mapStartingPosition, _modalStartingPosition;
    private float _maxXForDrag, _maxYForDrag;
    private bool _clickedScrollbar = false;
    private bool _draggingModal = false;
    private bool _modalPositionChanged = false;

    // Scrolling fields
    private float _scrollValue;

    // Modal-related items
    private Stack<ModalStackItem> _descriptionStack = new Stack<ModalStackItem>();
    private TextMeshProUGUI _modalText, _modalTitle;
    private Button _backButton;
    private RectTransform _modalRectTransform;

    // Tooltip-related items
    private Text _tooltipText;
    private RectTransform _tooltipRectTransform;

    // Character movement vectors
    private List<CharacterPathConnection> _currentNodeConnections = new List<CharacterPathConnection>();
    private PoolObject _characterPathLinePoolObject;
    private bool rerenderLines = false; // When zooming in/out, we have to force a re-render of the lines
    #endregion

    // Start is called before the first frame update
    void Start() {
        _minimumScaleFactor = ViewCanvasScalar.scaleFactor =
                (float)Camera.main.scaledPixelHeight / (float)MaxResolutionHeight;
        Instance = this;

        _modalText = Modal.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(item => item.name == "ModalText");
        _modalTitle = Modal.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(item => item.name == "ModalTitle");
        _backButton = Modal.GetComponentsInChildren<Button>().FirstOrDefault(item => item.name == "BackButton");

        _tooltipText = PopoverTooltip.GetComponentsInChildren<Text>().FirstOrDefault();

        _modalRectTransform = Modal.GetComponent<RectTransform>();
        _tooltipRectTransform =PopoverTooltip.GetComponent<RectTransform>();

        _characterPathLinePoolObject = CharacterPathLine.GetComponent<PoolObject>();
    }

    void LateUpdate() {
        if (rerenderLines) {
            RerenderFinishedLines();
            rerenderLines = false;
        }
        HandleClickInput();
        HandleKeyboardInput();
        HandleScrollInput();
    }

    public void ZoomIn() {
        if (_zoomFactor < MaxZoomFactor) {
            _zoomFactor = Mathf.Clamp(_zoomFactor + ZoomFactorChangeAmount, _minimumZoomFactor, MaxZoomFactor);
            UpdateFieldsFromZoomChange();
        }
    }

    public void ZoomOut() {
        if (_zoomFactor > _minimumZoomFactor) {
            _zoomFactor = Mathf.Clamp(_zoomFactor - ZoomFactorChangeAmount, _minimumZoomFactor, MaxZoomFactor);
            UpdateFieldsFromZoomChange();
        }
    }

    private void HandleClickInput() {
        if (Input.GetButton("Click")) {
            PopoverTooltip.SetActive(false);
            if (!IsDragging && !_clickedScrollbar && !_draggingModal) {
                PointerEventData eventData = new PointerEventData(EventSystem.current);
                eventData.position =  Input.mousePosition;
                List<RaycastResult> raycastResults = new List<RaycastResult>();
                EventSystem.current.RaycastAll( eventData, raycastResults );
                bool cancelDrag = false;

                foreach (RaycastResult result in raycastResults) {
                    if (result.gameObject.layer != LayerMask.NameToLayer("UI")) {
                        continue;
                    }
                    if (result.gameObject.name != "Map") {
                        cancelDrag = true;
                        if (result.gameObject.GetComponent<Scrollbar>() != null) {
                            _clickedScrollbar = true;
                        } else if (result.gameObject.name == "ModalTitle") {
                            _draggingModal = true;
                        }
                    }
                }

                if (cancelDrag) {
                    return;
                }
            }
            
            if (_startingDragPosition == Vector3.zero) {
                _startingDragPosition = Input.mousePosition;
                if (_draggingModal) {
                    _modalStartingPosition = _modalRectTransform.localPosition;
                } else {
                    _mapStartingPosition = MapRectTransform.localPosition;
                }
            }
            float distance = Vector3.Distance(_startingDragPosition, Input.mousePosition);
            if (distance / Camera.main.scaledPixelWidth >= DistanceForDrag) {
                if (_draggingModal) {
                    DragModal();
                    _modalPositionChanged = true;
                } else if (!_clickedScrollbar) {
                    DragMap();
                }
            }
        } else if (_startingDragPosition != Vector3.zero) {
            IsDragging = false;
            _startingDragPosition = Vector3.zero;
            _clickedScrollbar = false;
            _draggingModal = false;
        }
    }

    private void HandleKeyboardInput() {
        if (Input.GetButtonUp("Escape")) {
            MainMenuModal.gameObject.SetActive(!MainMenuModal.isActiveAndEnabled);
        }
    }

    private void HandleScrollInput() {
        // Don't permit scrolling to zoom in/out when modal is open
        if (Modal.activeSelf || MainMenuModal.gameObject.activeSelf) {
            return;
        }

        if ((_scrollValue = Input.GetAxisRaw("Mouse ScrollWheel")) != 0.0f) {
            if (_scrollValue < 0) {
                ZoomOut();
            } else {
                ZoomIn();
            }
            rerenderLines = true;
        }
    }

    private void RerenderFinishedLines() {
        for (int i = 0; i < _currentNodeConnections.Count; i++) {
            CharacterPathConnection currentNode = _currentNodeConnections[i];

            // Only do it for elements that have finished "lerping" (in case the user is moving/zooming while it's animating)
            if (currentNode.CurrentLerpTime > LineRenderingTime) {
                // In case the user is actively zooming in/out or moving, these have to be updated each frame unfortunately.
                Vector3 startRectPosition = _currentNodeConnections[i].NodeMapStartLocation.LocationRectTransform.position;
                Vector3 endRectPosition = _currentNodeConnections[i].NodeMapEndLocation.LocationRectTransform.position;
                Vector3 startPosition = new Vector3(startRectPosition.x, startRectPosition.y, 1);
                Vector3 endPosition = new Vector3(endRectPosition.x, endRectPosition.y, 1);
                currentNode.ConnectionRenderer.SetPosition(0, startPosition);
                currentNode.ConnectionRenderer.SetPosition(1, endPosition);
            }
        }
    }

    private void UpdateFieldsFromZoomChange() {
        ViewCanvasScalar.scaleFactor = _minimumScaleFactor * _zoomFactor;
        ZoomFactorText.text = _zoomFactor.ToString("0.00") + "x";
        _maxXForDrag = (MaxResolutionWidth - (MapRectTransform.rect.width / _zoomFactor)) / 2;
        _maxYForDrag = (MaxResolutionHeight - (MapRectTransform.rect.height / _zoomFactor)) / 2;

        MapRectTransform.localPosition = new Vector3(
            Mathf.Clamp(MapRectTransform.localPosition.x, -_maxXForDrag, _maxXForDrag),
            Mathf.Clamp(MapRectTransform.localPosition.y, -_maxYForDrag, _maxYForDrag),
            0
        );
    }

    private void DragMap() {
        // We divide by the view's current zoom factor so the drag is more natural
        IsDragging = true;
        Vector3 difference = (Input.mousePosition - _startingDragPosition) / ViewCanvasScalar.scaleFactor;
        MapRectTransform.localPosition = new Vector3(
            Mathf.Clamp(_mapStartingPosition.x + difference.x, -_maxXForDrag, _maxXForDrag),
            Mathf.Clamp(_mapStartingPosition.y + difference.y, -_maxYForDrag, _maxYForDrag),
            0
        );
        RerenderFinishedLines();
    }

    private void DragModal() {
        Vector3 difference = (Input.mousePosition - _startingDragPosition) / (HudCanvasScalar.scaleFactor * _minimumScaleFactor * 2);
        _modalRectTransform.localPosition = new Vector3(
            _modalStartingPosition.x + difference.x,
            _modalStartingPosition.y + difference.y,
            0
        );
    }

    public void ShowModal(string text, string title, bool linked = false) {
        _descriptionStack.Push(new ModalStackItem(text, title));
        _modalText.text = text;
        _modalTitle.text = title;

        // If this was clicked on the map and not a link, move the modal to a good spot
        // Also only moves if the user hasn't already moved the modal beforehand
        if (!linked && !_modalPositionChanged) {
            if (Input.mousePosition.x < Camera.main.scaledPixelWidth / 2) {
                _modalRectTransform.localPosition = new Vector3(
                    ModalXPaddingAmount,
                    _modalRectTransform.localPosition.y,
                    _modalRectTransform.localPosition.z);
            } else {
                _modalRectTransform.localPosition = new Vector3(
                    -ModalXPaddingAmount,
                    _modalRectTransform.localPosition.y,
                    _modalRectTransform.localPosition.z);
            }
        }

        Modal.SetActive(true);
        if (_descriptionStack.Count > 1) {
            _backButton.gameObject.SetActive(true);
        } else {
            _backButton.gameObject.SetActive(false);
        }
    }

    public void GoBack() {
        _descriptionStack.Pop();
        _modalText.text = _descriptionStack.Peek().Text;
        _modalTitle.text = _descriptionStack.Peek().Title;
        if (_descriptionStack.Count <= 1) {
            _backButton.gameObject.SetActive(false);
        }
    }

    public void Close() {
        _descriptionStack.Clear();
        _backButton.gameObject.SetActive(false);
        Modal.SetActive(false);
        _modalPositionChanged = false;
    }

    public void ClickUIItem(PointerEventData eventData) {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(_modalText, eventData.position, null);

        // If a '<link>' was found, apply the click to it
        if (linkIndex != -1) {
            TMP_LinkInfo linkInfo = _modalText.textInfo.linkInfo[linkIndex];
            string[] splitLinkInfo = linkInfo.GetLinkID().Split(':'); // Looks something like: 'MapLocation:Seram'
            if (splitLinkInfo.Length == 2) {
                HandleClickingItem(splitLinkInfo[0], splitLinkInfo[1]);
            }
        }
    }

    public void ToggleAllMapLocationNames() {
        ShowAllLocationNames = !ShowAllLocationNames;
        foreach (MapLocation mapLocation in MapLocation.MapLocationIndex.Values) {
            if (ShowAllLocationNames) {
                mapLocation.ShowMapLocationName();
            } else {
                mapLocation.HideMapLocationName();
            }
        }
    }

    private void HandleClickingItem(string itemType, string itemName) {
        switch (itemType) {
            case "Action":
                HandleAction(itemName);
                break;
            case "Calendar":
            case "CalendarItem":
                CalendarItem.ShowCalendarItemNotes(itemName, true);
                break;
            case "Character":
                Character.ShowCharacterNotes(itemName, true);
                break;
            case "Creature":
                Creature.ShowCreatureNotes(itemName, true);
                break;
            case "Location":
                Location.ShowLocationNotes(itemName, true);
                break;
            case "MapLocation":
                MapLocation.ShowMapLocationNotes(itemName, true);
                break;
            case "SourceId":
                int sourceId;
                if (int.TryParse(itemName, out sourceId)) {
                    Database.Source source = Database.Source.GetDocumentById(sourceId);
                    if (source != null) {
                        _tooltipText.text = source.Name;
                        Vector3 desiredPosition;

                        if (Input.mousePosition.x < Camera.main.scaledPixelWidth / 2) {
                            desiredPosition = new Vector3(
                                Input.mousePosition.x,
                                Input.mousePosition.y,
                                Input.mousePosition.z);
                        } else {
                            desiredPosition = new Vector3(
                                Input.mousePosition.x - (_tooltipRectTransform.rect.width * _tooltipRectTransform.transform.lossyScale.x),
                                Input.mousePosition.y,
                                Input.mousePosition.z);
                        }
                        _tooltipRectTransform.SetPositionAndRotation(desiredPosition, _tooltipRectTransform.rotation);
                        PopoverTooltip.SetActive(true);
                    }
                }
                break;
            case "TimelineEventId":
                TimelineEvent.ShowTimelineEventNotes(itemName, true);
                break;
            case "WorldFact":
                WorldFact.ShowWorldFactNotes(itemName, true);
                break;
            case "WorldItem":
                WorldItem.ShowWorldItemNotes(itemName, true);
                break;
            default:
                break;
        }
    }

    private void HandleAction(string actionName) {
        string [] actionSplitInfo = actionName.Split('_'); // Looks something like: 'CharacterPath_1' (id == 1)
        if (actionSplitInfo.Length == 2) {
            switch (actionSplitInfo[0]) {
                case "CharacterPath":
                    int characterId;
                    if (int.TryParse(actionSplitInfo[1], out characterId)) {
                        List<MapLocation> mapLocations = Character.GetCharacterPath(characterId);
                        if (mapLocations.Count > 0) {
                            StopAllCoroutines();
                            ObjectPoolManager.Instance.DisableAllObjectsInPool(_characterPathLinePoolObject);
                            StartCoroutine(RenderCharacterPath(mapLocations));
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }

    private System.Collections.IEnumerator RenderCharacterPath(List<MapLocation> mapLocations) {
        _currentNodeConnections = new List<CharacterPathConnection>();
        
        // Minus 2 since last node has no destination
        for (int i = 0; i < mapLocations.Count - 1; i++) {
            _currentNodeConnections.Add(new CharacterPathConnection(
                mapLocations[i],
                mapLocations[i+1],
                0,
                ObjectPoolManager.Instance.RetrieveNewlyActiveObject(_characterPathLinePoolObject, Vector3.zero, Quaternion.identity).GetComponent<LineRenderer>()
            ));
        }

        Vector3 startRectPosition, endRectPosition, startPosition, endPosition, movingVector;
        for (int i = 0; i < _currentNodeConnections.Count; i++) {
            CharacterPathConnection currentNode = _currentNodeConnections[i];
            for(; currentNode.CurrentLerpTime < LineRenderingTime; currentNode.CurrentLerpTime += Time.deltaTime) {
                // In case the user is actively zooming in/out or moving, these have to be updated each frame unfortunately.
                startRectPosition = _currentNodeConnections[i].NodeMapStartLocation.LocationRectTransform.position;
                endRectPosition = _currentNodeConnections[i].NodeMapEndLocation.LocationRectTransform.position;
                startPosition = new Vector3(startRectPosition.x, startRectPosition.y, 1);
                endPosition = new Vector3(endRectPosition.x, endRectPosition.y, 1);

                movingVector = Vector3.Lerp(startPosition, endPosition, currentNode.CurrentLerpTime / LineRenderingTime);
                currentNode.ConnectionRenderer.SetPosition(0, startPosition);
                currentNode.ConnectionRenderer.SetPosition(1, movingVector);
                yield return null;
            }
        }
    }
}
