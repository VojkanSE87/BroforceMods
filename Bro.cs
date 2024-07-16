using System;
using BroMakerLib;
using BroMakerLib.CustomObjects.Bros;
using BroMakerLib.Loggers;
using UnityEngine;
using World.Generation.MapGenV4;

namespace Cobro
{
    [HeroPreset("Cobro", HeroType.Rambro)]
    public class Cobro : CustomHero
    {
        private Material normalMaterial, stealthMaterial, normalGunMaterial, stealthGunMaterial, normalAvatarMaterial;
        private float specialAttackDuration = 3.5f;
        private float specialAttackCooldown = 5f;
        private float specialAttackTimer = 0f;
        private bool isSpecialAttackActive = false;
        private BulletCobro projectile;
        private int specialAmmo = 2;

        protected override void Awake()
        {
            this.isHero = true;
            base.Awake();

            this.normalMaterial = this.material;
            this.normalGunMaterial = this.gunSprite.meshRender.material;
            this.stealthMaterial = ResourcesController.GetMaterial("spriteSpecial.png");
            this.stealthGunMaterial = ResourcesController.GetMaterial("gunSpriteSpecial.png");
            this.normalAvatarMaterial = ResourcesController.GetMaterial("avatar.png");
            this.projectile = new BulletCobro();
            this.specialAmmo = 2;
    }

         protected override void Update()
        {
            base.Update();

            if (isSpecialAttackActive)
            {
                specialAttackTimer += Time.deltaTime;

                if (specialAttackTimer >= specialAttackDuration)
                {
                    EndSpecialAttack();
                }
            }
            else
            {
                if (Time.time - specialAttackTimer >= specialAttackCooldown)
                {
                    specialAttackTimer = Time.time;
                }
            }
        }

        protected override void PressSpecial()
        {
            if (this.hasBeenCoverInAcid || this.health <= 0)
            {
                return;
            }

            if (!isSpecialAttackActive && Time.time - specialAttackTimer >= specialAttackCooldown && specialAmmo > 0)
            {
                StartSpecialAttack();
                specialAmmo--;
                HeroController.SetSpecialAmmo(base.playerNum, specialAmmo);
            }

            if (isSpecialAttackActive)
            {
                FireSpecialProjectile(base.X + base.transform.localScale.x * 14f, base.Y + 9f, base.transform.localScale.x * 800f, (float)UnityEngine.Random.Range(-10, 10));
                PlayAttackSound();
                Map.DisturbWildLife(base.X, base.Y, 60f, base.playerNum);
                SortOfFollow.Shake(0.4f, 0.4f);
                this.pressSpecialFacingDirection = (int)base.transform.localScale.x;
                this.yI += 20f;
                this.xIBlast = -base.transform.localScale.x * 20f;
            }
            else
            {
                HeroController.FlashSpecialAmmo(base.playerNum);
                ActivateGun();
            }
        }

        private void StartSpecialAttack()
        {
            isSpecialAttackActive = true;
            specialAttackTimer = 0f;

            this.material = this.stealthMaterial;
            this.gunSprite.meshRender.material = this.stealthGunMaterial;
        }

        private void EndSpecialAttack()
        {
            isSpecialAttackActive = false;

            this.material = this.normalMaterial;
            this.gunSprite.meshRender.material = this.normalGunMaterial;
        }

        protected override void UseSpecial()
        {
            specialAmmo--;
            HeroController.SetSpecialAmmo(base.playerNum, specialAmmo);
            base.UseSpecial();
        }

        private void FireSpecialProjectile(float x, float y, float xSpeed, float ySpeed)
        {
            EffectsController.CreateMuzzleFlashBigEffect(x + Mathf.Sign(xSpeed) * 5f, y, -25f, xSpeed * 0.02f, ySpeed * 0.03f);
            ProjectileController.SpawnProjectileLocally(this.projectile, this, x, y, xSpeed, ySpeed, base.playerNum);
        }
    }
}
