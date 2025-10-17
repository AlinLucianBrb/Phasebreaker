using UnityEngine;

public class BrickHealth : MonoBehaviour
{
    [Header("Health")]
    [Min(1)] public int maxHP = 3;
    [Min(1)] int hp = 3;

    [Header("FX (optional)")]
    public ParticleSystem hitFx;
    public ParticleSystem breakFx;

    BrickHitPulse pulse;
    BrickStyle style;     
    BrickHealthPips pips;

    void Awake()
    {
        pulse = GetComponent<BrickHitPulse>();
        style = GetComponent<BrickStyle>();
        pips = GetComponent<BrickHealthPips>();

        hp = maxHP;

        style?.ApplyHP(hp, maxHP);
        pips?.Refresh(hp, maxHP);
    }

    void Start()
    {
        LevelPatternGenerator.Instance.spawned.Add(this.gameObject);
    }

    void OnDestroy()
    {
        LevelPatternGenerator.Instance.spawned.Remove(this.gameObject);
    }

    public void OnHit()
    {
        if (hp > 1)
        {
            hp--;
            if (hitFx) hitFx.Play();
            pips?.Reveal();
            pips?.Refresh(hp, maxHP);
            style?.ApplyHP(hp, maxHP);
            pulse?.Ping();
        }
        else
        {
            if (breakFx) Instantiate(breakFx, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }

    public void SetHP(int newHP, int newMaxHP = -1)
    {
        if (newMaxHP > 0) maxHP = Mathf.Max(1, newMaxHP);
        hp = Mathf.Clamp(newHP, 1, maxHP);
        style?.ApplyHP(hp, maxHP);
        pips?.Refresh(hp, maxHP);
    }

    void OnCollisionEnter2D(Collision2D _) => OnHit();
}
