using BroMakerLib.CustomObjects.Bros;
using BroMakerLib;
using System.IO;
using System.Reflection;
using UnityEngine;
using System;
using System.Collections;

namespace Cobro
{
    [HeroPreset("Cobro", HeroType.Rambro)]
    public class Cobro : CustomHero
    {
        private Projectile primaryProjectile;
        private Projectile specialProjectile;
        private Material normalMaterial, stealthMaterial, normalGunMaterial, stealthGunMaterial, normalAvatarMaterial;
        private float primaryAttackRange = 20f;
        private float primaryAttackSpeed = 480f;
        private float primaryProjectileLifetime = 0.19f; // range
        private ProjectileData specialProjectileData;
        public static AudioClip[] CobroGunSounds;
        public static AudioClip[] MachineGunSounds;
        private AudioClip emptyGunSound;
        private int specialAmmo = 6;

        private bool wasInvulnerable = false;
        private bool specialActive = false;
        private bool isUsingSpecial = false;
        private bool firstSpecialPress = true;
        private int usingSpecialFrame = 0;
        private bool specialMode = false;
        private new bool usingSpecial = false; // Track if special mode is active
        private const float frameDuration = 0.0334f; // Duration of one frame
        private const int specialAnimationFrames = 9; // Number of frames in the animation
        private float animationDuration = specialAnimationFrames * frameDuration; // Calculate animation duration

        private bool specialModeDeactivated = false;
        private bool isReversingSpecial = false;
        private bool FirstPressSpecial = true;

        private bool isAnimatingSpecial = false; // New flag to track animation state

        public Shrapnel bulletShell;

        protected override void Awake()
        {
            base.Awake();
            this.InitializeResources();
            this.InitializeProjectiles();
            this.InitializeAudioClips();
            this.gunSpriteHangingFrame = 9;
        }

        private void InitializeResources()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.normalMaterial = base.material;
            this.stealthMaterial = ResourcesController.GetMaterial(directoryName, "spriteSpecial.png");
            this.normalGunMaterial = this.gunSprite.meshRender.material;
            this.stealthGunMaterial = ResourcesController.GetMaterial(directoryName, "gunSpriteSpecial.png");
            this.normalAvatarMaterial = ResourcesController.GetMaterial(directoryName, "avatar.png");
        }

