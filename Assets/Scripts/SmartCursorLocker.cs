using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Умный скрипт для управления блокировкой курсора с поддержкой различных режимов и событий
/// </summary>
public class SmartCursorLocker : MonoBehaviour
{
    [Header("Основные настройки")]
    [SerializeField] private bool lockCursorOnStart = true;
    [SerializeField] private bool lockCursorOnFocus = true;
    [SerializeField] private bool showCursorOnPause = true;
    [SerializeField] private KeyCode toggleLockKey = KeyCode.Escape;
    [SerializeField] private KeyCode forceUnlockKey = KeyCode.F12;

    [Header("Режимы блокировки")]
    [SerializeField] private CursorLockMode defaultLockMode = CursorLockMode.Locked;
    [SerializeField] private CursorLockMode unlockedMode = CursorLockMode.None;

    [Header("Визуальные настройки")]
    [SerializeField] private bool hideCursorWhenLocked = true;
    [SerializeField] private bool showCursorWhenUnlocked = true;

    [Header("UI взаимодействие")]
    [SerializeField] private bool autoUnlockOverUI = true;
    [SerializeField] private LayerMask uiLayer = 1 << 5; // UI слой по умолчанию

    // События для других скриптов
    public delegate void CursorStateChanged(bool isLocked);
    public event CursorStateChanged OnCursorStateChanged;

    public delegate void CursorVisibilityChanged(bool isVisible);
    public event CursorVisibilityChanged OnCursorVisibilityChanged;

    // Текущее состояние
    public bool IsCursorLocked { get; private set; }
    public bool IsCursorVisible { get; private set; }
    public CursorLockMode CurrentLockMode { get; private set; }

