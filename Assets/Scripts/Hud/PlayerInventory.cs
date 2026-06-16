using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventory : MonoBehaviour
{
    [Header("Окна UI")]
    public GameObject globalFadeScreen;
    public GameObject inventoryPanel;

    [Header("Визуальные ячейки HUD (Рамочки быстрых slots)")]
    public Image[] hudSlotImages = new Image[4];
    public Color normalColor = Color.gray;
    public Color selectedColor = Color.green;

    [Header("Визуальные Иконки HUD")]
    public Image[] hudSlotIcons = new Image[4];

    [Header("Визуальные Иконки РЮКЗАКА")]
    public Image[] backpackSlotIcons = new Image[8];

    [Header("Настройки размера HUD")]
    public float normalScale = 1f;
    public float selectedScale = 1.2f;
    public float scaleSmoothTime = 10f;

    [Header("Слоты данных")]
    public ItemData[] quickSlots = new ItemData[4];
    public ItemData[] backpackSlots = new ItemData[8];

    [Header("Точка для предмета в руке")]
    public Transform handTransform;
    private GameObject currentInHandObject;

    [Header("Настройки выбрасывания (Кнопка G)")]
    public float throwForce = 4f;

    // СИСТЕМА АНИМАЦИИ
    private Vector3 defaultHandPos;
    private bool isSwitchingItem = false;
    private ItemData lastActiveItem;      // Следит за тем, что БЫЛО в руке кадр назад

    private int activeQuickSlot = 0;
    private bool isInventoryOpen = false;

    // БЛОКИРОВКА ИНВЕНТАРЯ ПРИ ТРИПЕ
    private bool isInputBlockedByTrip = false;

    private PlayerMovement movementScript;

    // ==========================================
    // Свойства для внешних скриптов предметов
    // ==========================================
    public bool IsInventoryOpen => isInventoryOpen;
    public bool IsSwitchingItem => isSwitchingItem;

    // Метод ручной очистки активного слота (например, для других предметов)
    public void ClearActiveSlot()
    {
        quickSlots[activeQuickSlot] = null;
        RefreshUI(false);
    }

    // Новый метод, который вызывается из KotonezItem в момент начала затухания
    public void BlockInputForTrip()
    {
        isInputBlockedByTrip = true;

        if (isInventoryOpen)
        {
            isInventoryOpen = false;
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
        }
    }

    public void UnblockInputAfterTrip()
    {
        isInputBlockedByTrip = false;
    }
    // ==========================================

    void Start()
    {
        movementScript = GetComponent<PlayerMovement>();
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        if (handTransform != null)
        {
            defaultHandPos = handTransform.localPosition;
        }

        lastActiveItem = quickSlots[activeQuickSlot];
        RefreshUI(true);
    }

    void Update()
    {
        if (isInputBlockedByTrip) return;

        if (Input.GetKeyDown(KeyCode.Tab)) ToggleInventory();

        if (isInventoryOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleInventory();
        }

        if (isInventoryOpen) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectQuickSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectQuickSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectQuickSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectQuickSlot(3);

        if (Input.GetKeyDown(KeyCode.G)) DropItemFromHand();

        AnimateSlotScales();
    }

    void SelectQuickSlot(int slotIndex)
    {
        if ((activeQuickSlot == slotIndex && currentInHandObject != null) || isSwitchingItem) return;

        StartCoroutine(MinecraftSwitchRoutine(slotIndex));
    }

    private IEnumerator MinecraftSwitchRoutine(int newSlotIndex)
    {
        isSwitchingItem = true;

        Vector3 loweredPos = defaultHandPos - new Vector3(0, 0.4f, 0);
        float speed = 12f;

        float time = 0;
        while (time < 1f)
        {
            time += Time.deltaTime * speed;
            handTransform.localPosition = Vector3.Lerp(defaultHandPos, loweredPos, time);
            yield return null;
        }
        handTransform.localPosition = loweredPos;

        activeQuickSlot = newSlotIndex;
        RefreshUI(false);

        yield return new WaitForSeconds(0.02f);

        time = 0;
        while (time < 1f)
        {
            time += Time.deltaTime * speed;
            handTransform.localPosition = Vector3.Lerp(loweredPos, defaultHandPos, time);
            yield return null;
        }
        handTransform.localPosition = defaultHandPos;

        isSwitchingItem = false;
    }

    void DropItemFromHand()
    {
        ItemData itemToDrop = quickSlots[activeQuickSlot];
        if (itemToDrop == null) return;

        if (itemToDrop.itemPrefab != null)
        {
            Vector3 spawnPosition = handTransform.position + handTransform.forward * 0.4f;
            GameObject droppedObj = Instantiate(itemToDrop.itemPrefab, spawnPosition, handTransform.rotation);

            Rigidbody itemRb = droppedObj.GetComponent<Rigidbody>();
            if (itemRb != null)
            {
                itemRb.isKinematic = false;
                Vector3 throwDirection = (handTransform.forward + Vector3.up * 0.1f).normalized;
                itemRb.AddForce(throwDirection * throwForce, ForceMode.Impulse);
            }
        }

        quickSlots[activeQuickSlot] = null;
        RefreshUI(false);
    }

    void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        if (isInventoryOpen)
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (movementScript != null) movementScript.enabled = false;

            RefreshUI(false);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (movementScript != null) movementScript.enabled = true;
        }
    }

    public void RefreshUI(bool instantSize)
    {
        for (int i = 0; i < hudSlotImages.Length; i++)
        {
            if (hudSlotImages[i] != null)
            {
                hudSlotImages[i].color = (i == activeQuickSlot) ? selectedColor : normalColor;
                if (instantSize) hudSlotImages[i].transform.localScale = Vector3.one * ((i == activeQuickSlot) ? selectedScale : normalScale);
            }

            if (hudSlotIcons[i] != null)
            {
                if (quickSlots[i] != null && quickSlots[i].itemIcon != null)
                {
                    hudSlotIcons[i].sprite = quickSlots[i].itemIcon;
                    hudSlotIcons[i].enabled = true;
                }
                else
                {
                    hudSlotIcons[i].enabled = false;
                }
            }
        }

        for (int i = 0; i < backpackSlotIcons.Length; i++)
        {
            if (backpackSlotIcons[i] != null)
            {
                if (backpackSlots[i] != null && backpackSlots[i].itemIcon != null)
                {
                    backpackSlotIcons[i].sprite = backpackSlots[i].itemIcon;
                    backpackSlotIcons[i].enabled = true;
                }
                else
                {
                    backpackSlotIcons[i].enabled = false;
                }
            }
        }

        UpdateItemInHand();
    }

    private void UpdateItemInHand()
    {
        if (handTransform == null) return;

        ItemData activeItem = quickSlots[activeQuickSlot];

        if (activeItem != lastActiveItem)
        {
            if (!isSwitchingItem)
            {
                lastActiveItem = activeItem;

                if (activeItem == null)
                {
                    if (currentInHandObject != null) Destroy(currentInHandObject);
                }
                else
                {
                    StartCoroutine(MinecraftSwitchRoutine(activeQuickSlot));
                    return;
                }
            }
            else
            {
                lastActiveItem = activeItem;
            }
        }

        if (currentInHandObject != null)
        {
            Destroy(currentInHandObject);
        }

        if (activeItem != null && activeItem.itemPrefab != null)
        {
            currentInHandObject = Instantiate(activeItem.itemPrefab, handTransform);

            KotonezItem kotonezScript = currentInHandObject.GetComponent<KotonezItem>();
            if (kotonezScript != null && globalFadeScreen != null)
            {
                kotonezScript.fadeScreen = globalFadeScreen.gameObject;
            }

            currentInHandObject.transform.localPosition = activeItem.handPositionOffset;
            currentInHandObject.transform.localRotation = Quaternion.Euler(activeItem.handRotationOffset);

            Rigidbody itemRb = currentInHandObject.GetComponent<Rigidbody>();
            if (itemRb != null) itemRb.isKinematic = true;

            Collider itemCollider = currentInHandObject.GetComponent<Collider>();
            if (itemCollider != null) itemCollider.enabled = false;
        }
    }

    public bool AddItem(ItemData item)
    {
        for (int i = 0; i < quickSlots.Length; i++)
        {
            if (quickSlots[i] == null)
            {
                quickSlots[i] = item;
                RefreshUI(false);
                Debug.Log($"[Подобрано] {item.itemName} в быстрый слот №{i + 1}");
                return true;
            }
        }

        for (int i = 0; i < backpackSlots.Length; i++)
        {
            if (backpackSlots[i] == null)
            {
                backpackSlots[i] = item;
                RefreshUI(false);
                Debug.Log($"[Подобрано] {item.itemName} отправлен в рюкзак.");
                return true;
            }
        }

        Debug.LogWarning("Нет места!");
        return false;
    }

    void AnimateSlotScales()
    {
        for (int i = 0; i < hudSlotImages.Length; i++)
        {
            if (hudSlotImages[i] != null)
            {
                float targetScale = (i == activeQuickSlot) ? selectedScale : normalScale;
                Vector3 currentScale = hudSlotImages[i].transform.localScale;
                hudSlotImages[i].transform.localScale = Vector3.Lerp(currentScale, Vector3.one * targetScale, Time.deltaTime * scaleSmoothTime);
            }
        }
    }

    // ИСПРАВЛЕНО И ПРОВЕРЕНО: Точечно очищает данные активного слота и уничтожает банку
    public void ConsumeSpecificItem(GameObject itemToDestroy)
    {
        // 1. Стираем карточку предмета строго в текущем активном слоте
        quickSlots[activeQuickSlot] = null;

        // 2. Запускаем перерисовку интерфейса (она сотрет иконку)
        RefreshUI(false);

        // 3. Безопасно уничтожаем физический объект съеденного майонеза
        if (itemToDestroy != null)
        {
            itemToDestroy.transform.SetParent(null);
            Destroy(itemToDestroy);
        }

        Debug.Log($"[Инвентарь] КотонеZ успешно съеден из слота №{activeQuickSlot + 1}. Остальные ячейки не тронуты!");
    }
}