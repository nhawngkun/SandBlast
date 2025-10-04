using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UILoss : UICanvas
{
    [Header("Score UI")]
    [SerializeField] private TextMeshProUGUI currentScoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    // Start is called before the first frame update
    void Start()
    {
        UpdateScoreTexts();
    }

    void OnEnable()
    {
        UpdateScoreTexts();
    }

    private void UpdateScoreTexts()
    {
        var uiCore = FindObjectOfType<UICore>();
        int currentScore = uiCore != null ? uiCore.GetCurrentScore() : PlayerPrefs.GetInt("CurrentScore", 0);
        int highScore = PlayerPrefs.GetInt("HighScore", 0);

        if (currentScoreText != null)
            currentScoreText.text =  FormatScore(currentScore);
        if (highScoreText != null)
            highScoreText.text =  FormatScore(highScore);
    }

    private string FormatScore(int score)
    {
        if (score >= 1_000_000)
            return (score / 1_000_000f).ToString("0.#") + "M";
        if (score >= 1_000)
            return (score / 1_000f).ToString("0.#") + "K";
        return score.ToString();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void Retry()
    {

        var uiCore = FindObjectOfType<UICore>();
        if (uiCore != null)
        {
            uiCore.ResetScore();
            PlayerPrefs.SetInt("CurrentScore", 0);
        }
        
        // Reset milestones về mặc định
        var uiGameplay = FindObjectOfType<UIgameplay>();
        if (uiGameplay != null)
        {
            uiGameplay.scoreMilestones = new List<int> { 0, 25000, 100000, 200000, 300000, 500000, 700000 };
            PlayerPrefs.SetString("CurrentMilestones", string.Join(",", uiGameplay.scoreMilestones));
            uiGameplay.SetScore(0);
        }
        // Reset cát (nên gọi hàm reset ở SandSimulation)
        var sandSim = FindObjectOfType<SandSimulation>();
        if (sandSim != null)
        {
            sandSim.ResetSandGrid();
        }
        var gameManager = FindObjectOfType<GameManager>();
    if (gameManager != null)
    {
        gameManager.ResetColors();
        gameManager.ResetDraggableBlocks(); // <-- ADD THIS LINE
    }

        SoundManager.Instance.PlayVFXSound(4);
        // Đóng UIgameplay, mở lại UIgameplay (hoặc UIHome nếu muốn về home)
        UIManager.Instance.CloseUI<UILoss>(0.5f);
        UIManager.Instance.OpenUI<UIgameplay>();


    }
}
