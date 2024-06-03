using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance; // Singleton instance

    public int currentCoins = 0; // Current coin count
    private int previousCoins = 0; // Previous coin count

    public int score = 0; // Score variable

    public TextMeshProUGUI scoreText; // Reference to the TextMeshPro text for score

    private void Awake()
    {
        // Ensure only one instance of GameManager exists
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }

    public void IncrementCoins(int amount)
    {
        currentCoins += amount;
    }

    public void DecrementCoins(int amount)
    {
        currentCoins -= amount;
        currentCoins = Mathf.Max(currentCoins, 0); // Ensure coins don't go negative
    }

    public void ResetCoins()
    {
        currentCoins = previousCoins;
    }

    public void UpdatePreviousCoins()
    {
        previousCoins = currentCoins;
    }

    public void IncrementScore(int amount)
    {
        score += amount;
        UpdateScoreText(); // Update score text when score changes
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score.ToString();
        }
    }
}
