
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


namespace Cobro
{
    [HeroPreset("Cobro", HeroType.Rambro)]
    public class Cobro : CustomHero

    {
        Projectile[] projectiles;
        private Projectile primaryProjectile;
        private Projectile specialProjectile;
        private Material normalMaterial, stealthMaterial, normalGunMaterial, stealthGunMaterial, normalAvatarMaterial;
        private float primaryAttackRange = 20f;
        private float primaryAttackSpeed = 480f;
        private float primaryProjectileLifetime = 0.19f; 
        private ProjectileData specialProjectileData;
        public static AudioClip[] CobroGunSounds;
        public static AudioClip[] MachineGunSounds;
        public static AudioClip[] DashingMeleeSounds;
        public static AudioClip[] CobroSmack;
        public static AudioClip[] CobroSmack2;
        private AudioClip emptyGunSound;
        private int specialAmmo = 6;


        private bool wasInvulnerable = false;

        //special_variables
        private bool UsingSpecial = false;
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
        
        CoorsCan coorscanPrefab;


        protected override void Awake()
        {
            base.Awake();
            this.InitializeResources();
            this.InitializeProjectiles();
            this.InitializeAudioClips();
            this.gunSpriteHangingFrame = 9;

            coorscanPrefab = new GameObject("CoorsCan", new Type[] { typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer), typeof(SpriteSM), typeof(CoorsCan) }).GetComponent<CoorsCan>();
            coorscanPrefab.enabled = false;

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

