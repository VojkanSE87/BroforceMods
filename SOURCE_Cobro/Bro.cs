using BroMakerLib.CustomObjects.Bros;
using BroMakerLib;
using System.IO;
using System.Reflection;
using UnityEngine;
using System;

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

        private bool wasInvulnerable = false;
        private bool specialActive = false;
        private bool isUsingSpecial = false;
        private bool firstSpecialPress = true;
        private int usingSpecialFrame = 0;

        private bool specialModeDeactivated = false;
        private bool isReversingSpecial = false;

        private bool isAnimatingSpecial = false;  



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
            if (this.invulnerable)
            {
                this.wasInvulnerable = true;
            }
            base.Update();

            if (this.usingSpecial && !this.specialActive && CanUseSpecial())
            {
                AnimateSpecial();
            }

            if (this.wasInvulnerable && !this.invulnerable)
            {
                normalMaterial.SetColor("_TintColor", Color.gray);
                stealthMaterial.SetColor("_TintColor", Color.gray);
                gunSprite.meshRender.material.SetColor("_TintColor", Color.gray);
            }

            if (Input.GetButtonDown("Special"))
            {
                PressSpecial();
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

        protected override void PressSpecial() //rewrite this to be more brobase ili bro lee primer sa f (!this.hasBeenCoverInAcid && this.health > 0 && this.SpecialAmmo > 0 && !this.usingSpecial) linija 839
        {
            if (this.specialActive)  // Check if the special mode is active
            {
                if (CanUseSpecial())  
                {
                    SetupSpecialAttack();  
                    FireSpecialWeapon();
                }
                else  // Special mode is active but no special ammo
                {
                    HeroController.FlashSpecialAmmo(base.playerNum);
                    Sound.GetInstance().PlaySoundEffectAt(this.emptyGunSound, 1f, base.transform.position);
                    StartReverseSpecialAnimation();
                }
            }
            else  // Special mode isn't active
            {
                if (CanUseSpecial())  // If special mode can be activated
                {
                    ActivateSpecialMode();
                    this.sprite.GetComponent<Renderer>().material = this.stealthMaterial;  
                    this.gunSprite.meshRender.material = this.stealthGunMaterial; 
                    this.firstSpecialPress = false;
                }
                else  // No special ammo and special mode isn't active
                {
                    HeroController.FlashSpecialAmmo(base.playerNum);
                }
            }
        }


        private bool CanUseSpecial()
        {
            return !this.hasBeenCoverInAcid && !this.isUsingSpecial && this.health > 0 && this.SpecialAmmo > 0;
        }

        private void StartReverseSpecialAnimation()
        {

            this.isUsingSpecial = true;
            this.usingSpecialFrame = 5; 
            this.isReversingSpecial = true; 

            
            this.gunSprite.gameObject.SetActive(false);
            foreach (Renderer renderer in this.gunSprite.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = false;
            }

            // Set the main sprite to the normal material but keep it active
            this.sprite.GetComponent<Renderer>().material = this.normalMaterial; 
            this.gunSprite.meshRender.material = this.normalGunMaterial; 

            this.specialActive = false; 
            this.specialModeDeactivated = true; // flag to indicate special mode deactivation
        }

        protected override void AnimateSpecial()
        {
            
            this.SetSpriteOffset(0f, 0f);
            this.DeactivateGun();
            this.frameRate = 0.0909f;

            int frameIndex;
            if (this.isReversingSpecial)
            {
                
                frameIndex = 23 + Mathf.Clamp(this.usingSpecialFrame, 0, 9);
                this.sprite.SetLowerLeftPixel(frameIndex * this.spritePixelWidth, 9 * this.spritePixelHeight);
                --this.usingSpecialFrame;
            }
            else
            {
                
                frameIndex = 23 + Mathf.Clamp(this.usingSpecialFrame, 0, 9);
                this.sprite.SetLowerLeftPixel(frameIndex * this.spritePixelWidth, 9 * this.spritePixelHeight);
                ++this.usingSpecialFrame;
            }

            
            if (this.usingSpecialFrame == 0)
            {
                this.UseSpecial();
            }

            if (this.usingSpecialFrame >= 9)
            {
                this.usingSpecialFrame = 0;
                this.isUsingSpecial = false;

                this.ActivateGun();
                this.ChangeFrame();
            }
        }

        protected override void ChangeFrame()
        {
            if (this.isUsingSpecial)
            {
                AnimateSpecial();
            }
            else
            {
                base.ChangeFrame();
            }
        }
      

        private void ActivateSpecialMode()
        {
            base.material = this.stealthMaterial;
            this.gunSprite.meshRender.material = this.stealthGunMaterial;
            //mozda ovde da bude cela special logika nakon gotove animacije
        }

        private void UseSpecialAmmo()
        {
            int specialAmmo = this.SpecialAmmo;
            this.SpecialAmmo = specialAmmo - 1;
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
            float num5 = base.transform.localScale.x * 850f;
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
            float x = base.X + base.transform.localScale.x * 26f;
            float y = base.Y + 8.3f;
            float xI = base.transform.localScale.x * 750f;
            float yI = (float)UnityEngine.Random.Range(-10, 10);
            ProjectileController.SpawnProjectileLocally(this.specialProjectile, this, x, y, xI, yI, base.playerNum);
            UseSpecialAmmo(); 
        }

        protected override void UseFire()
        {
            if (this.specialActive)
            {
                if (this.specialModeDeactivated) 
                {
                    this.specialModeDeactivated = false; 
                    this.specialActive = false; 
                }
                this.ActivateGun();
                this.HandlePrimaryFire();
            }
            else
            {
                this.ActivateGun();
                this.HandlePrimaryFire();
            }
        }

        private void HandlePrimaryFire()
        {
            base.material = this.normalMaterial;
            this.gunSprite.meshRender.material = this.normalGunMaterial;
            this.FirePrimaryWeapon();
        }

        private void FirePrimaryWeapon()
        {

           // if (this.attachedToZipline == null)
            //{
            //    this.gunFrame = ? 6 : 3);
           // }
            if (this.isUsingSpecial)
            {
                // Disable primary fire while using the special ability
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
            float num6 = (float)UnityEngine.Random.Range(-20, 20); //navodno disperse
            this.gunFrame = 3;
            this.SetGunSprite(this.gunFrame, 0);                                                             //10f vise gore i dole u odnosu + ili -        //25f veci veriety sto je veci br to menjaj
            ProjectileController.SpawnProjectileLocally(this.primaryProjectile, this, base.X + num, base.Y + num2, num5, num6 - 10f + UnityEngine.Random.value * 35f, base.playerNum).life = this.primaryProjectileLifetime;
            EffectsController.CreateMuzzleFlashEffect(base.X + num3, base.Y + num4, -21f, num5 * 0.15f, num6 * 0.15f, base.transform);
            Sound.GetInstance().PlaySoundEffectAt(Cobro.MachineGunSounds, 0.45f, base.transform.position, 1f + this.pitchShiftAmount, true, false, false, 0f);
        }

        protected override void AnimateZipline() //nije zavrseno
        {
            base.AnimateZipline();
            if (this.isUsingSpecial)
            {
                Vector3 b = new Vector3(0f, 1f, 0f); //mozda ovo podesavati
                this.gunSprite.transform.position = base.transform.position + b;
                this.gunFrame = 3;
                this.SetGunSprite(this.gunFrame, 0);
            }
        }

        protected override void StartCustomMelee() //not finished
        {
            if (!this.doingMelee || base.frame > 4) //izmeniti value
            {
                base.frame = 0;
                base.counter = -0.05f; //izmeniti value
                this.AnimateMelee(); //ovo nismo jos definisali
            }
            else
            {
                this.meleeFollowUp = true;
            }
            this.StartMeleeCommon();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}