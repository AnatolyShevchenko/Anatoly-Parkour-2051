using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventory : MonoBehaviour
{
    [Header("Окна UI")]
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
    private ItemData lastActiveItem;      // НОВОЕ: Следит за тем, что БЫЛО в руке кадр назад

    private int activeQuickSlot = 0;
    private bool isInventoryOpen = false;

    private PlayerMovement movementScript;

    void Start()
    {
        movementScript = GetComponent<PlayerMovement>();
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        if (handTransform != null)
        {
            defaultHandPos = handTransform.localPosition;
        }

        // НОВОЕ: Запоминаем стартовый предмет, чтобы не устраивать анимацию при загрузке игры
        lastActiveItem = quickSlots[activeQuickSlot];

        // Синхронизируем интерфейс и руки на старте
        RefreshUI(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) ToggleInventory();

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
        // Если этот слот уже выбран и в руке что-то есть, или анимация уже идет — игнорируем
        if ((activeQuickSlot == slotIndex && currentInHandObject != null) || isSwitchingItem) return;

        StartCoroutine(MinecraftSwitchRoutine(slotIndex));
    }

    // Универсальная Майнкрафт-анимация (Вниз -> Смена -> Вверх)
    private IEnumerator MinecraftSwitchRoutine(int newSlotIndex)
    {
        isSwitchingItem = true;

        Vector3 loweredPos = defaultHandPos - new Vector3(0, 0.4f, 0);
        float speed = 12f;

        // 1. Опускаем руку вниз
        float time = 0;
        while (time < 1f)
        {
            time += Time.deltaTime * speed;
            handTransform.localPosition = Vector3.Lerp(defaultHandPos, loweredPos, time);
            yield return null;
        }
        handTransform.localPosition = loweredPos;

        // 2. ВНИЗУ: Меняем индекс активного слота и перерисовываем
        activeQuickSlot = newSlotIndex;
        RefreshUI(false);

        yield return new WaitForSeconds(0.02f);

        // 3. Поднимаем руку вверх
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
        // 1. HUD СЛОТЫ
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

        // 2. РЮКЗАК
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

        // 3. ОБНОВЛЕНИЕ ПРЕДМЕТА В РУКЕ
        UpdateItemInHand();
    }

    private void UpdateItemInHand()
    {
        if (handTransform == null) return;

        ItemData activeItem = quickSlots[activeQuickSlot];

        // ==========================================
        // НОВОЕ: ГЛОБАЛЬНЫЙ ПЕРЕХВАТЧИК ИЗМЕНЕНИЙ В РУКЕ
        // ==========================================
        if (activeItem != lastActiveItem)
        {
            // Если мы НЕ в процессе анимации смены слотов (значит, предмет изменился извне)
            if (!isSwitchingItem)
            {
                lastActiveItem = activeItem;

                // Если предмет УДАЛИЛИ из руки (выкинули на G или съели) — убираем модельку мгновенно
                if (activeItem == null)
                {
                    if (currentInHandObject != null) Destroy(currentInHandObject);
                }
                // Если в руку ДОБАВИЛИ новый предмет — запускаем красивую майнкрафт-анимацию подъема!
                else
                {
                    StartCoroutine(MinecraftSwitchRoutine(activeQuickSlot));
                    return; // Прерываем мгновенный спавн, корутина сделает это правильно на дне экрана
                }
            }
            else
            {
                // Если мы уже внутри корутины, просто синхронизируем данные
                lastActiveItem = activeItem;
            }
        }

        // --- Стандартный код спавна модельки ---
        if (currentInHandObject != null)
        {
            Destroy(currentInHandObject);
        }

        if (activeItem != null && activeItem.itemPrefab != null)
        {
            currentInHandObject = Instantiate(activeItem.itemPrefab, handTransform);

            currentInHandObject.transform.localPosition = Vector3.zero;
            currentInHandObject.transform.localRotation = Quaternion.identity;

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
                RefreshUI(false); // Вызовет цепочку проверок и плавно поднимет руку!
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
}