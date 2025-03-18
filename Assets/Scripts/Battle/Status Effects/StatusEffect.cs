public abstract class StatusEffect
{
    public float duration; // Total duration.
    public float damagePerSecond;
    protected float elapsed = 0f;

    // Public property to expose elapsed time.
    public float Elapsed { get { return elapsed; } }

    public StatusEffect(float d, float dps)
    {
        duration = d;
        damagePerSecond = dps;
    }

    public abstract void UpdateEffect(WizardController wizard, float deltaTime);
    public bool IsExpired() => elapsed >= duration;
}