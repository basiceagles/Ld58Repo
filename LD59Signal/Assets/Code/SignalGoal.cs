using UnityEngine;
using UnityEngine.SceneManagement;

public class SignalGoal : MonoBehaviour
{
    public string sceneName;
    public Transform lineTarget;
    public void OnSignalReached()
    {
        Debug.Log("<color=green>ПОБЕДА! СИГНАЛ ДОШЕЛ ДО ЦЕЛИ!</color>");
        SceneManager.LoadScene(sceneName);
    }
}
