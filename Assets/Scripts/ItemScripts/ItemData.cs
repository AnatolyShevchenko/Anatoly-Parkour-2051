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

    [Header("Настройки Поедания (Идентично КотонеZу)")]
    [Tooltip("Поставьте галочку, если этот предмет можно съесть/выпить")]
    public bool isConsumable;

    [Tooltip("Сколько секунд нужно удерживать ЛКМ для съедения")]
    public float holdDuration = 2.0f;

    [Tooltip("Куда смещается моделька еды во время зажатия ЛКМ")]
    public Vector3 eatingPositionOffset = new Vector3(0, -0.15f, 0.1f);

    [Tooltip("Звук, который будет циклично играть, пока Анатолий это хомячит")]
    public AudioClip consumeSound;
}