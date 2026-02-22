
using System;
using System.IO;
using System.Reflection;
using BroMakerLib;
using BroMakerLib.CustomObjects.Bros;
using UnityEngine;


namespace Cobro
{
    [HeroPreset("Cobro", HeroType.Rambro)]
    public class Cobro : CustomHero

    {
        private Projectile primaryProjectile;       
        private Projectile specialProjectile;
        private Material normalMaterial;
        private Material stealthMaterial;
        private Material normalGunMaterial;
        private Material stealthGunMaterial;
        private float primaryAttackRange = 20f;
        private float primaryAttackSpeed = 480f;
        private float primaryProjectileLifetime = 0.19f;
        private Cobro.ProjectileData specialProjectileData;
        public static AudioClip[] CobroGunSounds;
        public static AudioClip[] MachineGunSounds;
        public static AudioClip[] DashingMeleeSounds;
        public static AudioClip[] CobroSmack;
        public static AudioClip[] CobroSmack2;
        private AudioClip emptyGunSound;
        private int specialAmmo = 6;
        private bool wasInvulnerable;
        private bool UsingSpecial;
        protected bool specialActive;
        private int usingSpecialFrame;
        private float specialAnimationTimer;
        private bool isReversingSpecial;
        private bool isDelayingPrimaryFire;
        private float primaryFireDelayTimer;
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
        private CoorsCan coorscanPrefab;

        protected override void Awake()
        {
            base.Awake();
            this.InitializeResources();
            this.InitializeProjectiles();
            this.InitializeAudioClips();
            this.gunSpriteHangingFrame = 9;
            this.coorscanPrefab = new GameObject("CoorsCan", new Type[]
            {
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(SpriteSM),
                typeof(CoorsCan)
            }).GetComponent<CoorsCan>();
            this.coorscanPrefab.enabled = false;
        }
        
