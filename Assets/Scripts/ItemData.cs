using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;

    // ДОБАВЬ ВОТ ЭТУ СТРОЧКУ:
    public GameObject itemPrefab;
}