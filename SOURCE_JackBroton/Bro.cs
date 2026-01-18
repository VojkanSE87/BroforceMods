using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BroMakerLib;
using BroMakerLib.CustomObjects.Bros;
using UnityEngine;

namespace JackBroton
{   
    [HeroPreset("Jack Broton", 0)]

    public class JackBroton : CustomHero

    {   
        private Projectile[] projectiles;

        private Projectile primaryProjectile;

        private Projectile specialProjectile;

        private JackBroton.ProjectileData specialProjectileData;

        private Material normalMaterial;

        private Material normalGunMaterial;

        private Material normalAvatarMaterial;

        private float primaryAttackRange = 15f;

        private float primaryAttackSpeed = 380f;

        private float primaryProjectileLifetime = 0.12f;

        public static AudioClip[] Tec9GunSounds;

        public static AudioClip[] BootKnifeSounds;

        public static AudioClip[] DashingMeleeSounds;

        public static AudioClip[] BrotonSmack;

        public static AudioClip[] Questions;

        public static AudioClip[] InvulSounds;

        private AudioClip emptyGunSound;

        private int specialAmmo = 3;

        public bool hitDeadUnits = true;

        private bool wasInvulnerable = false;

        private bool hasThrownProjectile = false;

        private bool UsingSpecial = false;

        private bool isUsingSecondSpecial;

        protected bool specialActive = false;

        private int usingSpecialFrame = 0;

        private bool specialSoundPlayed = false;

        private float specialAnimationTimer = 0f;

        private bool isReversingSpecial = false;

        private bool isDelayingPrimaryFire = false;

        private float primaryFireDelayTimer = 0f;

        public float muzzleFlashOffsetXOnZiplineLeft = 8f;

        public float muzzleFlashOffsetYOnZiplineLeft = 2.5f;

        public float muzzleFlashOffsetXOnZiplineRight = -9f;

        public float muzzleFlashOffsetYOnZiplineRight = 2f;

        public float muzzleFlashPrimaryOffsetXOnZiplineLeft = 9.5f;

        public float muzzleFlashPrimaryOffsetYOnZiplineLeft = 1.5f;

        public float muzzleFlashPrimaryOffsetXOnZiplineRight = -8f;

        public float muzzleFlashPrimaryOffsetYOnZiplineRight = 1f;

        private bool wasRunning;

        private bool skipNextMelee = false;

        private bool shouldThrowMook;

        private Mook mookToThrow;

        private int preDecrementAmmo;

        private bool invulnerabilityMode;

        private float invulnerabilityTime;

        private bool isInvulnerable;

        private float sweatInterval = 0.1f;

        private float sweatTimer;

        private bool invulnerabilityMode_WasActive = false;

        private bool isFinishingSpecial = false;

        private int specialStartAmmo;

        private Coroutine spriteFlashCoroutine;

        private Color defaultTintColor;

        private bool hasPlayedInvulSound;

        private bool soundPlayed = false;

        private int lastQuestionIndex = -1;

        private BootKnife bootknifePrefab;

        private List<int> questionPool;

        protected bool wallMelee;

        private bool _invulCoroutineStarted;


    protected override void Awake()
        {
            base.Awake();
            this.InitializeResources();
            this.InitializeProjectiles();
            this.InitializeAudioClips();
            this.gunSpriteHangingFrame = 9;
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.projectile = new GameObject("BootKnife", new Type[]
            {
                typeof(Transform),
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(SpriteSM),
                typeof(BootKnife)
            }).GetComponent<BootKnife>();
            this.bootknifePrefab = this.projectile.GetComponent<BootKnife>();
            this.bootknifePrefab.enabled = false;
            this.defaultTintColor = this.normalMaterial.GetColor("_TintColor");
            this.sweatTimer = this.sweatInterval;
            this.questionPool = new List<int>(JackBroton.Questions.Length);
            this.RefillQuestionPool();
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
            foreach (string path in spriteNames)
            {
                string path2 = Path.Combine(directoryPath, path);
                bool flag = File.Exists(path2);
                if (flag)
                {
                    string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    ResourcesController.GetMaterial(directoryName, "BootKnife.png");
                }
            }
        }

