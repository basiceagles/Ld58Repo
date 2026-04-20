using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExitToMainMenu : MonoBehaviour
{
    public string sceneName;
    [SerializeField] private float holdDuration = 2f;
    [SerializeField] private Image progressImage;
    
    private float holdTimer = 0f;
    private bool isHolding = false;

    void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            isHolding = true;
            holdTimer += Time.deltaTime;
            
            if (progressImage != null)
            {
                progressImage.fillAmount = holdTimer / holdDuration;
                progressImage.gameObject.SetActive(true);
            }
            
            if (holdTimer >= holdDuration)
            {
                SceneManager.LoadScene(sceneName);
            }
        }
        else
        {
            if (isHolding)
            {
                isHolding = false;
                holdTimer = 0f;
                
                if (progressImage != null)
                {
                    progressImage.fillAmount = 0f;
                    progressImage.gameObject.SetActive(false);
                }
            }
        }
    }
}
