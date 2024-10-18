using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BroMakerLib;
using RocketLib.Collections;
using Rogueforce;
using UnityEngine;
using Utility;

namespace JackBroton
{
    public class BootKnife : Grenade
    {
        public static Material storedMat;
        private Rigidbody rigidbody;
        private MonoBehaviour damageSender;
        private float maxDistance = 100f; // Set the maximum distance the projectile can travel
        private Vector3 startPosition;
        private int collisionLayers;

        protected override void Awake()
        {
            Renderer component = base.gameObject.GetComponent<MeshRenderer>();
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (BootKnife.storedMat == null)
            {
                BootKnife.storedMat = ResourcesController.GetMaterial(directoryName, "BootKnife.png");
            }
            if (base.GetComponent<Collider>() == null)
            {
                base.gameObject.AddComponent<BoxCollider>();
            }

            component.material = BootKnife.storedMat;

            this.sprite = base.gameObject.GetComponent<SpriteSM>();
            this.sprite.lowerLeftPixel = new Vector2(0f, 16f);
            this.sprite.pixelDimensions = new Vector2(16f, 16f);
            this.sprite.plane = SpriteBase.SPRITE_PLANE.XY;
            this.sprite.width = 18f;
            this.sprite.height = 22f;

            base.Awake();

            this.rigidbody = gameObject.AddComponent<Rigidbody>();
            this.rigidbody.useGravity = false;
            this.rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            this.rigidbody.drag = 0f;
            this.rigidbody.angularDrag = 0f;
        }

        protected override void Start()
        {
            this.mainMaterial = base.GetComponent<Renderer>().sharedMaterial;
            base.Start();
            this.groundLayer = Map.groundLayer;
            this.startPosition = transform.position;
        }

        public override void ThrowGrenade(float XI, float YI, float newX, float newY, int _playerNum)
        {
            base.enabled = true;
            base.transform.parent = null;
            this.SetXY(newX, newY);
            this.xI = XI;
            this.yI = YI;
            this.playerNum = _playerNum;
            this.rigidbody.position = new Vector3(newX, newY);
            this.rigidbody.velocity = new Vector3(this.xI, this.yI, 0f);
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
            this.rigidbody.velocity = new Vector3(xI, yI, 0f);
            this.SetPosition();
            base.enabled = true;
        }

        protected override bool Update()
        {
            bool retVal = base.Update();

            base.X = rigidbody.position.x;
            base.Y = rigidbody.position.y;

            // Check if the projectile has traveled the maximum distance
            if (Vector3.Distance(startPosition, rigidbody.position) >= maxDistance)
            {
                Death();
                return false;
            }

            Collider[] hitColliders = Physics.OverlapSphere(rigidbody.position, 0.5f);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.gameObject != this.gameObject)
                {
                    int layer = hitCollider.gameObject.layer;
                    if (((1 << layer) & collisionLayers) != 0)
                    {
                        HitAllLivingUnits(this, playerNum, 0, DamageType.Knifed, 20f, 10f, base.X, base.Y, xI, yI, true, true);
                    }
                }
            }

            return retVal;
        }

        private void HitAllLivingUnits(BootKnife bootKnife, int playerNum, int v1, DamageType knifed, float v2, float v3, float x, float y, float xI, float yI, bool v4, bool v5)
        {
            throw new NotImplementedException();
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
