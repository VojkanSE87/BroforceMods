using System;
using System.Collections.Generic;
using BroMakerLib;
using BroMakerLib.CustomObjects.Bros;
using BroMakerLib.Loggers;
using System.IO;
using System.Reflection;
using UnityEngine;
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
        private float primaryAttackRange = 15f;  //20
        private float primaryAttackSpeed = 380f; //480
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
        public AudioSource specialAudioSource;

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

        private bool invulnerabilityMode_WasActive = false;   //210525    
        bool isFinishingSpecial = false;
        private int specialStartAmmo;

        private Coroutine spriteFlashCoroutine;

        private Color defaultTintColor;

        private bool soundPlayed = false;

        private int lastQuestionIndex = -1;
       
        BootKnife bootknifePrefab;
        
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

            string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            projectile = new GameObject("BootKnife", new Type[] { typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer), typeof(SpriteSM), typeof(BootKnife) }).GetComponent<BootKnife>();
            bootknifePrefab = projectile.GetComponent<BootKnife>();
            bootknifePrefab.enabled = false;

            defaultTintColor = normalMaterial.GetColor("_TintColor");
            sweatTimer = sweatInterval;

            questionPool = new List<int>(Questions.Length);
            RefillQuestionPool();
        }

        private void InitializeResources()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.normalMaterial = base.material;
            this.normalGunMaterial = this.gunSprite.meshRender.material;
            this.normalAvatarMaterial = ResourcesController.GetMaterial(directoryName, "avatar.png");
        }

        public override void AfterPrefabSetup()
        {
            base.AfterPrefabSetup();
            
            specialAudioSource = gameObject.AddComponent<AudioSource>();
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
            if (JackBroton.BootKnifeSounds == null) 
            {
                JackBroton.BootKnifeSounds = new AudioClip[2];
                JackBroton.BootKnifeSounds[0] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "BootKnife_1.wav");
                JackBroton.BootKnifeSounds[1] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "BootKnife_2.wav");
            }
            if (JackBroton.InvulSounds == null)
            {
                JackBroton.InvulSounds = new AudioClip[1];
                JackBroton.InvulSounds[0] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "Potion.wav");
            }
            if (JackBroton.DashingMeleeSounds == null)
            {
                JackBroton.DashingMeleeSounds = new AudioClip[2];
                JackBroton.DashingMeleeSounds[0] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "BrotonSmack.wav");
                JackBroton.DashingMeleeSounds[1] = ResourcesController.GetAudioClip(Path.Combine(directoryName, "sounds"), "BrotonSmack2.wav");
            }
            if (JackBroton.Questions == null)
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
            questionPool.Clear();
            for (int i = 0; i < Questions.Length; i++)
            {
                questionPool.Add(i);
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

            if (usingSpecial && !doingMelee)
            {
                int currentSpecialFrame = Mathf.FloorToInt(specialAnimationTimer / frameRate);

                if (this.fire && !hasThrownProjectile)
                {
                    CancelSpecial();
                    return;
                }

                if (this.fire && hasThrownProjectile && preDecrementAmmo == 1 && currentSpecialFrame < 6)
                {
                    this.invulnerabilityMode = false;
                    this.invulnerable = false;

                    CancelSpecial();
                    return;
                }

                if (this.health <= 0)
                {
                    ResetSpecialState();
                    return;
                }

                AnimateSpecial();
                return; //2305
            }

            if (this.skipNextMelee && !this.doingMelee)
            {
                this.skipNextMelee = false;
            }

            if (this.invulnerable)
            {
                this.wasInvulnerable = true;
            }
            else if (this.wasInvulnerable)
            {
                normalMaterial.SetColor("_TintColor", Color.gray);
                gunSprite.meshRender.material.SetColor("_TintColor", Color.gray);
                this.wasInvulnerable = false;
            }

            if (invulnerabilityMode)
            {
                sweatTimer -= t;
                if (sweatTimer <= 0f)
                {
                    CreateSweatParticle();
                    sweatTimer = sweatInterval;
                }

                invulnerabilityTime -= t;
                if (invulnerabilityTime <= 0f)
                    invulnerabilityMode = false;
            }
            else
            {
                sweatTimer = sweatInterval;
            }

            if (invulnerabilityMode_WasActive && !invulnerabilityMode)
            {
                normalMaterial.SetColor("_TintColor", defaultTintColor);
                gunSprite.meshRender.material.SetColor("_TintColor", defaultTintColor);

                OnInvulnerabilityEnd();
            }

            invulnerabilityMode_WasActive = invulnerabilityMode;
        }

        private void CancelSpecial()
        {
            invulnerabilityMode = false;
            this.invulnerable = false;
            usingSpecial = false;
            specialActive = false;
            hasThrownProjectile = false;
            specialAnimationTimer = 0f;

            if (SpecialAmmo < specialStartAmmo)
                SpecialAmmo = specialStartAmmo;
            //ActivateGun();
        }

        private void InitializeProjectiles()
        {
            this.primaryProjectile = (HeroController.GetHeroPrefab(HeroType.SnakeBroSkin) as SnakeBroskin).projectile;
            this.specialProjectile = (HeroController.GetHeroPrefab(HeroType.Brochete) as Brochete).projectile;
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
        private void UseSpecialAmmo()
        {
            if (this.SpecialAmmo > 0)
            {
                this.SpecialAmmo--;
            }
        }

        protected override void UseFire() //ceo metod zamenjen 210525
        {
            if (this.doingMelee)
            {
                this.CancelMelee();
                return;
            }

            if (this.usingSpecial || this.specialActive)
                return;

            if (this.attachedToZipline == null)
            {
                this.FirePrimaryWeapon();
                return;
            }

            float rangeX = base.transform.localScale.x * this.primaryAttackRange;
            float baseY = 8f;
            float speedX = base.transform.localScale.x * this.primaryAttackSpeed;
            float spread = UnityEngine.Random.Range(-50, 50);

            this.gunFrame = 3;
            this.SetGunSprite(this.gunFrame, 0);

            ProjectileController.SpawnProjectileLocally(
                this.primaryProjectile,
                this,
                base.X + rangeX,
                base.Y + baseY,
                speedX,
                spread - 10f + UnityEngine.Random.value * 60f,
                base.playerNum
            ).life = this.primaryProjectileLifetime;

            Map.DisturbWildLife(base.X, base.Y, 60f, base.playerNum);

            float flashX = base.X + (base.transform.localScale.x > 0f ? 11f : -11f);
            float flashY = base.Y + 8.5f;

            if (base.transform.localScale.x > 0f)
            {
                flashX += muzzleFlashPrimaryOffsetXOnZiplineRight;
                flashY += muzzleFlashPrimaryOffsetYOnZiplineRight;
            }
            else
            {
                flashX += muzzleFlashPrimaryOffsetXOnZiplineLeft;
                flashY += muzzleFlashPrimaryOffsetYOnZiplineLeft;
            }

            EffectsController.CreateMuzzleFlashEffect(
                flashX, flashY, -21f,
                speedX * 0.15f, spread * 0.15f,
                base.transform
            );
            Sound.GetInstance().PlaySoundEffectAt(
                JackBroton.Tec9GunSounds,
                0.70f,
                base.transform.position,
                1f + this.pitchShiftAmount,
                true, false, false, 0f
            );
        }

        protected override void PressSpecial()
        {
            if (this.usingSpecial || this.specialActive)
            {
                return;
            }

            if (SpecialAmmo <= 0)
            {
                HeroController.FlashSpecialAmmo(playerNum);
                return;
            }

            specialStartAmmo = SpecialAmmo;

            if (CanUseSpecial())
            {
                this.ResetSpecialState();
                
                preDecrementAmmo = SpecialAmmo;

                UseSpecialAmmo();

                usingSpecial = true;
                specialAnimationTimer = 0f;
                hasThrownProjectile = false;
                
                if (preDecrementAmmo > 1)
                {
                    this.PlaySpecialSound();
                }

                if (specialStartAmmo == 1)
                {
                    invulnerabilityMode = true;
                    invulnerabilityTime = 7.5f;
                }
            }
        }
        protected override void AnimateSpecial() 
        {

            this.frameRate = 0.0667f;

            if (this.wasRunning)
            {
                ResetSpecialState();
                return;
            }

            if (!usingSpecial)
            {
                return;
            }

            this.DeactivateGun();
            int startRow, startColumn, endFrame, actionFrame;
            int currentFrame;

            if (this.wallClimbing || this.wallDrag)
            {
               if (preDecrementAmmo > 1)
                {                   
                    startRow = 15;
                    startColumn = 4;
                    endFrame = 12;  
                    actionFrame = 9;
                }
                else
                {
                    startRow = 16;
                    startColumn = 17;
                    endFrame = 14;  
                    actionFrame = 8;
                }
                this.PlaySpecialSound();

                specialAnimationTimer += Time.deltaTime;
                currentFrame = Mathf.Clamp((int)(specialAnimationTimer / frameRate), 0, endFrame);
                sprite.SetLowerLeftPixel(
                    (startColumn + currentFrame) * spritePixelWidth,
                    startRow * spritePixelHeight
                );

                if (currentFrame == actionFrame && !hasThrownProjectile)
                {
                    if (preDecrementAmmo > 1)
                    {
                        ThrowProjectile();
                        this.avatarGunFireTime = 0.60f;
                        HeroController.SetAvatarAngry(this.playerNum, this.usePrimaryAvatar);
                    }
                    else
                    {
                        this.invulnerable = true;
                        base.invulnerableTime = 7.5f;
                        ColorShiftController.SlowTimeEffect(invulnerabilityTime);
                    }
                    hasThrownProjectile = true;
                }

                if (specialAnimationTimer >= this.frameRate * endFrame)
                {
                    ResetSpecialState();
                }

                return;
            }

            if (preDecrementAmmo == 1)
            {
                startRow = 9;
                startColumn = 17;
                endFrame = 14;
                actionFrame = 8;
            }
            else if (preDecrementAmmo > 1)
            {
                startRow = 10;
                startColumn = 18;
                endFrame = 13;
                actionFrame = 11;
            }
            else
            {
                ResetSpecialState();
                return;
            }

            currentFrame = Mathf.Clamp((int)(specialAnimationTimer / this.frameRate), 0, endFrame);
            this.sprite.SetLowerLeftPixel((startColumn + currentFrame) * this.spritePixelWidth, startRow * this.spritePixelHeight);

            if (preDecrementAmmo == 1
                && currentFrame == actionFrame )
            {
                this.PlaySpecialSound(true);
            }

            if (currentFrame >= actionFrame && !hasThrownProjectile)
            {
                if (preDecrementAmmo > 1)
                {
                    ThrowProjectile();
                    this.avatarGunFireTime = 0.60f;
                    HeroController.SetAvatarAngry(this.playerNum, this.usePrimaryAvatar);
                }
                else if (preDecrementAmmo == 1)
                {
                    this.invulnerable = true;
                    base.invulnerableTime = 7.5f;
                    ColorShiftController.SlowTimeEffect(invulnerabilityTime);
                }

                hasThrownProjectile = true;
                isFinishingSpecial = true;
            }

            if (this.health <= 0)
            {
                this.ResetSpecialState();
                this.usingSpecial = false;
                return;
            }

            specialAnimationTimer += Time.deltaTime;

            if (specialAnimationTimer >= this.frameRate * endFrame)
            {
                ResetSpecialState();
                hasThrownProjectile = false;
                isFinishingSpecial = false;
            }

            if (preDecrementAmmo > 1 && currentFrame > actionFrame)
            {
                this.invulnerable = false;
            }

        }

        public void PlaySpecialSound( bool invulSounds = false )
        {
            if (!specialSoundPlayed)
            {
                if (invulSounds)
                {
                    Sound.GetInstance().PlaySoundEffectAt(
                        JackBroton.InvulSounds[0],
                        0.45f,
                        transform.position,
                        1f + this.pitchShiftAmount,
                        true, false, false, 0f
                    );
                }
                else
                {
                    int idx = UnityEngine.Random.Range(0, BootKnifeSounds.Length);
                    specialAudioSource.clip = BootKnifeSounds[idx];
                    specialAudioSource.Stop();
                    specialAudioSource.time = 0f;
                    specialAudioSource.volume = 0.45f;
                    specialAudioSource.pitch = 1f + pitchShiftAmount;
                    specialAudioSource.bypassReverbZones = true;
                    specialAudioSource.rolloffMode = AudioRolloffMode.Linear;
                    specialAudioSource.minDistance = 550f;
                    specialAudioSource.maxDistance = 600f;
                    specialAudioSource.spatialBlend = 1f;
                    specialAudioSource.dopplerLevel = 0f;
                    specialAudioSource.Play();
                }
                specialSoundPlayed = true;
            }
        }
        public override void Damage(int damage, DamageType damageType, float xI, float yI, int direction, MonoBehaviour damageSender, float hitX, float hitY)
        {
            if (!this.invulnerabilityMode)
            {
                base.Damage(damage, damageType, xI, yI, direction, damageSender, hitX, hitY);
            }
            else
            {
                Helicopter helicopter = damageSender as Helicopter;
                if (helicopter)
                {
                    helicopter.Damage(new DamageObject(helicopter.health, DamageType.Explosion, 0f, 0f, base.X, base.Y, this));
                }
                SawBlade sawBlade = damageSender as SawBlade;
                if (sawBlade != null)
                {
                    sawBlade.Damage(new DamageObject(sawBlade.health, DamageType.Explosion, 0f, 0f, base.X, base.Y, this));
                }
                MookDog mookDog = damageSender as MookDog;
                if (mookDog != null)
                {
                    mookDog.Panic((int)Mathf.Sign(xI) * -1, 2f, true);
                }
                this.xIBlast += xI * 0.1f + (float)damage * 0.03f;
                this.yI += yI * 0.1f + (float)damage * 0.03f;
            }
        }
        private void ResetSpecialState()
        {
            specialAnimationTimer = 0f;
            base.frame = 0;
            usingSpecial = false;
            specialActive = false;
            this.hasThrownProjectile = false;
            specialSoundPlayed = false;
        }
        protected override void SetGunPosition(float xOffset, float yOffset)
        {
            if (!this.specialActive)
            {
                if (this.attachedToZipline != null)
                {
                    if (this.right && (this.attachedToZipline.Direction.x < 0f || this.attachedToZipline.IsHorizontalZipline))
                    {
                        this.gunSprite.transform.localPosition = new Vector3(xOffset + 2f, yOffset + 1f, -1f);
                    }
                    else if (this.left && (this.attachedToZipline.Direction.x > 0f || this.attachedToZipline.IsHorizontalZipline))
                    {
                        this.gunSprite.transform.localPosition = new Vector3(xOffset - 2f, yOffset + 1f, -1f);
                    }
                }
                else
                {
                    this.gunSprite.transform.localPosition = new Vector3(xOffset + 0f, yOffset, -1f);
                }
            }
        }
        private void OnInvulnerabilityEnd() //210525
        {
            HeroController.FlashAvatar(this.playerNum, 0.5f, this.usePrimaryAvatar);

            if (spriteFlashCoroutine != null)
                StopCoroutine(spriteFlashCoroutine);
            spriteFlashCoroutine = StartCoroutine(FlashCharacterSpriteRoutine(0.5f, 0.1f));
        }

        private IEnumerator FlashCharacterSpriteRoutine(float totalDuration, float interval)
        {
            Color playerColor = HeroController.GetHeroColor(this.playerNum);
            float endTime = Time.time + totalDuration;
            bool usePlayerClr = true;

            while (Time.time < endTime)
            {
                Color tint = usePlayerClr ? playerColor : defaultTintColor;
                normalMaterial.SetColor("_TintColor", tint);
                gunSprite.meshRender.material.SetColor("_TintColor", tint);
                sprite.meshRender.material.SetColor("_TintColor", tint);

                usePlayerClr = !usePlayerClr;
                yield return new WaitForSeconds(interval);
            }

            normalMaterial.SetColor("_TintColor", defaultTintColor);
            gunSprite.meshRender.material.SetColor("_TintColor", defaultTintColor);
            sprite.meshRender.material.SetColor("_TintColor", defaultTintColor);

            spriteFlashCoroutine = null;
        }
        private IEnumerator FlashAvatarRoutine(float totalDuration, float interval)
        {
            float endTime = Time.time + totalDuration;
            while (Time.time < endTime)
            {
                HeroController.FlashAvatar(this.playerNum, interval * 2f, this.usePrimaryAvatar);
                yield return new WaitForSeconds(interval);
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

        private void ThrowProjectile()
        {
            BootKnife BootKnife;

            float x = this.transform.position.x;
            float y = this.transform.position.y;
            float xSpeed = 15f * this.transform.localScale.x;
            float ySpeed = 0f;

            this.TriggerBroFireEvent();

            BootKnife = ProjectileController.SpawnProjectileLocally(this.projectile, this, x, y, xSpeed, ySpeed, base.playerNum) as BootKnife;
            BootKnife.Setup();


        }
        private void FirePrimaryWeapon()
        {
            if (usingSpecial || specialActive || isDelayingPrimaryFire)
                return;

            float rangeX = transform.localScale.x * primaryAttackRange;
            float baseY = 8f;
            float flashX, flashY;
            if (transform.localScale.x > 0f)
            {
                flashX = 11f;
                flashY = 8.5f;
            }
            else
            {
                flashX = -11f;
                flashY = 8.5f;
            }

            float speedX = transform.localScale.x * primaryAttackSpeed;

            float spreadAngle = invulnerabilityMode ? 130f : 50f;
            float rand = UnityEngine.Random.Range(-spreadAngle, spreadAngle);

            gunFrame = 3;
            SetGunSprite(gunFrame, 0);

            var p = ProjectileController.SpawnProjectileLocally(primaryProjectile, this, X + rangeX, Y + baseY, speedX, rand - 10f + UnityEngine.Random.value * 60f, playerNum);
            p.life = primaryProjectileLifetime;

            EffectsController.CreateMuzzleFlashEffect(X + flashX, Y + flashY, -21f, speedX * 0.15f, rand * 0.15f, transform);

            Sound.GetInstance().PlaySoundEffectAt(Tec9GunSounds, 0.60f, transform.position, 1f + pitchShiftAmount, true, false, false, 0f);
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

            if (!this.right && !this.left && !this.attachedToZipline
                && !this.wallClimbing && !this.wallDrag && !this.jumpingMelee
                && this.nearbyMook != null && this.nearbyMook.CanBeThrown())
            {                
                base.frame = 0;
                base.counter = 0f;
                this.CancelMelee();
                ThrowBackMook(this.nearbyMook);
                this.nearbyMook = null;
                return;
            }                     

            if (!this.attachedToZipline && this.CanStartNewMelee())
            {
                base.frame = 0;
                base.counter -= 0.0667f;
                this.AnimateMelee();
            }
            else if (this.CanStartMeleeFollowUp())
            {
                this.meleeFollowUp = true;
            }

            xI = yI = 0f;
            this.StartMeleeCommon();
        }

        protected override void StartMeleeCommon()
        {

            if (!this.meleeFollowUp && this.CanStartNewMelee())
            {
                base.frame = 0;
                base.counter -= 0.0667f;
            }
            else
            {
                return;
            }

            this.ResetMeleeValues();
            this.lerpToMeleeTargetPos = 0f;
            this.doingMelee = true;
            this.showHighFiveAfterMeleeTimer = 0f;
            this.SetMeleeType();
            this.DeactivateGun();
            this.meleeStartPos = base.transform.position;
            this.AnimateMelee();
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
        protected override void SetMeleeType()
        {
            if (!this.useNewKnifingFrames)
            {
                this.standingMelee = true;
                this.jumpingMelee = false;
                this.dashingMelee = false;
                this.wallMelee = false;
            }
            else if (base.actionState == ActionState.Jumping || base.Y > this.groundHeight + 1f)
            {
                this.standingMelee = false;
                this.jumpingMelee = true;
                this.dashingMelee = false;
                this.wallMelee = false;
            }
            else if (this.wallClimbing || this.wallDrag)
            {
                this.standingMelee = false;
                this.jumpingMelee = false;
                this.dashingMelee = false;
                this.wallMelee = true;
            }
            else if (this.right || this.left)
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

        protected override void AnimateMelee()
        {
            this.SetMeleeType();

            if (base.actionState == ActionState.Jumping
        || base.Y > this.groundHeight + 1f)
            {
                if (!soundPlayed)
                {
                    if (questionPool.Count == 0)
                        RefillQuestionPool();

                    int poolIdx = UnityEngine.Random.Range(0, questionPool.Count);
                    int chosen = questionPool[poolIdx];
                    questionPool.RemoveAt(poolIdx);

                    AudioClip clip = Questions[chosen];
                    if (clip != null)
                    {
                        var src = Sound.GetInstance().PlaySoundEffectAt(
                            clip,
                            0.25f,
                            transform.position,
                            1f + pitchShiftAmount,
                            true, false, false, 0f
                        );
                        src.rolloffMode = AudioRolloffMode.Linear;
                        src.minDistance = 550f;
                        src.maxDistance = 600f;
                        src.spatialBlend = 1f;
                        src.dopplerLevel = 0f;
                    }

                    soundPlayed = true;
                }
                                
                base.frame = 0;
                CancelMelee();
                return;
            }

            if (!dashingMelee && standingMelee && base.frame == 2 && nearbyMook != null && nearbyMook.CanBeThrown())
            {
                ThrowBackMook(nearbyMook);
                nearbyMook = null;

                base.frame = 0;
                base.counter = 0f;
                CancelMelee();
                return;
            }

            xI = yI = 0f;
            frameRate = dashingMelee ? 0.0769f : 0.0667f;
            base.counter += Time.deltaTime;
            if (base.counter >= frameRate)
            {
                base.frame++;
                base.counter = 0f;
            }

            if (dashingMelee)
            {
                if (base.frame == 3 && !meleeHasHit)
                {
                    PerformKnifeMeleeAttack(true, true);
                    meleeHasHit = true;
                }

                int row = 6, colStart = 17, maxFrame = 7;
                int f = Mathf.Clamp(base.frame, 0, maxFrame);
                sprite.SetLowerLeftPixel((colStart + f) * spritePixelWidth,
                                         row * spritePixelHeight);

                avatarGunFireTime = 0.20f;
                HeroController.SetAvatarAngry(playerNum, usePrimaryAvatar);

                if (base.frame >= maxFrame)
                {
                    base.frame = 0;
                    CancelMelee();
                }
            }
            else if (this.wallClimbing || this.wallDrag)
            {
                int row = 14, colStart = 7, maxFrame = 8; 
                int f = Mathf.Clamp(base.frame, 0, maxFrame);
                sprite.SetLowerLeftPixel((colStart + f) * spritePixelWidth,
                                         row * spritePixelHeight);

                // Trigger voice line at frame 3
                if (base.frame == 3 && !soundPlayed)
                {
                    if (questionPool.Count == 0)
                    {
                        RefillQuestionPool();
                    }
                    int randomIndexInPool = UnityEngine.Random.Range(0, questionPool.Count);
                    int chosenSoundIndex = questionPool[randomIndexInPool];
                    questionPool.RemoveAt(randomIndexInPool);

                    AudioClip clip = Questions[chosenSoundIndex];
                    if (clip != null)
                    {
                        var src = Sound.GetInstance().PlaySoundEffectAt(
                            clip,
                            0.25f,
                            transform.position,
                            1f + pitchShiftAmount,
                            true, false, false, 0f
                        );

                        src.rolloffMode = AudioRolloffMode.Linear;
                        src.minDistance = 550f;
                        src.maxDistance = 600f;
                        src.spatialBlend = 1f;
                        src.dopplerLevel = 0f;
                    }

                    soundPlayed = true;
                }

                if (base.frame >= maxFrame)
                {
                    base.frame = 0;
                    CancelMelee();
                }
            }
            else
            {
                int row = 1, colStart = 23, maxFrame = 8;
                int f = Mathf.Clamp(base.frame, 0, maxFrame);
                sprite.SetLowerLeftPixel((colStart + f) * spritePixelWidth,
                                         row * spritePixelHeight);

                if (!dashingMelee && standingMelee && base.frame == 3 && !soundPlayed)
                {
                    if (questionPool.Count == 0)
                    {
                        RefillQuestionPool();
                    }
                    int randomIndexInPool = UnityEngine.Random.Range(0, questionPool.Count);
                    int chosenSoundIndex = questionPool[randomIndexInPool];

                    questionPool.RemoveAt(randomIndexInPool);

                    AudioClip clip = Questions[chosenSoundIndex];
                    if (clip != null)
                    {
                        var src = Sound.GetInstance().PlaySoundEffectAt(
                            clip,
                            0.25f,
                            transform.position,
                            1f + pitchShiftAmount,
                            true, false, false, 0f       // bypassReverb, bypassEffects, neverPool, delay
                        );

                        src.rolloffMode = AudioRolloffMode.Linear;
                        src.minDistance = 550f;
                        src.maxDistance = 600f;
                        src.spatialBlend = 1f;
                        src.dopplerLevel = 0f;
                    }                                       
                    soundPlayed = true;
                }

                if (base.frame >= maxFrame)
                {
                    base.frame = 0;
                    CancelMelee();
                }
            }
        }
        protected override void CancelMelee()
        {
            base.CancelMelee();
            soundPlayed = false;
            meleeHasHit = false;
            this.wallMelee = false;
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