        private void InitializeResources()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.normalMaterial = base.material;
            this.stealthMaterial = ResourcesController.GetMaterial(directoryName, "spriteSpecial.png");
            this.normalGunMaterial = this.gunSprite.meshRender.material;
            this.stealthGunMaterial = ResourcesController.GetMaterial(directoryName, "gunSpriteSpecial.png");
            ResourcesController.GetMaterial(directoryName, "avatar.png");
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
                this.normalMaterial.SetColor("_TintColor", Color.gray);
                this.stealthMaterial.SetColor("_TintColor", Color.gray);
                this.gunSprite.meshRender.material.SetColor("_TintColor", Color.gray);
            }
            if (this.UsingSpecial || (this.isReversingSpecial && !this.doingMelee))
            {
                this.AnimateSpecial();
            }
            if (this.isDelayingPrimaryFire)
            {
                this.primaryFireDelayTimer += Time.deltaTime;
                if (this.primaryFireDelayTimer >= 5f * this.frameRate)
                {
                    this.isDelayingPrimaryFire = false;
                    this.primaryFireDelayTimer = 0f;
                }
            }
            if (!this.specialActive)
            {
                int num = this.SpecialAmmo;
                return;
            }
        }

        private void InitializeProjectiles()
        {
            this.primaryProjectile = (HeroController.GetHeroPrefab((HeroType)0) as Rambro).projectile;
            this.specialProjectile = (HeroController.GetHeroPrefab((HeroType)13) as IndianaBrones).projectile;
            this.specialProjectileData = new Cobro.ProjectileData
            {
                bulletCount = 0,
                maxBulletCount = 6
            };
        }
        
        private bool CanUseSpecial()
        {
            return !this.hasBeenCoverInAcid && !this.UsingSpecial && this.health > 0 && this.SpecialAmmo > 0;
        }

        private void UseSpecialAmmo()
        {
            if (this.SpecialAmmo > 0)
            {
                int num = this.SpecialAmmo;
                this.SpecialAmmo = num - 1;
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
                    this.ReverseSpecialMode();
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
                float num7 = base.X + num3;
                float num8 = base.Y + num4;
                if (this.attachedToZipline != null)
                {
                    if (base.transform.localScale.x > 0f)
                    {
                        num7 += this.muzzleFlashPrimaryOffsetXOnZiplineRight;
                        num8 += this.muzzleFlashPrimaryOffsetYOnZiplineRight;
                    }
                    else
                    {
                        num7 += this.muzzleFlashPrimaryOffsetXOnZiplineLeft;
                        num8 += this.muzzleFlashPrimaryOffsetYOnZiplineLeft;
                    }
                }
                EffectsController.CreateMuzzleFlashEffect(num7, num8, -21f, num5 * 0.15f, num6 * 0.15f, base.transform);
                Sound.GetInstance().PlaySoundEffectAt(Cobro.MachineGunSounds, 0.7f, base.transform.position, 1f + this.pitchShiftAmount, true, false, false, 0f);
            }
        }
        
        protected override void PressSpecial()
        {
            if (this.doingMelee)
            {
                return;
            }
            if (!this.specialActive && this.CanUseSpecial())
            {
                this.UsingSpecial = true;
                this.sprite.GetComponent<Renderer>().material = this.stealthMaterial;
                this.gunSprite.meshRender.material = this.stealthGunMaterial;
                return;
            }
            if (this.specialActive)
            {
                if (this.SpecialAmmo > 0)
                {
                    this.SetupSpecialAttack();
                    this.FireSpecialWeapon();
                    return;
                }
                if (this.SpecialAmmo <= 0)
                {
                    HeroController.FlashSpecialAmmo(base.playerNum);
                    Sound.GetInstance().PlaySoundEffectAt(this.emptyGunSound, 1f, base.transform.position, 1f, true, false, false, 0f);
                    return;
                }
            }
            else if (!this.specialActive && this.SpecialAmmo <= 0)
            {
                HeroController.FlashSpecialAmmo(base.playerNum);
                Sound.GetInstance().PlaySoundEffectAt(this.emptyGunSound, 1f, base.transform.position, 1f, true, false, false, 0f);
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
                int num = Mathf.Clamp((int)(this.specialAnimationTimer / this.frameRate), 0, 8);
                this.sprite.SetLowerLeftPixel((float)((23 + num) * this.spritePixelWidth), (float)(9 * this.spritePixelHeight));
                this.specialAnimationTimer += Time.deltaTime;
                if (num >= 8)
                {
                    this.specialActive = true;
                    base.material = this.stealthMaterial;
                    this.gunSprite.meshRender.material = this.stealthGunMaterial;
                    this.SetGunPosition(3f, 0f);
                    this.ActivateGun();
                    this.UsingSpecial = false;
                    this.ChangeFrame();
                    this.specialAnimationTimer = 0f;
                    return;
                }
            }
            else if (this.isReversingSpecial)
            {
                this.DeactivateGun();
                int num2 = Mathf.Clamp(5 - (int)(this.specialAnimationTimer / this.frameRate), 0, 5);
                this.sprite.SetLowerLeftPixel((float)((23 + num2) * this.spritePixelWidth), (float)(9 * this.spritePixelHeight));
                this.specialAnimationTimer += Time.deltaTime;
                if (num2 <= 0)
                {
                    this.isReversingSpecial = false;
                    this.ActivateGun();
                    base.GetComponent<Renderer>().material = this.normalMaterial;
                    this.gunSprite.meshRender.material = this.normalGunMaterial;
                    this.ChangeFrame();
                    this.isDelayingPrimaryFire = true;
                    this.primaryFireDelayTimer = 0f;
                    this.specialAnimationTimer = 0f;
                }
            }
        }
        
        protected override void SetGunPosition(float xOffset, float yOffset)
        {
            if (!this.specialActive)
            {
                if (!(this.attachedToZipline != null))
                {
                    this.gunSprite.transform.localPosition = new Vector3(xOffset + 0f, yOffset, -1f);
                    return;
                }
                if (this.right && (this.attachedToZipline.Direction.x < 0f || this.attachedToZipline.IsHorizontalZipline))
                {
                    this.gunSprite.transform.localPosition = new Vector3(xOffset + 2f, yOffset + 1f, -1f);
                    return;
                }
                if (this.left && (this.attachedToZipline.Direction.x > 0f || this.attachedToZipline.IsHorizontalZipline))
                {
                    this.gunSprite.transform.localPosition = new Vector3(xOffset - 2f, yOffset + 1f, -1f);
                    return;
                }
            }
            else if (this.attachedToZipline != null)
            {
                if (this.right && (this.attachedToZipline.Direction.x < 0f || this.attachedToZipline.IsHorizontalZipline))
                {
                    this.gunSprite.transform.localPosition = new Vector3(xOffset + 4f, yOffset + 1f, -1f);
                    return;
                }
                if (this.left && (this.attachedToZipline.Direction.x > 0f || this.attachedToZipline.IsHorizontalZipline))
                {
                    this.gunSprite.transform.localPosition = new Vector3(xOffset - 4f, yOffset + 1f, -1f);
                    return;
                }
            }
            else
            {
                this.gunSprite.transform.localPosition = new Vector3(xOffset, yOffset + 0.4f, -1f);
            }
        }
        
        protected override void FireFlashAvatar()
        {
            if (this.isReversingSpecial || this.isDelayingPrimaryFire)
            {
                return;
            }
            
            base.FireFlashAvatar();
        }
        
        private void SetupSpecialAttack()
        {
            if (this.CanUseSpecial())
            {
                Vector3 localScale = base.transform.localScale;
                float num = (base.transform.localScale.x > 0f) ? 15f : -15f;
                float num2 = 8.3f;
                float num3 = base.transform.localScale.x * 750f;
                float num4 = (float)UnityEngine.Random.Range(-5, 5);
                this.gunFrame = 3;
                this.SetGunSprite(this.gunFrame, 0);
                float num5 = base.X + num;
                float num6 = base.Y + num2;
                if (this.attachedToZipline != null)
                {
                    if (base.transform.localScale.x > 0f)
                    {
                        num5 += this.muzzleFlashOffsetXOnZiplineRight;
                        num6 += this.muzzleFlashOffsetYOnZiplineRight;
                    }
                    else
                    {
                        num5 += this.muzzleFlashOffsetXOnZiplineLeft;
                        num6 += this.muzzleFlashOffsetYOnZiplineLeft;
                    }
                }
                EffectsController.CreateMuzzleFlashMediumEffect(num5, num6, -20f, num3 * 0.06f, num4 * 0.06f, base.transform);
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
                float num = base.X + base.transform.localScale.x * 26f;
                float num2 = base.Y + 8.3f;
                float num3 = base.transform.localScale.x * 750f;
                float num4 = (float)UnityEngine.Random.Range(-10, 10);
                ProjectileController.SpawnProjectileLocally(this.specialProjectile, this, num, num2, num3, num4, base.playerNum);
                Map.DisturbWildLife(base.X, base.Y, 60f, base.playerNum);
                this.UseSpecialAmmo();
                return;
            }
            HeroController.FlashSpecialAmmo(base.playerNum);
            Sound.GetInstance().PlaySoundEffectAt(this.emptyGunSound, 1f, base.transform.position, 1f, true, false, false, 0f);
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
                    this.ReverseSpecialMode();
                }
                return;
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
            Sound.GetInstance().PlaySoundEffectAt(Cobro.MachineGunSounds, 0.6f, base.transform.position, 0.85f + this.pitchShiftAmount, true, false, false, 0f);
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

        protected override void PressHighFiveMelee(bool forceHighFive = false)
        {
            if (this.health <= 0)
            {
                return;
            }

            if (this.MustIgnoreHighFiveMeleePress())
            {
                return;
            }

            this.SetGestureAnimation(GestureElement.Gestures.None);
            Grenade nearbyGrenade = Map.GetNearbyGrenade(20f, base.X, base.Y + this.waistHeight);
            this.FindNearbyMook();
            TeleportDoor nearbyTeleportDoor = Map.GetNearbyTeleportDoor(base.X, base.Y);
            if (nearbyTeleportDoor != null && this.CanUseSwitch() && nearbyTeleportDoor.Activate(this))
            {
                return;
            }

            Switch nearbySwitch = Map.GetNearbySwitch(base.X, base.Y);
            if (GameModeController.IsDeathMatchMode || GameModeController.GameMode == GameMode.BroDown)
            {
                if (nearbySwitch != null && this.CanUseSwitch())
                {
                    nearbySwitch.Activate(this);
                }
                else
                {
                    bool flag = false;
                    for (int i = -1; i < 4; i++)
                    {
                        if (i != base.playerNum && Map.IsUnitNearby(i, base.X + base.transform.localScale.x * 16f,
                                base.Y + 8f, 28f, 14f, true, out this.meleeChosenUnit))
                        {
                            this.StartMelee();
                            flag = true;
                        }
                    }

                    if (!flag && nearbySwitch != null && this.CanUseSwitch())
                    {
                        nearbySwitch.Activate(this);
                    }
                }
            }
            // Don't allow throwing can continuously
            if (nearbyGrenade != null && (!(nearbyGrenade is CoorsCan can) || can.lastThrownCooldown <= 0f))
            {
                this.doingMelee = (this.dashingMelee = false);
                this.ThrowBackGrenade(nearbyGrenade);
            }
            else if (!GameModeController.IsDeathMatchMode || !this.doingMelee)
            {
                if (Map.IsCitizenNearby(base.X, base.Y, 32, 32))
                {
                    if (!this.doingMelee)
                    {
                        this.StartHighFive();
                    }
                }
                else if (forceHighFive && !this.doingMelee)
                {
                    this.StartHighFive();
                }
                else if (nearbySwitch != null && this.CanUseSwitch())
                {
                    nearbySwitch.Activate(this);
                }
                else if (this.meleeChosenUnit == null && Map.IsUnitNearby(-1,
                             base.X + base.transform.localScale.x * 16f, base.Y + 8f, 28f, 14f, false,
                             out this.meleeChosenUnit))
                {
                    this.StartMelee();
                }
                else if (this.CheckBustCage())
                {
                    this.StartMelee();
                }
                else if (HeroController.IsAnotherPlayerNearby(base.playerNum, base.X, base.Y, 32f, 32f))
                {
                    if (!this.doingMelee)
                    {
                        this.StartHighFive();
                    }
                }
                else
                {
                    this.StartMelee();
                }
            }
        }

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
                    return;
                }
                if (base.frame > 3)
                {
                    this.ApplyFallingGravity();
                    return;
                }
                if (!this.isInQuicksand)
                {
                    this.xI = this.speed * 1f * base.transform.localScale.x;
                    return;
                }
            }
            else if (this.xI != 0f || this.yI != 0f)
            {
                this.CancelMelee();
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
                this.throwingMook = (this.nearbyMook != null && this.nearbyMook.CanBeThrown());
                this.ResetMeleeValues();
                this.lerpToMeleeTargetPos = 0f;
                this.doingMelee = true;
                this.showHighFiveAfterMeleeTimer = 0f;
                this.SetMeleeType();
                this.DeactivateGun();
                this.meleeStartPos = base.transform.position;
                this.AnimateMelee();
                return;
            }
        }

        protected override void SetMeleeType()
        {
            if (!this.useNewKnifingFrames)
            {
                this.standingMelee = true;
                this.jumpingMelee = false;
                this.dashingMelee = false;
                return;
            }
            if (base.actionState == (ActionState)3 || base.Y > this.groundHeight + 1f)
            {
                this.standingMelee = false;
                this.jumpingMelee = true;
                this.dashingMelee = false;
                return;
            }
            if (this.right || this.left)
            {
                this.standingMelee = false;
                this.jumpingMelee = false;
                this.dashingMelee = true;
                return;
            }
            this.standingMelee = true;
            this.jumpingMelee = false;
            this.dashingMelee = false;
        }
        
        protected override void AnimateMelee()
        {
            if ((this.wallClimbing || this.wallDrag || base.actionState == (ActionState)3) && base.actionState != (ActionState)6)
            {
                this.CancelMelee();
                return;
            }
            if (base.frame == 3 && this.dashingMelee)
            {
                this.PerformKnifeMeleeAttack(true, true);
            }
            this.xI = 0f;
            this.yI = 0f;
            this.frameRate = 0.0667f;
            base.counter += Time.deltaTime;
            if (base.counter >= this.frameRate)
            {
                int frame = base.frame;
                base.frame = frame + 1;
                base.counter = 0f;
            }
            if (this.dashingMelee)
            {
                int num = 6;
                int num2 = 17;
                int num3 = Mathf.Clamp(base.frame, 0, 7);
                this.sprite.SetLowerLeftPixel((float)(num2 * this.spritePixelWidth + num3 * this.spritePixelWidth), (float)(num * this.spritePixelHeight));
                this.avatarGunFireTime = 0.07f;
                HeroController.SetAvatarAngry(base.playerNum, this.usePrimaryAvatar);
                if (base.frame >= 7)
                {
                    base.frame = 0;
                    this.CancelMelee();
                    return;
                }
            }
            else
            {
                int num4 = 10;
                int num5 = 11;
                int num6 = Mathf.Clamp(base.frame, 0, 20);
                this.sprite.SetLowerLeftPixel((float)(num5 * this.spritePixelWidth + num6 * this.spritePixelWidth), (float)(num4 * this.spritePixelHeight));
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
            CoorsCan coorsCan;
            if (this.down && this.IsOnGround() && this.ducking)
            {
                coorsCan = (ProjectileController.SpawnGrenadeLocally(this.coorscanPrefab, this, base.X + Mathf.Sign(base.transform.localScale.x) * 6f, base.Y + 10f, 0.001f, 0.011f, Mathf.Sign(base.transform.localScale.x) * 30f, 70f, base.playerNum, 0) as CoorsCan);
            }
            else
            {
                coorsCan = (ProjectileController.SpawnGrenadeLocally(this.coorscanPrefab, this, base.X + Mathf.Sign(base.transform.localScale.x) * 6f, base.Y + 10f, 0.001f, 0.011f, Mathf.Sign(base.transform.localScale.x) * 300f, 250f, base.playerNum, 0) as CoorsCan);
            }
            coorsCan.enabled = true;
        }
        
        protected override bool MustIgnoreHighFiveMeleePress()
        {
            return this.heldGrenade != null || this.heldMook != null || this.usingSpecial || this.attachedToZipline || this.jumpingMelee || base.actionState == (ActionState)3 || this.doingMelee;
        }
        
        protected override void PerformKnifeMeleeAttack(bool shouldTryHitTerrain, bool playMissSound)
        {
            bool flag;
            Map.DamageDoodads(3, (DamageType)14, base.X + (float)(base.Direction * 4), base.Y, 0f, 0f, 6f, base.playerNum, out flag, null);
            base.KickDoors(24f);
            AudioClip audioClip;
            if (this.gunSprite.meshRender.material == this.normalGunMaterial)
            {
                audioClip = Cobro.DashingMeleeSounds[0];
            }
            else
            {
                audioClip = Cobro.DashingMeleeSounds[1];
            }
            if (Map.HitClosestUnit(this, base.playerNum, 4, (DamageType)14, 14f, 24f, base.X + base.transform.localScale.x * 8f, base.Y + 8f, base.transform.localScale.x * 200f, 500f, true, false, base.IsMine, false, true))
            {
                if (audioClip != null)
                {
                    this.sound.PlaySoundEffectAt(audioClip, 0.7f, base.transform.position, 1f, true, false, false, 0f);
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
                MapController.Damage_Networked(this, this.raycastHit.collider.gameObject, component.health, (DamageType)7, 0f, 40f, this.raycastHit.point.x, this.raycastHit.point.y);
                return true;
            }
            MapController.Damage_Networked(this, this.raycastHit.collider.gameObject, meleeDamage, (DamageType)7, 0f, 40f, this.raycastHit.point.x, this.raycastHit.point.y);
            if (this.currentMeleeType == null)
            {
                this.sound.PlaySoundEffectAt(this.soundHolder.alternateMeleeHitSound, 0.3f, base.transform.position, 1f, true, false, false, 0f);
            }
            else
            {
                this.sound.PlaySoundEffectAt(this.soundHolder.alternateMeleeHitSound, 0.3f, base.transform.position, 1f, true, false, false, 0f);
            }
            EffectsController.CreateProjectilePopWhiteEffect(base.X + this.width * base.transform.localScale.x, base.Y + this.height + 4f);
            return true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        public class ProjectileData
        {
          public int bulletCount;

          public int maxBulletCount = 6;
        }
    }
}