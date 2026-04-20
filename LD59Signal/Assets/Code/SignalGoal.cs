using UnityEngine;

public class SignalGoal : MonoBehaviour
{
    public Transform lineTarget;
    public void OnSignalReached()
    {
        Debug.Log("<color=green>ПОБЕДА! СИГНАЛ ДОШЕЛ ДО ЦЕЛИ!</color>");
    }
}
