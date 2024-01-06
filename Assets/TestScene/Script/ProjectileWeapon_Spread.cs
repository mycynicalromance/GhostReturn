using UnityEngine;
using System.Collections;
using MoreMountains.Tools;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;

namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// A weapon class aimed specifically at allowing the creation of various projectile weapons, from shotgun to machine gun, via plasma gun or rocket launcher
    /// </summary>
    [MMHiddenProperties("WeaponOnMissFeedback", "ApplyRecoilOnHitDamageable", "ApplyRecoilOnHitNonDamageable", "ApplyRecoilOnHitNothing", "ApplyRecoilOnKill")]
    [AddComponentMenu("Corgi Engine/Weapons/Projectile Weapon")]
    public class ProjectileWeapon_Spread : Weapon
    {
        [MMInspectorGroup("Projectile Spawn", true, 65)]

        /// the transform to use as the center reference point of the spawn
        [Tooltip("the transform to use as the center reference point of the spawn")]
        public Transform ProjectileSpawnTransform;
        /// the offset position at which the projectile will spawn
        [Tooltip("the offset position at which the projectile will spawn")]
        public Vector3 ProjectileSpawnOffset = Vector3.zero;
        /// the number of projectiles to spawn per shot
        [Tooltip("the number of projectiles to spawn per shot")]
        public int ProjectilesPerShot = 1;
        /// the spread (in degrees) to apply randomly (or not) on each angle when spawning a projectile
        [Tooltip("the spread (in degrees) to apply randomly (or not) on each angle when spawning a projectile")]
        public Vector3 Spread = Vector3.zero;
        /// whether or not the weapon should rotate to align with the spread angle
        [Tooltip("whether or not the weapon should rotate to align with the spread angle")]
        public bool RotateWeaponOnSpread = true;
        /// whether or not the spread should be random (if not it'll be equally distributed)
        [Tooltip("whether or not the spread should be random (if not it'll be equally distributed)")]
        public bool RandomSpread = true;
        /// the object pooler used to spawn projectiles, if left empty, this component will try to find one on its game object
        [Tooltip("the object pooler used to spawn projectiles, if left empty, this component will try to find one on its game object")]
        public MMObjectPooler ObjectPooler;
        /// the local position at which this projectile weapon should spawn projectiles
        [MMReadOnly]
        [Tooltip("the local position at which this projectile weapon should spawn projectiles")]
        public Vector3 SpawnPosition = Vector3.zero;
        protected Vector3 _flippedProjectileSpawnOffset;
        protected Vector3 _randomSpreadDirection;
        protected Vector3 _spawnPositionCenter;
        protected bool _poolInitialized = false;

        //반동에 의한 탄퍼짐 변수들
        
        [SerializeField] private float recoilSpreadDuration;
        [SerializeField] private float recoilSpreadAngle;
        [SerializeField] private float recoilCursorUpAngle;
        private bool isDuringAimSpread=false;
        private float aimSpreadDuration;

        /// <summary>
        /// Initialize this weapon
        /// </summary>
        public override void Initialization()
        {
            base.Initialization();
            _aimableWeapon = GetComponent<WeaponAim>();
            if (!_poolInitialized)
            {
                if (ObjectPooler == null)
                {
                    ObjectPooler = GetComponent<MMObjectPooler>();
                }
                if (ObjectPooler == null)
                {
                    Debug.LogWarning(this.name + " : no object pooler (simple or multiple) is attached to this Projectile Weapon, it won't be able to shoot anything.");
                    return;
                }

                _flippedProjectileSpawnOffset = ProjectileSpawnOffset;
                _flippedProjectileSpawnOffset.y = -_flippedProjectileSpawnOffset.y;
                _poolInitialized = true;
            }
        }
        
        protected override void LateUpdate()
        {
            base.LateUpdate();
            //탄 퍼짐에 의한 shoot angle을 결정해주자.
            DetermineShootAngle();
        }

        protected virtual void DetermineShootAngle(){
            //isDuringAimSpread 인 경우에만 aimSpreadAngle이 0도가 아니게 됨.
            if(!isDuringAimSpread){
                return;
            }

            spreadElapsedTime+=Time.deltaTime;

            //spreadTimeParameter는, 경과 시간을 나타내는 0~1 사이 지표로 탄퍼짐 시작한 직후는 0, 탄퍼짐 종료시에는 1이다. 
            float spreadTimeParameter = spreadElapsedTime / aimSpreadDuration;


            //탄퍼짐 발생 이후 경과시간이 총 duration을 넘으면, 탄퍼짐 활성을 종료한다.
            if (spreadElapsedTime > aimSpreadDuration){
                isDuringAimSpread = false;
                spreadElapsedTime=0f;
                Spread.z = 0;
                return;
            }
            else {
                //탄퍼짐 중이라면, 탄퍼짐 각도를 경과시간에 맞춰 조정한다.
                Spread.z = (1-spreadTimeParameter) * recoilSpreadAngle;
            }


        }
        /// <summary>
        /// Called everytime the weapon is used
        /// </summary>
        /// 
        
        //탄퍼짐이 시작한 이후로, 경과시간
        private float spreadElapsedTime;

        //현재 탄퍼짐의 각도
        private float nowSpreadAngle;

        //탄퍼짐 시간동안 duringAimSpread를 true로 만들어 Update에서 탄퍼짐이 활성화되게 한다.
        private void StartAimSpread(float spreadangle, float duration){
            isDuringAimSpread=true;
            spreadElapsedTime=0f;
            aimSpreadDuration=duration;
            nowSpreadAngle=spreadangle;
        }
        protected override void WeaponUse()
        {
            base.WeaponUse();
            //반동에 의한 탄퍼짐을 활성화 시킨다.
            StartAimSpread(recoilSpreadAngle,recoilSpreadDuration);
            MouseUpByRecoil();


            DetermineSpawnPosition();

            for (int i = 0; i < ProjectilesPerShot; i++)
            {
                SpawnProjectile(SpawnPosition, i, ProjectilesPerShot, true);
            }
        }

        protected virtual void MouseUpByRecoil()
        {
            Vector2 nowMousePosition = Input.mousePosition;
            //현재 마우스와 Weapon 사이 x좌표 차
            float xMouseToWeapon = nowMousePosition.x - transform.position.x;
            float angleByCursorAndGround = Mathf.Atan(nowMousePosition.y / nowMousePosition.x);
            //Mathf.Tan은 라디안 기준으로 각을 입력받기에, 각을 라디안으로 환산 시켜서 대입해줌.
            float deltaRecoildMouseMove = xMouseToWeapon * (Mathf.Tan(angleByCursorAndGround + recoilCursorUpAngle * MathF.PI / 180) - Mathf.Tan(angleByCursorAndGround));
            //커서가 목표한 반동각도만큼 상승.
            Mouse.current.WarpCursorPosition(nowMousePosition + new Vector2(0, deltaRecoildMouseMove));
        }
        /// <summary>
        /// Spawns a new object and positions/resizes it
        /// </summary>
        public virtual GameObject SpawnProjectile(Vector3 spawnPosition, int projectileIndex, int totalProjectiles, bool triggerObjectActivation = true)
        {
            /// we get the next object in the pool and make sure it's not null
            GameObject nextGameObject = ObjectPooler.GetPooledGameObject();

            // mandatory checks
            if (nextGameObject == null) { return null; }
            if (nextGameObject.GetComponent<MMPoolableObject>() == null)
            {
                throw new Exception(gameObject.name + " is trying to spawn objects that don't have a PoolableObject component.");
            }
            // we position the object
            nextGameObject.transform.position = spawnPosition;
            // we set its direction

            Projectile projectile = nextGameObject.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.SetWeapon(this);
                if (Owner != null)
                {
                    projectile.SetOwner(Owner.gameObject);
                }
            }
            // we activate the object
            nextGameObject.gameObject.SetActive(true);


            if (projectile != null)
            {
                if (RandomSpread)
                {
                    _randomSpreadDirection.x = UnityEngine.Random.Range(-Spread.x, Spread.x);
                    _randomSpreadDirection.y = UnityEngine.Random.Range(-Spread.y, Spread.y);
                    _randomSpreadDirection.z = UnityEngine.Random.Range(-Spread.z, Spread.z);
                }
                else
                {
                    if (totalProjectiles > 1)
                    {
                        _randomSpreadDirection.x = MMMaths.Remap(projectileIndex, 0, totalProjectiles - 1, -Spread.x, Spread.x);
                        _randomSpreadDirection.y = MMMaths.Remap(projectileIndex, 0, totalProjectiles - 1, -Spread.y, Spread.y);
                        _randomSpreadDirection.z = MMMaths.Remap(projectileIndex, 0, totalProjectiles - 1, -Spread.z, Spread.z);
                    }
                    else
                    {
                        _randomSpreadDirection = Vector3.zero;
                    }
                }

                Quaternion spread = Quaternion.Euler(_randomSpreadDirection);
                bool facingRight = (Owner == null) || Owner.IsFacingRight;
                projectile.SetDirection(spread * transform.right * (Flipped ? -1 : 1), transform.rotation, facingRight);
                if (RotateWeaponOnSpread)
                {
                    this.transform.rotation = this.transform.rotation * spread;
                }
            }

            if (triggerObjectActivation)
            {
                if (nextGameObject.GetComponent<MMPoolableObject>() != null)
                {
                    nextGameObject.GetComponent<MMPoolableObject>().TriggerOnSpawnComplete();
                }
            }

            return (nextGameObject);
        }

        /// <summary>
        /// Determines the spawn position based on the spawn offset and whether or not the weapon is flipped
        /// </summary>
        public virtual void DetermineSpawnPosition()
        {
            _spawnPositionCenter = (ProjectileSpawnTransform == null) ? this.transform.position : ProjectileSpawnTransform.transform.position;

            if (Flipped && FlipWeaponOnCharacterFlip)
            {
                SpawnPosition = _spawnPositionCenter - this.transform.rotation * _flippedProjectileSpawnOffset;
            }
            else
            {
                SpawnPosition = _spawnPositionCenter + this.transform.rotation * ProjectileSpawnOffset;
            }
        }

        /// <summary>
        /// When the weapon is selected, draws a circle at the spawn's position
        /// </summary>
        protected virtual void OnDrawGizmosSelected()
        {
            DetermineSpawnPosition();

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(SpawnPosition, 0.2f);
        }
    }
}
