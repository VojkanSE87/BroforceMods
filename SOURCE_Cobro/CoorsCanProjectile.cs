﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using BroMakerLib;
using RocketLib.Collections;
using Rogueforce;
using UnityEngine;
using Utility;

namespace Cobro
{
    public class CoorsCan : Grenade
    {
        public static Material storedMat;
        private Rigidbody rigidbody;
        private MonoBehaviour damageSender;
        AudioClip explosionSound;                                      
        public Vector3 centerOfMass = new Vector3(0f, -0.3f, 0f);
        private static bool bounceX;
        private static bool bounceY;
        private bool hasCollided = false;
        private float soundThreshold = 0.5f; 
        private AudioClip[] canSounds;  
        public AudioClip[] canRollSounds; 
        private AudioSource audioSource;
        public float impactThreshold = 1f; 
                                    
        private float nextImpactSoundTime = 0f;
        private float impactSoundCooldown = 0.5f; 
        public float yMovementThreshold = 0.5f; 
        private float timeSinceLastSound = 0f;
        private Vector3 lastPosition; 
        private Rigidbody rb;
        public LayerMask collisionLayers;

        private float currentVolume = 1f; 
        private float volumeReductionStep = 0.3f; 
        private float minVolume = 0.2f;

        private float nextSoundTime = 0f; 
        private float soundCooldown = 0.5f; 

        protected override void Awake()
        {
            Renderer component = base.gameObject.GetComponent<MeshRenderer>();
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        

            if (CoorsCan.storedMat == null)
            {
                CoorsCan.storedMat = ResourcesController.GetMaterial(directoryName, "CoorsCan.png");
            }
            if (base.GetComponent<Collider>() == null)
            {
                base.gameObject.AddComponent<BoxCollider>();
            }

            this.canSounds = new AudioClip[] 
            {
                ResourcesController.CreateAudioClip(Path.Combine(directoryName, "sounds"), "can1.wav"),
                ResourcesController.CreateAudioClip(Path.Combine(directoryName, "sounds"), "can2.wav"),
                ResourcesController.CreateAudioClip(Path.Combine(directoryName, "sounds"), "can3.wav"),
                ResourcesController.CreateAudioClip(Path.Combine(directoryName, "sounds"), "can4.wav")
            };

           

            component.material = CoorsCan.storedMat;

            this.sprite = base.gameObject.GetComponent<SpriteSM>();
            this.sprite.lowerLeftPixel = new Vector2(0f, 16f);
            this.sprite.pixelDimensions = new Vector2(16f, 16f);
            this.sprite.plane = SpriteBase.SPRITE_PLANE.XY;
            this.sprite.width = 18f;
            this.sprite.height = 22f;

            this.explosionSound = ResourcesController.CreateAudioClip(Path.Combine(directoryName, "sounds"), "emptyCan.wav");
            base.Awake();

            this.audioSource = gameObject.AddComponent<AudioSource>(); 
            this.audioSource.playOnAwake = false;                      
            this.audioSource.loop = false;                             
            this.fragileLayer = 1 << LayerMask.NameToLayer("DirtyHippie");
            this.trailRenderer = null;            
            this.bounceM = 0.2f;
            this.disabledAtStart = false;
            this.shrink = false;
            this.trailType = TrailType.None;
            this.lifeLossOnBounce = false;
            this.deathOnBounce = false;
            this.destroyInsideWalls = false;
            this.rotateAtRightAngles = false;
            this.fades = false;
            this.fadeUVs = false;
            this.useAngularFriction = true;
            this.shrapnelControlsMotion = false;
            collisionLayers = LayerMask.GetMask("Ground", "DirtyHippie", "Switches"); 
        }
      
        protected override void Start()
        {
            this.mainMaterial = base.GetComponent<Renderer>().sharedMaterial;
            base.Start();
            this.groundLayer = Map.groundLayer;           
            bool hit = Physics.CheckSphere(transform.position, collisionLayers);

            this.RegisterGrenade();
            if (this.disabledAtStart)
            {
                base.SetXY(base.transform);
                this.life = (2f + this.random.value) * this.lifeM;
                this.r = 0f;
                base.enabled = false;
            }
            audioSource = GetComponent<AudioSource>();
            lastPosition = transform.position;
        }

        protected new virtual void RegisterGrenade()
        {
            if (this.shootable)
            {
                Map.RegisterShootableGrenade(this);
            }
            Map.RegisterGrenade(this);
        }

