using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class SceneTrigger : MonoBehaviour
{
    [SerializeField] private string _targetSceneName;
    [SerializeField] [Min(0f)] private float _delay = 0f;

    private bool _triggered = false;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;

        _triggered = true;

        if (_delay > 0f)
            StartCoroutine(LoadWithDelay());
        else
            SceneManager.LoadScene(_targetSceneName);
    }

    private System.Collections.IEnumerator LoadWithDelay()
    {
        yield return new WaitForSeconds(_delay);
        SceneManager.LoadScene(_targetSceneName);
    }
}