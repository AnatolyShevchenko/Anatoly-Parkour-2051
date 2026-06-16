using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TripTimerManager : MonoBehaviour
{
    [Header("Настройки времени (в секундах)")]
    [Tooltip("1.5 минуты = 90 секунд")]
    public float tripDuration = 90f;

    [Header("Настройки обратного затухания")]
    public float fadeDuration = 2f;

    [Header("Имя домашней сцены (Запасной вариант)")]
    [Tooltip("Используется только если GameSceneManager сломался")]
    public string returnSceneName = "MainLevel";

    [Header("Ссылка на UI затухания")]
    [Tooltip("Сюда можно перетащить Panel или Image черного экрана этой сцены")]
    public GameObject fadeScreen;

    private Graphic fadeGraphic;
    private bool isReturning = false;

    void Start()
    {
        // Настраиваем экран затухания, если он привязан
        if (fadeScreen != null)
        {
            fadeGraphic = fadeScreen.GetComponent<Graphic>();
            if (fadeGraphic != null)
            {
                fadeScreen.SetActive(true);

                // На старте сцены паркура делаем экран наоборот — плавно ПРОЯВЛЯЮЩИМСЯ из черного
                StartCoroutine(FadeInRoutine());
            }
        }

        // Запускаем основной таймер трипа
        StartCoroutine(TripCountdownRoutine());
    }

    // Корутина плавного проявления сцены при старте паркура
    private IEnumerator FadeInRoutine()
    {
        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            if (fadeGraphic != null)
            {
                Color c = fadeGraphic.color;
                c.a = Mathf.Clamp01(1f - (time / fadeDuration));
                fadeGraphic.color = c;
            }
            yield return null;
        }

        if (fadeScreen != null) fadeScreen.SetActive(false);
    }

    // Корутина отсчета 1.5 минут
    private IEnumerator TripCountdownRoutine()
    {
        // Ждем ровно заданное количество секунд (90 секунд)
        yield return new WaitForSeconds(tripDuration);

        // По истечении времени запускаем процесс возврата
        StartCoroutine(ReturnToRealWorldRoutine());
    }

    // Корутина плавного затухания и перезагрузки основной сцены
    private IEnumerator ReturnToRealWorldRoutine()
    {
        if (isReturning) yield break;
        isReturning = true;

        Debug.Log("Время трипа истекло! Возвращаем Анатолия в реальность...");

        // Включаем черный экран обратно
        if (fadeScreen != null)
        {
            fadeScreen.SetActive(true);
            if (fadeGraphic == null) fadeGraphic = fadeScreen.GetComponent<Graphic>();
        }

        // Плавно уводим экран в темноту
        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            if (fadeGraphic != null)
            {
                Color c = fadeGraphic.color;
                c.a = Mathf.Clamp01(time / fadeDuration);
                fadeGraphic.color = c;
            }
            yield return null;
        }

        // Намертво фиксируем черноту
        if (fadeGraphic != null)
        {
            Color c = fadeGraphic.color;
            c.a = 1f;
            fadeGraphic.color = c;
        }

        // Ждем физического рендера черного кадра, чтобы избежать мигания при обратном переходе
        yield return new WaitForEndOfFrame();

        // ИЗМЕНЕНО: Возвращаемся через глобальный менеджер выгрузки сцен, восстанавливая старый мир
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.ReturnToMainScene();
        }
        else
        {
            Debug.LogWarning("GameSceneManager не найден! Полная перезагрузка сцены вслепую.");
            SceneManager.LoadScene(returnSceneName);
        }
    }
}