        public override void ThrowGrenade(float XI, float YI, float newX, float newY, int _playerNum)
        {
            base.enabled = true;
            base.transform.parent = null;
            this.SetXY(newX, newY);
            if (Mathf.Abs(XI) > 100f)
            {
                this.xI = Mathf.Sign(XI) * 300f;
                this.yI = 250f;
            }
            else
            {
                this.xI = XI;
                this.yI = YI;
            }
            this.playerNum = _playerNum;
            this.rigidbody.position = new Vector3(newX, newY);
            if (Mathf.Abs(this.xI) > 100f)
            {
                this.rI = -Mathf.Sign(this.xI) * (float)UnityEngine.Random.Range(20, 25);
            }
            else
            {
                this.rI = -Mathf.Sign(this.xI) * (float)UnityEngine.Random.Range(10, 15);
            }
            this.rigidbody.AddForce(new Vector3(this.xI, this.yI, 0f), ForceMode.VelocityChange);
            this.rigidbody.AddTorque(new Vector3(0f, 0f, this.rI), ForceMode.VelocityChange);
            base.SetMinLife(0.7f);
        }

        public override void Launch(float newX, float newY, float xI, float yI)
        {            
            if (this == null)
            {
                return;
            }
            this.SetXY(newX, newY);
            this.xI = xI;
            this.yI = yI;
            this.r = 0f;
            this.life = 6f;
            this.startLife = this.life;
            if (this.sprite != null)
            {
                this.spriteWidth = this.sprite.width;
                this.spriteHeight = this.sprite.height;
            }
            this.spriteWidthI = -this.spriteWidth / this.life * 1f;
            this.spriteHeightI = -this.spriteHeight / this.life * 1f;
            if (Mathf.Abs(xI) > 100f)
            {
                this.rI = -Mathf.Sign(xI) * (float)UnityEngine.Random.Range(20, 25);
            }
            else
            {
                this.rI = -Mathf.Sign(xI) * (float)UnityEngine.Random.Range(10, 15);
            }
            this.SetPosition();
            if (!this.shrapnelControlsMotion && base.GetComponent<Rigidbody>() == null)
            {
                BoxCollider boxCollider = base.gameObject.GetComponent<BoxCollider>();
                if (boxCollider == null)
                {
                    boxCollider = base.gameObject.AddComponent<BoxCollider>();
                }
                boxCollider.size = new Vector3(9f, 3f, 5f);
                this.rigidbody = base.gameObject.AddComponent<Rigidbody>();
                this.rigidbody.AddForce(new Vector3(xI, yI, 0f), ForceMode.VelocityChange);
                this.rigidbody.constraints = (RigidbodyConstraints)56;
                this.rigidbody.maxAngularVelocity = float.MaxValue;
                this.rigidbody.AddTorque(new Vector3(0f, 0f, this.rI), ForceMode.VelocityChange);
                this.rigidbody.drag = 0.8f;
                this.rigidbody.angularDrag = 0.7f;
                this.rigidbody.mass = 230f;
                Quaternion rotation = this.rigidbody.rotation;
                rotation.eulerAngles = new Vector3(0f, 0f, 90f);
                this.rigidbody.rotation = rotation;
            }
            base.enabled = true;
            this.lastTrailX = newX;
            this.lastTrailY = newY;
            if (Map.InsideWall(newX, newY, this.size, false))
            {
                if (xI > 0f && !Map.InsideWall(newX - 8f, newY, this.size, false))
                {
                    float num = 8f;
                    float num2 = 0f;
                    bool flag = false;
                    bool flag2 = false;
                    if (Map.ConstrainToBlocks(this, newX - 8f, newY, this.size, ref num, ref num2, ref flag, ref flag2, false))
                    {
                        newX = newX - 8f + num;
                        newY += num2;
                    }
                    xI = -xI * 0.6f;
                }
                else if (xI > 0f)
                {
                    float num3 = xI * 0.03f;
                    float num4 = yI * 0.03f;
                    bool flag3 = false;
                    bool flag4 = false;
                    if (Map.ConstrainToBlocks(this, newX, newY, this.size, ref num3, ref num4, ref flag3, ref flag4, false))
                    {
                        this.Bounce(flag3, flag4);
                    }
                    newX += num3;
                    newY += num4;
                    xI = -xI * 0.6f;
                }
                if (xI < 0f && !Map.InsideWall(newX + 8f, newY, this.size, false))
                {
                    float num5 = -8f;
                    float num6 = 0f;
                    bool flag5 = false;
                    bool flag6 = false;
                    if (Map.ConstrainToBlocks(this, newX + 8f, newY, this.size, ref num5, ref num6, ref flag5, ref flag6, false))
                    {
                        newX = newX + 8f + num5;
                        newY += num6;
                    }
                    xI = -xI * 0.6f;
                }
                else if (xI < 0f)
                {
                    float num7 = xI * 0.03f;
                    float num8 = yI * 0.03f;
                    bool flag7 = false;
                    bool flag8 = false;
                    if (Map.ConstrainToBlocks(this, newX, newY, this.size, ref num7, ref num8, ref flag7, ref flag8, false))
                    {
                        this.Bounce(flag7, flag8);
                    }
                    newX += num7;
                    newY += num8;
                    xI = -xI * 0.6f;
                }
            }
            this.SetPosition();
            this.sprite.offset = new Vector3(0f, -0.5f, 0f);
            currentVolume = 1f;
        }

        
        private void OnCollisionEnter(Collision collision) 
        {
            if (!hasCollided && (collisionLayers.value & (1 << collision.gameObject.layer)) != 0)
            {
                if (Mathf.Abs(this.xI) > 1f)
                {
                    PlayRandomCanSound(); 
                    hasCollided = true; 
                }
            }
        }


