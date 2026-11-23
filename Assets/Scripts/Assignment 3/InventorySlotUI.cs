using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attached to each tool slot prefab in the Inventory tab.
/// Allows the player to drag one tool sprite onto another to combine them.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public Image iconImage;
    public TextMeshProUGUI itemNameText; // Optional label below the sprite

    [HideInInspector]
    public string toolID;

    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 originalPosition;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void Setup(string id, string displayName, Sprite sprite)
    {
        toolID = id;
        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            iconImage.enabled = (sprite != null);
        }
        if (itemNameText != null)
        {
            itemNameText.text = displayName;
        }
    }

    private bool wasCombined = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        wasCombined = false;
        originalParent = transform.parent;
        originalPosition = transform.position;

        if (parentCanvas != null)
            transform.SetParent(parentCanvas.transform);

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.75f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // If this slot was consumed by a valid crafting combination, do not re-attach it
        if (wasCombined) return;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1.0f;

        transform.SetParent(originalParent);
        transform.position = originalPosition;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        InventorySlotUI draggedSlot = eventData.pointerDrag.GetComponent<InventorySlotUI>();
        if (draggedSlot != null && draggedSlot != this && InventoryAndCraftingSystem.Instance != null)
        {
            // Check if the combination succeeded
            bool combined = InventoryAndCraftingSystem.Instance.TryCombineTools(draggedSlot.toolID, this.toolID);
            if (combined)
            {
                draggedSlot.wasCombined = true;
                Destroy(draggedSlot.gameObject);
            }
        }
    }
}