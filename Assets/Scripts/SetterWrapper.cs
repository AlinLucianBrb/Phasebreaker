using UnityEngine;

public class SetterWrapper : MonoBehaviour
{
    public void SetVolume(float volume)
    {
        if (ManagerGame.I)
        {
            ManagerGame.I.SetSFXVolume(volume);
            ManagerGame.I.SetBGMVolume(volume);
        }
        else
            Debug.LogWarning("GameManager not found in scene!");
    }

    public void SetResolution()
    {
        if (ManagerGame.I)
            ManagerGame.I.SetResolution();
        else
            Debug.LogWarning("GameManager not found in scene!");
    }

    public void SetFullScreen()
    {
        if (ManagerGame.I)
            ManagerGame.I.SetFullScreen();
        else
            Debug.LogWarning("GameManager not found in scene!");
    }

    public void LoadScene(int index)
    {
        if (ManagerGame.I)
        {
            ManagerGame.I.LoadScene(index);
            SetPause(false);
        }
        else
            Debug.LogWarning("GameManager not found in scene!");
    }

    public void QuitGame()
    {
        if (ManagerGame.I)
            ManagerGame.I.QuitGame();
        else
            Debug.LogWarning("GameManager not found in scene!");
    }

    public void SetPause(bool state = false)
    {
        if (ManagerGame.I)
            ManagerGame.SetPause(state);
        else
            Debug.LogWarning("GameManager not found in scene!");
    }

    public void PlaySFX(string sfxName = "UI")
    {
        if (ManagerGame.I)
            ManagerGame.I.PlaySFX(sfxName);
        else
            Debug.LogWarning("AudioManager not found in scene!");
    }
}
