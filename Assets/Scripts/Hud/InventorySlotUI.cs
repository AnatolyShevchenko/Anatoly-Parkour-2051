using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Настройки слота")]
    public bool isQuickSlot;
    public int slotIndex;

    private PlayerInventory inventory;
    private static InventorySlotUI hoveredSlot; // Хранит слот, над которым СЕЙЧАС находится мышка

    void Start()
    {
        inventory = FindFirstObjectByType<PlayerInventory>();
    }

    void Update()
    {
        // Проверяем нажатия клавиш, ТОЛЬКО если мышка сейчас наведена именно на ЭТОТ слот
        if (hoveredSlot == this && inventory != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) TryHotkeySwap(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TryHotkeySwap(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TryHotkeySwap(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) TryHotkeySwap(3);

            // НОВОЕ: Если инвентарь открыт, мышка на слоте и нажали G — выбрасываем вещь
            if (Input.GetKeyDown(KeyCode.G)) TryHotkeyDrop();
        }
    }

    // ==========================================
    // НОВОЕ: ЛОГИКА БЫСТРОГО ВЫБРОСА НА КНОПКУ G
    // ==========================================
    private void TryHotkeyDrop()
    {
        // Вытаскиваем предмет из этого слота данных
        ItemData itemToDrop = isQuickSlot ? inventory.quickSlots[slotIndex] : inventory.backpackSlots[slotIndex];
        if (itemToDrop == null) return;

        // Спавним 3D модельку перед камерой/рукой (берём настройки из твоего PlayerInventory)
        if (itemToDrop.itemPrefab != null && inventory.handTransform != null)
        {
            Transform launchPoint = inventory.handTransform;

            // Чуть смещаем вперед, чтобы не врезалось в игрока
            Vector3 spawnPosition = launchPoint.position + launchPoint.forward * 0.4f;
            GameObject droppedObj = Instantiate(itemToDrop.itemPrefab, spawnPosition, launchPoint.rotation);

            Rigidbody itemRb = droppedObj.GetComponent<Rigidbody>();
            if (itemRb != null)
            {
                itemRb.isKinematic = false; // Включаем физику обратно

                // Даем импульс по направлению взгляда
                Vector3 throwDirection = (launchPoint.forward + Vector3.up * 0.1f).normalized;
                itemRb.AddForce(throwDirection * inventory.throwForce, ForceMode.Impulse);
            }
        }

        // Очищаем этот слот в данных
        SaveItemToInventory(null);

        // Перерисовываем весь интерфейс (картинка в инвентаре сотрется, а если это был активный слот — предмет исчезнет и из руки)
        inventory.RefreshUI(false);

        Debug.Log($"[Быстрый выброс] {itemToDrop.itemName} выброшен из инвентаря на клавишу G.");
    }

    // ==========================================
    // ЛОГИКА КЛИКОВ И DRAG & DROP
    // ==========================================
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging) return;
        HandleSlotInteraction();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inventory == null) return;

        ItemData itemInSlot = isQuickSlot ? inventory.quickSlots[slotIndex] : inventory.backpackSlots[slotIndex];

        if (!UICursorItem.Instance.IsHoldingItem() && itemInSlot != null)
        {
            UICursorItem.Instance.SetItem(itemInSlot);
            SaveItemToInventory(null);
            inventory.RefreshUI(false);
        }
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnDrop(PointerEventData eventData)
    {
        HandleSlotInteraction();
    }

    private void HandleSlotInteraction()
    {
        if (inventory == null) return;

        ItemData itemInSlot = isQuickSlot ? inventory.quickSlots[slotIndex] : inventory.backpackSlots[slotIndex];
        bool cursorHasItem = UICursorItem.Instance.IsHoldingItem();

        if (!cursorHasItem && itemInSlot != null)
        {
            UICursorItem.Instance.SetItem(itemInSlot);
            SaveItemToInventory(null);
        }
        else if (cursorHasItem && itemInSlot == null)
        {
            SaveItemToInventory(UICursorItem.Instance.currentHoldingItem);
            UICursorItem.Instance.SetItem(null);
        }
        else if (cursorHasItem && itemInSlot != null)
        {
            ItemData tempItem = itemInSlot;
            SaveItemToInventory(UICursorItem.Instance.currentHoldingItem);
            UICursorItem.Instance.SetItem(tempItem);
        }

        inventory.RefreshUI(false);
    }

    // ==========================================
    // СЛЕЖКА ЗА КУРСОРОМ МЫШИ
    // ==========================================
    public void OnPointerEnter(PointerEventData eventData)
    {
        hoveredSlot = this;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoveredSlot == this) hoveredSlot = null;
    }

    private void TryHotkeySwap(int targetQuickSlotIndex)
    {
        if (isQuickSlot && slotIndex == targetQuickSlotIndex) return;

        ItemData currentItem = isQuickSlot ? inventory.quickSlots[slotIndex] : inventory.backpackSlots[slotIndex];
        ItemData quickSlotItem = inventory.quickSlots[targetQuickSlotIndex];

        inventory.quickSlots[targetQuickSlotIndex] = currentItem;
        SaveItemToInventory(quickSlotItem);

        inventory.RefreshUI(false);
    }

    private void SaveItemToInventory(ItemData item)
    {
        if (isQuickSlot) inventory.quickSlots[slotIndex] = item;
        else inventory.backpackSlots[slotIndex] = item;
    }
}