
using System;
using System.Collections.Generic;
using BroMakerLib;
using BroMakerLib.CustomObjects.Bros;
using BroMakerLib.Loggers;
using System.IO;
using System.Reflection;
using UnityEngine;
using System.Net;
using HarmonyLib;
using Rogueforce;
using Newtonsoft.Json;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Collections;


namespace JackBroton
{
    [HeroPreset("Jack Broton", HeroType.Rambro)]
    public class JackBroton : CustomHero

    {
        Projectile[] projectiles;
        private Projectile primaryProjectile;
        private Projectile specialProjectile;
        private JackBroton.ProjectileData specialProjectileData;
        private Material normalMaterial, normalGunMaterial, normalAvatarMaterial;
        private float primaryAttackRange = 20f;
        private float primaryAttackSpeed = 480f;
        private float primaryProjectileLifetime = 0.12f;

        public static AudioClip[] Tec9GunSounds;
        public static AudioClip[] BootKnifeSounds;
        public static AudioClip[] DashingMeleeSounds;
        public static AudioClip[] BrotonSmack;
        public static AudioClip[] Questions;
        private AudioClip emptyGunSound;
        private int specialAmmo = 3;


        private bool wasInvulnerable = false;

        //special_variables
        private bool UsingSpecial = false;
        private bool isUsingSecondSpecial;
        protected bool specialActive = false;
        private int usingSpecialFrame = 0;

        private float specialAnimationTimer = 0f;
        private bool isReversingSpecial = false; // New flag to track reverse animation state

        private bool isDelayingPrimaryFire = false; // New flag to track primary fire delay
        private float primaryFireDelayTimer = 0f; // Timer for primary fire delay

        public float muzzleFlashOffsetXOnZiplineLeft = 8f;
        public float muzzleFlashOffsetYOnZiplineLeft = 2.5f;
        public float muzzleFlashOffsetXOnZiplineRight = -9f;
        public float muzzleFlashOffsetYOnZiplineRight = 2f;

        public float muzzleFlashPrimaryOffsetXOnZiplineLeft = 9.5f;
        public float muzzleFlashPrimaryOffsetYOnZiplineLeft = 1.5f;
        public float muzzleFlashPrimaryOffsetXOnZiplineRight = -8f;
        public float muzzleFlashPrimaryOffsetYOnZiplineRight = 1f;

        private bool wasRunning;
        protected bool throwingMook;

        BootKnife bootknifePrefab;


        protected override void Awake()
        {
            base.Awake();
            this.InitializeResources();
            this.InitializeProjectiles();
            this.InitializeAudioClips();
            this.gunSpriteHangingFrame = 9;

            bootknifePrefab = new GameObject("BootKnife", new Type[] { typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer), typeof(SpriteSM), typeof(BootKnife) }).GetComponent<BootKnife>();
            bootknifePrefab.enabled = false;

        }

