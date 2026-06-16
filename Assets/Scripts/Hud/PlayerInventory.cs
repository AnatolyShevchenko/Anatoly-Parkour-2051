using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventory : MonoBehaviour
{
    [Header("Окна UI")]
    // ИЗМЕНЕНО: Сменили тип на GameObject, чтобы Unity принимала абсолютно любой объект (Panel, Image, RawImage)
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

    // Метод очистки слота, который вызывает KotonezItem
    public void ClearActiveSlot()
    {
        // 1. Запоминаем, какой именно предмет Анатолий сейчас держит/ест
        ItemData itemToRemove = quickSlots[activeQuickSlot];

        // 2. Очищаем текущий активный слот
        quickSlots[activeQuickSlot] = null;

        // 3. БУЛЛЕТПРУФ-ПРОВЕРКА: Если игрок переключил слот во время затухания экрана,
        // мы все равно находим этот съеденный предмет во ВСЕМ инвентаре и уничтожаем его данные,
        // чтобы он не остался в рюкзаке или соседних ячейках.
        if (itemToRemove != null)
        {
            for (int i = 0; i < quickSlots.Length; i++)
            {
                if (quickSlots[i] == itemToRemove) quickSlots[i] = null;
            }
            for (int i = 0; i < backpackSlots.Length; i++)
            {
                if (backpackSlots[i] == itemToRemove) backpackSlots[i] = null;
            }
        }

        // Обновляем UI, что мгновенно сотрет иконку и уберет трехмерную модельку
        RefreshUI(false);
    }

    // Новый метод, который вызывается из KotonezItem в момент начала затухания
    public void BlockInputForTrip()
    {
        isInputBlockedByTrip = true;

        // Если инвентарь был открыт в момент поглощения — принудительно закрываем его
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

        // Запоминаем стартовый предмет, чтобы не устраивать анимацию при загрузке игры
        lastActiveItem = quickSlots[activeQuickSlot];

        // Синхронизируем интерфейс и руки на старте
        RefreshUI(true);
    }

    void Update()
    {
        // ГЛОБАЛЬНЫЙ ЗАПРЕТ ВВОДА: Если Анатолий под котонезом — клавиатура и мышь для инвентаря полностью мертвы
        if (isInputBlockedByTrip) return;

        // Открытие/закрытие инвентаря на Tab
        if (Input.GetKeyDown(KeyCode.Tab)) ToggleInventory();

        // Если инвентарь открыт и игрок нажимает Esc — закрываем его
        if (isInventoryOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleInventory();
        }

        // Блокируем игровой ввод игрока, если открыт инвентарь
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
        // ГЛОБАЛЬНЫЙ ПЕРЕХВАТЧИК ИЗМЕНЕНИЙ В РУКЕ
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

        // --- Измененный код спавна модельки с учетом смещений ---
        if (currentInHandObject != null)
        {
            Destroy(currentInHandObject);
        }

        if (activeItem != null && activeItem.itemPrefab != null)
        {
            currentInHandObject = Instantiate(activeItem.itemPrefab, handTransform);

            // ИЗМЕНЕНО: Достаем компонент Image из переданного GameObject и отдаем скрипту майонеза
            KotonezItem kotonezScript = currentInHandObject.GetComponent<KotonezItem>();
            if (kotonezScript != null && globalFadeScreen != null)
            {
                kotonezScript.fadeScreen = globalFadeScreen.gameObject;
            }

            // Применяем индивидуальные координаты и углы поворота из файла ItemData
            currentInHandObject.transform.localPosition = activeItem.handPositionOffset;
            currentInHandObject.transform.localRotation = Quaternion.Euler(activeItem.handRotationOffset);

            // Убеждаемся, что физика не мешает отображению в руке
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