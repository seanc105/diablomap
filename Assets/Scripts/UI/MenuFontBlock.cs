using UnityEngine;
using UnityEngine.EventSystems;

using TMPro;

[AddComponentMenu("Scripts/UI/MenuFontBlock")]
[RequireComponent(typeof(TMP_Text))]
public class MenuFontBlock : MonoBehaviour, IPointerClickHandler {
    public void OnPointerClick(PointerEventData eventData) {
        ViewController.Instance.ClickUIItem(eventData);
    }
}