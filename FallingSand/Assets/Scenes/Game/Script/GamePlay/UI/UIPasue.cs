// UIPasue.cs

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPasue : UICanvas
{
    [Header("Sound & Vibration UI")]
    [SerializeField] private GameObject soundOnObj;
    [SerializeField] private GameObject soundOffObj;
    [SerializeField] private GameObject vibrationOnObj;
    [SerializeField] private GameObject vibrationOffObj;

    private bool isSoundOn = true;
    private bool isVibrationOn = true;

    void Start()
    {

    }

    void Update()
    {

    }

    public void Back()
    {
        UIManager.Instance.CloseUI<UIPasue>(0.4f);
        UIManager.Instance.ResumeGame();
        SoundManager.Instance.PlayVFXSound(4);
    }

    public void OnHomeButton()
    {
        var uiCore = FindObjectOfType<UICore>();
        var uiGameplay = FindObjectOfType<UIgameplay>();
        if (uiCore != null)
        {
            PlayerPrefs.SetInt("CS", uiCore.GetCurrentScore());
        }
        if (uiGameplay != null)
        {
            PlayerPrefs.SetString("CM", string.Join(",", uiGameplay.scoreMilestones));
        }

        UIManager.Instance.CloseUI<UIgameplay>(0);
        var uiHome = UIManager.Instance.OpenUI<UIHome>();
        UIManager.Instance.CloseUI<UIPasue>(0f);

        if (uiHome != null)
        {
            uiHome.UpdateCurrentScoreUI();
        }
        SoundManager.Instance.PlayVFXSound(4);
        UIManager.Instance.ResumeGame();
        UIManager.Instance.CloseUI<UICore>(0f);
    }

    public void OnResetButton()
    {
        // Reset điểm
        var uiCore = FindObjectOfType<UICore>();
        if (uiCore != null)
        {
            uiCore.ResetScore();
            PlayerPrefs.SetInt("CS", 0);
        }

        // Reset milestones về mặc định
        var uiGameplay = FindObjectOfType<UIgameplay>();
        if (uiGameplay != null)
        {
            uiGameplay.scoreMilestones = new List<int> { 0, 25000, 100000, 200000, 300000,500000,700000 };
            PlayerPrefs.SetString("CM", string.Join(",", uiGameplay.scoreMilestones));
            uiGameplay.SetScore(0);
        }

        // Reset màu và các khối block
        var gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.ResetColors();
            gameManager.ResetDraggableBlocks(); // <-- ADD THIS LINE
        }

        // Reset cát
        var sandSim = FindObjectOfType<SandSimulation>();
        if (sandSim != null)
        {
            sandSim.ResetSandGrid();
        }

        SoundManager.Instance.PlayVFXSound(4);
        UIManager.Instance.CloseUI<UIPasue>(0.5f);
        UIManager.Instance.OpenUI<UIgameplay>();
        UIManager.Instance.ResumeGame();
    }

    void OnEnable()
    {
        SoundManager.Instance.TurnOn = isSoundOn;
        SoundManager.Instance.ToggleMusic(isSoundOn);
        SoundManager.Instance.ToggleSFX(isSoundOn);

        if (soundOnObj != null) soundOnObj.SetActive(isSoundOn);
        if (soundOffObj != null) soundOffObj.SetActive(!isSoundOn);

        if (vibrationOnObj != null) vibrationOnObj.SetActive(isVibrationOn);
        if (vibrationOffObj != null) vibrationOffObj.SetActive(!isVibrationOn);
    }

    public void ToggleSound()
    {
        isSoundOn = !isSoundOn;
        SoundManager.Instance.TurnOn = isSoundOn;
        SoundManager.Instance.ToggleMusic(isSoundOn);
        SoundManager.Instance.ToggleSFX(isSoundOn);

        PlayerPrefs.SetInt("SO", isSoundOn ? 1 : 0);
        PlayerPrefs.Save();

        if (soundOnObj != null) soundOnObj.SetActive(isSoundOn);
        if (soundOffObj != null) soundOffObj.SetActive(!isSoundOn);
        SoundManager.Instance.PlayVFXSound(4);
    }

    public void ToggleVibration()
    {
        isVibrationOn = !isVibrationOn;
        PlayerPrefs.SetInt("VO", isVibrationOn ? 1 : 0);
        PlayerPrefs.Save();

        if (vibrationOnObj != null) vibrationOnObj.SetActive(isVibrationOn);
        if (vibrationOffObj != null) vibrationOffObj.SetActive(!isVibrationOn);
        SoundManager.Instance.PlayVFXSound(4);
    }
}