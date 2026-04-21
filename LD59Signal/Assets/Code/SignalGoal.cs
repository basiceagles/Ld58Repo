using UnityEngine;
using UnityEngine.SceneManagement;

public class SignalGoal : MonoBehaviour
{
    [SerializeField] private string _sceneIfChanged;    // сцена если сигнал изменён
    [SerializeField] private string _sceneIfNotChanged; // сцена если сигнал не изменён

    public Transform lineTarget;

    public void OnSignalReached()
    {
        Debug.Log("<color=green>ПОБЕДА! СИГНАЛ ДОШЕЛ ДО ЦЕЛИ!</color>");

        bool changed = PlayerProgressManager.Instance != null
                    && PlayerProgressManager.Instance.ChangedSignal;

        string targetScene = changed ? _sceneIfChanged : _sceneIfNotChanged;

        if (!string.IsNullOrEmpty(targetScene))
            SceneManager.LoadScene(targetScene);
        else
            Debug.LogWarning("[SignalGoal] Имя сцены не назначено!", this);
    }
}