        private void InitializeAudioClips()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (Cobro.MachineGunSounds == null)
            {
                Cobro.MachineGunSounds = new AudioClip[3];
                Cobro.MachineGunSounds[0] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "MachineGun_1.wav");
                Cobro.MachineGunSounds[1] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "MachineGun_2.wav");
                Cobro.MachineGunSounds[2] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "MachineGun_3.wav");
            }
            if (Cobro.CobroGunSounds == null)
            {
                Cobro.CobroGunSounds = new AudioClip[5];
                Cobro.CobroGunSounds[0] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "CobroGun_2.wav");
                Cobro.CobroGunSounds[1] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "CobroGun_3_Ricochet.wav");
                Cobro.CobroGunSounds[2] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "CobroGun_4_Ricochet.wav");
                Cobro.CobroGunSounds[3] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "CobroGun_5_Ricochet.wav");
                Cobro.CobroGunSounds[4] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "CobroGun_7.wav");

            }
            this.emptyGunSound = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "EmptyGun.wav"); // Initialize empty gun sound
        }

        protected override void Update()
        {
            if (Input.GetButtonDown("Special")) // Replace with your actual input check
            {
                if (!usingSpecial && !isAnimatingSpecial)
                {
                    this.isAnimatingSpecial = true; // Start special animation
                }
                else if (usingSpecial)
                {
                    SetupSpecialAttack();
                    FireSpecialWeapon();
                }
            }

            // Call AnimateSpecial to keep updating the animation frame
            if (isAnimatingSpecial)
            {
                AnimateSpecial();
            }

            // Deactivation logic
            if (specialAmmo <= 0)
            {
                usingSpecial = false;
            }

            if (this.invulnerable)
            {
                this.wasInvulnerable = true;
            }
            base.Update();

            if (this.wasInvulnerable && !this.invulnerable)
            {
                normalMaterial.SetColor("_TintColor", Color.gray);
                stealthMaterial.SetColor("_TintColor", Color.gray);
                gunSprite.meshRender.material.SetColor("_TintColor", Color.gray);
            }
        }

        private void InitializeProjectiles()
        {
            this.primaryProjectile = (HeroController.GetHeroPrefab(HeroType.Rambro) as Rambro).projectile;
            this.specialProjectile = (HeroController.GetHeroPrefab(HeroType.DirtyHarry) as DirtyHarry).projectile;
            this.specialProjectileData = new Cobro.ProjectileData
            {
                bulletCount = 0,
                maxBulletCount = 6
            };
        }

        public class ProjectileData
        {
            public int bulletCount;
            public int maxBulletCount = 6;
        }


        private bool CanUseSpecial()
        {
            return !this.hasBeenCoverInAcid && !this.isUsingSpecial && this.health > 0 && this.SpecialAmmo > 0;
        }
        private void UseSpecialAmmo()
        {
            int specialAmmo = this.SpecialAmmo;
            this.SpecialAmmo = specialAmmo - 1;
        }

        protected override void UseFire()
        {
            if (this.isUsingSpecial)
            {
                return; // Do not allow primary fire when using special
            }

            if (this.doingMelee)
            {
                this.CancelMelee();
            }

            float num = base.transform.localScale.x;
            if (!base.IsMine && base.Syncronize)
            {
                num = (float)this.syncedDirection;
            }
            if (Connect.IsOffline)
            {
                this.syncedDirection = (int)base.transform.localScale.x;
            }

            // Call the FirePrimaryWeapon method
            this.FirePrimaryWeapon();
        }

        private void ActivateSpecialMode()
        {
            this.isUsingSpecial = true;
            base.material = this.stealthMaterial;
            this.gunSprite.meshRender.material = this.stealthGunMaterial;
        }

        protected override void UseSpecial()
        {
            if (this.specialProjectile != null)
            {
                float num = base.transform.localScale.x * 26f;
                float num2 = 8.3f;
                float num3;
                float num4;
                if (base.transform.localScale.x > 0f)
                {
                    num3 = 15f;     //ovo pomeraj za muzzle i ovo ispod u minusu isto
                    num4 = 8.3f;
                }
                else
                {
                    num3 = -15f;
                    num4 = 8.3f;
                }
                float num5 = base.transform.localScale.x * 750f;
                float num6 = (float)UnityEngine.Random.Range(-10, 10);
                this.gunFrame = 3;
                this.SetGunSprite(this.gunFrame, 0);
                EffectsController.CreateMuzzleFlashMediumEffect(base.X + num3, base.Y + num4, -20f, num5 * 0.06f, num6 * 0.06f, base.transform);
                Sound.GetInstance().PlaySoundEffectAt(Cobro.CobroGunSounds, 0.9f, base.transform.position, 0.9f + this.pitchShiftAmount, true, false, false, 0f); //mozda ovo 1f podesiti
                Map.DisturbWildLife(base.X, base.Y, 60f, base.playerNum);
                SortOfFollow.Shake(0.4f, 0.4f);
                this.pressSpecialFacingDirection = (int)base.transform.localScale.x;
                this.yI += 10f;
                this.xIBlast = -base.transform.localScale.x * 15f;

                if (this.specialAmmo <= 0)
                {
                   this.DeactivateSpecialMode();
                }
            }
        }

        private void SetupSpecialAttack()
        {
            float num = base.transform.localScale.x * 26f;
            float num2 = 8.3f;
            float num3;
            float num4;
            if (base.transform.localScale.x > 0f)
            {
                num3 = 15f;     //ovo pomeraj za muzzle i ovo ispod u minusu isto
                num4 = 8.3f;
            }
            else
            {
                num3 = -15f;
                num4 = 8.3f;
            }
            float num5 = base.transform.localScale.x * 750f;
            float num6 = (float)UnityEngine.Random.Range(-10, 10);
            this.gunFrame = 3;
            this.SetGunSprite(this.gunFrame, 0);
            EffectsController.CreateMuzzleFlashMediumEffect(base.X + num3, base.Y + num4, -20f, num5 * 0.06f, num6 * 0.06f, base.transform);
            Sound.GetInstance().PlaySoundEffectAt(Cobro.CobroGunSounds, 0.9f, base.transform.position, 0.9f + this.pitchShiftAmount, true, false, false, 0f); //mozda ovo 1f podesiti
            Map.DisturbWildLife(base.X, base.Y, 60f, base.playerNum);
            SortOfFollow.Shake(0.4f, 0.4f);
            this.pressSpecialFacingDirection = (int)base.transform.localScale.x;
            this.yI += 10f;
            this.xIBlast = -base.transform.localScale.x * 15f;
        }

        private void FireSpecialWeapon()
        {
            EffectsController.CreateShrapnel(this.bulletShell, base.X + base.transform.localScale.x * 2f, base.Y + 13f, 2f, 30f, 6f, -base.transform.localScale.x * 50f, 90f);
            float x = base.X + base.transform.localScale.x * 26f;
            float y = base.Y + 8.3f;
            float xI = base.transform.localScale.x * 750f;
            float yI = (float)UnityEngine.Random.Range(-10, 10);
            ProjectileController.SpawnProjectileLocally(this.specialProjectile, this, x, y, xI, yI, base.playerNum);
            UseSpecialAmmo(); // Decrement special ammo here
        }
        private IEnumerator AnimateAndActivateSpecial()
        {
            if (this.isAnimatingSpecial)
            {
                yield break;
            }

            this.isAnimatingSpecial = true;
            AnimateSpecial(); // Assuming this method handles the animation frame update

            yield return new WaitForSeconds(animationDuration);

            ActivateSpecialMode();
            this.isUsingSpecial = true;
            this.specialMode = true;     
            this.isAnimatingSpecial = false;
        }

        protected override void AnimateSpecial()
        {
            this.SetSpriteOffset(0f, 0f);
            this.DeactivateGun();
            this.frameRate = 0.0334f;

            // Use Mathf.Clamp to ensure frame stays within the desired range
            int frame = Mathf.Clamp(base.frame, 0, 8);
            this.sprite.SetLowerLeftPixel((23 + frame) * this.spritePixelWidth, 9 * this.spritePixelHeight);

            // Check if the animation has reached its end
            if (base.frame >= 8)
            {
                base.frame = 0;
                this.isAnimatingSpecial = false;
                this.specialMode = true; // Activate special mode
                this.isUsingSpecial = true;
                this.ActivateSpecialMode();
                this.ActivateGun();
            }
        }
        private void DeactivateSpecialMode()
        {
            if (isReversingSpecial)
            {
                return;
            }

            ReverseAnimation();
            this.isUsingSpecial = false;
            this.specialMode = false;
            base.material = this.normalMaterial;
            this.gunSprite.meshRender.material = this.normalGunMaterial;
            HeroController.FlashSpecialAmmo(base.playerNum);
            Sound.GetInstance().PlaySoundEffectAt(this.emptyGunSound, 1f, base.transform.position);
            isReversingSpecial = false;
        }
        private void ReverseAnimation()
        {
            if (this.usingSpecialFrame > 0)
            {
                --this.usingSpecialFrame;
                this.sprite.SetLowerLeftPixel((23 + this.usingSpecialFrame) * this.spritePixelWidth, 9 * this.spritePixelHeight);
            }
        }
        private void FirePrimaryWeapon()
        {
            if (this.isUsingSpecial)
            {
                return; // Primary fire is disabled when special mode is active
            }

            float num = base.transform.localScale.x * this.primaryAttackRange;
            float num2 = 8f;
            float num3;
            float num4;
            if (base.transform.localScale.x > 0f)
            {
                num3 = 14f;
                num4 = 8.5f;
            }
            else
            {
                num3 = -14f;
                num4 = 8.5f;
            }
            float num5 = base.transform.localScale.x * this.primaryAttackSpeed;
            float num6 = (float)UnityEngine.Random.Range(-15, 15); //navodno disperse
            this.gunFrame = 3;
            this.SetGunSprite(this.gunFrame, 0);                                                             //10f vise gore i dole u odnosu + ili -        //25f veci veriety sto je veci br to menjaj
            ProjectileController.SpawnProjectileLocally(this.primaryProjectile, this, base.X + num, base.Y + num2, num5, num6 - 10f + UnityEngine.Random.value * 35f, base.playerNum).life = this.primaryProjectileLifetime;
            EffectsController.CreateMuzzleFlashEffect(base.X + num3, base.Y + num4, -21f, num5 * 0.15f, num6 * 0.15f, base.transform);
            Sound.GetInstance().PlaySoundEffectAt(Cobro.MachineGunSounds, 0.45f, base.transform.position, 1f + this.pitchShiftAmount, true, false, false, 0f);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}