using UnityEngine;

[CreateAssetMenu(fileName = "New Item Data", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;
    public GameObject itemPrefab;

    [Header("Настройки удержания в руке")]
    [Tooltip("Смещение относительно центра руки HandHolder")]
    public Vector3 handPositionOffset = Vector3.zero;

    [Tooltip("Поворот предмета в руке (в градусах)")]
    public Vector3 handRotationOffset = Vector3.zero;
}