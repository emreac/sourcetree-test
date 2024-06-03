using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameStartManager : MonoBehaviour
{
    public GameObject startPanel;
    public PlayerMovement playerMovement; // Reference to the PlayerMovement script

    private void Start()
    {
        // Ensure the start panel is active
        if (startPanel != null)
        {
            startPanel.SetActive(true);
        }

        // Disable player movement at the start
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
    }

    public void StartGame()
    {
        // Hide the start panel
        if (startPanel != null)
        {
            startPanel.SetActive(false);
        }

        // Enable player movement
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
    }

    public void NextLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
    public void Restart()
    {
        GameManager.instance.UpdatePreviousCoins();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoFirstLevel()
    {
        SceneManager.LoadScene("Level1");
    }
}
