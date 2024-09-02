using BroMakerLib;
using Rogueforce;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Utility;
using static UnityEngine.UI.CanvasScaler;

namespace Cobro
{
    public class CoorsCan : Grenade
    {
        public static Material storedMat;
        // Token: 0x0400186D RID: 6253
        protected float hitProjectileDelay;

        Rigidbody rigidbody;
        AudioClip explosionSound; //replace sa empty can sound or something
        public Vector3 centerOfMass = new Vector3(0f, -0.3f, 0f);

        protected override void Awake()
        {
            MeshRenderer renderer = this.gameObject.GetComponent<MeshRenderer>();

            string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (storedMat == null)
            {
                storedMat = ResourcesController.GetMaterial(directoryPath, "CoorsCan.png");
            }

            renderer.material = storedMat;

            this.sprite = this.gameObject.GetComponent<SpriteSM>();
            sprite.lowerLeftPixel = new Vector2(0, 16);
            sprite.pixelDimensions = new Vector2(16, 16);

            sprite.plane = SpriteBase.SPRITE_PLANE.XY;
            sprite.width = 18;
            sprite.height = 22;         //pronaci koje su meni ovo values
                                        //nema ovaj zvuk ubaciti
            this.explosionSound = ResourcesController.CreateAudioClip(Path.Combine(directoryPath, "sounds"), "emptyCan.wav");

            base.Awake();

            // Setup Variables
            this.bounceM = 0.2f;                 // promenio sa 0.3f
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
        }

        public override void ThrowGrenade(float XI, float YI, float newX, float newY, int _playerNum)
        {
            base.enabled = true;
            base.transform.parent = null;
            this.SetXY(newX, newY);
            if (Mathf.Abs(XI) > 100)
            {
                this.xI = Mathf.Sign(XI) * 300f;    //sta je ovo videti
                this.yI = 250f;
            }
            else
            {
                this.xI = XI;
                this.yI = YI;
            }

            this.playerNum = _playerNum;
            rigidbody.position = new Vector3(newX, newY);
            if (Mathf.Abs(xI) > 100)
            {
                this.rI = -Mathf.Sign(xI) * UnityEngine.Random.Range(20, 25);
            }
            else
            {
                this.rI = -Mathf.Sign(xI) * UnityEngine.Random.Range(10, 15);
            }
            rigidbody.AddForce(new Vector3(xI, yI, 0f), ForceMode.VelocityChange);
            rigidbody.AddTorque(new Vector3(0f, 0f, this.rI), ForceMode.VelocityChange);
            this.SetMinLife(0.7f);
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
            this.r = 0;
            this.life = 4f;                      //promenio sa 2f
            this.startLife = this.life;
            if (this.sprite != null)
            {
                this.spriteWidth = this.sprite.width;
                this.spriteHeight = this.sprite.height;
            }
            this.spriteWidthI = -this.spriteWidth / this.life * 1f;
            this.spriteHeightI = -this.spriteHeight / this.life * 1f;
            if (Mathf.Abs(xI) > 100)
            {
                this.rI = -Mathf.Sign(xI) * UnityEngine.Random.Range(20, 25);
            }
            else
            {
                this.rI = -Mathf.Sign(xI) * UnityEngine.Random.Range(10, 15);
            }
            this.SetPosition();
            if (!this.shrapnelControlsMotion && base.GetComponent<Rigidbody>() == null)
            {
                BoxCollider boxCollider = base.gameObject.GetComponent<BoxCollider>();
                if (boxCollider == null)
                {
                    boxCollider = base.gameObject.AddComponent<BoxCollider>();
                }
                boxCollider.size = new Vector3(20, 6, 6f);
                rigidbody = base.gameObject.AddComponent<Rigidbody>();
                rigidbody.AddForce(new Vector3(xI, yI, 0f), ForceMode.VelocityChange);
                rigidbody.constraints = (RigidbodyConstraints)56;
                rigidbody.maxAngularVelocity = float.MaxValue;
                rigidbody.AddTorque(new Vector3(0f, 0f, this.rI), ForceMode.VelocityChange); //u ovom rigidbody podesavati za fiziku i torque i to
                rigidbody.drag = 0.8f;
                rigidbody.angularDrag = 0.7f; //the higher the number brze ce se usporiti rotacija
                rigidbody.mass = 230;                           //ppromenjeno sa 200
                Quaternion rotation = rigidbody.rotation;
                rotation.eulerAngles = new Vector3(0f, 0f, 90f);
                rigidbody.rotation = rotation;
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
                    bool bounceX = false;
                    bool bounceY = false;
                    if (Map.ConstrainToBlocks(this, newX, newY, this.size, ref num3, ref num4, ref bounceX, ref bounceY, false))
                    {
                        this.Bounce(bounceX, bounceY);
                    }
                    newX += num3;
                    newY += num4;
                    xI = -xI * 0.6f;
                }
                if (xI < 0f && !Map.InsideWall(newX + 8f, newY, this.size, false))
                {
                    float num5 = -8f;
                    float num6 = 0f;
                    bool flag3 = false;
                    bool flag4 = false;
                    if (Map.ConstrainToBlocks(this, newX + 8f, newY, this.size, ref num5, ref num6, ref flag3, ref flag4, false))
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
                    bool bounceX2 = false;
                    bool bounceY2 = false;
                    if (Map.ConstrainToBlocks(this, newX, newY, this.size, ref num7, ref num8, ref bounceX2, ref bounceY2, false))
                    {
                        this.Bounce(bounceX2, bounceY2);
                    }
                    newX += num7;
                    newY += num8;
                    xI = -xI * 0.6f;
                }
            }
            this.SetPosition();
            sprite.offset = new Vector3(0f, -0.5f, 0f);
        }

