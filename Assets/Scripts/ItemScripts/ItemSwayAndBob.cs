using UnityEngine;

public class ItemSwayAndBob : MonoBehaviour
{
    [Header("Sway (Сдвиг от движения мыши)")]
    public float swayAmount = 0.015f;
    public float maxSwayAmount = 0.05f;
    public float swaySmoothness = 6f;

    [Header("Sway Rotation (Наклон от движения мыши)")]
    public float rotationSwayAmount = 4f;
    public float maxRotationSway = 10f;
    public float rotationSmoothness = 5f;

    [Header("Bobbing (Покачивание при ходьбе)")]
    public float bobSpeed = 12f;
    public float bobAmountX = 0.02f; // Амплитуда влево-вправо
    public float bobAmountY = 0.015f; // Амплитуда вверх-вниз

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float bobTimer = 0f;

    void Start()
    {
        // Запоминаем исходную позицию контейнера относительно камеры
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    void Update()
    {
        // 1. РАСЧЕТ SWAY (Движение мыши)
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Позиционный сдвиг (направление противоположно движению мыши)
        float moveX = Mathf.Clamp(-mouseX * swayAmount, -maxSwayAmount, maxSwayAmount);
        float moveY = Mathf.Clamp(-mouseY * swayAmount, -maxSwayAmount, maxSwayAmount);
        Vector3 DocsSwayOffset = new Vector3(moveX, moveY, 0);

        // Вращательный наклон (скручивание предмета при поворотах)
        float tiltX = Mathf.Clamp(mouseY * rotationSwayAmount, -maxRotationSway, maxRotationSway);
        float tiltY = Mathf.Clamp(-mouseX * rotationSwayAmount, -maxRotationSway, maxRotationSway);
        Quaternion targetSwayRotation = Quaternion.Euler(tiltX, tiltY, tiltY * 0.5f);


        // 2. РАСЧЕТ BOBBING (Ходьба)
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");

        // Проверяем, жмет ли игрок на клавиши движения W, A, S, D
        bool isMoving = (Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputY) > 0.1f);

        Vector3 currentBobOffset = Vector3.zero;

        if (isMoving)
        {
            // Наращиваем таймер синусоиды во времени
            bobTimer += Time.deltaTime * bobSpeed;

            // Классическая математическая "восьмерка" для FPS-игр
            float bobX = Mathf.Sin(bobTimer) * bobAmountX;
            float bobY = Mathf.Sin(bobTimer * 2f) * bobAmountY;

            currentBobOffset = new Vector3(bobX, bobY, 0);
        }
        else
        {
            // Если стоим на месте, плавно возвращаем таймер к нулю
            bobTimer = Mathf.Lerp(bobTimer, 0f, Time.deltaTime * 5f);
        }


        // 3. ПРИМЕНЕНИЕ РЕЗУЛЬТАТОВ
        // Складываем стартовую позицию, эффект увода мыши и эффект шагов
        Vector3 finalTargetPosition = startPosition + DocsSwayOffset + currentBobOffset;
        Quaternion finalTargetRotation = startRotation * targetSwayRotation;

        // Плавно двигаем контейнер к финальным значениям
        transform.localPosition = Vector3.Lerp(transform.localPosition, finalTargetPosition, Time.deltaTime * swaySmoothness);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, finalTargetRotation, Time.deltaTime * rotationSmoothness);
    }
}