        protected override bool Update()
        {
            bool retVal = base.Update();

            base.X = rigidbody.position.x;
            base.Y = rigidbody.position.y;

            Collider[] hitColliders = Physics.OverlapSphere(rigidbody.position, 0.5f); 
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.gameObject != this.gameObject)
                {
                    hasCollided = true;

                    int layer = hitCollider.gameObject.layer;
                    if (((1 << layer) & collisionLayers) != 0 && Mathf.Abs(xI) > 1f && Time.time >= nextImpactSoundTime)
                    {
                        PlayImpactSound();
                        nextImpactSoundTime = Time.time + impactSoundCooldown; 
                    }

                    break;
                }
            }

            if (hasCollided)
            {
                Map.AttractMooks(base.X, base.Y, 200f, 100f);
                Map.AttractAliens(base.X, base.Y, 200f, 100f);
                HitAllLivingUnits(this, playerNum, 0, DamageType.None, 20f, 10f, base.X, base.Y, xI, yI, false, true);
                hasCollided = false; 
            }
            if (Mathf.Abs(yI) < 0.1f && Mathf.Abs(xI) > 0.1f) 
            {
                PlayRollingSound();
            }

            return retVal;
        }

        private void PlayRollingSound()
        {
            if (canRollSounds.Length > 0)
            {
                AudioClip rollClip = canRollSounds[UnityEngine.Random.Range(0, canRollSounds.Length)];
                audioSource.PlayOneShot(rollClip);
            }
        }

        private void PlayImpactSound()
        {
            if (canSounds.Length > 0)
            {
                AudioClip impactClip = canSounds[UnityEngine.Random.Range(0, canSounds.Length)];
                audioSource.PlayOneShot(impactClip);
            }
        }

        public static bool HitAllLivingUnits(MonoBehaviour damageSender, int playerNum, int damage, DamageType damageType, float xRange, float yRange, float x, float y, float xI, float yI, bool penetrates, bool knock)
        {
            if (Map.units == null)
            {
                return false;
            }
            bool result = false;
            Mathf.CeilToInt((float)damage * 0f);
            for (int i = Map.units.Count - 1; i >= 0; i--)
            {
                Unit unit = Map.units[i];
                if (unit != null && playerNum != unit.playerNum && !unit.invulnerable && unit.health > 0 && Mathf.Abs(unit.X - x) - xRange < unit.width)
                {
                    float num = 0f;
                    float num2 = 0f;
                    switch (unit.GetMookType())
                    {
                        case MookType.Trooper:
                        case MookType.Suicide:
                        case MookType.Scout:
                        case MookType.RiotShield:
                        case MookType.Grenadier:
                        case MookType.Villager:
                        case MookType.UndeadTrooper:
                        case MookType.UndeadSuicide:
                            num = unit.Y + unit.height * 0.78f;
                            num2 = unit.height * 0.22f;
                            break;
                        case MookType.BigGuy:
                        case MookType.Melter:
                        case MookType.Boomer:
                        case MookType.HellBigGuy:
                            num = unit.Y + unit.height * 0.8f;
                            num2 = unit.height * 0.2f;
                            break;
                        case MookType.Dog:
                        case MookType.Alien:
                        case MookType.FaceHugger:
                        case MookType.HellDog:
                        case MookType.ArmouredGuy:
                        case MookType.General:
                            num = unit.Y + unit.height * 0.74f;
                            num2 = unit.height * 0.26f;
                            break;
                    }
                    if (Mathf.Abs(num - y) < num2)
                    {
                        (damageSender as CoorsCan).Bounce(true, true);
                        if (!penetrates)
                        {
                            return true;
                        }
                    }
                }

            }
            return result;
        }
        private void PlayRandomCanSound()
        {
            int randomIndex = UnityEngine.Random.Range(0, canSounds.Length);
            audioSource.clip = canSounds[randomIndex];
            audioSource.volume = currentVolume;
            audioSource.Play();
            currentVolume = Mathf.Max(currentVolume - volumeReductionStep, minVolume);
        }

        public override void Death()
        {
            if (base.FiredLocally)
            {
                bool friendlyFire = this.friendlyFire;
            }
            if (!this.dontMakeEffects)
            {
                this.MakeEffects();
            }
            this.DestroyGrenade();
        }

        protected override void RunWarnings()
        {
        }
        
        protected override void MakeEffects()
        {
            if (this.sound == null)
            {
                this.sound = Sound.GetInstance();
            }
            if (this.sound != null)
            {
                //this.sound.PlaySoundEffectAt(this.explosionSound, 0.4f, base.transform.position, 1f, true, false, false, 0f);
            }
        }
    }
}
