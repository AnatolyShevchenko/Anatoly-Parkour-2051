using UnityEngine;

public class BouncingSausages : MonoBehaviour
{
    [Header("Объекты колбасок")]
    [Tooltip("Перетащи сюда дочерние объекты колбасок из префаба")]
    public Transform[] sausages;

    [Header("Ограничения (Чтобы не падали с хлеба)")]
    public float maxDrift = 0.03f;

    [Header("Настройки Инерции (Движение мыши)")]
    public float mouseSensitivity = 0.01f;
    public float mouseBounceHeight = 0.02f;
    public float inertiaSmoothness = 8f;

    [Header("Настройки ХОДЬБЫ (Жесткий турбо-режим)")]
    public float walkMoveSpeed = 15f;
    public float walkBounceHeight = 0.045f;
    public float walkMinTime = 0.02f;
    public float walkMaxTime = 0.12f;

    [Header("Настройки ПРЫЖКА ИГРОКА (Мега-взрыв)")]
    public float playerJumpBounceHeight = 0.09f; // Почти в 2 раза выше, чем при ходьбе!
    public float playerJumpMoveSpeed = 22f;     // Максимальная резкость взлета

    [Header("Эффект желе (Наклон при прыжке)")]
    public float jellyTiltAmount = 25f;

    private Vector3[] startPositions;
    private Vector3[] targetPositions;
    private Quaternion[] startRotations;
    private float[] moveTimers;
    private float[] jumpProgress;
    private bool[] isMegaJumping; // Флаг: находится ли конкретная колбаска в режиме овер-прыжка

    private Rigidbody playerRb;

