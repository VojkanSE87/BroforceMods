using System;
using System.IO;
using System.Reflection;
using BroMakerLib;
using UnityEngine;

namespace JackBroton
{
    internal class BootKnife : Projectile
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
            Rigidbody component = base.GetComponent<Rigidbody>();
            bool flag = component != null;
            if (flag)
            {
                component.collisionDetectionMode = 1;
            }
            MeshRenderer component2 = base.gameObject.GetComponent<MeshRenderer>();
            bool flag2 = BootKnife.storedMat == null;
            if (flag2)
            {
                string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                BootKnife.storedMat = ResourcesController.GetMaterial(directoryName, "BootKnife.png");
            }
            component2.material = BootKnife.storedMat;
            SpriteSM component3 = base.gameObject.GetComponent<SpriteSM>();
            component3.lowerLeftPixel = new Vector2(0f, 16f);
            component3.pixelDimensions = new Vector2(34f, 16f);
            component3.plane = 0;
            component3.width = 23f;
            component3.height = 11f;
            component3.offset = Vector3.zero;
            this.storedSprite = component3;
            this.damageType = 3;
            this.damage = 8;
            this.damageInternal = this.damage;
            this.fullDamage = this.damage;
            this.life = 9f;
        }

        protected override void Update()
        {
            this.DeflectEnemyProjectiles();
            this.life -= Time.deltaTime;
            bool flag = this.life <= 0f;
            if (flag)
            {
                this.DeregisterProjectile();
                Object.Destroy(base.gameObject);
            }
            else
            {
                this.SetPosition(base.X + this.xI * Time.deltaTime, base.Y + this.yI * Time.deltaTime);
                this.HitUnits();
                Map.DisturbWildLife(base.X, base.Y, 60f, this.playerNum);
                Map.HurtWildLife(base.X, base.Y, this.projectileSize / 2f);
                Map.ShakeTrees(base.X, base.Y, 24f, 20f, 60f);
                Map.DisturbAlienEggs(base.X, base.Y, this.playerNum);
                Map.JiggleDoodads(base.X, base.Y, 24f, 16f, 60f);
                float num = 0f;
                float num2 = 0f;
                Map.HitGrenades(this.playerNum, 12f, base.X, base.Y, this.xI, this.yI, ref num, ref num2);
            }
        }

        public override void Fire(float newX, float newY, float xI, float yI, float _zOffset, int playerNum, MonoBehaviour FiredBy)
        {
            float num = 19f * Mathf.Sign(xI);
            newX += num;
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
            float num = 16f;
            float num2 = base.X + Mathf.Sign(this.xI) * 6f;
            float num3 = base.Y + 6f;
            float num4 = Mathf.Sign(this.xI) * 200f;
            bool flag = true;
            bool flag2 = Map.DeflectProjectiles(this, this.playerNum, num, num2, num3, num4, flag);
            if (flag2)
            {
                this.hasHitWithWall = true;
            }
        }

        protected override void HitUnits()
        {
            float x = base.X;
            float y = base.Y;
            Vector3 vector = (this.xI > 0f) ? Vector3.right : Vector3.left;
            float num = Mathf.Abs(this.xI * Time.deltaTime + this.projectileSize);
            bool flag = Physics.Raycast(new Vector3(x, y, 0f), vector, ref this.raycastHit, num, 1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("IndestructibleGround") | 1 << LayerMask.NameToLayer("Parachute") | 1 << LayerMask.NameToLayer("LargeObjects"));
            if (flag)
            {
                MapController.DamageGround(this, 2, 8, 10f, this.raycastHit.point.x, this.raycastHit.point.y, null, true);
                this.penetrateCount++;
                bool flag2 = this.penetrateCount >= this.maxPenetrations;
                if (flag2)
                {
                    this.DeregisterProjectile();
                    Object.Destroy(base.gameObject);
                    return;
                }
                bool flag3 = this.raycastHit.collider.gameObject.layer == 30;
                if (flag3)
                {
                    this.raycastHit.collider.gameObject.SendMessage("Damage", new DamageObject(60, 3, this.xI, this.yI, this.raycastHit.point.x, this.raycastHit.point.y, this));
                    this.MakeEffects(true, base.X, base.Y, false, this.raycastHit.normal, this.raycastHit.point);
                    this.DeregisterProjectile();
                    Object.Destroy(base.gameObject);
                    return;
                }
                this.raycastHit.collider.gameObject.SendMessage("Damage", new DamageObject(2, 8, this.xI, this.yI, this.raycastHit.point.x, this.raycastHit.point.y, this));
            }
            bool flag4 = Map.HitLivingUnits(this, this.playerNum, this.damageInternal, this.damageType, this.projectileSize, this.projectileSize / 2f, x, y, this.xI, this.yI, true, true, true, true);
            bool flag5 = flag4;
            if (flag5)
            {
                this.penetrateCount++;
                bool flag6 = this.penetrateCount >= this.maxPenetrations;
                if (flag6)
                {
                    this.DeregisterProjectile();
                    Object.Destroy(base.gameObject);
                }
            }
            else
            {
                JackBroton jackBroton = this.firedBy as JackBroton;
                bool flag7 = jackBroton != null && jackBroton.hitDeadUnits;
                if (flag7)
                {
                    bool flag8 = Map.HitDeadUnits(this, 2, 8, this.projectileSize, x, y, this.xI, this.yI, false, true);
                    bool flag9 = flag8;
                    if (flag9)
                    {
                        this.penetrateCount++;
                        bool flag10 = this.penetrateCount >= this.maxPenetrations;
                        if (flag10)
                        {
                            this.DeregisterProjectile();
                            Object.Destroy(base.gameObject);
                            return;
                        }
                        return;
                    }
                }
                bool flag12;
                bool flag11 = BootKnife.DamageDoodads(this.damage, this.damageType, x, y, this.xI, this.yI, 5f, this.playerNum, out flag12, null);
                if (flag11)
                {
                    bool flag13 = flag12;
                    if (flag13)
                    {
                        this.DeregisterProjectile();
                        Object.Destroy(base.gameObject);
                    }
                }
            }
        }

        protected override void TryHitUnitsAtSpawn()
        {
            bool flag = this.hitDeadUnits;
            if (flag)
            {
                base.TryHitUnitsAtSpawn();
            }
            else
            {
                bool flag2 = Map.HitLivingUnits(this.firedBy, this.playerNum, this.damageInternal * 2, this.damageType, (this.playerNum < 0) ? 0f : (this.projectileSize * 0.5f), base.X - ((this.playerNum < 0) ? 0f : (this.projectileSize * 0.5f)) * (float)((int)Mathf.Sign(this.xI)), base.Y, this.xI, this.yI, false, false, true, false);
                if (flag2)
                {
                    this.MakeEffects(false, base.X, base.Y, false, this.raycastHit.normal, this.raycastHit.point);
                    Object.Destroy(base.gameObject);
                    this.hasHit = true;
                }
            }
        }

        protected void KickDoors(float range)
        {
            bool flag = Physics.Raycast(new Vector3(base.X - 6f * base.transform.localScale.x, base.Y + this.waistHeight, 0f), new Vector3(base.transform.localScale.x, 1f, 1f), ref this.raycastHit, 6f + range, this.fragileLayer) && this.raycastHit.collider.gameObject.GetComponent<Parachute>() == null;
            if (flag)
            {
                this.raycastHit.collider.gameObject.SendMessage("Open", (int)base.transform.localScale.x);
                MapController.Damage_Networked(this, this.raycastHit.collider.gameObject, 1, 4, base.transform.localScale.x * 500f, 50f, base.X, base.Y);
            }
        }

        protected override void Bounce(RaycastHit raycastHit)
        {
            this.MakeEffects(true, raycastHit.point.x + raycastHit.normal.x * 3f, raycastHit.point.y + raycastHit.normal.y * 3f, true, raycastHit.normal, raycastHit.point);
            this.ProjectileApplyDamageToBlock(raycastHit.collider.gameObject, this.damageInternal, this.damageType, this.xI, this.yI);
            bool flag = this.penetrateWalls && this.wallPenetrateCount < this.maxWallPenetrations;
            if (flag)
            {
                this.wallPenetrateCount++;
            }
            else
            {
                this.DeregisterProjectile();
                Object.Destroy(base.gameObject);
            }
        }

        protected override void HitProjectiles()
        {
            bool flag = Map.HitProjectiles(this.playerNum, this.damageInternal, this.damageType, this.projectileSize, base.X, base.Y, this.xI, this.yI, 0.1f);
            if (flag)
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
                bool flag = !(doodad == null);
                if (flag)
                {
                    bool flag2 = doodad.IsPointInRange(x, y, range);
                    if (flag2)
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
                bool flag = !(doodad == null) && (playerNum >= 0 || doodad.CanBeDamagedByMooks);
                if (flag)
                {
                    bool flag2 = playerNum < 0 || !doodad.immuneToHeroDamage;
                    if (flag2)
                    {
                        bool flag3 = doodad.IsPointInRange(x, y, range);
                        if (flag3)
                        {
                            bool flag4 = false;
                            doodad.DamageOptional(new DamageObject(damage, damageType, xI, yI, x, y, sender), ref flag4);
                            bool flag5 = flag4;
                            if (flag5)
                            {
                                result = true;
                                bool isImpenetrable = doodad.isImpenetrable;
                                if (isImpenetrable)
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
            bool flag2 = array.Length != 0;
            if (flag2)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    Collider collider = null;
                    bool flag3 = this.firedBy != null;
                    if (flag3)
                    {
                        collider = this.firedBy.GetComponent<Collider>();
                    }
                    bool flag4 = this.firedBy == null || array[i] != collider;
                    if (flag4)
                    {
                        this.ProjectileApplyDamageToBlock(array[i].gameObject, this.damageInternal, this.damageType, this.xI, this.yI);
                        flag = true;
                    }
                }
                bool flag5 = flag;
                if (flag5)
                {
                    this.MakeEffects(false, base.X, base.Y, false, this.raycastHit.normal, this.raycastHit.point);
                    this.hasHit = true;
                }
            }
            return flag;
        }

        protected override void CheckSpawnPoint()
        {
            bool flag = this.CheckWallsAtSpawnPoint();
            Debug.DrawRay(new Vector3(base.X, base.Y, 0f), Random.onUnitSphere * 5f, Color.cyan, 10f);
            bool flag2;
            Map.DamageDoodads(this.damageInternal, this.damageType, base.X, base.Y, this.xI, this.yI, this.projectileSize, this.playerNum, ref flag2, this);
            bool flag3 = !flag;
            if (flag3)
            {
                this.RegisterProjectile();
            }
            this.CheckReturnZones();
            bool flag4 = (this.canReflect && this.playerNum >= 0 && this.horizontalProjectile && Physics.Raycast(new Vector3(base.X - Mathf.Sign(this.xI) * this.projectileSize * 2f, base.Y, 0f), new Vector3(this.xI, this.yI, 0f), ref this.raycastHit, this.projectileSize * 3f, this.barrierLayer)) || (!this.horizontalProjectile && Physics.Raycast(new Vector3(base.X, base.Y, 0f), new Vector3(this.xI, this.yI, 0f), ref this.raycastHit, this.projectileSize + this.startProjectileSpeed * this.t, this.barrierLayer));
            if (flag4)
            {
                this.ReflectProjectile(this.raycastHit);
            }
            else
            {
                bool flag5 = (this.canReflect && this.playerNum < 0 && this.horizontalProjectile && Physics.Raycast(new Vector3(base.X - Mathf.Sign(this.xI) * this.projectileSize * 2f, base.Y, 0f), new Vector3(this.xI, this.yI, 0f), ref this.raycastHit, this.projectileSize * 3f, this.friendlyBarrierLayer)) || (!this.horizontalProjectile && Physics.Raycast(new Vector3(base.X, base.Y, 0f), new Vector3(this.xI, this.yI, 0f), ref this.raycastHit, this.projectileSize + this.startProjectileSpeed * this.t, this.friendlyBarrierLayer));
                if (flag5)
                {
                    this.playerNum = 5;
                    this.firedBy = null;
                    this.ReflectProjectile(this.raycastHit);
                }
                else
                {
                    this.TryHitUnitsAtSpawn();
                }
            }
            this.CheckSpawnPointFragile();
        }

        public void Setup()
        {
            base.enabled = true;
        }
        
    }
}
