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

    // Ссылки на компоненты игрока (Инвентарь настроит fadeScreen сам при спавне)
    [HideInInspector] public GameObject fadeScreen;
    private PlayerInventory inventory;
    private PlayerMovement movement;
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

        // ИСПРАВЛЕНО: При старте гарантируем, что черный экран выключен и не мешает кликать
        if (fadeScreen != null)
        {
            fadeGraphic = fadeScreen.GetComponent<Graphic>();
            fadeScreen.SetActive(false);
        }
    }

    void Update()
    {
        if (transform.parent == null || isTriggered) return;

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

            if (inventory != null)
            {
                inventory.UpdateProgressCircle(progress);
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

        if (inventory != null)
        {
            inventory.HideProgressCircle();
        }
    }

    private IEnumerator TriggerTripRoutine()
    {
        isTriggered = true;

        if (inventory != null)
        {
            inventory.HideProgressCircle();
            inventory.BlockInputForTrip();
        }

        Debug.Log("КотонеZ проглочен! Эффекты активированы.");

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        if (movement != null) movement.enabled = false;

        Transform camTransform = movement != null ? movement.anatolyCamera : null;
        float effectTime = 0f;

        // ИСПРАВЛЕНО: Включаем объект в иерархии СТРОГО в момент начала трипа
        if (fadeScreen != null)
        {
            fadeScreen.SetActive(true);
            if (fadeGraphic == null) fadeGraphic = fadeScreen.GetComponent<Graphic>();

            // Начинаем с полной прозрачности
            Color c = fadeGraphic.color;
            c.a = 0f;
            fadeGraphic.color = c;
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

            // Плавно закрашиваем экран в черный
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