        protected override bool Update()
        {
            bool retVal = base.Update();


            base.X = rigidbody.position.x;
            base.Y = rigidbody.position.y;
            return retVal;

            base.Update();
            this.HitProjectiles();
            return true;
        }

        protected override bool CanBounceOnEnemies()
        {
            return Mathf.Abs(this.xI) > 160f || this.yI < -120f;
        }

        public override void Death()
        {
            this.MakeEffects();
            this.DestroyGrenade();
        }

        protected override void RunWarnings() //sta je ovo videti da li treba da se override ili brise
        {
        }

        public static bool HitAllLivingUnits(MonoBehaviour damageSender, int playerNum, int damage, DamageType damageType, float xRange, float yRange, float x, float y, float xI, float yI, bool penetrates, bool knock)
        {
            if (Map.units == null)
            {
                return false;
            }
            bool result = false;
            for (int i = Map.units.Count - 1; i >= 0; i--)
            {
                Unit unit = Map.units[i];
                if (unit != null && playerNum != unit.playerNum && !unit.invulnerable && unit.health > 0)
                {
                    float f = unit.X - x;
                    if (Mathf.Abs(f) - xRange < unit.width)
                    {
                        float num = unit.Y + unit.height / 2f + 3f - y;
                        if (Mathf.Abs(num) - yRange < unit.height && (Demonstration.projectilesHitWalls || unit.health > 0))
                        {
                            if (num < -unit.height && unit.CanHeadShot())
                            {
                                Map.HeadShotUnit(damageSender, unit, ValueOrchestrator.GetModifiedDamage(damage, playerNum), damageType, xI, yI, (int)Mathf.Sign(xI), knock, x, y);
                            }
                            else
                            {
                                Map.KnockAndDamageUnit(damageSender, unit, ValueOrchestrator.GetModifiedDamage(damage, playerNum), damageType, xI, yI, (int)Mathf.Sign(xI), knock, x, y, false);
                            }
                            if (!penetrates)
                            {
                                return true;
                            }
                            result = true;
                        }
                    }
                }
            }
            return result;
        }

