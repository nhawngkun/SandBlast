using DG.Tweening;
using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class UIHome : UICanvas
{
    [SerializeField] private HomeTab[] homeTabs;
    [HideInInspector] public int currentTabIndex = 0;

    [Header("Current Score UI")]
    [SerializeField] private TextMeshProUGUI currentScoreText;

    public void PlayBtn()
    {
        // 1. Close this UI and play a sound effect.
        UIManager.Instance.CloseUI<UIHome>(0.3f);
        SoundManager.Instance.PlayVFXSound(1);

        // 2. Ask the UIManager to open the gameplay UIs after a 0.5-second delay.
        UIManager.Instance.OpenGameplayUIsAfterDelay(0.5f);
    }

    private void Start()
    {
        OnTabClick(0);
        UpdateCurrentScoreUI();
    }

    public void UpdateCurrentScoreUI()
    {
        var uiGameplay = FindObjectOfType<UIgameplay>();
        string milestoneText = "";

        if (uiGameplay != null)
        {
            milestoneText = uiGameplay.GetLeftMilestoneText();
            if (string.IsNullOrEmpty(milestoneText))
            {
                milestoneText = "0";
            }
        }
        else
        {
            int currentScore = PlayerPrefs.GetInt("CurrentScore", 0);

            string milestonesStr = PlayerPrefs.GetString("CurrentMilestones", "");
            List<int> milestones = new List<int>();
            if (!string.IsNullOrEmpty(milestonesStr))
            {
                var arr = milestonesStr.Split(',');
                foreach (var s in arr)
                {
                    if (int.TryParse(s, out int val))
                        milestones.Add(val);
                }
            }
            if (milestones.Count < 2)
                milestones = new List<int> { 0, 25000, 100000, 200000, 300000, 500000, 700000 };

            int leftMilestone = milestones[0];
            for (int i = 0; i < milestones.Count - 1; i++)
            {
                if (currentScore >= milestones[i] && currentScore < milestones[i + 1])
                {
                    leftMilestone = milestones[i];
                    break;
                }
            }
            
            if (currentScore >= milestones[milestones.Count - 1])
            {
                leftMilestone = milestones[milestones.Count - 2];
            }
            milestoneText = FormatScore(leftMilestone);
        }
        if (currentScoreText != null)
            currentScoreText.text = milestoneText;
    }

    private string FormatScore(int score)
    {
        if (score >= 1_000_000)
            return (score / 1_000_000f).ToString("0.#") + "M";
        if (score >= 1_000)
            return (score / 1_000f).ToString("0.#") + "K";
        return score.ToString();
    }

    public void OnTabClick(int index)
    {
        foreach (var tab in homeTabs)
        {
            if (tab.tabIndex == index)
            {
                tab.AnimationOn();
            }
            else
            {
                tab.AnimationOff();
            }
        }
    }

    public void SettingBtn()
    {
        // Add settings functionality here
    }
}