        private void InitializeAudioClips()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            bool flag = JackBroton.Tec9GunSounds == null;
            if (flag)
            {
                JackBroton.Tec9GunSounds = new AudioClip[3];
                JackBroton.Tec9GunSounds[0] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "Tec9_1.wav");
                JackBroton.Tec9GunSounds[1] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "Tec9_2.wav");
                JackBroton.Tec9GunSounds[2] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "Tec9_3.wav");
            }
            bool flag2 = JackBroton.BootKnifeSounds == null;
            if (flag2)
            {
                JackBroton.BootKnifeSounds = new AudioClip[2];
                JackBroton.BootKnifeSounds[0] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "BootKnife_1.wav");
                JackBroton.BootKnifeSounds[1] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "BootKnife_2.wav");
            }
            bool flag3 = JackBroton.InvulSounds == null;
            if (flag3)
            {
                JackBroton.InvulSounds = new AudioClip[1];
                JackBroton.InvulSounds[0] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "Potion.wav");
            }
            bool flag4 = JackBroton.DashingMeleeSounds == null;
            if (flag4)
            {
                JackBroton.DashingMeleeSounds = new AudioClip[2];
                JackBroton.DashingMeleeSounds[0] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "BrotonSmack.wav");
                JackBroton.DashingMeleeSounds[1] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "BrotonSmack2.wav");
            }
            bool flag5 = JackBroton.Questions == null;
            if (flag5)
            {
                JackBroton.Questions = new AudioClip[7];
                JackBroton.Questions[0] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "question1.wav");
                JackBroton.Questions[1] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "question2.wav");
                JackBroton.Questions[2] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "question3.wav");
                JackBroton.Questions[3] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "question4.wav");
                JackBroton.Questions[4] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "question5.wav");
                JackBroton.Questions[5] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "question6.wav");
                JackBroton.Questions[6] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "question7.wav");
            }
        }

        private void RefillQuestionPool()
        {
            this.questionPool.Clear();
            for (int i = 0; i < JackBroton.Questions.Length; i++)
            {
                this.questionPool.Add(i);
            }
        }

        public override bool invulnerable
        {
            get
            {
                return base.invulnerable;
            }
            set
            {
                base.invulnerable = value;
            }
        }
       
            protected override void Update()
        {
            base.Update();
            bool flag = this.usingSpecial && !this.doingMelee;
            if (flag)
            {
                int num = Mathf.FloorToInt(this.specialAnimationTimer / this.frameRate);
                bool flag2 = this.fire && !this.hasThrownProjectile;
                if (flag2)
                {
                    this.CancelSpecial();
                }
                else
                {
                    bool flag3 = this.fire && this.hasThrownProjectile && this.preDecrementAmmo == 1 && num < 6;
                    if (flag3)
                    {
                        this.invulnerabilityMode = false;
                        this.invulnerable = false;
                        this.CancelSpecial();
                    }
                    else
                    {
                        bool flag4 = this.health <= 0;
                        if (flag4)
                        {
                            this.ResetSpecialState();
                        }
                        else
                        {
                            this.AnimateSpecial();
                        }
                    }
                }
            }
            else
            {
                bool flag5 = this.skipNextMelee && !this.doingMelee;
                if (flag5)
                {
                    this.skipNextMelee = false;
                }
                bool invulnerable = this.invulnerable;
                if (invulnerable)
                {
                    this.wasInvulnerable = true;
                }
                else
                {
                    bool flag6 = this.wasInvulnerable;
                    if (flag6)
                    {
                        this.normalMaterial.SetColor("_TintColor", Color.gray);
                        this.gunSprite.meshRender.material.SetColor("_TintColor", Color.gray);
                        this.wasInvulnerable = false;
                    }
                }
                bool flag7 = this.invulnerabilityMode;
                if (flag7)
                {
                    this.sweatTimer -= this.t;
                    bool flag8 = this.sweatTimer <= 0f;
                    if (flag8)
                    {
                        base.CreateSweatParticle();
                        this.sweatTimer = this.sweatInterval;
                    }
                    this.invulnerabilityTime -= this.t;
                    bool flag9 = this.invulnerabilityTime <= 0f;
                    if (flag9)
                    {
                        this.invulnerabilityMode = false;
                    }
                }
                else
                {
                    this.sweatTimer = this.sweatInterval;
                }
                bool flag10 = this.invulnerabilityMode_WasActive && !this.invulnerabilityMode;
                if (flag10)
                {
                    this.normalMaterial.SetColor("_TintColor", this.defaultTintColor);
                    this.gunSprite.meshRender.material.SetColor("_TintColor", this.defaultTintColor);
                    this.OnInvulnerabilityEnd();
                }
                this.invulnerabilityMode_WasActive = this.invulnerabilityMode;
            }
        }

        private void CancelSpecial()
        {
            this.invulnerabilityMode = false;
            this.invulnerable = false;
            this.usingSpecial = false;
            this.specialActive = false;
            this.hasThrownProjectile = false;
            this.specialAnimationTimer = 0f;
            bool flag = this.SpecialAmmo < this.specialStartAmmo;
            if (flag)
            {
                this.SpecialAmmo = this.specialStartAmmo;
            }
        }

        private void InitializeProjectiles()
        {
            this.primaryProjectile = (HeroController.GetHeroPrefab(11) as SnakeBroskin).projectile;
            this.specialProjectile = (HeroController.GetHeroPrefab(17) as Brochete).projectile;
            this.specialProjectileData = new JackBroton.ProjectileData
            {
                bulletCount = 0,
                maxBulletCount = 3
            };
        }

        // Token: 0x0600000B RID: 11 RVA: 0x000027C8 File Offset: 0x000009C8
        private bool CanUseSpecial()
        {
            return !this.hasBeenCoverInAcid && !this.UsingSpecial && this.health > 0 && this.SpecialAmmo > 0;
        }

        // Token: 0x0600000C RID: 12 RVA: 0x00002800 File Offset: 0x00000A00
        private void UseSpecialAmmo()
        {
            bool flag = this.SpecialAmmo > 0;
            if (flag)
            {
                int num = this.SpecialAmmo;
                this.SpecialAmmo = num - 1;
            }
        }

        protected override void UseFire()
        {
            bool doingMelee = this.doingMelee;
            if (doingMelee)
            {
                this.CancelMelee();
            }
            else
            {
                bool flag = this.usingSpecial || this.specialActive;
                if (!flag)
                {
                    bool flag2 = this.attachedToZipline == null;
                    if (flag2)
                    {
                        this.FirePrimaryWeapon();
                    }
                    else
                    {
                        float num = base.transform.localScale.x * this.primaryAttackRange;
                        float num2 = 8f;
                        float num3 = base.transform.localScale.x * this.primaryAttackSpeed;
                        float num4 = (float)Random.Range(-50, 50);
                        this.gunFrame = 3;
                        this.SetGunSprite(this.gunFrame, 0);
                        ProjectileController.SpawnProjectileLocally(this.primaryProjectile, this, base.X + num, base.Y + num2, num3, num4 - 10f + Random.value * 60f, base.playerNum).life = this.primaryProjectileLifetime;
                        Map.DisturbWildLife(base.X, base.Y, 60f, base.playerNum);
                        float num5 = base.X + ((base.transform.localScale.x > 0f) ? 11f : -11f);
                        float num6 = base.Y + 8.5f;
                        bool flag3 = base.transform.localScale.x > 0f;
                        if (flag3)
                        {
                            num5 += this.muzzleFlashPrimaryOffsetXOnZiplineRight;
                            num6 += this.muzzleFlashPrimaryOffsetYOnZiplineRight;
                        }
                        else
                        {
                            num5 += this.muzzleFlashPrimaryOffsetXOnZiplineLeft;
                            num6 += this.muzzleFlashPrimaryOffsetYOnZiplineLeft;
                        }
                        EffectsController.CreateMuzzleFlashEffect(num5, num6, -21f, num3 * 0.15f, num4 * 0.15f, base.transform);
                        Sound.GetInstance().PlaySoundEffectAt(JackBroton.Tec9GunSounds, 0.7f, base.transform.position, 1f + this.pitchShiftAmount, true, false, false, 0f);
                    }
                }
            }
        }

        protected override void PressSpecial()
        {
            bool flag = this.usingSpecial || this.specialActive;
            if (!flag)
            {
                bool flag2 = this.SpecialAmmo <= 0;
                if (flag2)
                {
                    HeroController.FlashSpecialAmmo(base.playerNum);
                }
                else
                {
                    this.specialStartAmmo = this.SpecialAmmo;
                    this.hasPlayedInvulSound = false;
                    bool flag3 = this.CanUseSpecial();
                    if (flag3)
                    {
                        this.preDecrementAmmo = this.SpecialAmmo;
                        this.UseSpecialAmmo();
                        this.usingSpecial = true;
                        this.specialAnimationTimer = 0f;
                        this.hasThrownProjectile = false;
                        bool flag4 = this.preDecrementAmmo > 1;
                        if (flag4)
                        {
                            int num = Random.Range(0, JackBroton.BootKnifeSounds.Length);
                            AudioSource audioSource = Sound.GetInstance().PlaySoundEffectAt(JackBroton.BootKnifeSounds[num], 0.45f, base.transform.position, 1f + this.pitchShiftAmount, true, false, false, 0f);
                            audioSource.rolloffMode = 1;
                            audioSource.minDistance = 550f;
                            audioSource.maxDistance = 600f;
                            audioSource.spatialBlend = 1f;
                            audioSource.dopplerLevel = 0f;
                        }
                        bool flag5 = this.specialStartAmmo == 1;
                        if (flag5)
                        {
                            this.invulnerabilityMode = true;
                            this.invulnerabilityTime = 7.5f;
                        }
                    }
                }
            }
        }

        protected override void AnimateSpecial()
        {
            this.frameRate = 0.0667f;
            bool flag = this.wasRunning;
            if (flag)
            {
                this.ResetSpecialState();
            }
            else
            {
                bool flag2 = !this.usingSpecial;
                if (!flag2)
                {
                    this.DeactivateGun();
                    bool flag3 = this.wallClimbing || this.wallDrag;
                    if (flag3)
                    {
                        bool flag4 = this.preDecrementAmmo > 1;
                        int num;
                        int num2;
                        int num3;
                        int num4;
                        if (flag4)
                        {
                            num = 15;
                            num2 = 4;
                            num3 = 12;
                            num4 = 9;
                            bool flag5 = !this.specialSoundPlayed;
                            if (flag5)
                            {
                                int num5 = Random.Range(0, JackBroton.BootKnifeSounds.Length);
                                Sound.GetInstance().PlaySoundEffectAt(JackBroton.BootKnifeSounds[num5], 0.45f, base.transform.position, 1f + this.pitchShiftAmount, true, false, false, 0f);
                                this.specialSoundPlayed = true;
                            }
                        }
                        else
                        {
                            num = 16;
                            num2 = 17;
                            num3 = 14;
                            num4 = 8;
                            bool flag6 = !this.specialSoundPlayed;
                            if (flag6)
                            {
                                Sound.GetInstance().PlaySoundEffectAt(JackBroton.InvulSounds[0], 0.45f, base.transform.position, 1f + this.pitchShiftAmount, true, false, false, 0f);
                                this.specialSoundPlayed = true;
                            }
                        }
                        this.specialAnimationTimer += Time.deltaTime;
                        int num6 = Mathf.Clamp((int)(this.specialAnimationTimer / this.frameRate), 0, num3);
                        this.sprite.SetLowerLeftPixel((float)((num2 + num6) * this.spritePixelWidth), (float)(num * this.spritePixelHeight));
                        bool flag7 = num6 == num4 && !this.hasThrownProjectile;
                        if (flag7)
                        {
                            bool flag8 = this.preDecrementAmmo > 1;
                            if (flag8)
                            {
                                this.ThrowProjectile();
                                this.avatarGunFireTime = 0.6f;
                                HeroController.SetAvatarAngry(base.playerNum, this.usePrimaryAvatar);
                            }
                            else
                            {
                                this.invulnerable = true;
                                base.invulnerableTime = 7.5f;
                                ColorShiftController.SlowTimeEffect(this.invulnerabilityTime);
                            }
                            this.hasThrownProjectile = true;
                        }
                        bool flag9 = this.specialAnimationTimer >= this.frameRate * (float)num3;
                        if (flag9)
                        {
                            this.ResetSpecialState();
                        }
                    }
                    else
                    {
                        bool flag10 = this.preDecrementAmmo == 1;
                        int num;
                        int num2;
                        int num3;
                        int num4;
                        if (flag10)
                        {
                            num = 9;
                            num2 = 17;
                            num3 = 14;
                            num4 = 8;
                        }
                        else
                        {
                            bool flag11 = this.preDecrementAmmo > 1;
                            if (!flag11)
                            {
                                this.ResetSpecialState();
                                return;
                            }
                            num = 10;
                            num2 = 18;
                            num3 = 13;
                            num4 = 11;
                        }
                        int num6 = Mathf.Clamp((int)(this.specialAnimationTimer / this.frameRate), 0, num3);
                        this.sprite.SetLowerLeftPixel((float)((num2 + num6) * this.spritePixelWidth), (float)(num * this.spritePixelHeight));
                        bool flag12 = this.preDecrementAmmo == 1 && num6 == num4 && !this.hasPlayedInvulSound;
                        if (flag12)
                        {
                            Sound.GetInstance().PlaySoundEffectAt(JackBroton.InvulSounds[0], 0.45f, base.transform.position, 1f + this.pitchShiftAmount, true, false, false, 0f);
                            this.hasPlayedInvulSound = true;
                        }
                        bool flag13 = num6 >= num4 && !this.hasThrownProjectile;
                        if (flag13)
                        {
                            bool flag14 = this.preDecrementAmmo > 1;
                            if (flag14)
                            {
                                this.ThrowProjectile();
                                this.avatarGunFireTime = 0.6f;
                                HeroController.SetAvatarAngry(base.playerNum, this.usePrimaryAvatar);
                            }
                            else
                            {
                                bool flag15 = this.preDecrementAmmo == 1;
                                if (flag15)
                                {
                                    this.invulnerable = true;
                                    base.invulnerableTime = 7.5f;
                                    ColorShiftController.SlowTimeEffect(this.invulnerabilityTime);
                                }
                            }
                            this.hasThrownProjectile = true;
                            this.isFinishingSpecial = true;
                        }
                        bool flag16 = this.health <= 0;
                        if (flag16)
                        {
                            this.ResetSpecialState();
                            this.usingSpecial = false;
                        }
                        else
                        {
                            this.specialAnimationTimer += Time.deltaTime;
                            bool flag17 = this.specialAnimationTimer >= this.frameRate * (float)num3;
                            if (flag17)
                            {
                                this.ResetSpecialState();
                                this.hasThrownProjectile = false;
                                this.isFinishingSpecial = false;
                            }
                            bool flag18 = this.preDecrementAmmo > 1 && num6 > num4;
                            if (flag18)
                            {
                                this.invulnerable = false;
                            }
                        }
                    }
                }
            }
        }

        public override void Damage(int damage, DamageType damageType, float xI, float yI, int direction, MonoBehaviour damageSender, float hitX, float hitY)
        {
            bool flag = !this.invulnerabilityMode;
            if (flag)
            {
                base.Damage(damage, damageType, xI, yI, direction, damageSender, hitX, hitY);
            }
            else
            {
                Helicopter helicopter = damageSender as Helicopter;
                bool flag2 = helicopter;
                if (flag2)
                {
                    helicopter.Damage(new DamageObject(helicopter.health, 2, 0f, 0f, base.X, base.Y, this));
                }
                SawBlade sawBlade = damageSender as SawBlade;
                bool flag3 = sawBlade != null;
                if (flag3)
                {
                    sawBlade.Damage(new DamageObject(sawBlade.health, 2, 0f, 0f, base.X, base.Y, this));
                }
                MookDog mookDog = damageSender as MookDog;
                bool flag4 = mookDog != null;
                if (flag4)
                {
                    mookDog.Panic((int)Mathf.Sign(xI) * -1, 2f, true);
                }
                this.xIBlast += xI * 0.1f + (float)damage * 0.03f;
                this.yI += yI * 0.1f + (float)damage * 0.03f;
            }
        }

        private void ResetSpecialState()
        {
            this.specialAnimationTimer = 0f;
            base.frame = 0;
            this.usingSpecial = false;
            this.specialActive = false;
            this.hasThrownProjectile = false;
            this.specialSoundPlayed = false;
            this.hasPlayedInvulSound = false;
        }

        protected override void SetGunPosition(float xOffset, float yOffset)
        {
            bool flag = !this.specialActive;
            if (flag)
            {
                bool flag2 = this.attachedToZipline != null;
                if (flag2)
                {
                    bool flag3 = this.right && (this.attachedToZipline.Direction.x < 0f || this.attachedToZipline.IsHorizontalZipline);
                    if (flag3)
                    {
                        this.gunSprite.transform.localPosition = new Vector3(xOffset + 2f, yOffset + 1f, -1f);
                    }
                    else
                    {
                        bool flag4 = this.left && (this.attachedToZipline.Direction.x > 0f || this.attachedToZipline.IsHorizontalZipline);
                        if (flag4)
                        {
                            this.gunSprite.transform.localPosition = new Vector3(xOffset - 2f, yOffset + 1f, -1f);
                        }
                    }
                }
                else
                {
                    this.gunSprite.transform.localPosition = new Vector3(xOffset + 0f, yOffset, -1f);
                }
            }
        }

        private void OnInvulnerabilityEnd()
        {
            HeroController.FlashAvatar(base.playerNum, 0.5f, this.usePrimaryAvatar);
            bool flag = this.spriteFlashCoroutine != null;
            if (flag)
            {
                base.StopCoroutine(this.spriteFlashCoroutine);
            }
            this.spriteFlashCoroutine = base.StartCoroutine(this.FlashCharacterSpriteRoutine(0.5f, 0.1f));
        }

        private IEnumerator FlashCharacterSpriteRoutine(float totalDuration, float interval)
        {
            Color playerColor = HeroController.GetHeroColor(base.playerNum);
            float endTime = Time.time + totalDuration;
            bool usePlayerClr = true;
            while (Time.time < endTime)
            {
                Color tint = usePlayerClr ? playerColor : this.defaultTintColor;
                this.normalMaterial.SetColor("_TintColor", tint);
                this.gunSprite.meshRender.material.SetColor("_TintColor", tint);
                this.sprite.meshRender.material.SetColor("_TintColor", tint);
                usePlayerClr = !usePlayerClr;
                yield return new WaitForSeconds(interval);
                tint = default(Color);
            }
            this.normalMaterial.SetColor("_TintColor", this.defaultTintColor);
            this.gunSprite.meshRender.material.SetColor("_TintColor", this.defaultTintColor);
            this.sprite.meshRender.material.SetColor("_TintColor", this.defaultTintColor);
            this.spriteFlashCoroutine = null;
            yield break;
        }

        private IEnumerator FlashAvatarRoutine(float totalDuration, float interval)
        {
            float endTime = Time.time + totalDuration;
            while (Time.time < endTime)
            {
                HeroController.FlashAvatar(base.playerNum, interval * 2f, this.usePrimaryAvatar);
                yield return new WaitForSeconds(interval);
            }
            yield break;
        }

        protected override void FireFlashAvatar()
        {
            bool flag = this.isReversingSpecial || this.isDelayingPrimaryFire;
            if (!flag)
            {
                base.FireFlashAvatar();
            }
        }

        private void ThrowProjectile()
        {
            float x = base.transform.position.x;
            float y = base.transform.position.y;
            float num = 15f * base.transform.localScale.x;
            float num2 = 0f;
            this.TriggerBroFireEvent();
            BootKnife bootKnife = ProjectileController.SpawnProjectileLocally(this.projectile, this, x, y, num, num2, base.playerNum) as BootKnife;
            bootKnife.Setup();
        }

        private void FirePrimaryWeapon()
        {
            bool flag = this.usingSpecial || this.specialActive || this.isDelayingPrimaryFire;
            if (!flag)
            {
                float num = base.transform.localScale.x * this.primaryAttackRange;
                float num2 = 8f;
                bool flag2 = base.transform.localScale.x > 0f;
                float num3;
                float num4;
                if (flag2)
                {
                    num3 = 11f;
                    num4 = 8.5f;
                }
                else
                {
                    num3 = -11f;
                    num4 = 8.5f;
                }
                float num5 = base.transform.localScale.x * this.primaryAttackSpeed;
                float num6 = this.invulnerabilityMode ? 130f : 50f;
                float num7 = Random.Range(-num6, num6);
                this.gunFrame = 3;
                this.SetGunSprite(this.gunFrame, 0);
                Projectile projectile = ProjectileController.SpawnProjectileLocally(this.primaryProjectile, this, base.X + num, base.Y + num2, num5, num7 - 10f + Random.value * 60f, base.playerNum);
                projectile.life = this.primaryProjectileLifetime;
                EffectsController.CreateMuzzleFlashEffect(base.X + num3, base.Y + num4, -21f, num5 * 0.15f, num7 * 0.15f, base.transform);
                Sound.GetInstance().PlaySoundEffectAt(JackBroton.Tec9GunSounds, 0.6f, base.transform.position, 1f + this.pitchShiftAmount, true, false, false, 0f);
            }
        }

        protected override void RunGun()
        {
            bool flag = !this.WallDrag && this.gunFrame > 0;
            if (flag)
            {
                this.gunCounter += this.t;
                bool flag2 = this.gunCounter > 0.0334f;
                if (flag2)
                {
                    this.gunCounter -= 0.0334f;
                    this.gunFrame--;
                    bool flag3 = this.gunFrame == 3;
                    if (flag3)
                    {
                        this.gunFrame = 0;
                    }
                    this.SetGunSprite(this.gunFrame, 0);
                }
            }
        }

        protected override void StartCustomMelee()
        {
            bool flag = !this.right && !this.left && !this.attachedToZipline && !this.wallClimbing && !this.wallDrag && !this.jumpingMelee && this.nearbyMook != null && this.nearbyMook.CanBeThrown();
            if (flag)
            {
                base.frame = 0;
                base.counter = 0f;
                this.CancelMelee();
                this.ThrowBackMook(this.nearbyMook);
                this.nearbyMook = null;
            }
            else
            {
                bool flag2 = !this.attachedToZipline && this.CanStartNewMelee();
                if (flag2)
                {
                    base.frame = 0;
                    base.counter -= 0.0667f;
                    this.AnimateMelee();
                }
                else
                {
                    bool flag3 = this.CanStartMeleeFollowUp();
                    if (flag3)
                    {
                        this.meleeFollowUp = true;
                    }
                }
                this.xI = (this.yI = 0f);
                this.StartMeleeCommon();
            }
        }

        protected override void StartMeleeCommon()
        {
            bool flag = !this.meleeFollowUp && this.CanStartNewMelee();
            if (flag)
            {
                base.frame = 0;
                base.counter -= 0.0667f;
                this.ResetMeleeValues();
                this.lerpToMeleeTargetPos = 0f;
                this.doingMelee = true;
                this.showHighFiveAfterMeleeTimer = 0f;
                this.SetMeleeType();
                this.DeactivateGun();
                this.meleeStartPos = base.transform.position;
                this.AnimateMelee();
            }
        }

        protected override void RunKnifeMeleeMovement()
        {
            bool flag = this.wallClimbing || this.wallDrag;
            if (!flag)
            {
                bool dashingMelee = this.dashingMelee;
                if (dashingMelee)
                {
                    bool flag2 = base.frame <= 1;
                    if (flag2)
                    {
                        this.xI = 0f;
                        this.yI = 0f;
                    }
                    else
                    {
                        bool flag3 = base.frame <= 3;
                        if (flag3)
                        {
                            bool flag4 = !this.isInQuicksand;
                            if (flag4)
                            {
                                this.xI = this.speed * 1f * base.transform.localScale.x;
                            }
                        }
                        else
                        {
                            this.ApplyFallingGravity();
                        }
                    }
                }
                else
                {
                    bool flag5 = this.xI != 0f || this.yI != 0f;
                    if (flag5)
                    {
                        this.CancelMelee();
                    }
                }
            }
        }

        protected override void SetMeleeType()
        {
            bool flag = !this.useNewKnifingFrames;
            if (flag)
            {
                this.standingMelee = true;
                this.jumpingMelee = false;
                this.dashingMelee = false;
                this.wallMelee = false;
            }
            else
            {
                bool flag2 = base.actionState == 3 || base.Y > this.groundHeight + 1f;
                if (flag2)
                {
                    this.standingMelee = false;
                    this.jumpingMelee = true;
                    this.dashingMelee = false;
                    this.wallMelee = false;
                }
                else
                {
                    bool flag3 = this.wallClimbing || this.wallDrag;
                    if (flag3)
                    {
                        this.standingMelee = false;
                        this.jumpingMelee = false;
                        this.dashingMelee = false;
                        this.wallMelee = true;
                    }
                    else
                    {
                        bool flag4 = this.right || this.left;
                        if (flag4)
                        {
                            this.standingMelee = false;
                            this.jumpingMelee = false;
                            this.dashingMelee = true;
                            this.wallMelee = false;
                        }
                        else
                        {
                            this.standingMelee = true;
                            this.jumpingMelee = false;
                            this.dashingMelee = false;
                            this.wallMelee = false;
                        }
                    }
                }
            }
        }

        protected override void AnimateMelee()
        {
            this.SetMeleeType();
            bool flag = base.actionState == 3 || base.Y > this.groundHeight + 1f;
            if (flag)
            {
                bool flag2 = !this.soundPlayed;
                if (flag2)
                {
                    bool flag3 = this.questionPool.Count == 0;
                    if (flag3)
                    {
                        this.RefillQuestionPool();
                    }
                    int index = Random.Range(0, this.questionPool.Count);
                    int num = this.questionPool[index];
                    this.questionPool.RemoveAt(index);
                    AudioClip audioClip = JackBroton.Questions[num];
                    bool flag4 = audioClip != null;
                    if (flag4)
                    {
                        AudioSource audioSource = Sound.GetInstance().PlaySoundEffectAt(audioClip, 0.25f, base.transform.position, 1f + this.pitchShiftAmount, true, false, false, 0f);
                        audioSource.rolloffMode = 1;
                        audioSource.minDistance = 550f;
                        audioSource.maxDistance = 600f;
                        audioSource.spatialBlend = 1f;
                        audioSource.dopplerLevel = 0f;
                    }
                    this.soundPlayed = true;
                }
                base.frame = 0;
                this.CancelMelee();
            }
            else
            {
                bool flag5 = !this.dashingMelee && this.standingMelee && base.frame == 2 && this.nearbyMook != null && this.nearbyMook.CanBeThrown();
                if (flag5)
                {
                    this.ThrowBackMook(this.nearbyMook);
                    this.nearbyMook = null;
                    base.frame = 0;
                    base.counter = 0f;
                    this.CancelMelee();
                }
                else
                {
                    this.xI = (this.yI = 0f);
                    this.frameRate = (this.dashingMelee ? 0.0769f : 0.0667f);
                    base.counter += Time.deltaTime;
                    bool flag6 = base.counter >= this.frameRate;
                    if (flag6)
                    {
                        int frame = base.frame;
                        base.frame = frame + 1;
                        base.counter = 0f;
                    }
                    bool dashingMelee = this.dashingMelee;
                    if (dashingMelee)
                    {
                        bool flag7 = base.frame == 3 && !this.meleeHasHit;
                        if (flag7)
                        {
                            this.PerformKnifeMeleeAttack(true, true);
                            this.meleeHasHit = true;
                        }
                        int num2 = 6;
                        int num3 = 17;
                        int num4 = 7;
                        int num5 = Mathf.Clamp(base.frame, 0, num4);
                        this.sprite.SetLowerLeftPixel((float)((num3 + num5) * this.spritePixelWidth), (float)(num2 * this.spritePixelHeight));
                        this.avatarGunFireTime = 0.2f;
                        HeroController.SetAvatarAngry(base.playerNum, this.usePrimaryAvatar);
                        bool flag8 = base.frame >= num4;
                        if (flag8)
                        {
                            base.frame = 0;
                            this.CancelMelee();
                        }
                    }
                    else
                    {
                        bool flag9 = this.wallClimbing || this.wallDrag;
                        if (flag9)
                        {
                            int num6 = 14;
                            int num7 = 7;
                            int num8 = 8;
                            int num9 = Mathf.Clamp(base.frame, 0, num8);
                            this.sprite.SetLowerLeftPixel((float)((num7 + num9) * this.spritePixelWidth), (float)(num6 * this.spritePixelHeight));
                            bool flag10 = base.frame == 3 && !this.soundPlayed;
                            if (flag10)
                            {
                                bool flag11 = this.questionPool.Count == 0;
                                if (flag11)
                                {
                                    this.RefillQuestionPool();
                                }
                                int index2 = Random.Range(0, this.questionPool.Count);
                                int num10 = this.questionPool[index2];
                                this.questionPool.RemoveAt(index2);
                                AudioClip audioClip2 = JackBroton.Questions[num10];
                                bool flag12 = audioClip2 != null;
                                if (flag12)
                                {
                                    AudioSource audioSource2 = Sound.GetInstance().PlaySoundEffectAt(audioClip2, 0.25f, base.transform.position, 1f + this.pitchShiftAmount, true, false, false, 0f);
                                    audioSource2.rolloffMode = 1;
                                    audioSource2.minDistance = 550f;
                                    audioSource2.maxDistance = 600f;
                                    audioSource2.spatialBlend = 1f;
                                    audioSource2.dopplerLevel = 0f;
                                }
                                this.soundPlayed = true;
                            }
                            bool flag13 = base.frame >= num8;
                            if (flag13)
                            {
                                base.frame = 0;
                                this.CancelMelee();
                            }
                        }
                        else
                        {
                            int num11 = 1;
                            int num12 = 23;
                            int num13 = 8;
                            int num14 = Mathf.Clamp(base.frame, 0, num13);
                            this.sprite.SetLowerLeftPixel((float)((num12 + num14) * this.spritePixelWidth), (float)(num11 * this.spritePixelHeight));
                            bool flag14 = !this.dashingMelee && this.standingMelee && base.frame == 3 && !this.soundPlayed;
                            if (flag14)
                            {
                                bool flag15 = this.questionPool.Count == 0;
                                if (flag15)
                                {
                                    this.RefillQuestionPool();
                                }
                                int index3 = Random.Range(0, this.questionPool.Count);
                                int num15 = this.questionPool[index3];
                                this.questionPool.RemoveAt(index3);
                                AudioClip audioClip3 = JackBroton.Questions[num15];
                                bool flag16 = audioClip3 != null;
                                if (flag16)
                                {
                                    AudioSource audioSource3 = Sound.GetInstance().PlaySoundEffectAt(audioClip3, 0.25f, base.transform.position, 1f + this.pitchShiftAmount, true, false, false, 0f);
                                    audioSource3.rolloffMode = 1;
                                    audioSource3.minDistance = 550f;
                                    audioSource3.maxDistance = 600f;
                                    audioSource3.spatialBlend = 1f;
                                    audioSource3.dopplerLevel = 0f;
                                }
                                this.soundPlayed = true;
                            }
                            bool flag17 = base.frame >= num13;
                            if (flag17)
                            {
                                base.frame = 0;
                                this.CancelMelee();
                            }
                        }
                    }
                }
            }
        }

        protected override void CancelMelee()
        {
            base.CancelMelee();
            this.soundPlayed = false;
            this.meleeHasHit = false;
            this.wallMelee = false;
        }

        // Token: 0x06000020 RID: 32 RVA: 0x00003EF4 File Offset: 0x000020F4
        protected override bool MustIgnoreHighFiveMeleePress()
        {
            return this.heldGrenade != null || this.heldMook != null || this.usingSpecial || this.attachedToZipline || this.doingMelee;
        }

        protected override void PerformKnifeMeleeAttack(bool shouldTryHitTerrain, bool playMissSound)
        {
            bool flag;
            Map.DamageDoodads(3, 14, base.X + (float)(base.Direction * 4), base.Y, 0f, 0f, 6f, base.playerNum, ref flag, null);
            base.KickDoors(24f);
            bool flag2 = this.gunSprite.meshRender.material == this.normalGunMaterial;
            AudioClip audioClip;
            if (flag2)
            {
                audioClip = JackBroton.DashingMeleeSounds[0];
            }
            else
            {
                audioClip = JackBroton.DashingMeleeSounds[1];
            }
            bool flag3 = Map.HitClosestUnit(this, base.playerNum, 4, 14, 14f, 24f, base.X + base.transform.localScale.x * 8f, base.Y + 8f, base.transform.localScale.x * 200f, 500f, true, false, base.IsMine, false, true);
            if (flag3)
            {
                bool flag4 = audioClip != null;
                if (flag4)
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
            bool flag5 = shouldTryHitTerrain && this.TryMeleeTerrain(0, 2);
            if (flag5)
            {
                this.meleeHasHit = true;
            }
        }

        protected override bool TryMeleeTerrain(int offset = 0, int meleeDamage = 2)
        {
            bool flag = Physics.Raycast(new Vector3(base.X - base.transform.localScale.x * 4f, base.Y + 4f, 0f), new Vector3(base.transform.localScale.x, 0f, 0f), ref this.raycastHit, (float)(16 + offset), this.groundLayer);
            bool result;
            if (flag)
            {
                Cage component = this.raycastHit.collider.GetComponent<Cage>();
                bool flag2 = component == null && this.raycastHit.collider.transform.parent != null;
                if (flag2)
                {
                    component = this.raycastHit.collider.transform.parent.GetComponent<Cage>();
                }
                bool flag3 = component != null;
                if (flag3)
                {
                    MapController.Damage_Networked(this, this.raycastHit.collider.gameObject, component.health, 7, 0f, 40f, this.raycastHit.point.x, this.raycastHit.point.y);
                    result = true;
                }
                else
                {
                    MapController.Damage_Networked(this, this.raycastHit.collider.gameObject, meleeDamage, 7, 0f, 40f, this.raycastHit.point.x, this.raycastHit.point.y);
                    bool flag4 = this.currentMeleeType == 0;
                    if (flag4)
                    {
                        this.sound.PlaySoundEffectAt(this.soundHolder.alternateMeleeHitSound, 0.3f, base.transform.position, 1f, true, false, false, 0f);
                    }
                    else
                    {
                        this.sound.PlaySoundEffectAt(this.soundHolder.alternateMeleeHitSound, 0.3f, base.transform.position, 1f, true, false, false, 0f);
                    }
                    EffectsController.CreateProjectilePopWhiteEffect(base.X + this.width * base.transform.localScale.x, base.Y + this.height + 4f);
                    result = true;
                }
            }
            else
            {
                result = false;
            }
            return result;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }  

        public class ProjectileData

        {
            public int bulletCount;

            public int maxBulletCount = 3;
        }
    }
}