        public static bool HitAllLivingUnits(MonoBehaviour damageSender, int playerNum, int damage, DamageType damageType, float xRange, float yRange, float x, float y, float xI, float yI, bool penetrates, bool knock, List<Unit> alreadyHit)
        {
            if (Map.units == null)
            {
                return false;
            }
            bool result = false;
            for (int i = Map.units.Count - 1; i >= 0; i--)
            {
                Unit unit = Map.units[i];
                if (unit != null && playerNum != unit.playerNum && !unit.invulnerable && unit.health > 0 && !alreadyHit.Contains(unit))
                {
                    float f = unit.X - x;
                    if (Mathf.Abs(f) - xRange < unit.width)
                    {
                        float num = unit.Y + unit.height / 2f + 3f - y;
                        if (Mathf.Abs(num) - yRange < unit.height && (Demonstration.projectilesHitWalls || unit.health > 0))
                        {
                            alreadyHit.Add(unit);
                            if (num < -unit.height && unit.CanHeadShot())
                            {
                                Map.HeadShotUnit(damageSender, unit, ValueOrchestrator.GetModifiedDamage(damage, playerNum), damageType, xI, yI, (int)Mathf.Sign(xI), knock, x, y);
                            }
                            else
                            {
                                Map.KnockAndDamageUnit(damageSender, unit, ValueOrchestrator.GetModifiedDamage(damage, playerNum), damageType, xI, yI, (int)Mathf.Sign(xI), knock, x, y, false);
                            }
                            if (!penetrates)
                            {
                                return true;
                            }
                            result = true;
                        }
                    }
                }
            }
            return result;
        }

        public static bool KnockMooks(MonoBehaviour damageSender, DamageType damageType, float xRange, float yRange, float x, float y, float xI, float yI, bool penetrates, bool livingUnits, bool onlyGroundUnits = true)
        {
            if (Map.units == null)
            {
                return false;
            }
            bool result = false;
            for (int i = Map.units.Count - 1; i >= 0; i--)
            {
                Unit unit = Map.units[i];
                if (unit != null && unit.playerNum < 0 && !unit.invulnerable && (!livingUnits || unit.health > 0))
                {
                    float f = unit.X - x;
                    if (Mathf.Abs(f) - xRange < unit.width)
                    {
                        float f2 = unit.Y + unit.height / 2f + 3f - y;
                        if (Mathf.Abs(f2) - yRange < unit.height && (!onlyGroundUnits || (unit.IsOnGround() && unit.actionState != ActionState.Jumping && unit.actionState != ActionState.Fallen)))
                        {
                            Map.KnockAndDamageUnit(damageSender, unit, 0, damageType, xI, yI, (int)Mathf.Sign(xI), true, x, y, false);
                            unit.BackSomersault(true);
                            if (!penetrates)
                            {
                                return true;
                            }
                            result = true;
                        }
                    }
                }
            }
            return result;
        }

        // Token: 0x060050DF RID: 20703 RVA: 0x00254B08 File Offset: 0x00252D08
        public static bool KnockUnits(MonoBehaviour damageSender, DamageType damageType, float xRange, float yRange, float x, float y, float xI, float yI, bool penetrates, bool livingUnits, bool onlyGroundUnits = true)
        {
            if (Map.units == null)
            {
                return false;
            }
            bool result = false;
            for (int i = Map.units.Count - 1; i >= 0; i--)
            {
                Unit unit = Map.units[i];
                if (unit != null && !unit.invulnerable && (!livingUnits || unit.health > 0))
                {
                    float f = unit.X - x;
                    if (Mathf.Abs(f) - xRange < unit.width)
                    {
                        float f2 = unit.Y + unit.height / 2f + 3f - y;
                        if (Mathf.Abs(f2) - yRange < unit.height && (!onlyGroundUnits || (unit.IsOnGround() && unit.actionState != ActionState.Jumping && unit.actionState != ActionState.Fallen)))
                        {
                            Map.KnockAndDamageUnit(damageSender, unit, 0, damageType, xI, yI, (int)Mathf.Sign(xI), true, x, y, false);
                            unit.BackSomersault(true);
                            if (!penetrates)
                            {
                                return true;
                            }
                            result = true;
                        }
                    }
                }
            }
            return result;
        }

