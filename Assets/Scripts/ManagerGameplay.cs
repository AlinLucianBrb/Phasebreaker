using TMPro;
using UnityEngine;

public class ManagerGameplay : MonoBehaviour
{
    public static ManagerGameplay I { get; private set; }

    public GameObject winDisplay;
    public GameObject loseDisplay;
    public GameObject pauseDisplay;

    bool triggered = false;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
    }

    void Update()
    {
        if(!triggered)
        {
            if (LevelPatternGenerator.Instance.spawned.Count == 0)
            {
                ManagerGame.SetPause(true);
                winDisplay.SetActive(true);
                ManagerGame.I.PlaySFX("Win");
                triggered = true;
                return;
            }

            if (PlayerHealth.Instance.health == 0)
            {
                ManagerGame.SetPause(true);
                loseDisplay.SetActive(true);
                ManagerGame.I.PlaySFX("Lose");
                triggered = true;
                return;
            }

            if (pauseDisplay.activeSelf && !ManagerGame.IsPaused)
            {
                pauseDisplay.SetActive(false);
                return;
            }
            pauseDisplay.SetActive(ManagerGame.IsPaused);
        }
    }
}
