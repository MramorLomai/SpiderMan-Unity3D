using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderManController : SpiderMan
{
    [SerializeField] private Transform cameraTracker;
    [SerializeField] private float rotateSpeed;
    [SerializeField] private float walkSpeed = 3f; // Скорость для ходьбы
    [SerializeField] private float runSpeed = 6f;  // Скорость для бега
    [SerializeField] private float speedSmoothTime = 0.1f;
    [SerializeField] private AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    // Animator parameter IDs (cached)
    private int animIDSpeed;
    private int animIDMove;

    // Флаг для бега
    private bool isRunning = false;

    private void Start()
    {
        animIDMove = Animator.StringToHash("Move");
        animIDSpeed = Animator.StringToHash("Speed");
    }

    private void Update()
    {
        moveVelocity = Vector3.zero;
        horizontalAxis = Input.GetAxisRaw("Horizontal");
        verticalAxis = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horizontalAxis, 0f, verticalAxis).normalized;
        float animationSpeedPercent = ((isRunning) ? runSpeed : walkSpeed) * direction.magnitude;
        _animator.SetFloat(animIDSpeed, animationSpeedPercent, speedSmoothTime, Time.deltaTime);

        isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (!isSwinging) moveSpidey();
    }

    private void moveSpidey()
    {
        // Получаем направление движения относительно камеры
        Vector3 movementDirection = Vector3.zero;

        if (verticalAxis != 0 || horizontalAxis != 0)
        {
            // Направление относительно камеры
            Vector3 cameraForward = cameraTracker.forward;
            Vector3 cameraRight = cameraTracker.right;

            // Обнуляем вертикальную компоненту, чтобы персонаж не наклонялся
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // Вычисляем направление движения
            movementDirection = (cameraForward * verticalAxis + cameraRight * horizontalAxis).normalized;

            // Поворачиваем персонажа в направлении движения
            if (movementDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
            }

            // Выбираем скорость в зависимости от состояния бега
            float currentSpeed = isRunning ? runSpeed : walkSpeed;

            // Двигаем персонажа
            moveVelocity = movementDirection * currentSpeed;
        }

        // Для Blend Tree используем одно значение Speed
        // Idle: 0-1, Walk: 1-3, Sprint: 3-6
        float speedValue = moveVelocity.magnitude;

        // Если персонаж бежит, но скорость меньше порога бега, 
        // все равно передаем значение для активации анимации бега
        if (isRunning && speedValue > 0)
        {
            speedValue = Mathf.Max(speedValue, 4f); // Минимум 4 для уверенного перехода в бег
        }

        //_animator.SetFloat(animIDSpeed, speedValue);
        _animator.SetBool(animIDMove, moveVelocity.magnitude > 0.1f);
    }

    public void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f && FootstepAudioClips.Length > 0)
        {
            var index = Random.Range(0, FootstepAudioClips.Length);
            AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.position, FootstepAudioVolume);
        }
    }

    private void FixedUpdate()
    {
        rBody.velocity = moveVelocity + jumpVelocity;
    }
}