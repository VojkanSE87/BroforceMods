using System;
using System.IO;
using System.Reflection;
using BroMakerLib;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace JackBroton
{
    class BootKnife : Projectile
    {
        public static Material storedMat;
        private SpriteSM storedSprite;

        public bool hitDeadUnits = true;
        protected float waistHeight = 10f;
        public bool penetrateWalls;
        public int maxPenetrations = 19;
        public int maxWallPenetrations = 14;
        protected int penetrateCount;
        private int wallPenetrateCount;
        private bool hasHitWithWall;
        private bool attackHasHit;

        protected override void Awake()
        {
            base.Awake();
            Rigidbody rb = this.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            MeshRenderer renderer = this.gameObject.GetComponent<MeshRenderer>();

            if (storedMat == null)
            {
                string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                storedMat = ResourcesController.GetMaterial(directoryPath, "BootKnife.png");
            }
            renderer.material = storedMat;

            SpriteSM sprite = this.gameObject.GetComponent<SpriteSM>();
            sprite.lowerLeftPixel = new Vector2(0, 16);
            sprite.pixelDimensions = new Vector2(34, 16);
            sprite.plane = SpriteBase.SPRITE_PLANE.XY;
            sprite.width = 23;
            sprite.height = 11;
            sprite.offset = Vector3.zero;

            storedSprite = sprite;

            this.damageType = DamageType.Normal;
            this.damage = 8;
            this.damageInternal = this.damage;
            this.fullDamage = this.damage;
            this.life = 9f; 
        }

        protected override void Update()
        { 
           
            this.DeflectEnemyProjectiles();  
            this.life -= Time.deltaTime;
            if (this.life <= 0f)
            {
                DeregisterProjectile();
                Destroy(this.gameObject);
                return;
            }
            this.SetPosition(base.X + this.xI * Time.deltaTime, base.Y + this.yI * Time.deltaTime);
            this.HitUnits();
            Map.DisturbWildLife(base.X, base.Y, 60f, base.playerNum);
           
            Map.HurtWildLife(base.X, base.Y, this.projectileSize / 2f);
            Map.ShakeTrees(base.X, base.Y, 24f, 20f, 60f);
            Map.DisturbAlienEggs(base.X, base.Y, base.playerNum);
            Map.JiggleDoodads(base.X, base.Y, 24f, 16f, 60f);
            float grenadeX = 0f;
            float grenadeY = 0f;
            Map.HitGrenades(base.playerNum, 12f, base.X, base.Y, base.xI, base.yI, ref grenadeX, ref grenadeY);

        }

        public override void Fire(float newX, float newY, float xI, float yI, float _zOffset, int playerNum, MonoBehaviour FiredBy)
        {
            float offset = 19f * Mathf.Sign(xI);
            newX += offset;
            newY += 13f;

            xI *= 22f;

            base.Fire(newX, newY, xI, yI, _zOffset, playerNum, FiredBy);            

        }

        private void SetPosition(float newX, float newY)
        {
            base.X = newX;
            base.Y = newY;
            base.transform.position = new Vector3(base.X, base.Y, base.transform.position.z);
        }

        protected void DeflectEnemyProjectiles()
        {
            float range = 16f;
            float originX = base.X + Mathf.Sign(this.xI) * 6f;
            float originY = base.Y + 6f;
            float reflectedVelocityX = Mathf.Sign(this.xI) * 200f;
            bool giveDeflectAchievement = true;
                        
            if (Map.DeflectProjectiles(
                    this,
                    base.playerNum,
                    range,
                    originX,
                    originY,
                    reflectedVelocityX,
                    giveDeflectAchievement
                ))
            {
                this.hasHitWithWall = true;
            }
        }

        protected override void HitUnits()
        {
            float x = base.X;
            float y = base.Y;
            
            Vector3 direction = this.xI > 0 ? Vector3.right : Vector3.left;
            float rayLength = Mathf.Abs(this.xI * Time.deltaTime + this.projectileSize);

            if (Physics.Raycast(new Vector3(x, y, 0f), direction, out this.raycastHit, rayLength, 1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("IndestructibleGround") | 1 << LayerMask.NameToLayer("Parachute") | 1<< LayerMask.NameToLayer("LargeObjects")))
            {
                MapController.DamageGround(this, 2, DamageType.Chainsaw, 10f, this.raycastHit.point.x, this.raycastHit.point.y, null, true);

                this.penetrateCount++;
                if (this.penetrateCount >= this.maxPenetrations)
                {
                    this.DeregisterProjectile();
                    Destroy(this.gameObject);
                    return;
                }
                if (this.raycastHit.collider.gameObject.layer == 30)
                {
                    this.raycastHit.collider.gameObject.SendMessage("Damage", new DamageObject(60, DamageType.Normal, this.xI, this.yI, this.raycastHit.point.x, this.raycastHit.point.y, this));

                    this.MakeEffects(true, base.X, base.Y, false, this.raycastHit.normal, this.raycastHit.point);

                    this.DeregisterProjectile();
                    Destroy(this.gameObject);
                    return;

                }
                this.raycastHit.collider.gameObject.SendMessage("Damage", new DamageObject( 2, DamageType.Chainsaw, this.xI, this.yI, this.raycastHit.point.x, this.raycastHit.point.y, this)
                );
            }

            bool hitLiving = Map.HitLivingUnits(
                this,                     
                this.playerNum,           
                this.damageInternal,      
                this.damageType,          
                this.projectileSize,      
                this.projectileSize / 2f, 
                x,                        
                y,                       
                this.xI,                 
                this.yI,                 
                true,   // allow penetrating multiple living units
                true,   // apply knockback to living units
                true,   // play hit effects (sparks, blood, etc.)
                true    // allow splash damage to nearby units
            );

            if (hitLiving)
            {
                this.penetrateCount++;
                if (this.penetrateCount >= this.maxPenetrations)
                {
                    DeregisterProjectile();
                    Destroy(this.gameObject);
                    return; 
                }
                return;
            }

            JackBroton jack = this.firedBy as JackBroton;
            if (jack != null && jack.hitDeadUnits)
            {
                bool didHitDead = Map.HitDeadUnits( this, 2, DamageType.Chainsaw, this.projectileSize, x, y, xI, yI, false, true);
                if (didHitDead)
                {
                    penetrateCount++;
                    if (penetrateCount >= maxPenetrations)
                    {
                        DeregisterProjectile();
                        Destroy(this.gameObject);
                        return;
                    }
                    return; 
                }
            }
            bool hitImpenetrableDoodad;
            if (DamageDoodads(
                    this.damage,    
                    this.damageType,
                    x, y,
                    this.xI, this.yI,
                    5f,               
                    this.playerNum,
                    out hitImpenetrableDoodad
                ))
            {
                if (hitImpenetrableDoodad)
                {
                    DeregisterProjectile();
                    Destroy(this.gameObject);
                }
            }
        }

        protected override void TryHitUnitsAtSpawn()
        {
            if (this.hitDeadUnits)
            {
                base.TryHitUnitsAtSpawn();
            }
            else if (Map.HitLivingUnits(this.firedBy, this.playerNum, this.damageInternal * 2, this.damageType, (this.playerNum < 0) ? 0f : (this.projectileSize * 0.5f), base.X - ((this.playerNum < 0) ? 0f : (this.projectileSize * 0.5f)) * (float)((int)Mathf.Sign(this.xI)), base.Y, this.xI, this.yI, false, false, true, false))
            {
                this.MakeEffects(false, base.X, base.Y, false, this.raycastHit.normal, this.raycastHit.point);
                UnityEngine.Object.Destroy(base.gameObject);
                this.hasHit = true;
            }
        }

        protected void KickDoors(float range)
        {
            if (Physics.Raycast(new Vector3(base.X - 6f * base.transform.localScale.x, base.Y + waistHeight, 0f), new Vector3(base.transform.localScale.x, 1f, 1f), out raycastHit, 6f + range, fragileLayer) && raycastHit.collider.gameObject.GetComponent<Parachute>() == null)
            {
                raycastHit.collider.gameObject.SendMessage("Open", (int)base.transform.localScale.x);
                MapController.Damage_Networked(this, raycastHit.collider.gameObject, 1, DamageType.Crush, base.transform.localScale.x * 500f, 50f, base.X, base.Y);
            }
        }
        protected override void Bounce(RaycastHit raycastHit)
        {
            MakeEffects(true, raycastHit.point.x + raycastHit.normal.x * 3f, raycastHit.point.y + raycastHit.normal.y * 3f, true, raycastHit.normal, raycastHit.point);
    ProjectileApplyDamageToBlock(raycastHit.collider.gameObject, this.damageInternal, this.damageType, this.xI, this.yI);

    if (penetrateWalls && wallPenetrateCount < maxWallPenetrations)
    {
        wallPenetrateCount++;
    }
    else
    {
        DeregisterProjectile();
        Destroy(base.gameObject);
    }
}
        protected override void HitProjectiles()
        {
            if (Map.HitProjectiles(this.playerNum, this.damageInternal, this.damageType, this.projectileSize, base.X, base.Y, this.xI, this.yI, 0.1f))
            {
                this.MakeEffects(false, base.X, base.Y, false, this.raycastHit.normal, this.raycastHit.point);
            }
        }

        public static bool DamageStaticDoodads(float x, float y, float xI, float yI, float range, MonoBehaviour sender = null)
        {
            bool result = false;
            for (int i = Map.staticDoodads.Count - 1; i >= 0; i--)
            {
                Doodad doodad = Map.staticDoodads[i];
                if (!(doodad == null))
                {
                    if (doodad.IsPointInRange(x, y, range))
                    {
                        result = true;
                        doodad.Collapse();
                    }
                }
            }
            return result;
        }

        public static bool DamageDoodads(int damage, DamageType damageType, float x, float y, float xI, float yI, float range, int playerNum, out bool hitImpenetrableDoodad, MonoBehaviour sender = null)
        {
            hitImpenetrableDoodad = false;
            bool result = false;
            for (int i = Map.destroyableDoodads.Count - 1; i >= 0; i--)
            {
                Doodad doodad = Map.destroyableDoodads[i];
                if (!(doodad == null) && (playerNum >= 0 || doodad.CanBeDamagedByMooks))
                {
                    if (playerNum < 0 || !doodad.immuneToHeroDamage)
                    {
                        if (doodad.IsPointInRange(x, y, range))
                        {
                            bool flag = false;
                            doodad.DamageOptional(new DamageObject(damage, damageType, xI, yI, x, y, sender), ref flag);
                            if (flag)
                            {
                                result = true;
                                if (doodad.isImpenetrable)
                                {
                                    hitImpenetrableDoodad = true;
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }         


        public static void DamageBlock(MonoBehaviour damageSender, Block b, int damage, DamageType damageType, float forceX, float forceY)
        {
            MapController.Damage_Networked(damageSender, b.gameObject, damage, damageType, forceX, forceY, b.X, b.Y);
        }

        protected override bool CheckWallsAtSpawnPoint()
        {
            Collider[] array = Physics.OverlapSphere(new Vector3(base.X, base.Y, 0f), 5f, this.groundLayer);
            bool flag = false;
            if (array.Length > 0)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    Collider y = null;
                    if (this.firedBy != null)
                    {
                        y = this.firedBy.GetComponent<Collider>();
                    }
                    if (this.firedBy == null || array[i] != y)
                    {
                        this.ProjectileApplyDamageToBlock(array[i].gameObject, this.damageInternal, this.damageType, this.xI, this.yI);
                        flag = true;
                    }
                }

                if (flag)
                {
                    this.MakeEffects(false, base.X, base.Y, false, this.raycastHit.normal, this.raycastHit.point);
                    this.hasHit = true;
                    // Do NOT destroy the projectile
                }
            }
            return flag;
        }
        protected override void CheckSpawnPoint()
        {
            bool flag = this.CheckWallsAtSpawnPoint();
            Debug.DrawRay(new Vector3(base.X, base.Y, 0f), UnityEngine.Random.onUnitSphere * 5f, Color.cyan, 10f);
            bool flag2;
            Map.DamageDoodads(this.damageInternal, this.damageType, base.X, base.Y, this.xI, this.yI, this.projectileSize, this.playerNum, out flag2, this);
                        
            // if (flag2)
            // {
            //     UnityEngine.Object.Destroy(base.gameObject);
            // }

            if (!flag)
            {
                this.RegisterProjectile();
            }

            this.CheckReturnZones();
                        
            if ((this.canReflect && this.playerNum >= 0 && this.horizontalProjectile &&
                 Physics.Raycast(new Vector3(base.X - Mathf.Sign(this.xI) * this.projectileSize * 2f, base.Y, 0f),
                 new Vector3(this.xI, this.yI, 0f), out this.raycastHit, this.projectileSize * 3f, this.barrierLayer)) ||
                (!this.horizontalProjectile &&
                 Physics.Raycast(new Vector3(base.X, base.Y, 0f), new Vector3(this.xI, this.yI, 0f), out this.raycastHit,
                 this.projectileSize + this.startProjectileSpeed * this.t, this.barrierLayer)))
            {
                this.ReflectProjectile(this.raycastHit);
            }
            else if ((this.canReflect && this.playerNum < 0 && this.horizontalProjectile &&
                      Physics.Raycast(new Vector3(base.X - Mathf.Sign(this.xI) * this.projectileSize * 2f, base.Y, 0f),
                      new Vector3(this.xI, this.yI, 0f), out this.raycastHit, this.projectileSize * 3f, this.friendlyBarrierLayer)) ||
                     (!this.horizontalProjectile &&
                      Physics.Raycast(new Vector3(base.X, base.Y, 0f), new Vector3(this.xI, this.yI, 0f), out this.raycastHit,
                      this.projectileSize + this.startProjectileSpeed * this.t, this.friendlyBarrierLayer)))
            {
                this.playerNum = 5;
                this.firedBy = null;
                this.ReflectProjectile(this.raycastHit);
            }
            else
            {
                this.TryHitUnitsAtSpawn();
            }

            this.CheckSpawnPointFragile();
        }

        public void Setup()
        {
            this.enabled = true;
        }
    }
}
   