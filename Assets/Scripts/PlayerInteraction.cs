using UnityEngine;
using TMPro; // НОВОЕ: Обязательно для работы с TextMeshPro

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Distance from which the player can reach an item")]
    public float interactionDistance = 3f;

    [Tooltip("Assign the Items layer here")]
    public LayerMask interactableLayer;

    [Header("UI Hints")]
    [Tooltip("Drag your TextMeshPro - Text object here from the Canvas")]
    public TextMeshProUGUI interactionText; // Использован правильный тип для TMP в Canvas

    private Transform cameraTransform;
    private PlayerInventory playerInventory;

    void Start()
    {
        playerInventory = GetComponent<PlayerInventory>();

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            cameraTransform = movement.anatolyCamera;
        }

        // Прячем подсказку на старте игры
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (cameraTransform == null || playerInventory == null) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        bool hitAnItem = false;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
        {
            ItemPickup item = hit.collider.GetComponent<ItemPickup>();

            if (item != null && item.itemData != null)
            {
                hitAnItem = true;

                // Выводим текст на английском через TextMeshPro
                if (interactionText != null)
                {
                    interactionText.text = $"[F] Pick up {item.itemData.itemName}";
                    interactionText.gameObject.SetActive(true);
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    item.Interact(playerInventory);
                    if (interactionText != null) interactionText.gameObject.SetActive(false);
                }
            }
        }

        // Если луч ушел с предмета — выключаем подсказку
        if (!hitAnItem && interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }
    }
}