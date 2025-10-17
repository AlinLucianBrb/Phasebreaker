using Unity.Mathematics;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    public int health = 3;

    public GameObject healthIcon;

    public Transform healthDisplayTransofrm;

    //something to display

    void OnEnable()
    {
        GameEvents.OnBallLost += UpdateHealth;
    }

    void OnDisable()
    {
        GameEvents.OnBallLost -= UpdateHealth;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        UpdateHealth();
    }

    void UpdateHealth(int amount = 0)
    {
        health += amount;
        for (int i = healthDisplayTransofrm.childCount - 1; i >= 0; i--)
        {
            var c = healthDisplayTransofrm.GetChild(i);
            Destroy(c.gameObject);
        }

        for (int i = 0; i < health; i++)
        {
            var go = Instantiate(healthIcon);
            go.transform.SetParent(healthDisplayTransofrm, false);
            go.transform.localPosition = GetPatternPosition(i); 
        }
    }

    Vector2 GetPatternPosition(int i, float step = 100f)
    {
        // each "pair" of steps adds +step in X and -step in Y
        int pair = i / 2;
        bool isOdd = (i % 2) == 1;

        float x = pair * step;
        if (isOdd) x += step;

        float y = -pair * step;
        return new Vector2(x, y);
    }

}