    // Для отслеживания UI
    private PointerEventData _pointerEventData;
    private EventSystem _eventSystem;
    private float _lastUnlockTime;
    private bool _wasLockedBeforePause;
    private bool _isPaused;

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        if (lockCursorOnStart)
        {
            LockCursor();
        }
        else
        {
            UnlockCursor();
        }
    }

    private void InitializeComponents()
    {
        _eventSystem = EventSystem.current;
        if (_eventSystem == null)
        {
            Debug.LogWarning("EventSystem не найден. Создаю новый...");
            _eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            _eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }

        _pointerEventData = new PointerEventData(_eventSystem);
    }

    private void Update()
    {
        HandleInput();
        HandleUIAutoUnlock();
        HandlePause();
    }

    private void HandleInput()
    {
        // Переключение блокировки по клавише
        if (Input.GetKeyDown(toggleLockKey))
        {
            ToggleCursorLock();
        }

        // Принудительная разблокировка
        if (Input.GetKeyDown(forceUnlockKey))
        {
            ForceUnlockCursor();
        }

        // Блокировка по клику мыши (если разблокирована и не над UI)
        if (Input.GetMouseButtonDown(0) && !IsCursorLocked && !IsPointerOverUI())
        {
            LockCursor();
        }
    }

    private void HandleUIAutoUnlock()
    {
        if (!autoUnlockOverUI || !IsCursorLocked) return;

        // Проверяем, находится ли курсор над UI
        if (IsPointerOverUI())
        {
            // Временно разблокируем курсор
            UnlockCursor();
            _lastUnlockTime = Time.time;
        }
        else if (!IsCursorLocked && Time.time - _lastUnlockTime > 0.5f)
        {
            // Блокируем обратно, если прошло достаточно времени
            LockCursor();
        }
    }

    private void HandlePause()
    {
        if (!showCursorOnPause) return;

        // Проверяем паузу (например, при открытии меню)
        bool isPaused = Time.timeScale == 0;

        if (isPaused != _isPaused)
        {
            if (isPaused)
            {
                _wasLockedBeforePause = IsCursorLocked;
                UnlockCursor();
            }
            else
            {
                if (_wasLockedBeforePause)
                {
                    LockCursor();
                }
            }
            _isPaused = isPaused;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (lockCursorOnFocus && hasFocus && !IsPointerOverUI())
        {
            LockCursor();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && IsCursorLocked)
        {
            UnlockCursor();
            _wasLockedBeforePause = true;
        }
    }

    /// <summary>
    /// Блокировка курсора
    /// </summary>
    public void LockCursor()
    {
        SetCursorState(true, defaultLockMode);
    }

    /// <summary>
    /// Разблокировка курсора
    /// </summary>
    public void UnlockCursor()
    {
        SetCursorState(false, unlockedMode);
    }

    /// <summary>
    /// Переключение состояния курсора
    /// </summary>
    public void ToggleCursorLock()
    {
        if (IsCursorLocked)
            UnlockCursor();
        else
            LockCursor();
    }

    /// <summary>
    /// Принудительная разблокировка (игнорирует проверки)
    /// </summary>
    public void ForceUnlockCursor()
    {
        SetCursorState(false, unlockedMode, true);
        Debug.Log("Курсор принудительно разблокирован");
    }

    private void SetCursorState(bool lockCursor, CursorLockMode lockMode, bool force = false)
    {
        // Проверяем, не над UI ли курсор
        if (!force && lockCursor && IsPointerOverUI())
        {
            Debug.Log("Не блокирую курсор - он над UI элементом");
            return;
        }

        // Устанавливаем состояние
        Cursor.lockState = lockMode;
        Cursor.visible = !(lockCursor && hideCursorWhenLocked) && (showCursorWhenUnlocked || !lockCursor);

        // Обновляем состояние
        bool wasLocked = IsCursorLocked;
        bool wasVisible = IsCursorVisible;

        IsCursorLocked = lockCursor;
        IsCursorVisible = Cursor.visible;
        CurrentLockMode = lockMode;

        // Вызываем события при изменении состояния
        if (wasLocked != IsCursorLocked)
        {
            OnCursorStateChanged?.Invoke(IsCursorLocked);
        }

        if (wasVisible != IsCursorVisible)
        {
            OnCursorVisibilityChanged?.Invoke(IsCursorVisible);
        }
    }

    /// <summary>
    /// Проверка, находится ли курсор над UI элементом
    /// </summary>
    private bool IsPointerOverUI()
    {
        if (_eventSystem == null) return false;

        _pointerEventData.position = Input.mousePosition;
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        _eventSystem.RaycastAll(_pointerEventData, raycastResults);

        return raycastResults.Count > 0;
    }

    /// <summary>
    /// Временная блокировка с автоматической разблокировкой через время
    /// </summary>
    public void LockCursorForDuration(float duration)
    {
        LockCursor();
        Invoke(nameof(UnlockCursor), duration);
    }

    /// <summary>
    /// Блокировка курсора с определенным режимом
    /// </summary>
    public void SetCustomLockMode(CursorLockMode mode)
    {
        SetCursorState(true, mode);
    }

    /// <summary>
    /// Получить информацию о состоянии курсора для отладки
    /// </summary>
    public string GetCursorInfo()
    {
        return $"Заблокирован: {IsCursorLocked}\n" +
               $"Видим: {IsCursorVisible}\n" +
               $"Режим: {Cursor.lockState}\n" +
               $"Над UI: {IsPointerOverUI()}";
    }

#if UNITY_EDITOR
    /*private void OnGUI()
    {
        if (UnityEngine.Debug.isDebugBuild)
        {
            // Отладочная информация
            GUI.Label(new Rect(10, 10, 300, 20), $"Cursor: {(IsCursorLocked ? "Locked" : "Unlocked")}");
            GUI.Label(new Rect(10, 30, 300, 20), $"Visible: {IsCursorVisible}");
            GUI.Label(new Rect(10, 50, 300, 20), $"Mode: {Cursor.lockState}");
            GUI.Label(new Rect(10, 70, 300, 20), $"Over UI: {IsPointerOverUI()}");
        }
    }
    */
#endif
}

/// <summary>
/// Компонент для автоматического управления курсором при входе в триггеры
/// </summary>
[RequireComponent(typeof(Collider))]
public class TriggerCursorLocker : MonoBehaviour
{
    [SerializeField] private bool lockOnEnter = true;
    [SerializeField] private bool unlockOnExit = true;
    [SerializeField] private string playerTag = "Player";

    private SmartCursorLocker _cursorLocker;

    private void Start()
    {
        _cursorLocker = FindObjectOfType<SmartCursorLocker>();
        if (_cursorLocker == null)
        {
            Debug.LogError("SmartCursorLocker не найден в сцене!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && lockOnEnter && _cursorLocker != null)
        {
            _cursorLocker.LockCursor();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) && unlockOnExit && _cursorLocker != null)
        {
            _cursorLocker.UnlockCursor();
        }
    }
}