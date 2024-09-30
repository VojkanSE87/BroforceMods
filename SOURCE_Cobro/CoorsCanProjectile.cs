using System;
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
        AudioClip explosionSound;                                       //replace sa empty can sound or something
        public Vector3 centerOfMass = new Vector3(0f, -0.3f, 0f);
        private static bool bounceX;
        private static bool bounceY;
        private bool hasCollided = false;
        private float soundThreshold = 0.5f; // 2309 Movement threshold to play sounds
        private AudioClip[] canSounds;  //2309
        public AudioClip[] canRollSounds; // 2309 Array for rolling sounds
        private AudioSource audioSource;//2309
        public float impactThreshold = 1f; // 2309 Time threshold in seconds for impact sound
                                             // Cooldown between impact sounds
        private float nextImpactSoundTime = 0f;
        private float impactSoundCooldown = 0.5f; // The minimum time that must pass before playing another impact sound
        public float yMovementThreshold = 0.5f; // 2309 Threshold to determine significant y-axis movement
        private float timeSinceLastSound = 0f; //2309 Timer to track time since last sound
        private Vector3 lastPosition; // 2309 To track the can's previous position
        private Rigidbody rb; //2309
        public LayerMask collisionLayers; //2309


        private float currentVolume = 1f; // Added to track volume
        private float volumeReductionStep = 0.3f; // The amount to reduce volume per collision
        private float minVolume = 0.2f; // Minimum volume


        // Cooldown-related variables
        private float nextSoundTime = 0f; // Tracks when the next sound can be played
        private float soundCooldown = 0.5f; // Cooldown duration in seconds

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

            this.canSounds = new AudioClip[]    //2309
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

            this.audioSource = gameObject.AddComponent<AudioSource>(); //2309
            this.audioSource.playOnAwake = false;                       //2309
            this.audioSource.loop = false;                              //2309
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
            collisionLayers = LayerMask.GetMask("Ground", "DirtyHippie", "Switches"); //2709 "Platform" "MobileBarriers" "LargeObjects" "IndestructibleGround" ,

        }

        // Token: 0x0600001E RID: 30 RVA: 0x000039D8 File Offset: 0x00001BD8
        protected override void Start()
        {
            this.mainMaterial = base.GetComponent<Renderer>().sharedMaterial;
            base.Start();
            this.groundLayer = Map.groundLayer;
            //2709 collisionLayers = LayerMask.GetMask("Ground", "Switches"); //"Platform" "MobileBarriers" "LargeObjects" "IndestructibleGround" "DirtyHippie",
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

        // Token: 0x0600001F RID: 31 RVA: 0x00003A51 File Offset: 0x00001C51
        protected new virtual void RegisterGrenade()
        {
            if (this.shootable)
            {
                Map.RegisterShootableGrenade(this);
            }
            Map.RegisterGrenade(this);
        }

        // Token: 0x06000020 RID: 32 RVA: 0x00003A68 File Offset: 0x00001C68
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

        // Token: 0x06000021 RID: 33 RVA: 0x00003B84 File Offset: 0x00001D84
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
            //2709 Reset volume when launching again
            currentVolume = 1f;
        }

        
        private void OnCollisionEnter(Collision collision) //2309
        {

            // 2309 
            // Check if collision is with the specified layers and playif the can hasn't collided yet
            if (!hasCollided && (collisionLayers.value & (1 << collision.gameObject.layer)) != 0)
            {
                // Ensure significant movement before playing the sound
                if (Mathf.Abs(this.xI) > 1f)
                {
                    PlayRandomCanSound(); // Play the random sound on first impact
                    hasCollided = true;   // Set the flag to true to prevent further sound playing
                }
            }
        }


        protected override bool Update()
        {
            bool retVal = base.Update();

            base.X = rigidbody.position.x;
            base.Y = rigidbody.position.y;

            Collider[] hitColliders = Physics.OverlapSphere(rigidbody.position, 0.5f); // Adjust the radius as needed
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.gameObject != this.gameObject)
                {
                    hasCollided = true;

                    int layer = hitCollider.gameObject.layer;
                    if (((1 << layer) & collisionLayers) != 0 && Mathf.Abs(xI) > 1f && Time.time >= nextImpactSoundTime)
                    {
                        PlayImpactSound();
                        nextImpactSoundTime = Time.time + impactSoundCooldown; // Adjust the cooldown here
                    }

                    break;
                }
            }

            if (hasCollided)
            {
                Map.AttractMooks(base.X, base.Y, 200f, 100f);
                Map.AttractAliens(base.X, base.Y, 200f, 100f);

                // Call HitAllLivingUnits to apply stun effect
                HitAllLivingUnits(this, playerNum, 0, DamageType.None, 20f, 10f, base.X, base.Y, xI, yI, false, true);

                hasCollided = false; // Reset the collision flag
            }

            // Check for movement in Y axis (lower threshold for smaller y movement)
            if (Mathf.Abs(yI) < 0.1f && Mathf.Abs(xI) > 0.1f) // Y-axis threshold is very small for rolling detection
            {
                PlayRollingSound();
            }

            return retVal;
        }

        private void PlayRollingSound()
        {
            // Play a rolling sound from the canRollSounds array
            if (canRollSounds.Length > 0)
            {
                AudioClip rollClip = canRollSounds[UnityEngine.Random.Range(0, canRollSounds.Length)];
                audioSource.PlayOneShot(rollClip);
            }
        }

        private void PlayImpactSound()
        {
            // Play a heavy impact sound from the canImpactSounds array
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
        private void PlayRandomCanSound() //2309
        {
            // 2709 Play a random sound from the array with decreasing volume
            int randomIndex = UnityEngine.Random.Range(0, canSounds.Length);
            audioSource.clip = canSounds[randomIndex];
            audioSource.volume = currentVolume;
            audioSource.Play();

            // Reduce the volume for the next collision, ensuring it doesn't drop below the minimum volume
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