using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] private List<UICanvas> uiCanvases;
    public Transform _effects;
    private bool isPaused = false;

    public override void Awake()
    {
        base.Awake();
        InitializeUICanvases();
    }

    private void InitializeUICanvases()
    {
        foreach (var canvas in uiCanvases)
        {
            canvas.gameObject.SetActive(false);
        }
    }

    public T OpenUI<T>() where T : UICanvas
    {
        T canvas = GetUI<T>();
        if (canvas != null)
        {
            canvas.Setup();
            canvas.Open();
        }
        return canvas;
    }

    public void CloseUI<T>(float time) where T : UICanvas
    {
        T canvas = GetUI<T>();
        if (canvas != null)
        {
            canvas.Close(time);
        }
    }

    public void CloseUIDirectly<T>() where T : UICanvas
    {
        T canvas = GetUI<T>();
        if (canvas != null)
        {
            canvas.CloseDirectly();
        }
    }

    public bool IsUIOpened<T>() where T : UICanvas
    {
        T canvas = GetUI<T>();
        return canvas != null && canvas.gameObject.activeSelf;
    }

    public T GetUI<T>() where T : UICanvas
    {
        return uiCanvases.Find(c => c is T) as T;
    }
    
    /// <summary>
    /// Opens the gameplay UIs (UIGameplay, UICore) after a specified delay.
    /// </summary>
    /// <param name="delay">Time to wait in seconds before opening the UIs.</param>
    public void OpenGameplayUIsAfterDelay(float delay)
    {
        StartCoroutine(IE_OpenGameplayUIs(delay));
    }

    private IEnumerator IE_OpenGameplayUIs(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        OpenUI<UIgameplay>();
        OpenUI<UICore>();
    }

    public void CloseAll()
    {
        foreach (var canvas in uiCanvases)
        {
            if (canvas.gameObject.activeSelf)
            {
                canvas.Close(0);
            }
        }
    }

    public void PauseGame()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0 : 1;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}