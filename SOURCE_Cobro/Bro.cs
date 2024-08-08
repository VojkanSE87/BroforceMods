using BroMakerLib.CustomObjects.Bros;
using BroMakerLib;
using System.IO;
using System.Reflection;
using UnityEngine;
using System;
using System.Collections;
using RocketLib.Collections;

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

        //special_variables
        private bool UsingSpecial = false;
        protected bool specialActive = false;
        private int usingSpecialFrame = 0;

        private float specialAnimationTimer = 0f;
        private bool isReversingSpecial = false; 

        private bool isDelayingPrimaryFire = false; 
        private float primaryFireDelayTimer = 0f;

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
            
            if (this.UsingSpecial || this.isReversingSpecial)
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
            
            if (this.specialActive && this.specialAmmo <= 0)
            {
                ReverseSpecialMode();
                this.specialActive = false; 
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
            return !this.hasBeenCoverInAcid && !this.UsingSpecial && this.health > 0 && this.SpecialAmmo > 0;
        }
        private void UseSpecialAmmo()
        {
            int specialAmmo = this.SpecialAmmo;
            this.SpecialAmmo = specialAmmo - 1;
        }

        protected override void UseFire()
        {
            
            if (!this.usingSpecial && !this.specialActive && !this.doingMelee)
            {
                this.FirePrimaryWeapon(); 
            }
            else
            {                
                if (this.usingSpecial || this.specialActive)
                {
                    ReverseSpecialMode();
                }

                if (this.doingMelee)
                {
                    this.CancelMelee();
                }
            }
        }

        protected override void PressSpecial()
        {
            if (!specialActive && CanUseSpecial())
            {
                this.UsingSpecial = true; 
                sprite.GetComponent<Renderer>().material = stealthMaterial;
                gunSprite.meshRender.material = stealthGunMaterial;
            }
            else if (specialActive)
            {               
                if (this.specialAmmo > 0)
                {
                    SetupSpecialAttack();
                    FireSpecialWeapon();
                }
                else if (this.SpecialAmmo <= 0)
                {                    
                    this.specialActive = false; // ensure specialActive is reset when ammo is depleted
                    HeroController.FlashSpecialAmmo(base.playerNum);
                    Sound.GetInstance().PlaySoundEffectAt(emptyGunSound, 1f, base.transform.position);
                    ReverseSpecialMode(); 
                    return;
                }
            }
        }

        protected override void AnimateSpecial() 
        {
            this.frameRate = 0.0334f;
            
            if (this.wallClimbing || this.wallDrag || this.slidingOnZipline && ((this.left && (this.attachedToZipline.Direction.x > 0f || this.attachedToZipline.IsHorizontalZipline)) || (this.right && (this.attachedToZipline.Direction.x < 0f || this.attachedToZipline.IsHorizontalZipline))))
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
            // fix arms being offset from body
            if (!this.specialActive)
            {
                // primary mode positions
                if (this.attachedToZipline != null)
                {
                    if (this.right && (this.attachedToZipline.Direction.x < 0f || this.attachedToZipline.IsHorizontalZipline)) // Going right on the zipline
                    {                                                    //nazad/napred   gore/dole  2f je otprilike 1 piksel
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
                // special mode positions
                if (this.attachedToZipline != null)
                {
                    if (this.right && (this.attachedToZipline.Direction.x < 0f || this.attachedToZipline.IsHorizontalZipline)) // Going right on the zipline
                    {
                        this.gunSprite.transform.localPosition = new Vector3(xOffset + 4f, yOffset + 1f, -1f); // Adjust X and Y for special weapon when moving right (up)
                    }
                    else if (this.left && (this.attachedToZipline.Direction.x > 0f || this.attachedToZipline.IsHorizontalZipline)) // Going left on the zipline
                    {
                        this.gunSprite.transform.localPosition = new Vector3(xOffset - 4f, yOffset + 1f, -1f); // Adjust X and Y for special weapon when moving left (up)
                    }
                }
                else
                {
                    this.gunSprite.transform.localPosition = new Vector3(xOffset, yOffset + 0.4f, -1f); // Default special position
                }
            }
        }



        protected override void FireFlashAvatar()
        {
            if (this.isReversingSpecial || this.isDelayingPrimaryFire)
            {                
                return; //an avatar flashing issue upon switching back from special
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
                float num6 = (float)UnityEngine.Random.Range(-10, 10);

                this.gunFrame = 3;
                this.SetGunSprite(this.gunFrame, 0);
                if (this.attachedToZipline != null || this.slidingOnZipline)
                {
                    {
                        num = base.transform.localScale.x * 12f;
                        num4 = 6.3f;

                    }
                }
                EffectsController.CreateMuzzleFlashMediumEffect(base.X + num3, base.Y + num4, -20f, num5 * 0.06f, num6 * 0.06f, base.transform);
                Sound.GetInstance().PlaySoundEffectAt(Cobro.CobroGunSounds, 0.9f, base.transform.position, 0.9f + this.pitchShiftAmount, true, false, false, 0f);
                Map.DisturbWildLife(base.X, base.Y, 60f, base.playerNum);
                SortOfFollow.Shake(0.4f, 0.4f);
                this.pressSpecialFacingDirection = (int)base.transform.localScale.x;
                this.yI += 10f;
                this.xIBlast = -base.transform.localScale.x * 15f;
            }
        }

        private void FireSpecialWeapon()
             {
            if (this.specialAmmo > 0)
            {
                EffectsController.CreateShrapnel(this.bulletShell, base.X + base.transform.localScale.x * 2f, base.Y + 13f, 2f, 30f, 6f, -base.transform.localScale.x * 50f, 90f);
                float x = base.X + base.transform.localScale.x * 26f;
                float y = base.Y + 8.3f;
                float xI = base.transform.localScale.x * 750f;
                float yI = (float)UnityEngine.Random.Range(-10, 10);
                ProjectileController.SpawnProjectileLocally(this.specialProjectile, this, x, y, xI, yI, base.playerNum);
                UseSpecialAmmo(); 
            }
            else if (this.specialAmmo <= 0)     
            {
                HeroController.FlashSpecialAmmo(base.playerNum);
                Sound.GetInstance().PlaySoundEffectAt(this.emptyGunSound, 1f, base.transform.position);
                ReverseSpecialMode(); 
                this.specialActive = false;
                return;
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
                return; // do not fire the primary weapon if special mode is active or reverse animation is in progress
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

        protected override void OnDestroy() //memory crash issue
        {
            base.OnDestroy();
        }
    }
}
