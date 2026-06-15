using UnityEngine;
using UnityEngine.UI;

public class UICursorItem : MonoBehaviour
{
    public static UICursorItem Instance;

    private Image iconImage;
    [HideInInspector] public ItemData currentHoldingItem;

    void Awake()
    {
        Instance = this;
        iconImage = GetComponent<Image>();
    }

    void Start()
    {
        UpdateCursorUI();
    }

    void Update()
    {
        // Если в руке есть предмет, картинка летает за системным курсором
        if (currentHoldingItem != null)
        {
            transform.position = Input.mousePosition;
        }
    }

    public void SetItem(ItemData newItem)
    {
        currentHoldingItem = newItem;
        UpdateCursorUI();
    }

    public bool IsHoldingItem()
    {
        return currentHoldingItem != null;
    }

    private void UpdateCursorUI()
    {
        if (currentHoldingItem == null)
        {
            iconImage.enabled = false; // Рука пустая — прячем иконку
        }
        else
        {
            iconImage.sprite = currentHoldingItem.itemIcon; // Берем иконку из ItemData
            iconImage.enabled = true;
        }
    }
}