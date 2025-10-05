using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIgameplay : UICanvas
{
    [Header("Score Bar UI")]
    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI leftMilestoneText;
    [SerializeField] private TextMeshProUGUI rightMilestoneText;
    [SerializeField] private TextMeshProUGUI currentScoreText;

    [Header("Milestones (ví dụ: 0, 25000, 100000, 200000, ...)")]
    public List<int> scoreMilestones = new List<int> { 0, 25000, 100000, 200000, 300000,500000,700000 };

    private int currentScore = 0;
    private int currentMilestoneIndex = 0;

    void Start()
    {
        RestoreMilestones();
        int savedScore = PlayerPrefs.GetInt("CS", 0);
        SetScore(savedScore);
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.SetString("CM", string.Join(",", scoreMilestones));
        PlayerPrefs.Save();
    }

    public void Pause()
    {
        UIManager.Instance.OpenUI<UIPasue>();
        UIManager.Instance.PauseGame();
        SoundManager.Instance.PlayVFXSound(4);
    }

    public void SetScore(int score)
    {
        currentScore = score;
        UpdateMilestoneUI();
        UpdateBarAndScore();
        
        // Cập nhật màu dựa trên số mốc đã qua
        UpdateColorsBasedOnScore();
    }

    private void UpdateMilestoneUI()
    {
        for (int i = 0; i < scoreMilestones.Count - 1; i++)
        {
            if (currentScore >= scoreMilestones[i] && currentScore < scoreMilestones[i + 1])
            {
                currentMilestoneIndex = i;
                break;
            }
            if (currentScore >= scoreMilestones[scoreMilestones.Count - 1])
            {
                currentMilestoneIndex = scoreMilestones.Count - 2;
            }
        }
        leftMilestoneText.text = FormatScore(scoreMilestones[currentMilestoneIndex]);
        rightMilestoneText.text = FormatScore(scoreMilestones[currentMilestoneIndex + 1]);
    }

    private void UpdateBarAndScore()
    {
        int left = scoreMilestones[currentMilestoneIndex];
        int right = scoreMilestones[currentMilestoneIndex + 1];
        float t = Mathf.InverseLerp(left, right, currentScore);

        progressBar.DOFillAmount(Mathf.Clamp01(t), 0.35f).SetEase(Ease.OutQuad);
        currentScoreText.text = FormatScore(currentScore);
    }

    // Cập nhật màu dựa trên số mốc đã vượt qua
    private void UpdateColorsBasedOnScore()
    {
        // Đếm số mốc đã vượt qua (không tính mốc 0)
        int milestonesPassed = 0;
        for (int i = 1; i < scoreMilestones.Count; i++)
        {
            if (currentScore >= scoreMilestones[i])
            {
                milestonesPassed++;
            }
            else
            {
                break;
            }
        }

        // Gọi GameManager để cập nhật màu
        var gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.UpdateColorsBasedOnMilestones(milestonesPassed);
        }
    }

    private string FormatScore(int score)
    {
        if (score >= 1_000_000)
            return (score / 1_000_000f).ToString("0.#") + "M";
        if (score >= 1_000)
            return (score / 1_000f).ToString("0.#") + "K";
        return score.ToString();
    }

    private void RestoreMilestones()
    {
        string milestonesStr = PlayerPrefs.GetString("CM", "");
        if (!string.IsNullOrEmpty(milestonesStr))
        {
            var arr = milestonesStr.Split(',');
            scoreMilestones = new List<int>();
            foreach (var s in arr)
            {
                if (int.TryParse(s, out int val))
                    scoreMilestones.Add(val);
            }
            if (scoreMilestones.Count < 2)
                scoreMilestones = new List<int> { 0, 25000, 100000, 200000, 300000, 500000, 700000 };
        }
    }

    public string GetLeftMilestoneText()
    {
        return leftMilestoneText != null ? leftMilestoneText.text : "";
    }
}