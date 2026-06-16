using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class KotonezItem : MonoBehaviour
{
    [Header("Настройки удержания (Поедание)")]
    public float holdDuration = 2.0f;
    private float currentHoldTime = 0.0f;

    [Header("Настройки затухания экрана")]
    public float fadeDuration = 2.5f;

    [Header("Настройки сцены")]
    [Tooltip("Имя сцены, в которую переместится Анатолий после поедания майонеза")]
    public string targetSceneName = "AnatolyParkour";

    [Header("Визуальное смещение при зажатии")]
    public Vector3 eatingPositionOffset = new Vector3(0, -0.15f, 0.1f);
    private Vector3 originalLocalPos;

    [Header("Ссылки на UI (Инвентарь настроит сам!)")]
    public GameObject fadeScreen;

    private PlayerInventory inventory;
    private PlayerMovement movement;
    private Slider uiProgressBar;

    private Graphic fadeGraphic;
    private bool isTriggered = false;

    void Start()
    {
        inventory = GetComponentInParent<PlayerInventory>();
        movement = GetComponentInParent<PlayerMovement>();
        originalLocalPos = transform.localPosition;

        GameObject sliderObj = GameObject.Find("UseProgressBar");
        if (sliderObj != null)
        {
            uiProgressBar = sliderObj.GetComponent<Slider>();
            uiProgressBar.gameObject.SetActive(false);
        }

        SetupFadeScreen();
    }

    private void SetupFadeScreen()
    {
        if (fadeScreen != null)
        {
            fadeGraphic = fadeScreen.GetComponent<Graphic>();

            if (fadeGraphic != null)
            {
                fadeScreen.SetActive(true);
                Color c = fadeGraphic.color;
                c.a = 0f;
                fadeGraphic.color = c;
            }
        }
    }

    void Update()
    {
        // ЕСЛИ ПРЕДМЕТ ВЫБРОШЕН (У него нет родителя-руки), ИГНОРИРУЕМ ВСЮ ЛОГИКУ ПОЕДАНИЯ
        if (transform.parent == null) return;

        if (isTriggered) return;

        // Перестраховка: если мы в руке, но ссылку на инвентарь почему-то потеряли — пытаемся найти её снова
        if (inventory == null) inventory = GetComponentInParent<PlayerInventory>();
        if (movement == null) movement = GetComponentInParent<PlayerMovement>();

        if (inventory == null || inventory.IsInventoryOpen || inventory.IsSwitchingItem)
        {
            ResetProgress();
            return;
        }

        // ЗАЖАТИЕ ЛКМ
        if (Input.GetMouseButton(0))
        {
            currentHoldTime += Time.deltaTime;

            float progress = currentHoldTime / holdDuration;
            transform.localPosition = Vector3.Lerp(originalLocalPos, originalLocalPos + eatingPositionOffset, progress);

            if (uiProgressBar != null)
            {
                uiProgressBar.gameObject.SetActive(true);
                uiProgressBar.value = progress;
            }

            if (currentHoldTime >= holdDuration)
            {
                StartCoroutine(TriggerTripRoutine());
            }
        }

        // ОТПУСТИЛ ЛКМ
        if (Input.GetMouseButtonUp(0))
        {
            ResetProgress();
        }
    }

    void ResetProgress()
    {
        // Сбрасываем позицию только если мы привязаны к руке, чтобы не ломать физику полета
        if (transform.parent != null)
        {
            transform.localPosition = originalLocalPos;
        }

        currentHoldTime = 0f;
        if (uiProgressBar != null) uiProgressBar.gameObject.SetActive(false);
    }

    private IEnumerator TriggerTripRoutine()
    {
        isTriggered = true;
        ResetProgress();

        // ВКЛЮЧАЕМ НАМЕРТВУЮ БЛОКИРОВКУ КЛАВИАТУРЫ И МЫШИ ДЛЯ ИНВЕНТАРЯ
        if (inventory != null) inventory.BlockInputForTrip();

        Debug.Log("КотонеZ проглочен! Эффекты активированы.");

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        if (movement != null) movement.enabled = false;

        Transform camTransform = movement != null ? movement.anatolyCamera : null;
        float effectTime = 0f;

        if (fadeScreen != null)
        {
            fadeScreen.SetActive(true);
            if (fadeGraphic == null) fadeGraphic = fadeScreen.GetComponent<Graphic>();
        }

        while (effectTime < fadeDuration)
        {
            effectTime += Time.deltaTime;
            float progress = effectTime / fadeDuration;

            // 1. Безумное покачивание камеры
            if (camTransform != null)
            {
                float zTilt = Mathf.Sin(Time.time * 12f) * 15f * (1f - progress);
                camTransform.localRotation = Quaternion.Euler(camTransform.localRotation.eulerAngles.x, camTransform.localRotation.eulerAngles.y, zTilt);
            }

            // 2. ПЛАВНОЕ ЗАТУХАНИЕ
            if (fadeGraphic != null)
            {
                Color c = fadeGraphic.color;
                c.a = Mathf.Clamp01(progress);
                fadeGraphic.color = c;
            }

            yield return null;
        }

        // === ИСПРАВЛЕНИЕ МИГАНИЯ СЦЕНЫ ===
        if (fadeGraphic != null)
        {
            Color c = fadeGraphic.color;
            c.a = 1f;
            fadeGraphic.color = c;
        }

        yield return new WaitForEndOfFrame();

        // Очищаем активный слот в инвентаре (логика данных)
        if (inventory != null) inventory.ClearActiveSlot();

        // ИЗМЕНЕНО: Загружаем сцену аддитивно через глобальный менеджер, чтобы мир сохранился
        if (GameSceneManager.Instance != null)
        {
            Debug.Log($"Отправляем Анатолия в трип аддитивно: {targetSceneName}");
            GameSceneManager.Instance.StartParkour(targetSceneName);
        }
        else
        {
            Debug.LogWarning("GameSceneManager не найден на сцене! Используем обычную перезагрузку сцены.");
            SceneManager.LoadScene(targetSceneName);
        }

        // ПОЛНОЕ УНИЧТОЖЕНИЕ ОБЪЕКТА КОТОНЕЗА ИЗ ПАМЯТИ
        Destroy(gameObject);
    }
}