    void Start()
    {
        if (sausages == null || sausages.Length == 0) return;

        startPositions = new Vector3[sausages.Length];
        targetPositions = new Vector3[sausages.Length];
        startRotations = new Quaternion[sausages.Length];
        moveTimers = new float[sausages.Length];
        jumpProgress = new float[sausages.Length];
        isMegaJumping = new bool[sausages.Length];

        for (int i = 0; i < sausages.Length; i++)
        {
            if (sausages[i] != null)
            {
                startPositions[i] = sausages[i].localPosition;
                targetPositions[i] = startPositions[i];
                startRotations[i] = sausages[i].localRotation;
                jumpProgress[i] = 1f;
                isMegaJumping[i] = false;
            }
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) playerRb = player.GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Только если Бутермур в руке
        if (transform.parent == null)
        {
            ResetSausagesToDefault();
            return;
        }

        // ==========================================
        // ЛОВИМ НАЖАТИЕ ПРОБЕЛА (ПРЫЖОК ИГРОКА)
        // ==========================================
        if (Input.GetKeyDown(KeyCode.Space))
        {
            for (int i = 0; i < sausages.Length; i++)
            {
                if (sausages[i] == null) continue;

                isMegaJumping[i] = true;
                jumpProgress[i] = 0f; // Сбрасываем анимацию в ноль для мощного старта

                // Разбрасываем их в случайные стороны чуть сильнее обычного от перегрузки
                float randomX = Random.Range(-maxDrift * 1.2f, maxDrift * 1.2f);
                float randomZ = Random.Range(-maxDrift * 1.2f, maxDrift * 1.2f);
                targetPositions[i] = startPositions[i] + new Vector3(randomX, startPositions[i].y, randomZ);
            }
        }

        // Проверка ходьбы для остальных состояний
        bool isWalking = false;
        if (playerRb != null)
        {
            isWalking = new Vector3(playerRb.linearVelocity.x, 0, playerRb.linearVelocity.z).magnitude > 0.2f;
        }
        else
        {
            isWalking = (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f);
        }

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        bool isMouseMoving = (Mathf.Abs(mouseX) > 0.05f || Mathf.Abs(mouseY) > 0.05f);


        // ПРОСЧЕТ КАЖДОЙ КОЛБАСКИ
        for (int i = 0; i < sausages.Length; i++)
        {
            if (sausages[i] == null) continue;

            // --------------------------------------------------
            // ОВЕРРАЙД СТАТУС: МЕГА-ПРЫЖОК (Прыгает сам игрок)
            // --------------------------------------------------
            if (isMegaJumping[i])
            {
                // Наращиваем прогресс дуги с огромной скоростью
                jumpProgress[i] += Time.deltaTime * playerJumpMoveSpeed;

                float jumpArc = Mathf.Sin(Mathf.Clamp01(jumpProgress[i]) * Mathf.PI);
                float newY = startPositions[i].y + (jumpArc * playerJumpBounceHeight);

                Vector3 currentLocalPos = sausages[i].localPosition;
                float newX = Mathf.Lerp(currentLocalPos.x, targetPositions[i].x, Time.deltaTime * playerJumpMoveSpeed);
                float newZ = Mathf.Lerp(currentLocalPos.z, targetPositions[i].z, Time.deltaTime * playerJumpMoveSpeed);

                sausages[i].localPosition = new Vector3(newX, newY, newZ);

                // Экстремальное заваливание на бок (эффект перегрузки)
                Vector3 moveDirection = (targetPositions[i] - currentLocalPos).normalized;
                if (jumpArc > 0.01f && moveDirection.magnitude > 0.1f)
                {
                    float tiltX = moveDirection.z * (jellyTiltAmount * 1.6f) * jumpArc;
                    float tiltZ = -moveDirection.x * (jellyTiltAmount * 1.6f) * jumpArc;
                    Quaternion targetRot = startRotations[i] * Quaternion.Euler(tiltX, 0, tiltZ);
                    sausages[i].localRotation = Quaternion.Slerp(sausages[i].localRotation, targetRot, Time.deltaTime * playerJumpMoveSpeed);
                }

                // Прыжок завершен — выключаем оверрайд
                if (jumpProgress[i] >= 1f)
                {
                    isMegaJumping[i] = false;
                    moveTimers[i] = Random.Range(walkMinTime, walkMaxTime); // Сбрасываем обычный таймер
                }

                continue; // Переходим к следующей колбаске, игнорируя код ходьбы/мыши ниже
            }

            // --------------------------------------------------
            // ОБЫЧНЫЙ РЕЖИМ 1: ЖЕСТКАЯ ХОДЬБА
            // --------------------------------------------------
            if (isWalking)
            {
                moveTimers[i] -= Time.deltaTime;
                if (moveTimers[i] <= 0f)
                {
                    float randomX = Random.Range(-maxDrift, maxDrift);
                    float randomZ = Random.Range(-maxDrift, maxDrift);

                    targetPositions[i] = startPositions[i] + new Vector3(randomX, startPositions[i].y, randomZ);
                    moveTimers[i] = Random.Range(walkMinTime, walkMaxTime);
                    jumpProgress[i] = 0f;
                }

                Vector3 currentLocalPos = sausages[i].localPosition;
                float newX = Mathf.Lerp(currentLocalPos.x, targetPositions[i].x, Time.deltaTime * walkMoveSpeed);
                float newZ = Mathf.Lerp(currentLocalPos.z, targetPositions[i].z, Time.deltaTime * walkMoveSpeed);

                if (jumpProgress[i] < 1f) jumpProgress[i] += Time.deltaTime * (walkMoveSpeed * 1.5f);
                float jumpArc = Mathf.Sin(Mathf.Clamp01(jumpProgress[i]) * Mathf.PI);
                float newY = startPositions[i].y + (jumpArc * walkBounceHeight);

                sausages[i].localPosition = new Vector3(newX, newY, newZ);

                Vector3 moveDirection = (targetPositions[i] - currentLocalPos).normalized;
                if (jumpArc > 0.01f && moveDirection.magnitude > 0.1f)
                {
                    float tiltX = moveDirection.z * jellyTiltAmount * jumpArc;
                    float tiltZ = -moveDirection.x * jellyTiltAmount * jumpArc;
                    Quaternion targetRot = startRotations[i] * Quaternion.Euler(tiltX, 0, tiltZ);
                    sausages[i].localRotation = Quaternion.Slerp(sausages[i].localRotation, targetRot, Time.deltaTime * walkMoveSpeed);
                }
            }
            // --------------------------------------------------
            // ОБЫЧНЫЙ РЕЖИМ 2: ИНЕРЦИЯ МЫШИ
            // --------------------------------------------------
            else if (isMouseMoving)
            {
                float targetInertiaX = Mathf.Clamp(-mouseX * mouseSensitivity, -maxDrift, maxDrift);
                float targetInertiaZ = Mathf.Clamp(-mouseY * mouseSensitivity, -maxDrift, maxDrift);

                float individualOffset = (i + 1) * 0.15f;
                Vector3 targetPos = startPositions[i] + new Vector3(targetInertiaX, 0, targetInertiaZ) * (1f + individualOffset);

                sausages[i].localPosition = Vector3.Lerp(sausages[i].localPosition, targetPos, Time.deltaTime * inertiaSmoothness);

                float mouseSpeedCombined = (Mathf.Abs(mouseX) + Mathf.Abs(mouseY));
                float jumpY = Mathf.Sin(Mathf.Clamp01(mouseSpeedCombined * 0.1f) * Mathf.PI) * mouseBounceHeight;

                sausages[i].localPosition = new Vector3(sausages[i].localPosition.x, startPositions[i].y + jumpY, sausages[i].localPosition.z);

                float tiltX = -mouseY * jellyTiltAmount * 0.5f;
                float tiltZ = mouseX * jellyTiltAmount * 0.5f;
                Quaternion targetRot = startRotations[i] * Quaternion.Euler(tiltX, 0, tiltZ);
                sausages[i].localRotation = Quaternion.Slerp(sausages[i].localRotation, targetRot, Time.deltaTime * inertiaSmoothness);
            }
            // --------------------------------------------------
            // ОБЫЧНЫЙ РЕЖИМ 3: ПОЛНЫЙ ПОКОЙ
            // --------------------------------------------------
            else
            {
                sausages[i].localPosition = Vector3.Lerp(sausages[i].localPosition, startPositions[i], Time.deltaTime * inertiaSmoothness);
                sausages[i].localRotation = Quaternion.Slerp(sausages[i].localRotation, startRotations[i], Time.deltaTime * inertiaSmoothness);
                targetPositions[i] = startPositions[i];
            }
        }
    }

    void ResetSausagesToDefault()
    {
        for (int i = 0; i < sausages.Length; i++)
        {
            if (sausages[i] != null)
            {
                isMegaJumping[i] = false;
                sausages[i].localPosition = Vector3.Lerp(sausages[i].localPosition, startPositions[i], Time.deltaTime * 10f);
                sausages[i].localRotation = Quaternion.Slerp(sausages[i].localRotation, startRotations[i], Time.deltaTime * 10f);
                targetPositions[i] = startPositions[i];
            }
        }
    }
}