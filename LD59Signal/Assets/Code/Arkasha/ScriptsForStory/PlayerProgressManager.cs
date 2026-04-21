using System;
using UnityEngine;

public class PlayerProgressManager : MonoBehaviour
{
    public static PlayerProgressManager Instance { get; private set; }

    // Игрок подобрал бумажку с кодами
    public bool HasDecipherKey { get; private set; }

    // Сколько фрагментов расшифровано глобально (0–5)
    public int GlobalFragmentsDeciphered { get; private set; }

    // Все 5 фрагментов расшифрованы
    public bool AllFragmentsDeciphered => GlobalFragmentsDeciphered >= TOTAL_FRAGMENTS;

    // Игрок получил текст сигнала на факсе
    public bool HasSignalText { get; private set; }

    // Игрок изменил содержимое сигнала
    public bool ChangedSignal { get; private set; }

    public const int TOTAL_FRAGMENTS = 5;

    public event Action OnDecipherKeyObtained;
    public event Action<int> OnFragmentDeciphered;
    public event Action OnAllFragmentsDeciphered;
    public event Action OnSignalTextObtained;
    public event Action OnSignalChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
    }

    // Вызывается когда игрок подобрал бумажку с кодами
    public void ObtainDecipherKey()
    {
        if (HasDecipherKey) return;
        HasDecipherKey = true;
        OnDecipherKeyObtained?.Invoke();

        #if UNITY_EDITOR
        Debug.Log("[PlayerProgressManager] Получена бумажка с кодами.");
        #endif
    }

    // Вызывается когда игрок расшифровал фрагмент
    public void RegisterFragmentDeciphered()
    {
        if (AllFragmentsDeciphered) return;

        GlobalFragmentsDeciphered++;
        OnFragmentDeciphered?.Invoke(GlobalFragmentsDeciphered);

        #if UNITY_EDITOR
        Debug.Log($"[PlayerProgressManager] Фрагмент {GlobalFragmentsDeciphered}/{TOTAL_FRAGMENTS} расшифрован.");
        #endif

        if (AllFragmentsDeciphered)
            OnAllFragmentsDeciphered?.Invoke();
    }

    // Вызывается FaxInteractable когда игрок получил текст сигнала
    public void ObtainSignalText()
    {
        if (HasSignalText) return;
        HasSignalText = true;
        OnSignalTextObtained?.Invoke();

        #if UNITY_EDITOR
        Debug.Log("[PlayerProgressManager] Получен текст сигнала.");
        #endif
    }

    // Вызывается SignalRadioInteractable когда игрок изменил сигнал
    public void RegisterSignalChanged()
    {
        if (ChangedSignal) return;
        ChangedSignal = true;
        OnSignalChanged?.Invoke();

        #if UNITY_EDITOR
        Debug.Log("[PlayerProgressManager] Содержимое сигнала изменено.");
        #endif
    }
    [ContextMenu("Debug/Complete All Fragments")]
    public void DebugCompleteAllFragments()
    {
        while (!AllFragmentsDeciphered)
            RegisterFragmentDeciphered();
    }

    private void Update()
    {
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.P))
            DebugCompleteAllFragments();
        #endif
    }
}