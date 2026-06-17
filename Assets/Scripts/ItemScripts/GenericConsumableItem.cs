using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GenericConsumableItem : MonoBehaviour
{
    private float currentHoldTime = 0.0f;
    private Vector3 originalLocalPos;

    private PlayerInventory inventory;
    private AudioSource audioSource;
    private ItemData cachedItemData;
    private bool isTriggered = false;

    void Start()
    {
        inventory = GetComponentInParent<PlayerInventory>();
        originalLocalPos = transform.localPosition;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;

        if (inventory != null && cachedItemData == null)
        {
            cachedItemData = inventory.quickSlots[inventory.ActiveQuickSlot];
        }

        SetupSound();
    }

    public void InitializeData(ItemData data)
    {
        cachedItemData = data;
        SetupSound();
    }

    private void SetupSound()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (cachedItemData != null && cachedItemData.consumeSound != null)
        {
            audioSource.clip = cachedItemData.consumeSound;
        }
    }

    void Update()
    {
        if (transform.parent == null || isTriggered) return;
        if (inventory == null) inventory = GetComponentInParent<PlayerInventory>();

        if (inventory == null || inventory.IsInventoryOpen || inventory.IsSwitchingItem)
        {
            ResetProgress();
            return;
        }

        if (cachedItemData == null && inventory != null)
        {
            cachedItemData = inventory.quickSlots[inventory.ActiveQuickSlot];
            SetupSound();
        }

        if (cachedItemData == null || !cachedItemData.isConsumable) return;

        // Зажимаем ЛКМ — кушаем
        if (Input.GetMouseButton(0))
        {
            currentHoldTime += Time.deltaTime;

            if (audioSource.clip != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }

            float progress = currentHoldTime / cachedItemData.holdDuration;
            transform.localPosition = Vector3.Lerp(originalLocalPos, originalLocalPos + cachedItemData.eatingPositionOffset, progress);

            // КРАСИВО: Говорим инвентарю отобразить прогресс в центре экрана
            if (inventory != null)
            {
                inventory.UpdateProgressCircle(progress);
            }

            if (currentHoldTime >= cachedItemData.holdDuration)
            {
                ExecuteConsumption();
            }
        }

        // Отпустили ЛКМ
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

        // КРАСИВО: Говорим инвентарю спрятать кружок
        if (inventory != null)
        {
            inventory.HideProgressCircle();
        }
    }

    private void ExecuteConsumption()
    {
        isTriggered = true;
        ResetProgress();

        Debug.Log($"[Успех] Анатолий полностью сожрал: {cachedItemData.itemName}!");

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = false;

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