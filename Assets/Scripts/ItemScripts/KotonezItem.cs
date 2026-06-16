using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class KotonezItem : MonoBehaviour
{
    [Header("Настройки удержания (Поедание)")]
    public float holdDuration = 2.0f;
    private float currentHoldTime = 0.0f;

    [Header("Звуковые эффекты")]
    [Tooltip("Звук, который будет циклично играть, ПОКА зажата кнопка поедания")]
    public AudioClip eatingLoopSound;

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

    // ИСПРАВЛЕНО: Теперь используем Image вместо Slider для эффекта "бублика"
    private Image uiProgressCircle;

    private Graphic fadeGraphic;
    private bool isTriggered = false;
    private AudioSource audioSource;

    void Start()
    {
        inventory = GetComponentInParent<PlayerInventory>();
        movement = GetComponentInParent<PlayerMovement>();
        originalLocalPos = transform.localPosition;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        if (eatingLoopSound != null)
        {
            audioSource.clip = eatingLoopSound;
        }

        // ИСПРАВЛЕНО: Ищем наш круглый индикатор по имени UseProgressCircle
        GameObject circleObj = GameObject.Find("UseProgressCircle");
        if (circleObj != null)
        {
            uiProgressCircle = circleObj.GetComponent<Image>();
            uiProgressCircle.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[КотонеZ] Не найден UI объект с именем 'UseProgressCircle'!");
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

            if (audioSource.clip != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }

            float progress = currentHoldTime / holdDuration;
            transform.localPosition = Vector3.Lerp(originalLocalPos, originalLocalPos + eatingPositionOffset, progress);

            // ИСПРАВЛЕНО: Заполняем "бублик" через fillAmount (значение от 0.0f до 1.0f)
            if (uiProgressCircle != null)
            {
                uiProgressCircle.gameObject.SetActive(true);
                uiProgressCircle.fillAmount = progress;
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

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        currentHoldTime = 0f;

        // ИСПРАВЛЕНО: Прячем круг при отмене
        if (uiProgressCircle != null) uiProgressCircle.gameObject.SetActive(false);
    }

    private IEnumerator TriggerTripRoutine()
    {
        isTriggered = true;

        // Прячем заполняющийся кружок, так как Анатолий уже всё съел
        if (uiProgressCircle != null) uiProgressCircle.gameObject.SetActive(false);

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

        if (audioSource != null) audioSource.Stop();

        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.StartParkour(targetSceneName);
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }

        if (inventory != null)
        {
            inventory.ConsumeSpecificItem(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}