        public static bool HitUnits(MonoBehaviour damageSender, int playerNum, int damage, int corpseDamage, DamageType damageType, float xRange, float yRange, float x, float y, float xI, float yI, bool penetrates, bool knock, bool canGib, List<Unit> alreadyHitUnits, bool ignoreDeadUnits, bool canHeadshot)
        {
            if (Map.units == null)
            {
                return false;
            }
            bool result = false;
            bool flag = false;
            int num = 999999;
            for (int i = Map.units.Count - 1; i >= 0; i--)
            {
                Unit unit = Map.units[i];
                if (unit != null && GameModeController.DoesPlayerNumDamage(playerNum, unit.playerNum) && !unit.invulnerable && unit.health <= num)
                {
                    if (!ignoreDeadUnits || unit.health > 0)
                    {
                        float f = unit.X - x;
                        if (Mathf.Abs(f) - xRange < unit.width)
                        {
                            float num2 = unit.Y + unit.height / 2f + 3f - y;
                            if (Mathf.Abs(num2) - yRange < unit.height && !alreadyHitUnits.Contains(unit))
                            {
                                alreadyHitUnits.Add(unit);
                                if (!penetrates && unit.health > 0)
                                {
                                    num = 0;
                                    flag = true;
                                }
                                if (canHeadshot && unit.health > 0 && num2 < -unit.height && unit.CanHeadShot())
                                {
                                    Map.HeadShotUnit(damageSender, unit, ValueOrchestrator.GetModifiedDamage(damage, playerNum), damageType, xI, yI, (int)Mathf.Sign(xI), knock, x, y);
                                }
                                else if (!canGib && unit.health <= 0)
                                {
                                    Map.KnockAndDamageUnit(damageSender, unit, 0, damageType, xI, yI, (int)Mathf.Sign(xI), knock, x, y, false);
                                }
                                else if (unit.health <= 0)
                                {
                                    Map.KnockAndDamageUnit(damageSender, unit, ValueOrchestrator.GetModifiedDamage(corpseDamage, playerNum), damageType, xI, yI, (int)Mathf.Sign(xI), knock, x, y, false);
                                }
                                else
                                {
                                    Map.KnockAndDamageUnit(damageSender, unit, ValueOrchestrator.GetModifiedDamage(damage, playerNum), damageType, xI, yI, (int)Mathf.Sign(xI), knock, x, y, false);
                                }
                                result = true;
                                if (flag)
                                {
                                    return result;
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        // Token: 0x060050E8 RID: 20712 RVA: 0x002554F8 File Offset: 0x002536F8
        public static bool HitUnits(MonoBehaviour damageSender, int playerNum, int damage, int corpseDamage, DamageType damageType, float xRange, float yRange, float x, float y, float xI, float yI, bool penetrates, bool knock, bool canGib, List<BroforceObject> alreadyHitObjects, bool canHeadshot = false)
        {
            if (Map.units == null)
            {
                return false;
            }
            bool result = false;
            bool flag = false;
            int num = 999999;
            for (int i = Map.units.Count - 1; i >= 0; i--)
            {
                Unit unit = Map.units[i];
                if (unit != null && GameModeController.DoesPlayerNumDamage(playerNum, unit.playerNum) && !unit.invulnerable && unit.health <= num)
                {
                    float f = unit.X - x;
                    if (Mathf.Abs(f) - xRange < unit.width)
                    {
                        float num2 = unit.Y + unit.height / 2f + 3f - y;
                        if (Mathf.Abs(num2) - yRange < unit.height && !alreadyHitObjects.Contains(unit))
                        {
                            alreadyHitObjects.Add(unit);
                            if (!penetrates && unit.health > 0)
                            {
                                num = 0;
                                flag = true;
                            }
                            if (canHeadshot && unit.health > 0 && num2 < -unit.height && unit.CanHeadShot())
                            {
                                Map.HeadShotUnit(damageSender, unit, ValueOrchestrator.GetModifiedDamage(damage, playerNum), damageType, xI, yI, (int)Mathf.Sign(xI), knock, x, y);
                            }
                            else if (!canGib && unit.health <= 0)
                            {
                                Map.KnockAndDamageUnit(damageSender, unit, 0, damageType, xI, yI, (int)Mathf.Sign(xI), knock, x, y, false);
                            }
                            else if (unit.health <= 0)
                            {
                                Map.KnockAndDamageUnit(damageSender, unit, ValueOrchestrator.GetModifiedDamage(corpseDamage, playerNum), damageType, xI, yI, (int)Mathf.Sign(xI), knock, x, y, false);
                            }
                            else
                            {
                                Map.KnockAndDamageUnit(damageSender, unit, ValueOrchestrator.GetModifiedDamage(damage, playerNum), damageType, xI, yI, (int)Mathf.Sign(xI), knock, x, y, false);
                            }
                            result = true;
                            if (flag)
                            {
                                return result;
                            }
                        }
                    }
                }
            }
            return result;
        }

        public static bool HitUnits(MonoBehaviour damageSender, int damage, DamageType damageType, float range, float x, float y, float xI, float yI, bool penetrates, bool knock)
        {
            return Map.HitUnits(damageSender, damage, damageType, range, range, x, y, xI, yI, penetrates, knock);
        }

        // Token: 0x060050F1 RID: 20721 RVA: 0x00255D70 File Offset: 0x00253F70
        public static bool HitUnits(MonoBehaviour damageSender, int damage, DamageType damageType, float xRange, float yRange, float x, float y, float xI, float yI, bool penetrates, bool knock)
        {
            if (Map.units == null)
            {
                return false;
            }
            bool result = false;
            int num = 999999;
            for (int i = Map.units.Count - 1; i >= 0; i--)
            {
                Unit unit = Map.units[i];
                if (unit != null && !unit.invulnerable && unit.health <= num && unit != damageSender)
                {
                    float f = unit.X - x;
                    if (Mathf.Abs(f) - xRange < unit.width)
                    {
                        float f2 = unit.Y + unit.height / 2f + 3f - y;
                        if (Mathf.Abs(f2) - yRange < unit.height)
                        {
                            Map.KnockAndDamageUnit(damageSender, unit, damage, damageType, xI, yI, (int)Mathf.Sign(xI), knock, x, y, false);
                            if (!penetrates && unit.health > 0)
                            {
                                num = 0;
                            }
                            result = true;
                        }
                    }
                }
            }
            return result;
        }

        // Token: 0x060050F2 RID: 20722 RVA: 0x00255E70 File Offset: 0x00254070
        public static bool HitUnits(MonoBehaviour damageSender, int damage, DamageType damageType, float range, float x, float y, float xI, float yI, bool penetrates, bool knock, ref BloodColor bloodColor)
        {
            return Map.HitUnits(damageSender, damage, damageType, range, range, x, y, xI, yI, penetrates, knock, ref bloodColor);
        }

        // Token: 0x060050F3 RID: 20723 RVA: 0x00255E98 File Offset: 0x00254098
        public static bool HitUnits(MonoBehaviour damageSender, int damage, DamageType damageType, float xRange, float yRange, float x, float y, float xI, float yI, bool penetrates, bool knock, ref BloodColor bloodColor)
        {
            if (Map.units == null)
            {
                return false;
            }
            bool result = false;
            int num = 999999;
            for (int i = Map.units.Count - 1; i >= 0; i--)
            {
                Unit unit = Map.units[i];
                if (unit != null && !unit.invulnerable && unit.health <= num)
                {
                    float f = unit.X - x;
                    if (Mathf.Abs(f) - xRange < unit.width)
                    {
                        float f2 = unit.Y + unit.height / 2f + 3f - y;
                        if (Mathf.Abs(f2) - yRange < unit.height)
                        {
                            Map.KnockAndDamageUnit(damageSender, unit, damage, damageType, xI, yI, (int)Mathf.Sign(xI), knock, x, y, false);
                            if (!penetrates && unit.health > 0)
                            {
                                num = 0;
                            }
                            bloodColor = unit.bloodColor;
                            result = true;
                        }
                    }
                }
            }
            return result;
        }

        // Token: 0x0600510E RID: 20750 RVA: 0x00257860 File Offset: 0x00255A60
        public static void AttractAliens(float x, float y, float xRange, float yRange)
        {
            if (Map.units == null)
            {
                return;
            }
            for (int i = Map.units.Count - 1; i >= 0; i--)
            {
                Unit unit = Map.units[i];
                if (unit != null && (unit.playerNum == -2 || unit is Alien) && !unit.invulnerable)
                {
                    float num = unit.X - x;
                    if (Mathf.Abs(num) - xRange < unit.width && (unit.Y != y || num != 0f))
                    {
                        float f = unit.Y + unit.height / 2f + 3f - y;
                        if (Mathf.Abs(f) - yRange < unit.height)
                        {
                            unit.Attract(x, y);
                        }
                    }
                }
            }
        }

        public static void AttractMooks(float x, float y, float xRange, float yRange)
        {
            if (Map.units == null)
            {
                return;
            }
            for (int i = Map.units.Count - 1; i >= 0; i--)
            {
                Unit unit = Map.units[i];
                if (unit != null && unit.playerNum < 0 && !unit.invulnerable)
                {
                    float num = unit.X - x;
                    if (Mathf.Abs(num) - xRange < unit.width && (unit.Y != y || num != 0f))
                    {
                        float f = unit.Y + unit.height / 2f + 3f - y;
                        if (Mathf.Abs(f) - yRange < unit.height)
                        {
                            unit.Attract(x, y);
                        }
                    }
                }
            }
        }

        // Token: 0x0600510C RID: 20748 RVA: 0x002576C0 File Offset: 0x002558C0
        public static void StunMooks(float x, float y, float xRange, float yRange, float time)
        {
            if (Map.units == null)
            {
                return;
            }
            for (int i = Map.units.Count - 1; i >= 0; i--)
            {
                Unit unit = Map.units[i];
                if (unit != null && unit.playerNum < 0 && !unit.invulnerable)
                {
                    float num = unit.X - x;
                    if (Mathf.Abs(num) - xRange < unit.width && (unit.Y != y || num != 0f))
                    {
                        float f = unit.Y + unit.height / 2f + 3f - y;
                        if (Mathf.Abs(f) - yRange < unit.height)
                        {
                            unit.Stun(time);
                        }
                    }
                }
            }
        }

        // Token: 0x0600510F RID: 20751 RVA: 0x0025793C File Offset: 0x00255B3C
        public static void AlertNearbyMooks(float x, float y, float xRange, float yRange, int playerNum, GridPoint startPoint)
        {
            if (Map.units == null)
            {
                return;
            }
            for (int i = Map.units.Count - 1; i >= 0; i--)
            {
                Unit unit = Map.units[i];
                if (unit != null && unit.playerNum < 0 && !unit.invulnerable)
                {
                    float num = unit.X - x;
                    if (Mathf.Abs(num) - xRange < unit.width && (unit.Y != y || num != 0f))
                    {
                        float f = unit.Y + unit.height / 2f + 3f - y;
                        if (Mathf.Abs(f) - yRange < unit.height)
                        {
                            unit.FullyAlert(x, y, playerNum);
                        }
                    }
                }
            }
        }

        protected virtual void HitProjectiles()
        {
            this.hitProjectileDelay -= this.t;
            if (this.hitProjectileDelay <= 0f && Map.HitProjectiles(this.playerNum, this.damage, this.damageType, 4f, base.X, base.Y, this.xI, this.yI, 0.1f))
            {
                this.yI += 50f;
                this.hitProjectileDelay = 0.204f;
                EffectsController.CreateProjectilePopEffect(base.X, base.Y);
            }
        }
                
        protected override void MakeEffects()
        {
            if (this.sound == null)
            {
                this.sound = Sound.GetInstance();
            }
            if (this.sound != null)
            {                                   //sta je dobro za replace ovoga explosionSOund
                this.sound.PlaySoundEffectAt(this.explosionSound, 0.4f, base.transform.position, 1f, true, false, false, 0f);
            }

            //MapController.BurnUnitsAround_NotNetworked(this, -15, 5, 160f, X, Y, true, true); //pre neli alert units ako ima mada je to mozda gore
            //Map.HitProjectiles(base.playerNum, 15, DamageType.Explosion, 80f, X, Y, 0f, 0f, 0.25f); //sta je ovo hitprojectiles definitivno ne explosion

        }
    }
}