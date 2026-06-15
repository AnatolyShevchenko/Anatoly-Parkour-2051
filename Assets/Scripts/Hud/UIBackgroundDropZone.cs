using UnityEngine;
using UnityEngine.EventSystems;

public class UIBackgroundDropZone : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    private PlayerInventory inventory;

    void Start()
    {
        inventory = FindFirstObjectByType<PlayerInventory>();
    }

    // Сработает, если просто кликнули по фону
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging) return; // Игнорируем, если это был конец перетаскивания
        DropItem();
    }

    // Сработает, если перетащили вещь из слота и отпустили над фоном
    public void OnDrop(PointerEventData eventData)
    {
        DropItem();
    }

    private void DropItem()
    {
        if (UICursorItem.Instance.IsHoldingItem() && inventory != null)
        {
            ItemData itemToDrop = UICursorItem.Instance.currentHoldingItem;

            if (itemToDrop.itemPrefab != null)
            {
                // Используем точку руки (handTransform) вместо тела игрока!
                Transform launchPoint = inventory.handTransform;

                // Если вдруг handTransform не назначен, подстрахуемся телом игрока
                if (launchPoint == null) launchPoint = inventory.transform;

                // Спавним чуть впереди направления взгляда
                Vector3 spawnPosition = launchPoint.position + launchPoint.forward * 0.6f + Vector3.up * 0.1f;

                // Спавним с поворотом камеры/руки
                GameObject droppedObj = Instantiate(itemToDrop.itemPrefab, spawnPosition, launchPoint.rotation);

                Rigidbody objRb = droppedObj.GetComponent<Rigidbody>();
                if (objRb != null)
                {
                    // Включаем физику
                    objRb.isKinematic = false;

                    // Даем пинок точно туда, куда направлен взгляд из руки
                    Vector3 throwDirection = (launchPoint.forward + Vector3.up * 0.1f).normalized;
                    objRb.AddForce(throwDirection * 3.0f, ForceMode.Impulse);
                }
            }

            UICursorItem.Instance.SetItem(null);
        }
    }
}