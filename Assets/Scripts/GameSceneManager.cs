using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    private List<GameObject> mainSceneRootObjects = new List<GameObject>();
    private string currentParkourScene;

    private Canvas overlayCanvas;
    private Image overlayImage;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateOverlay();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CreateOverlay()
    {
        // ИСПРАВЛЕНО: Создаем Canvas как КОРНЕВОЙ объект (без родителя), 
        // чтобы он никогда не ломал разметку и не сжимался в левый нижний угол
        GameObject canvasObj = new GameObject("TransitionCanvas");
        overlayCanvas = canvasObj.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 9999; // Поверх абсолютно всего в игре

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Защищаем сам Canvas от удаления при чистке сцен
        DontDestroyOnLoad(canvasObj);

        GameObject imgObj = new GameObject("BlackOverlay");
        // ИСПРАВЛЕНО: Передаем false, чтобы RectTransform не искажался под влиянием мировых координат
        imgObj.transform.SetParent(canvasObj.transform, false);
        overlayImage = imgObj.AddComponent<Image>();
        overlayImage.color = Color.black;

        RectTransform rect = overlayImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        canvasObj.SetActive(false);
    }

    public void StartParkour(string parkourSceneName)
    {
        currentParkourScene = parkourSceneName;
        StartCoroutine(LoadParkourRoutine());
    }

    private IEnumerator LoadParkourRoutine()
    {
        overlayImage.color = Color.black;
        overlayCanvas.gameObject.SetActive(true);

        Scene mainScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = mainScene.GetRootGameObjects();

        mainSceneRootObjects.Clear();
        foreach (GameObject obj in rootObjects)
        {
            if (obj == gameObject) continue;

            // Защищаем наш технический Canvas от выключения
            if (obj == overlayCanvas.gameObject) continue;

            if (obj.activeSelf)
            {
                mainSceneRootObjects.Add(obj);
                obj.SetActive(false);
            }
        }

        AsyncOperation loadAsync = SceneManager.LoadSceneAsync(currentParkourScene, LoadSceneMode.Additive);
        while (!loadAsync.isDone)
        {
            yield return null;
        }

        Scene parkourScene = SceneManager.GetSceneByName(currentParkourScene);
        SceneManager.SetActiveScene(parkourScene);

        overlayCanvas.gameObject.SetActive(false);
    }

    public void ReturnToMainScene()
    {
        StartCoroutine(UnloadParkourRoutine());
    }

    private IEnumerator UnloadParkourRoutine()
    {
        overlayImage.color = Color.black;
        overlayCanvas.gameObject.SetActive(true);

        AsyncOperation unloadAsync = SceneManager.UnloadSceneAsync(currentParkourScene);
        while (!unloadAsync.isDone)
        {
            yield return null;
        }

        Scene mainScene = SceneManager.GetActiveScene();

        foreach (GameObject obj in mainSceneRootObjects)
        {
            if (obj != null) obj.SetActive(true);
        }

        // 1. Возвращаем управление инвентарю и НАМЕРТВУЮ убираем старый экран затухания
        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory != null)
        {
            inventory.UnblockInputAfterTrip();

            if (inventory.globalFadeScreen != null)
            {
                Graphic mainFadeGraphic = inventory.globalFadeScreen.GetComponent<Graphic>();
                if (mainFadeGraphic != null)
                {
                    Color c = mainFadeGraphic.color;
                    c.a = 0f;
                    mainFadeGraphic.color = c;
                }

                // ИСПРАВЛЕНО: Полностью выключаем игровой объект экрана затухания,
                // чтобы он физически не мог отрисовать однофреймовый баг разметки поверх твоего HUD
                inventory.globalFadeScreen.SetActive(false);
            }
        }

        // 2. Активируем ноги Анатолия
        PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
        if (movement != null)
        {
            movement.enabled = true;

            if (movement.anatolyCamera != null)
            {
                Vector3 camRot = movement.anatolyCamera.localRotation.eulerAngles;
                movement.anatolyCamera.localRotation = Quaternion.Euler(camRot.x, camRot.y, 0f);
            }
        }

        // 3. Запираем мышь обратно в игру
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 4. Плавно убираем чистый, нелагающий оверлей
        float time = 0f;
        float fadeDuration = 2f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            Color c = overlayImage.color;
            c.a = Mathf.Clamp01(1f - (time / fadeDuration));
            overlayImage.color = c;
            yield return null;
        }

        overlayCanvas.gameObject.SetActive(false);
    }
}