        public static void PreloadSprites(string directoryPath, List<string> spriteNames)
        {
            foreach (var spriteName in spriteNames)
            {
                string spritePath = Path.Combine(directoryPath, spriteName);
                if (File.Exists(spritePath))
                {
                    string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    ResourcesController.GetMaterial(directoryName, "CoorsCan.png");
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
            if (Cobro.DashingMeleeSounds == null)
            {
                Cobro.DashingMeleeSounds = new AudioClip[2];
                Cobro.DashingMeleeSounds[0] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "CobroSmack.wav");
                Cobro.DashingMeleeSounds[1] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "CobroSmack2.wav");
                Cobro.DashingMeleeSounds[2] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "CobroTerrainHit.wav");
            }
            this.emptyGunSound = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "EmptyGun.wav"); 

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
                stealthMaterial.SetColor("_TintColor", Color.gray);
                gunSprite.meshRender.material.SetColor("_TintColor", Color.gray);
            }
            
            if (this.UsingSpecial || this.isReversingSpecial && !this.doingMelee)
            {
                AnimateSpecial(); 
            }
            
            if (isDelayingPrimaryFire)
            {
                primaryFireDelayTimer += Time.deltaTime;
                if (primaryFireDelayTimer >= 5 * this.frameRate) 
                {
                    isDelayingPrimaryFire = false;
                    primaryFireDelayTimer = 0f;
                }
            }
            // If special ammo is depleted and reverse animation is finished, keep gun active until next special button press
            if (!this.specialActive && this.SpecialAmmo <= 0)
            {
                return;
            }
        }

        private void InitializeProjectiles()
        {
            this.primaryProjectile = (HeroController.GetHeroPrefab(HeroType.Rambro) as Rambro).projectile;
            this.specialProjectile = (HeroController.GetHeroPrefab(HeroType.IndianaBrones) as IndianaBrones).projectile;
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
            else            {
               
                if (this.usingSpecial || this.specialActive)
                {
                    ReverseSpecialMode();
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
                float num6 = (float)UnityEngine.Random.Range(-15, 15); 
                this.gunFrame = 3;
                this.SetGunSprite(this.gunFrame, 0);
                ProjectileController.SpawnProjectileLocally(this.primaryProjectile, this, base.X + num, base.Y + num2, num5, num6 - 10f + UnityEngine.Random.value * 35f, base.playerNum).life = this.primaryProjectileLifetime;
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
                Sound.GetInstance().PlaySoundEffectAt(Cobro.MachineGunSounds, 0.70f, base.transform.position, 1f + this.pitchShiftAmount, true, false, false, 0f);
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
                sprite.GetComponent<Renderer>().material = stealthMaterial;
                gunSprite.meshRender.material = stealthGunMaterial;
            }
            else if (specialActive)
            {               
                if (this.SpecialAmmo > 0)
                {
                    SetupSpecialAttack();
                    FireSpecialWeapon();
                }
                else if (this.SpecialAmmo <= 0)
                {

                    HeroController.FlashSpecialAmmo(base.playerNum);
                    Sound.GetInstance().PlaySoundEffectAt(emptyGunSound, 1f, base.transform.position);
                    // Do not reverse special mode immediately, wait for another press
                    return;
                }
            }
            else if (!specialActive && this.SpecialAmmo <= 0)
            {               
                HeroController.FlashSpecialAmmo(base.playerNum);
                Sound.GetInstance().PlaySoundEffectAt(emptyGunSound, 1f, base.transform.position);
            }
        }

        protected override void AnimateSpecial()
        {
            this.frameRate = 0.0334f;
            if (this.wasRunning)
            {
                this.UsingSpecial = false;
                this.isReversingSpecial = false;
                return;
            }

            if (this.wallClimbing || this.wallDrag)
            {
                // Skip the animation logic but still switch the mode/sprite
                if (this.UsingSpecial)
                {
                    this.specialActive = true;
                    base.material = this.stealthMaterial;
                    this.gunSprite.meshRender.material = this.stealthGunMaterial;
                    this.SetGunPosition(3f, 0f);
                    this.ActivateGun();
                    this.UsingSpecial = false;
                    this.ChangeFrame();
                }
                return; 
            }
           
            if (this.UsingSpecial)
            {
                this.DeactivateGun();
                int frame = Mathf.Clamp((int)(specialAnimationTimer / this.frameRate), 0, 8);
                this.sprite.SetLowerLeftPixel((23 + frame) * this.spritePixelWidth, 9 * this.spritePixelHeight);
                specialAnimationTimer += Time.deltaTime;

                if (frame >= 8)
                {
                    this.specialActive = true;
                    base.material = this.stealthMaterial;
                    this.gunSprite.meshRender.material = this.stealthGunMaterial;
                    this.SetGunPosition(3f, 0f);
                    this.ActivateGun();
                    this.UsingSpecial = false;
                    this.ChangeFrame();
                    specialAnimationTimer = 0f;
                }
            }
            else if (this.isReversingSpecial)
            {
                this.DeactivateGun();
                int frame = Mathf.Clamp(5 - (int)(specialAnimationTimer / this.frameRate), 0, 5);
                this.sprite.SetLowerLeftPixel((23 + frame) * this.spritePixelWidth, 9 * this.spritePixelHeight);
                specialAnimationTimer += Time.deltaTime;

                if (frame <= 0)
                {
                    this.isReversingSpecial = false;
                    this.ActivateGun();
                    base.GetComponent<Renderer>().material = normalMaterial;
                    gunSprite.meshRender.material = normalGunMaterial;
                    this.ChangeFrame();

                    isDelayingPrimaryFire = true;
                    primaryFireDelayTimer = 0f;
                    specialAnimationTimer = 0f;
                }

            }
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

                EffectsController.CreateMuzzleFlashMediumEffect(flashX, flashY, -20f, num5 * 0.06f, num6 * 0.06f, base.transform);
                Sound.GetInstance().PlaySoundEffectAt(Cobro.CobroGunSounds, 1f, base.transform.position, 0.88f + this.pitchShiftAmount, true, false, false, 0f);
                Map.DisturbWildLife(base.X, base.Y, 60f, base.playerNum);
                SortOfFollow.Shake(0.4f, 0.4f);
                this.avatarGunFireTime = 0.06f;
                HeroController.SetAvatarFire(base.playerNum, this.usePrimaryAvatar);
                this.pressSpecialFacingDirection = (int)base.transform.localScale.x;
                this.yI += 10f;
                this.xIBlast = -base.transform.localScale.x * 15f;
            }
        }

        private void FireSpecialWeapon()
        {
            if (this.SpecialAmmo > 0)
            {
                float x = base.X + base.transform.localScale.x * 26f;
                float y = base.Y + 8.3f;
                float xI = base.transform.localScale.x * 750f;
                float yI = (float)UnityEngine.Random.Range(-10, 10);

                if (this.attachedToZipline != null)
                {
                    //ne radi x += base.transform.localScale.x > 0f ? 1f : -1f;
                }

                ProjectileController.SpawnProjectileLocally(this.specialProjectile, this, x, y, xI, yI, base.playerNum);
                Map.DisturbWildLife(base.X, base.Y, 60f, base.playerNum);
                UseSpecialAmmo(); // Decrement special ammo here
            }
            else
            {
                HeroController.FlashSpecialAmmo(base.playerNum);
                Sound.GetInstance().PlaySoundEffectAt(emptyGunSound, 1f, base.transform.position);
            }
        }

        private void ReverseSpecialMode()
        {

            this.UsingSpecial = false;
            this.specialActive = false;
            this.isReversingSpecial = true;
            this.usingSpecialFrame = 8;
            this.specialAnimationTimer = 0f;
        }

        private void FirePrimaryWeapon()
        {
            if (this.usingSpecial || this.specialActive || this.isReversingSpecial || this.isDelayingPrimaryFire)
            {
                if (this.usingSpecial || this.specialActive)
                {
                    ReverseSpecialMode();
                }
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
            float num6 = (float)UnityEngine.Random.Range(-15, 15); 
            this.gunFrame = 3;
            this.SetGunSprite(this.gunFrame, 0);                                                           
            ProjectileController.SpawnProjectileLocally(this.primaryProjectile, this, base.X + num, base.Y + num2, num5, num6 - 10f + UnityEngine.Random.value * 35f, base.playerNum).life = this.primaryProjectileLifetime;
            EffectsController.CreateMuzzleFlashEffect(base.X + num3, base.Y + num4, -21f, num5 * 0.15f, num6 * 0.15f, base.transform);
            Sound.GetInstance().PlaySoundEffectAt(Cobro.MachineGunSounds, 0.60f, base.transform.position, 0.85f + this.pitchShiftAmount, true, false, false, 0f);
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
                int num = 10;
                int num2 = 11;
                int frame = Mathf.Clamp(base.frame, 0, 20);

                this.sprite.SetLowerLeftPixel((float)(num2 * this.spritePixelWidth + frame * this.spritePixelWidth), (float)(num * this.spritePixelHeight));

                if (base.frame == 18)
                {
                    this.ThrowProjectile();
                }
                if (base.frame >= 20)
                {
                    base.frame = 0;
                    this.CancelMelee();
                }
                if (base.frame == 2 && this.nearbyMook != null && this.nearbyMook.CanBeThrown() && (this.highFive || this.standingMelee))
                {
                    this.CancelMelee();
                    this.ThrowBackMook(this.nearbyMook);
                    this.nearbyMook = null;
                }
            }
        }

        private void ThrowProjectile()
        {
            CoorsCan Coorscan;
            if (this.down && this.IsOnGround() && this.ducking)
            {
                Coorscan = ProjectileController.SpawnGrenadeLocally(this.coorscanPrefab, this, base.X + Mathf.Sign(base.transform.localScale.x) * 6f, base.Y + 10f, 0.001f, 0.011f, Mathf.Sign(base.transform.localScale.x) * 30f, 70f, base.playerNum, 0) as CoorsCan;
            }
            else
            {
                Coorscan = ProjectileController.SpawnGrenadeLocally(this.coorscanPrefab, this, base.X + Mathf.Sign(base.transform.localScale.x) * 6f, base.Y + 10f, 0.001f, 0.011f, Mathf.Sign(base.transform.localScale.x) * 300f, 250f, base.playerNum, 0) as CoorsCan;
            }
            Coorscan.enabled = true;
        }


        protected override bool MustIgnoreHighFiveMeleePress()
        {
            return this.heldGrenade != null || this.heldMook != null || this.usingSpecial || this.attachedToZipline || this.jumpingMelee || base.actionState == ActionState.Jumping || this.doingMelee;
        }

        protected override void PerformKnifeMeleeAttack(bool shouldTryHitTerrain, bool playMissSound) 
        {
            bool flag;

            Map.DamageDoodads(3, DamageType.Knock, base.X + (float)(base.Direction * 4), base.Y, 0f, 0f, 6f, base.playerNum, out flag, null);
            base.KickDoors(24f);

            AudioClip selectedSound;
            if (this.gunSprite.meshRender.material == this.normalGunMaterial)
            {
                selectedSound = Cobro.DashingMeleeSounds[0];
            }
            else
            {
                selectedSound = Cobro.DashingMeleeSounds[1];
            }
            if (Map.HitClosestUnit(this, base.playerNum, 4, DamageType.Knock, 14f, 24f, base.X + base.transform.localScale.x * 8f, base.Y + 8f, base.transform.localScale.x * 200f, 500f, true, false, base.IsMine, false, true))
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
            if (Physics.Raycast(new Vector3(base.X - base.transform.localScale.x * 4f, base.Y + 4f, 0f), new Vector3(base.transform.localScale.x, 0f, 0f), out raycastHit, 16 + offset, groundLayer))
            {
                Cage component = raycastHit.collider.GetComponent<Cage>();
                if (component == null && raycastHit.collider.transform.parent != null)
                {
                    component = raycastHit.collider.transform.parent.GetComponent<Cage>();
                }

                if (component != null)
                {
                    MapController.Damage_Networked(this, raycastHit.collider.gameObject, component.health, DamageType.Melee, 0f, 40f, raycastHit.point.x, raycastHit.point.y);
                    return true;
                }

                MapController.Damage_Networked(this, raycastHit.collider.gameObject, meleeDamage, DamageType.Melee, 0f, 40f, raycastHit.point.x, raycastHit.point.y);
                if (currentMeleeType == MeleeType.Knife)
                {
                    sound.PlaySoundEffectAt(soundHolder.alternateMeleeHitSound, 0.3f, base.transform.position);
                }
                else
                {
                    sound.PlaySoundEffectAt(soundHolder.alternateMeleeHitSound, 0.3f, base.transform.position);
                }

                EffectsController.CreateProjectilePopWhiteEffect(base.X + width * base.transform.localScale.x, base.Y + height + 4f);
                return true;
            }

            return false;
        }
        #endregion

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
