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
        if (transform.parent == null) return;
        if (isTriggered) return;

        if (inventory == null) inventory = GetComponentInParent<PlayerInventory>();
        if (movement == null) movement = GetComponentInParent<PlayerMovement>();

        if (inventory == null || inventory.IsInventoryOpen || inventory.IsSwitchingItem)
        {
            ResetProgress();
            return;
        }

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

        if (Input.GetMouseButtonUp(0))
        {
            ResetProgress();
        }
    }

    void ResetProgress()
    {
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

            if (camTransform != null)
            {
                float zTilt = Mathf.Sin(Time.time * 12f) * 15f * (1f - progress);
                camTransform.localRotation = Quaternion.Euler(camTransform.localRotation.eulerAngles.x, camTransform.localRotation.eulerAngles.y, zTilt);
            }

            if (fadeGraphic != null)
            {
                Color c = fadeGraphic.color;
                c.a = Mathf.Clamp01(progress);
                fadeGraphic.color = c;
            }

            yield return null;
        }

        if (fadeGraphic != null)
        {
            Color c = fadeGraphic.color;
            c.a = 1f;
            fadeGraphic.color = c;
        }

        yield return new WaitForEndOfFrame();

        // ИСПРАВЛЕНО: Сначала запускаем аддитивную сцену
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.StartParkour(targetSceneName);
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }

        // ИСПРАВЛЕНО: Передаем СЕБЯ (конкретный экземпляр) инвентарю на съедение.
        // Инвентарь удалит только этот конкретный игровой объект, а банку во 2 слоте оставит в покое!
        if (inventory != null)
        {
            inventory.ConsumeSpecificItem(gameObject);
        }
        else
        {
            // На всякий случай, если инвентарь пропал
            Destroy(gameObject);
        }
    }
}