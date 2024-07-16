using BroMakerLib;
using UnityEngine;

public class SpecialProjectile : Projectile
{
    public float rotationSpeed = 0f;
    public float maxDamageRange = 10f;
    public float minDamageRange = 5f;

    public override void Damage(DamageObject damage)
    {
        // Apply increased damage if the target is within a certain range
        float distance = Vector2.Distance(transform.position, damage.position);
        if (distance <= maxDamageRange && distance >= minDamageRange)
        {
            damage.damage *= 1.5f; // Increase damage by 50%
        }

        // Apply additional effects based on the target type
        if (damage.target.GetComponent<BossBlockPiece>() != null || damage.target.layer == LayerMask.NameToLayer("LargeObjects"))
        {
            damage.damage++;
        }

        base.Damage(damage);
    }

    private void Update()
    {
        // Update the projectile's rotation based on the rotationSpeed
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Play a special sound effect or create a unique visual effect when the projectile hits a target
        SoundController.Instance.PlaySoundEffectAt(this, "SpecialProjectileHit", 0.8f, transform.position, 1f);
        EffectsController.CreateEffect("SpecialProjectileHitEffect", transform.position.x, transform.position.y);
    }
}