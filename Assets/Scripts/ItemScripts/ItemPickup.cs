using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Перетащи сюда файл данных предмета")]
    public ItemData itemData;

    // Этот метод будет вызывать сам игрок, когда посмотрит на предмет и нажмет F
    public void Interact(PlayerInventory playerInventory)
    {
        if (itemData == null)
        {
            Debug.LogError($"[Ошибка] На объекте {gameObject.name} не привязан файл ItemData в инспекторе!");
            return;
        }

        if (playerInventory == null)
        {
            Debug.LogError("[Ошибка] Скрипт подбора не получил ссылку на PlayerInventory игрока!");
            return;
        }

        Debug.Log($"[Попытка] Пытаемся положить {itemData.itemName} в инвентарь...");

        // Вызываем твой готовый метод из PlayerInventory
        if (playerInventory.AddItem(itemData))
        {
            Debug.Log($"[Успех] Предмет {itemData.itemName} подобран! Удаляем из мира.");
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning("[Отказ] Инвентарь вернул false (возможно нет места).");
        }
    }
}