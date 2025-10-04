using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UICore : UICanvas
{
    [Header("Score UI")]

    [SerializeField] private TextMeshProUGUI highScoreText;

    private int currentScore = 0;
    private int highScore = 0;

    // Start is called before the first frame update
    void Start()
    {
        // Load high score từ PlayerPrefs
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        currentScore = PlayerPrefs.GetInt("CurrentScore", 0); // Khôi phục điểm hiện tại
        UpdateScoreUI();
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.SetInt("CurrentScore", currentScore); // Lưu điểm hiện tại khi thoát
        PlayerPrefs.Save();
    }

    // Gọi hàm này khi có điểm mới (vd: từ SandScoring)
    public void AddScore(int amount)
    {
        currentScore += amount;
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
        }
        UpdateScoreUI();

        // Cập nhật UIgameplay nếu có
        var uiGameplay = FindObjectOfType<UIgameplay>();
        if (uiGameplay != null)
        {
            uiGameplay.SetScore(currentScore);
        }
    }

    // Đặt lại điểm hiện tại (vd: khi bắt đầu game mới)
    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreUI();

        var uiGameplay = FindObjectOfType<UIgameplay>();
        if (uiGameplay != null)
        {
            uiGameplay.SetScore(currentScore);
        }
    }

    private void UpdateScoreUI()
    {

        highScoreText.text = FormatScore(highScore);
    }

    // Định dạng điểm: 1K, 1.2M, v.v.
    private string FormatScore(int score)
    {
        if (score >= 1_000_000)
            return (score / 1_000_000f).ToString("0.#") + "M";
        if (score >= 1_000)
            return (score / 1_000f).ToString("0.#") + "K";
        return score.ToString();
    }

    public int GetCurrentScore()
    {
        return currentScore;
    }
}