        private void InitializeResources()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.normalMaterial = base.material;            
            this.normalGunMaterial = this.gunSprite.meshRender.material;
            this.normalAvatarMaterial = ResourcesController.GetMaterial(directoryName, "avatar.png");
        }

        public static void PreloadSprites(string directoryPath, List<string> spriteNames)
        {
            foreach (var spriteName in spriteNames)
            {
                string spritePath = Path.Combine(directoryPath, spriteName);
                if (File.Exists(spritePath))
                {
                    string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    ResourcesController.GetMaterial(directoryName, "BootKnife.png"); 
                }
                else
                {
                    //Debug.LogWarning($"Sprite not found: {spritePath}");
                }
            }
        }

        private void InitializeAudioClips()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (JackBroton.Tec9GunSounds == null)
            {
                JackBroton.Tec9GunSounds = new AudioClip[3];
                JackBroton.Tec9GunSounds[0] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "Tec9_1.wav");
                JackBroton.Tec9GunSounds[1] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "Tec9_2.wav");
                JackBroton.Tec9GunSounds[2] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "Tec9_3.wav");
            }
            if (JackBroton.BootKnifeSounds == null) //ovo mozda ide u bootKnife.cs
            {
                JackBroton.BootKnifeSounds = new AudioClip[2];
                JackBroton.BootKnifeSounds[0] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "BootKnife_1.wav");
                JackBroton.BootKnifeSounds[1] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "BootKnife_2.wav");
            }
            if (JackBroton.DashingMeleeSounds == null)
            {
                JackBroton.DashingMeleeSounds = new AudioClip[2];
                JackBroton.DashingMeleeSounds[0] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "BrotonSmack.wav");
                JackBroton.DashingMeleeSounds[1] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "BrotonSmack2.wav");
            }
            if (JackBroton.Questions == null)
            {
                JackBroton.Questions = new AudioClip[2];
                JackBroton.Questions[0] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "question1.wav");
                JackBroton.Questions[1] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "question2.wav");
            }

        }

        protected override void Update()
        {
            base.Update();

            if (this.invulnerable)
            {
                this.wasInvulnerable = true;
            }

            if (this.wasInvulnerable && !this.invulnerable)
            {
                normalMaterial.SetColor("_TintColor", Color.gray);
                gunSprite.meshRender.material.SetColor("_TintColor", Color.gray);
            }

            if (this.usingSpecial && !this.doingMelee)
            {
                AnimateSpecial();
            }
            else if (this.usingSpecial && this.doingMelee)
            {
                // Handle the case where usingSpecial is true and doingMelee is true
                // This could be a bug or a design decision, depending on the game mechanics
            }
        }

        private void InitializeProjectiles()
        {
            this.primaryProjectile = (HeroController.GetHeroPrefab(HeroType.SnakeBroSkin) as SnakeBroskin).projectile;
            this.specialProjectile = (HeroController.GetHeroPrefab(HeroType.Rambro) as Rambro).projectile;
            this.specialProjectileData = new JackBroton.ProjectileData
            {
                bulletCount = 0,
                maxBulletCount = 3
            };
        }

        public class ProjectileData
        {
            public int bulletCount;
            public int maxBulletCount = 3;
        }

        private bool CanUseSpecial()
        {
            return !this.hasBeenCoverInAcid && !this.UsingSpecial && this.health > 0 && this.SpecialAmmo > 0;
        }
        private void UseSpecialAmmo() //infinite ammo problem...
        {
            if (this.SpecialAmmo > 0)
            {
                this.SpecialAmmo--;
            }
        }


        protected override void UseFire()
        {
            if (!this.usingSpecial && !this.specialActive && !this.doingMelee && !this.attachedToZipline)
            {
                this.FirePrimaryWeapon();
            }
            else
            {

                if (this.usingSpecial || this.specialActive)
                {
                    //
                }

                if (this.doingMelee)
                {
                    this.CancelMelee();
                }
            }
            if (this.attachedToZipline != null)
            {
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
                float num6 = (float)UnityEngine.Random.Range(-50, 50);
                this.gunFrame = 3;
                this.SetGunSprite(this.gunFrame, 0);
                ProjectileController.SpawnProjectileLocally(this.primaryProjectile, this, base.X + num, base.Y + num2, num5, num6 - 10f + UnityEngine.Random.value * 60f, base.playerNum).life = this.primaryProjectileLifetime;
                Map.DisturbWildLife(base.X, base.Y, 60f, base.playerNum);
                float flashX = base.X + num3;
                float flashY = base.Y + num4;

                if (this.attachedToZipline != null)
                {
                    // Adjust flash position for zipline based on facing direction
                    if (base.transform.localScale.x > 0f)
                    {
                        // Facing right
                        flashX += muzzleFlashPrimaryOffsetXOnZiplineRight;
                        flashY += muzzleFlashPrimaryOffsetYOnZiplineRight;
                    }
                    else
                    {
                        // Facing left
                        flashX += muzzleFlashPrimaryOffsetXOnZiplineLeft;
                        flashY += muzzleFlashPrimaryOffsetYOnZiplineLeft;
                    }
                }

                EffectsController.CreateMuzzleFlashEffect(flashX, flashY, -21f, num5 * 0.15f, num6 * 0.15f, base.transform);
                Sound.GetInstance().PlaySoundEffectAt(JackBroton.Tec9GunSounds, 0.70f, base.transform.position, 1f + this.pitchShiftAmount, true, false, false, 0f);
            }
        }

        protected override void PressSpecial()
        {
            if (this.doingMelee)
            {
                return;
            }

            if (!specialActive && CanUseSpecial())
            {
                this.UsingSpecial = true;

                if (this.specialProjectileData.bulletCount < 2)
                {
                    // First two uses throw the BootKnife
                    this.ThrowProjectile();
                    this.specialProjectileData.bulletCount++;
                }
                else if (this.specialProjectileData.bulletCount == 2)
                {
                    // Third use activates invincibility mode
                    this.InvincibilityMode();
                    this.specialProjectileData.bulletCount = 0; // Reset counter after third use
                }

                // Reduce Special Ammo
                UseSpecialAmmo();
            }
            else
            {
                HeroController.FlashSpecialAmmo(base.playerNum);
            }
        }

        protected override void AnimateSpecial()
        {
            this.frameRate = 0.0667f;
            if (this.wasRunning)
            {
                this.UsingSpecial = false;
                this.isUsingSecondSpecial = false;
                return;
            }

            if (this.wallClimbing || this.wallDrag) // NE ZNAM TREBA LI
            {                
                if (this.UsingSpecial)
                {
                    this.specialActive = true;
                    this.SetGunPosition(3f, 0f);
                    this.ActivateGun();
                    this.UsingSpecial = false;
                    this.ChangeFrame();
                }
                return;
            }

            if (this.UsingSpecial) //invincibility

            {
                this.DeactivateGun();
                int num = 10;
                int num2 = 18;
                int frame = Mathf.Clamp(base.frame, 0, 13);

                this.sprite.SetLowerLeftPixel((float)(num2 * this.spritePixelWidth + frame * this.spritePixelWidth), (float)(num * this.spritePixelHeight));

                if (base.frame == 11)
                {
                    this.ThrowProjectile();
                }
                if (base.frame >= 13)
                {
                    base.frame = 0;
                    this.UsingSpecial = false;  // Stop the special action
                    this.specialActive = false; // Stop animation
                    return;
                }
            }

            if (this.isUsingSecondSpecial)
            {
                this.DeactivateGun();
                int frame = Mathf.Clamp((int)(specialAnimationTimer / this.frameRate), 0, 14);
                this.sprite.SetLowerLeftPixel((17 + frame) * this.spritePixelWidth, 9 * this.spritePixelHeight);
                specialAnimationTimer += Time.deltaTime;

                if (frame >= 7) //frejm kada bi trebalo da se aktivira invincibility
                {
                    this.specialActive = true;

                    //specialAnimationTimer = 0f;
                }
                if (frame > 14)
                    this.SetGunPosition(3f, 0f);
                this.ActivateGun();
                this.UsingSpecial = false;
                specialAnimationTimer = 0f;
                this.ChangeFrame();

            }
        }
        private void InvincibilityMode()
        {
            this.isUsingSecondSpecial = true; // Flag for invincibility special
            this.invulnerable = true; // Activate invulnerability
            this.normalMaterial.SetColor("_TintColor", Color.yellow); // Optional: change color to indicate invincibility
            this.gunSprite.meshRender.material.SetColor("_TintColor", Color.yellow); // Change gun sprite color

            // Set a timer or condition to exit invincibility mode after some time
            StartCoroutine(EndInvincibilityMode());
        }

        private IEnumerator EndInvincibilityMode()
        {
            yield return new WaitForSeconds(5.0f); // Example: Invincibility lasts 5 seconds

            this.invulnerable = false; // Turn off invincibility
            this.normalMaterial.SetColor("_TintColor", Color.gray); // Return to normal color
            this.gunSprite.meshRender.material.SetColor("_TintColor", Color.gray);
            this.isUsingSecondSpecial = false;
        }

        protected override void SetGunPosition(float xOffset, float yOffset)
        {
            // Fixes arms being offset from body
            if (!this.specialActive)
            {
                // Primary mode positions
                if (this.attachedToZipline != null)
                {
                    if (this.right && (this.attachedToZipline.Direction.x < 0f || this.attachedToZipline.IsHorizontalZipline)) // Going right on the zipline
                    {
                        this.gunSprite.transform.localPosition = new Vector3(xOffset + 2f, yOffset + 1f, -1f); // Adjust X and Y for primary weapon when moving right (up)
                    }
                    else if (this.left && (this.attachedToZipline.Direction.x > 0f || this.attachedToZipline.IsHorizontalZipline)) // Going left on the zipline
                    {
                        this.gunSprite.transform.localPosition = new Vector3(xOffset - 2f, yOffset + 1f, -1f); // Adjust X and Y for primary weapon when moving left (up)
                    }
                }
                else
                {
                    this.gunSprite.transform.localPosition = new Vector3(xOffset + 0f, yOffset, -1f); // Default primary position
                }
            }
            else
            {
                // Special mode positions
                if (this.attachedToZipline != null)
                {
                    if (this.right && (this.attachedToZipline.Direction.x < 0f || this.attachedToZipline.IsHorizontalZipline))
                    {
                        this.gunSprite.transform.localPosition = new Vector3(xOffset + 4f, yOffset + 1f, -1f);
                    }
                    else if (this.left && (this.attachedToZipline.Direction.x > 0f || this.attachedToZipline.IsHorizontalZipline))
                    {
                        this.gunSprite.transform.localPosition = new Vector3(xOffset - 4f, yOffset + 1f, -1f);
                    }
                }
                else
                {
                    this.gunSprite.transform.localPosition = new Vector3(xOffset, yOffset + 0.4f, -1f);
                }
            }
        }

        protected override void FireFlashAvatar()
        {
            if (this.isReversingSpecial || this.isDelayingPrimaryFire)
            {
                // Skip the avatar flash logic if reversing special mode or during the primary fire delay
                return;
            }

            base.FireFlashAvatar();
        }

        private void SetupSpecialAttack()
        {
            if (CanUseSpecial())
            {
                float num = base.transform.localScale.x * 26f;
                float num3 = base.transform.localScale.x > 0f ? 15f : -15f;
                float num4 = 8.3f;
                float num5 = base.transform.localScale.x * 750f;
                float num6 = (float)UnityEngine.Random.Range(-5, 5);

                this.gunFrame = 3;
                this.SetGunSprite(this.gunFrame, 0);

                float flashX = base.X + num3;
                float flashY = base.Y + num4;

                if (this.attachedToZipline != null)
                {
                    if (base.transform.localScale.x > 0f)
                    {
                        // Facing right
                        flashX += muzzleFlashOffsetXOnZiplineRight;
                        flashY += muzzleFlashOffsetYOnZiplineRight;
                    }
                    else
                    {
                        // Facing left
                        flashX += muzzleFlashOffsetXOnZiplineLeft;
                        flashY += muzzleFlashOffsetYOnZiplineLeft;
                    }
                }

                
                Sound.GetInstance().PlaySoundEffectAt(JackBroton.BootKnifeSounds, 1f, base.transform.position, 0.88f + this.pitchShiftAmount, true, false, false, 0f);
                Map.DisturbWildLife(base.X, base.Y, 60f, base.playerNum);                
                this.avatarGunFireTime = 0.06f;
                HeroController.SetAvatarFire(base.playerNum, this.usePrimaryAvatar);
                this.pressSpecialFacingDirection = (int)base.transform.localScale.x;
                this.yI += 10f;
                this.xIBlast = -base.transform.localScale.x * 15f;
            }
        }

        private void ThrowProjectile()
        {
            BootKnife BootKnife;
            if (this.down && this.IsOnGround() && this.ducking)
            {
                BootKnife = ProjectileController.SpawnGrenadeLocally(this.bootknifePrefab, this, base.X + Mathf.Sign(base.transform.localScale.x) * 6f, base.Y + 10f, 0.001f, 0.011f, Mathf.Sign(base.transform.localScale.x) * 30f, 70f, base.playerNum, 0) as BootKnife;
            }
            else
            {
                BootKnife = ProjectileController.SpawnGrenadeLocally(this.bootknifePrefab, this, base.X + Mathf.Sign(base.transform.localScale.x) * 6f, base.Y + 10f, 0.001f, 0.011f, Mathf.Sign(base.transform.localScale.x) * 300f, 250f, base.playerNum, 0) as BootKnife;
            }
            BootKnife.enabled = true;
        }

        private void FirePrimaryWeapon()
        {
            if (this.usingSpecial || this.specialActive || this.isDelayingPrimaryFire)
            {                
                return; // Do not fire the primary weapon if special mode is active or reverse animation is in progress
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
            float num6 = (float)UnityEngine.Random.Range(-50, 50);
            this.gunFrame = 3;
            this.SetGunSprite(this.gunFrame, 0);
            ProjectileController.SpawnProjectileLocally(this.primaryProjectile, this, base.X + num, base.Y + num2, num5, num6 - 10f + UnityEngine.Random.value * 60f, base.playerNum).life = this.primaryProjectileLifetime;
            EffectsController.CreateMuzzleFlashEffect(base.X + num3, base.Y + num4, -21f, num5 * 0.15f, num6 * 0.15f, base.transform);
            Sound.GetInstance().PlaySoundEffectAt(JackBroton.Tec9GunSounds, 0.60f, base.transform.position, 1f + this.pitchShiftAmount, true, false, false, 0f);
        }

        protected override void RunGun()
        {
            if (!this.WallDrag && this.gunFrame > 0)
            {
                this.gunCounter += this.t;
                if (this.gunCounter > 0.0334f)
                {
                    this.gunCounter -= 0.0334f;
                    this.gunFrame--;
                    if (this.gunFrame == 3)
                    {
                        this.gunFrame = 0;
                    }
                    this.SetGunSprite(this.gunFrame, 0);
                }
            }
        }

        #region Melee
        protected override void StartCustomMelee()
        {
            if (this.wallClimbing || this.wallDrag || this.jumpingMelee)
            {
                return;
            }

            if (!this.attachedToZipline && this.CanStartNewMelee())
            {
                base.frame = 0;
                base.counter -= 0.0667f;

                this.AnimateMelee();



                this.throwingMook = (this.nearbyMook != null && this.nearbyMook.CanBeThrown());
            }
            else if (this.CanStartMeleeFollowUp())
            {
                this.meleeFollowUp = true;
            }

            // Lock movement during melee 
            this.xI = 0f;
            this.yI = 0f;

            this.StartMeleeCommon();
        }

        protected override void RunKnifeMeleeMovement()
        {
            if (this.wallClimbing || this.wallDrag)
            {
                return;
            }
            if (this.dashingMelee)
            {
                if (base.frame <= 1)
                {
                    this.xI = 0f;
                    this.yI = 0f;
                }
                else if (base.frame <= 3)
                {
                    if (!this.isInQuicksand)
                    {
                        this.xI = this.speed * 1f * base.transform.localScale.x;
                    }
                }
                else
                {
                    this.ApplyFallingGravity();
                }
            }
            else
            {
                if (this.xI != 0f || this.yI != 0f)
                {
                    this.CancelMelee();
                }
            }
        }

        protected override void StartMeleeCommon()
        {

            if (this.wallClimbing || this.wallDrag || this.jumpingMelee)
            {
                return;
            }

            if (!this.meleeFollowUp && this.CanStartNewMelee())
            {
                base.frame = 0;
                base.counter -= 0.0667f;

            }
            else
            {
                return;
            }
            this.throwingMook = (this.nearbyMook != null && this.nearbyMook.CanBeThrown());
            this.ResetMeleeValues();
            this.lerpToMeleeTargetPos = 0f;
            this.doingMelee = true;
            this.showHighFiveAfterMeleeTimer = 0f;
            this.SetMeleeType();
            this.DeactivateGun();
            this.meleeStartPos = base.transform.position;
            this.AnimateMelee();
        }

        protected override void SetMeleeType()
        {
            if (!this.useNewKnifingFrames)
            {
                this.standingMelee = true;
                this.jumpingMelee = false;
                this.dashingMelee = false;
            }
            else if (base.actionState == ActionState.Jumping || base.Y > this.groundHeight + 1f)
            {
                this.standingMelee = false;
                this.jumpingMelee = true;
                this.dashingMelee = false;
            }
            else if (this.right || this.left)
            {
                this.standingMelee = false;
                this.jumpingMelee = false;
                this.dashingMelee = true;
            }
            else
            {
                this.standingMelee = true;
                this.jumpingMelee = false;
                this.dashingMelee = false;
            }
        }

        protected override void AnimateMelee()
        {
            if (this.wallClimbing || this.wallDrag || base.actionState == ActionState.Jumping)
            {
                if (base.actionState != ActionState.ClimbingLadder)
                {
                    this.CancelMelee();
                    return;
                }
            }

            if (base.frame == 3)
            {
                if (this.dashingMelee)
                {
                    PerformKnifeMeleeAttack(shouldTryHitTerrain: true, playMissSound: true);
                }
            }

            this.xI = 0f;
            this.yI = 0f;

            this.frameRate = 0.0667f;
            base.counter += Time.deltaTime;

            if (base.counter >= this.frameRate)
            {
                base.frame++;
                base.counter = 0f;
            }

            if (this.dashingMelee)
            {
                int num = 6;
                int num2 = 17;
                int frame = Mathf.Clamp(base.frame, 0, 7);

                this.sprite.SetLowerLeftPixel((float)(num2 * this.spritePixelWidth + frame * this.spritePixelWidth), (float)(num * this.spritePixelHeight));
                this.avatarGunFireTime = 0.07f;
                HeroController.SetAvatarAngry(base.playerNum, this.usePrimaryAvatar);
                if (base.frame >= 7)
                {
                    base.frame = 0;
                    this.CancelMelee();
                }
            }
            else
            {
                this.frameRate = 0.0883f; //NE ZNAM MOZE LI OVO OVDE
                int num = 1;
                int num2 = 23;
                int frame = Mathf.Clamp(base.frame, 0, 8);

                this.sprite.SetLowerLeftPixel((float)(num2 * this.spritePixelWidth + frame * this.spritePixelWidth), (float)(num * this.spritePixelHeight));
                Sound.GetInstance().PlaySoundEffectAt(JackBroton.Questions, 0.70f, base.transform.position, 1f + this.pitchShiftAmount, true, false, false, 0f);
               
                /*if (base.frame == 18)     ovo je logika koju mozemo ubaciti u special recimo
                {
                    this.ThrowProjectile();
                }*/
                if (base.frame >= 8)
                {
                    base.frame = 0;
                    this.CancelMelee();
                }
                if (base.frame == 0 && this.nearbyMook != null && this.nearbyMook.CanBeThrown() && (this.highFive || this.standingMelee))
                {
                    this.CancelMelee();
                    this.ThrowBackMook(this.nearbyMook);
                    this.nearbyMook = null;
                }
            }
        }

        protected override bool MustIgnoreHighFiveMeleePress()
        {
            return this.heldGrenade != null || this.heldMook != null || this.usingSpecial || this.attachedToZipline || this.doingMelee;
        }

        protected override void PerformKnifeMeleeAttack(bool shouldTryHitTerrain, bool playMissSound)
        {
            bool flag;

            Map.DamageDoodads(3, DamageType.Knock, base.X + (float)(base.Direction * 4), base.Y, 0f, 0f, 6f, base.playerNum, out flag, null);
            base.KickDoors(24f);

            AudioClip selectedSound;
            if (this.gunSprite.meshRender.material == this.normalGunMaterial)
            {
                selectedSound = JackBroton.DashingMeleeSounds[0];
            }
            else
            {
                selectedSound = JackBroton.DashingMeleeSounds[1];
            }
            if (Map.HitClosestUnit(this, base.playerNum, 4, DamageType.Knifed, 14f, 24f, base.X + base.transform.localScale.x * 8f, base.Y + 8f, base.transform.localScale.x * 200f, 500f, true, false, base.IsMine, false, true))
            {
                if (selectedSound != null)
                {
                    this.sound.PlaySoundEffectAt(selectedSound, 0.7f, base.transform.position, 1f, true, false, false, 0f);
                }
                this.meleeHasHit = true;
            }
            else if (playMissSound)
            {
                this.sound.PlaySoundEffectAt(this.soundHolder.missSounds, 0.7f, base.transform.position, 1f, true, false, false, 0f);
            }
            this.meleeChosenUnit = null;
            if (shouldTryHitTerrain && TryMeleeTerrain(0, 2))
            {
                this.meleeHasHit = true;
            }
        }
        protected override bool TryMeleeTerrain(int offset = 0, int meleeDamage = 2)
        {
            if (!Physics.Raycast(new Vector3(base.X - base.transform.localScale.x * 4f, base.Y + 4f, 0f), new Vector3(base.transform.localScale.x, 0f, 0f), out this.raycastHit, (float)(16 + offset), this.groundLayer))
            {
                return false;
            }
            Cage component = this.raycastHit.collider.GetComponent<Cage>();
            if (component == null && this.raycastHit.collider.transform.parent != null)
            {
                component = this.raycastHit.collider.transform.parent.GetComponent<Cage>();
            }
            if (component != null)
            {
                MapController.Damage_Networked(this, this.raycastHit.collider.gameObject, component.health, DamageType.Melee, 0f, 40f, this.raycastHit.point.x, this.raycastHit.point.y);
                return true;
            }
            MapController.Damage_Networked(this, this.raycastHit.collider.gameObject, meleeDamage, DamageType.Melee, 0f, 40f, this.raycastHit.point.x, this.raycastHit.point.y);
            if (this.currentMeleeType == BroBase.MeleeType.Knife)
            {
                this.sound.PlaySoundEffectAt(this.soundHolder.meleeHitTerrainSound, 0.3f, base.transform.position, 1f, true, false, false, 0f);
            }
            else
            {
                this.sound.PlaySoundEffectAt(this.soundHolder.alternateMeleeHitSound, 0.3f, base.transform.position, 1f, true, false, false, 0f);
            }
            EffectsController.CreateProjectilePopWhiteEffect(base.X + this.width * base.transform.localScale.x, base.Y + this.height + 4f);
            return true;
        }
        #endregion

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }

    internal class ProjectileData
    {
        public int bulletCount { get; internal set; }
        public int maxBulletCount { get; internal set; }
    }
}