public class BurningStatusEffect : StatusEffect
{
    public BurningStatusEffect(float d, float dps) : base(d, dps) { }

    public override void UpdateEffect(WizardController wizard, float deltaTime)
    {
        elapsed += deltaTime;
        wizard.TakeDamage(damagePerSecond * deltaTime);
    }
}