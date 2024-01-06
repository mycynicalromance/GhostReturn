using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using MoreMountains.Tools;
using System;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// Add this component to a Weapon and you'll be able to aim it (meaning you'll rotate it)
    /// Supported control modes are mouse, primary movement (you aim wherever you direct your character) and secondary movement (using a secondary axis, separate from the movement).
    /// </summary>
    [RequireComponent(typeof(Weapon))]
    [AddComponentMenu("Corgi Engine/Weapons/Weapon Aim")]
    public class WeaponAim_Spread : CorgiMonoBehaviour
    {
        /// the list of possible control modes
        public enum AimControls { Off, PrimaryMovement, SecondaryMovement, Mouse, Script }
        /// the list of possible rotation modes
        public enum RotationModes { Free, Strict4Directions, Strict8Directions }

        [Header("Control Mode")]
        [MMInformation("Add this component to a Weapon and you'll be able to aim (rotate) it. It supports three different control modes : mouse (the weapon aims towards the pointer), primary movement (you'll aim towards the current input direction), or secondary movement (aims towards a second input axis, think twin stick shooters).", MoreMountains.Tools.MMInformationAttribute.InformationType.Info, false)]

        /// the aim control mode of choice (off : no control, primary movement (typically your left stick), secondary  (right stick), mouse, script : when you want a script to drive your aim (typically for AI, but not only)
        [Tooltip("the aim control mode of choice (off : no control, primary movement (typically your left stick), secondary  (right stick), mouse, script : when you want a script to drive your aim (typically for AI, but not only)")]
        public AimControls AimControl = AimControls.SecondaryMovement;

        [Header("Weapon Rotation")]
        [MMInformation("Here you can define whether the rotation is free, strict in 4 directions (top, bottom, left, right), or 8 directions (same + diagonals). You can also define a rotation speed, and a min and max angle. For example, if you don't want your character to be able to aim in its back, set min angle to -90 and max angle to 90.", MoreMountains.Tools.MMInformationAttribute.InformationType.Info, false)]

        /// the rotation mode
        [Tooltip("the rotation mode")]
        public RotationModes RotationMode = RotationModes.Free;
        /// the the speed at which the weapon reaches its new position. Set it to zero if you want movement to directly follow input
        [Tooltip("the the speed at which the weapon reaches its new position. Set it to zero if you want movement to directly follow input")]
        public float WeaponRotationSpeed = 1f;
        /// if this is true, a flip will be instant, regardless of the weapon rotation speed
        [Tooltip("if this is true, a flip will be instant, regardless of the weapon rotation speed")]
        public bool InstantFlip = false;
        /// if this is true, you won't be able to aim this weapon's aim while it's in use
        [Tooltip("if this is true, you won't be able to aim this weapon's aim while it's in use")]
        public bool PreventAimWhileWeaponIsInUse = false;
        /// the minimum angle at which the weapon's rotation will be clamped
        [Range(-180, 180)]
        [Tooltip("the minimum angle at which the weapon's rotation will be clamped")]
        public float MinimumAngle = -180f;
        /// the maximum angle at which the weapon's rotation will be clamped
        [Range(-180, 180)]
        [Tooltip("the maximum angle at which the weapon's rotation will be clamped")]
        public float MaximumAngle = 180f;
        /// if this is true, slope rotation will be ignored
        [Tooltip("if this is true, slope rotation will be ignored")]
        public bool IgnoreSlopeRotation = false;
        /// if this is true, aiming down won't impact this WeaponAim, similar to how Contra games do it
        [Tooltip("if this is true, aiming down won't impact this WeaponAim, similar to how Contra games do it")]
        [MMEnumCondition("AimControl", (int)AimControls.PrimaryMovement)]
        public bool IgnoreDownWhenGrounded = false;

        [Header("Reticle")]
        [MMInformation("You can also display a reticle on screen to check where you're aiming at. Leave it blank if you don't want to use one. If you set the reticle distance to 0, it'll follow the cursor, otherwise it'll be on a circle centered on the weapon. You can also ask it to follow the mouse, even replace the mouse pointer. You can also decide if the pointer should rotate to reflect aim angle or remain stable.", MoreMountains.Tools.MMInformationAttribute.InformationType.Info, false)]

        /// the gameobject to display as the aim's reticle/crosshair. Leave it blank if you don't want a reticle
        [Tooltip("the gameobject to display as the aim's reticle/crosshair. Leave it blank if you don't want a reticle")]
        public GameObject Reticle;
        /// if set to false, the reticle won't be added and displayed
        [Tooltip("if set to false, the reticle won't be added and displayed")]
        public bool DisplayReticle = true;
        /// the distance at which the reticle will be from the weapon
        [Tooltip("the distance at which the reticle will be from the weapon")]
        [MMCondition("DisplayReticle")]
        public float ReticleDistance;
        /// the z position of the reticle
        [Tooltip("the z position of the reticle")]
        [MMCondition("DisplayReticle")]
        public float ReticleZPosition;
        /// if set to true, the reticle will be placed at the mouse's position (like a pointer)
        [Tooltip("if set to true, the reticle will be placed at the mouse's position (like a pointer)")]
        [MMCondition("DisplayReticle")]
        public bool ReticleAtMousePosition;
        /// if set to true, the reticle will rotate on itself to reflect the weapon's rotation. If not it'll remain stable.
        [Tooltip("if set to true, the reticle will rotate on itself to reflect the weapon's rotation. If not it'll remain stable.")]
        [MMCondition("DisplayReticle")]
        public bool RotateReticle = false;
        /// if set to true, the reticle will replace the mouse pointer
        [Tooltip("if set to true, the reticle will replace the mouse pointer")]
        [MMCondition("DisplayReticle")]
        public bool ReplaceMousePointer = true;
        /// whether or not the reticle should be hidden when the character is dead
        [Tooltip("whether or not the reticle should be hidden when the character is dead")]
        [MMCondition("DisplayReticle")]
        public bool DisableReticleOnDeath = true;

        [Header("Recoil")]
        public bool Recoil;
        [MMCondition("Recoil")]
        public float recoilDegree;
        [MMCondition("Recoil")]
        public float recoilDuration;
        [Tooltip("It determines that how fast weapon rotates in Recoil")]
        [MMCondition("Recoil")]
        public float recoilRotateSpeed;
        [MMCondition("Recoil")]
        public float recoilCursorDegree;

        

        /// the weapon's current rotation
        public Quaternion CurrentRotation { get { return transform.rotation; } }
        /// the current angle the weapon is aiming at
        public float CurrentAngle { get; protected set; }
        /// the current angle the weapon is aiming at, adjusted to compensate for the current orientation of the character
        public float CurrentAngleRelative
        {
            get
            {
                if (_weapon != null)
                {
                    if (_weapon.Owner != null)
                    {
                        if (_weapon.Owner.IsFacingRight)
                        {
                            return CurrentAngle;
                        }
                        else
                        {
                            return -CurrentAngle;
                        }
                    }
                }
                return 0;
            }
        }



        public Vector2 CurrentAimMultiplier { get; set; }

        protected Weapon _weapon;
        protected List<Weapon> _weapons;
        protected Vector3 _currentAim = Vector3.zero;
        protected Quaternion _lookRotation;
        protected Vector3 _direction;
        protected float[] _possibleAngleValues;
        protected Vector3 _mousePosition;
        protected float _additionalAngle;
        protected Quaternion _initialRotation;
        protected Camera _mainCamera;
        protected CharacterGravity _characterGravity;
        protected CorgiController _controller;
        protected Vector3 _reticlePosition;
        protected bool WasFacingRightLastFrame;
        protected GameObject _reticle;
        protected Vector2 _regularHorizontalAimMultiplier = new Vector2(1f, 1f);

        /// <summary>
        /// On Start(), we trigger the initialization
        /// </summary>
        protected virtual void Start()
        {
            Initialization();
        }

        /// <summary>
        /// Grabs the weapon component, initializes the angle values
        /// </summary>
        protected virtual void Initialization()
        {
            //정재헌이 추가한 부분
            isFirstProcessingRecoil = false;
            isProcessingRecoil = false;
            //

            _weapon = this.gameObject.GetComponent<Weapon>();
            _weapons = this.gameObject.GetComponents<Weapon>().ToList();

            if (_weapon.Owner != null)
            {
                _characterGravity = _weapon.Owner?.FindAbility<CharacterGravity>();
                _controller = _weapon.Owner.GetComponent<CorgiController>();
            }

            if (RotationMode == RotationModes.Strict4Directions)
            {
                _possibleAngleValues = new float[5];
                _possibleAngleValues[0] = -180f;
                _possibleAngleValues[1] = -90f;
                _possibleAngleValues[2] = 0f;
                _possibleAngleValues[3] = 90f;
                _possibleAngleValues[4] = 180f;
            }
            if (RotationMode == RotationModes.Strict8Directions)
            {
                _possibleAngleValues = new float[9];
                _possibleAngleValues[0] = -180f;
                _possibleAngleValues[1] = -135f;
                _possibleAngleValues[2] = -90f;
                _possibleAngleValues[3] = -45f;
                _possibleAngleValues[4] = 0f;
                _possibleAngleValues[5] = 45f;
                _possibleAngleValues[6] = 90f;
                _possibleAngleValues[7] = 135f;
                _possibleAngleValues[8] = 180f;
            }
            _initialRotation = transform.rotation;
            InitializeReticle();
            _mainCamera = Camera.main;

            if ((_weapon.Owner.LinkedInputManager == null) && (AimControl == AimControls.PrimaryMovement))
            {
                Debug.LogError("You've set the WeaponAim on " + this.name + " to be driven by PrimaryMovement, yet it's either driven by an AI or doesn't have an associated InputManager. Maybe you meant to have a Script AimControl instead.");
            }
            if ((_weapon.Owner.LinkedInputManager == null) && (AimControl == AimControls.SecondaryMovement))
            {
                Debug.LogError("You've set the WeaponAim on " + this.name + " to be driven by SecondaryMovement, yet it's either driven by an AI or doesn't have an associated InputManager. Maybe you meant to have a Script AimControl instead.");
            }
        }

        /// <summary>
        /// Aims the weapon towards a new point
        /// </summary>
        /// <param name="newAim">New aim.</param>
        public virtual void SetCurrentAim(Vector3 newAim)
        {
            _currentAim = newAim;
        }

        /// <summary>
        /// Computes the current aim direction
        /// </summary>
        protected virtual void GetCurrentAim()
        {
            if (_weapon.Owner == null)
            {
                return;
            }

            if ((_weapon.Owner.LinkedInputManager == null) && (_weapon.Owner.CharacterType == Character.CharacterTypes.Player))
            {
                return;
            }

            switch (AimControl)
            {
                case AimControls.Off:
                    if (_weapon.Owner == null) { return; }

                    _currentAim = Vector2.right;
                    _direction = Vector2.right;
                    if (_characterGravity != null)
                    {
                        _currentAim = _characterGravity.transform.right * CurrentAimMultiplier;
                        _direction = _characterGravity.transform.right * CurrentAimMultiplier;
                    }
                    break;

                case AimControls.Script:
                    _currentAim = (_weapon.Owner.IsFacingRight) ? _currentAim : -_currentAim;
                    _direction = -(transform.position - _currentAim);
                    break;

                case AimControls.PrimaryMovement:
                    if ((_weapon.Owner == null) || (_weapon.Owner.LinkedInputManager == null))
                    {
                        return;
                    }

                    bool contraMode = (IgnoreDownWhenGrounded && (_controller != null) && (_controller.State.IsGrounded));

                    if (_weapon.Owner.IsFacingRight)
                    {
                        _currentAim = _weapon.Owner.LinkedInputManager.PrimaryMovement;
                        _currentAim *= CurrentAimMultiplier;
                        _direction = transform.position + _currentAim;
                        if (contraMode && (_currentAim.y < 0))
                        {
                            _currentAim.y = 0;
                            _direction.y = 0;
                        }
                    }
                    else
                    {
                        _currentAim = -_weapon.Owner.LinkedInputManager.PrimaryMovement;
                        _currentAim *= CurrentAimMultiplier;
                        _direction = -(transform.position - _currentAim);
                        if (contraMode && (_currentAim.y > 0))
                        {
                            _currentAim.y = 0;
                            _direction.y = 0;
                        }
                    }

                    if (_characterGravity != null)
                    {
                        _currentAim = MMMaths.RotateVector2(_currentAim, _characterGravity.GravityAngle);
                        if (_characterGravity.ShouldReverseInput())
                        {
                            _currentAim = -_currentAim;
                        }
                    }
                    break;

                case AimControls.SecondaryMovement:
                    if ((_weapon.Owner == null) || (_weapon.Owner.LinkedInputManager == null))
                    {
                        return;
                    }

                    if (_weapon.Owner.IsFacingRight)
                    {
                        _currentAim = _weapon.Owner.LinkedInputManager.SecondaryMovement;
                        _currentAim *= CurrentAimMultiplier;
                        _direction = transform.position + _currentAim;
                    }
                    else
                    {
                        _currentAim = -_weapon.Owner.LinkedInputManager.SecondaryMovement;
                        _currentAim *= CurrentAimMultiplier;
                        _direction = -(transform.position - _currentAim);
                    }
                    break;

                case AimControls.Mouse:
                    if (_weapon.Owner == null)
                    {
                        return;
                    }

#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
                    _mousePosition = Input.mousePosition;
#else
					_mousePosition = Mouse.current.position.ReadValue();
#endif
                    _mousePosition.z = _mainCamera.transform.position.z * -1;

                    _direction = _mainCamera.ScreenToWorldPoint(_mousePosition);
                    _direction.z = transform.position.z;

                    if (_weapon.Owner.IsFacingRight)
                    {
                        _currentAim = _direction - transform.position;
                    }
                    else
                    {
                        _currentAim = transform.position - _direction;
                    }
                    break;
            }
        }

        /// <summary>
        /// Every frame, we compute the aim direction and rotate the weapon accordingly
        /// </summary>
        protected virtual void LateUpdate()
        {
            bool weaponInUse = false;
            foreach (Weapon weapon in _weapons)
            {
                if (weapon.WeaponState.CurrentState == Weapon.WeaponStates.WeaponDelayBeforeUse
                    || weapon.WeaponState.CurrentState == Weapon.WeaponStates.WeaponUse
                    || weapon.WeaponState.CurrentState == Weapon.WeaponStates.WeaponDelayBetweenUses)
                {
                    weaponInUse = true;
                }
            }

            if (PreventAimWhileWeaponIsInUse && weaponInUse)
            {
                return;
            }
            GetCurrentAim();
            DetermineWeaponRotation();
            MoveReticle();
            HideReticle();
            ResetCurrentAimMultiplier();

            //디버깅 부분

            //반동 활성화 버튼은 R이다.
            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("R키 눌림. 총기회전 반동을 활성화합니다.");
                isFirstProcessingRecoil = true;
            }
            if(Input.GetKeyDown(KeyCode.S)){
                Debug.Log("S키 눌림. 탄퍼짐을 활성화 합니다.");
            }
            if (Input.GetKeyDown(KeyCode.T))
            {
                Debug.Log("T키 눌림. 마우스 커서 이동 반동을 활성화합니다.");
                RecoilMouseMove();
            }
        }

        //혹시나 왜 계산이 이렇게 되는지 궁금하다면, 정재헌한테 문의해주세요.
        protected virtual void RecoilMouseMove(){
            Vector2 nowMousePosition = Input.mousePosition;
            //현재 마우스와 Weapon 사이 x좌표 차
            float xMouseToWeapon = nowMousePosition.x-transform.position.x;
            float angleByCursorAndGround = Mathf.Atan(nowMousePosition.y/nowMousePosition.x); 
            float deltaRecoildMouseMove = xMouseToWeapon*(Mathf.Tan(angleByCursorAndGround+recoilCursorDegree*MathF.PI/180)-Mathf.Tan(angleByCursorAndGround));
            
            Mouse.current.WarpCursorPosition(nowMousePosition+new Vector2(0,deltaRecoildMouseMove));
        }

        /// <summary>
        /// Resets the current aim multiplier at the end of the frame
        /// </summary>
        protected virtual void ResetCurrentAimMultiplier()
        {
            CurrentAimMultiplier = _regularHorizontalAimMultiplier;
        }

        //반동을 처음 처리할 경우 true
        //Debug를 위해 public으로 놔둠.
        public bool isFirstProcessingRecoil;
        //반동을 처음 처리했고, 반동 시간중이라면, true
        private bool isProcessingRecoil;
        private float WeaponRotationSpeedStorage;

        protected virtual void StartRecoil()
        {

            isFirstProcessingRecoil = false;
            isProcessingRecoil = true;
            Debug.Log(isProcessingRecoil);
            StartRecoilUnitask().Forget();

        }

        private Quaternion targetRecoilRotationR;
        private Quaternion targetRecoilRotationL;

        private async UniTaskVoid StartRecoilUnitask()
        {
            //반동 중에 다시 반동이 들어온 경우 로직 추가 필요. StopRecoilUnitask필요.

            //WeaponRotationSpeed를, 창고에 넣어둠
            WeaponRotationSpeedStorage = WeaponRotationSpeed;
            WeaponRotationSpeed = recoilRotateSpeed;
            //현재 무기의 각도를 구함.
            float currentWeaponAngle = transform.rotation.eulerAngles.z;
            //반동 이후 최종 지점 각도를 구함.
            float targetWeaponAngleR = currentWeaponAngle + recoilDegree;
            float targetWeaponAngleL = currentWeaponAngle - recoilDegree;
            ////반동에 의한 무기 각도의 최대치는, 수직상방 혹은 수직 하방임.
            Mathf.Clamp(targetWeaponAngleR, -90f, 90f);
            Mathf.Clamp(targetWeaponAngleL, -90f, 90f);

            targetRecoilRotationR = Quaternion.Euler(targetWeaponAngleR * Vector3.forward);
            targetRecoilRotationL = Quaternion.Euler(targetWeaponAngleL * Vector3.forward);

            await UniTask.Delay(TimeSpan.FromSeconds(recoilDuration));
            //다시 Weapon Rotation Speed를 되돌림
            isProcessingRecoil = false;
            WeaponRotationSpeed = WeaponRotationSpeedStorage;
        }

        protected void DuringRecoil()
        {
            if (_weapon.Owner.IsFacingRight)
            {
                RotateWeapon(targetRecoilRotationR);
            }
            else
            {
                RotateWeapon(targetRecoilRotationL);
            }
        }
        /// <summary>
        /// Determines the weapon rotation based on the current aim direction
        /// </summary>
        protected virtual void DetermineWeaponRotation()
        {
            ///정재헌이 추가한 코드 부분이다. 반동의 영향을 받고 있는 중이라면, 무기의 방향은 현재 조준과 무관하게 결정됨. 
            if (isFirstProcessingRecoil)
            {
                StartRecoil();
                return;
            }
            else if (isProcessingRecoil)
            {
                DuringRecoil();
                return;
            }

            if (_currentAim != Vector3.zero)
            {
                if (_direction != Vector3.zero)
                {
                    // we compute our angle in degrees
                    CurrentAngle = Mathf.Atan2(_currentAim.y, _currentAim.x) * Mathf.Rad2Deg;

                    // we round to the closest angle
                    if (RotationMode == RotationModes.Strict4Directions || RotationMode == RotationModes.Strict8Directions)
                    {
                        CurrentAngle = (_weapon.Owner.IsFacingRight) ? MMMaths.RoundToClosest(CurrentAngle, _possibleAngleValues) : -MMMaths.RoundToClosest(-CurrentAngle, _possibleAngleValues);
                    }

                    // we add our additional angle
                    CurrentAngle += _additionalAngle;

                    // we clamp the angle to the min/max values set in the inspector
                    if (_weapon.Owner.IsFacingRight)
                    {
                        CurrentAngle = Mathf.Clamp(CurrentAngle, MinimumAngle, MaximumAngle);
                    }
                    else
                    {
                        CurrentAngle = Mathf.Clamp(CurrentAngle, -MaximumAngle, -MinimumAngle);
                    }
                    _lookRotation = Quaternion.Euler(CurrentAngle * Vector3.forward);
                    RotateWeapon(_lookRotation);
                }
            }
            else
            {
                CurrentAngle = 0f;
                if (_characterGravity == null)
                {
                    RotateWeapon(_initialRotation);
                }
                else
                {
                    RotateWeapon(_characterGravity.transform.rotation);
                }
                if (_additionalAngle != 0f)
                {
                    CurrentAngle += _additionalAngle;
                    _lookRotation = Quaternion.Euler(CurrentAngle * Vector3.forward);
                    RotateWeapon(_lookRotation);
                }

            }
            if (_weapon.Owner.IsFacingRight)
            {
                MMDebug.DebugDrawArrow(this.transform.position, Vector2.right.MMRotate(CurrentAngle).normalized, Color.green);
            }
            else
            {
                MMDebug.DebugDrawArrow(this.transform.position, Vector2.left.MMRotate(CurrentAngle).normalized, Color.green);
            }
            WasFacingRightLastFrame = _weapon.Owner.IsFacingRight;
        }

        /// <summary>
        /// Rotates the weapon, optionnally applying a lerp to it.
        /// </summary>
        /// <param name="newRotation">New rotation.</param>
        protected virtual void RotateWeapon(Quaternion newRotation)
        {
            if (GameManager.Instance.Paused)
            {
                return;
            }
            // if the rotation speed is == 0, we have instant rotation
            if (WeaponRotationSpeed == 0)
            {
                transform.rotation = newRotation;
            }
            // otherwise we lerp the rotation
            else
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, WeaponRotationSpeed * Time.deltaTime);
            }

            if (InstantFlip && (WasFacingRightLastFrame != _weapon.Owner.IsFacingRight))
            {
                transform.rotation = newRotation;
            }
        }

        /// <summary>
        /// If a reticle has been set, instantiates the reticle and positions it
        /// </summary>
        protected virtual void InitializeReticle()
        {
            if (Reticle == null) { return; }
            if (!DisplayReticle) { return; }

            _reticle = (GameObject)Instantiate(Reticle);
            if (_weapon.Owner != null)
            {
                _reticle.transform.SetParent(_weapon.transform);
            }

            _reticle.transform.localPosition = ReticleDistance * Vector3.right;
        }

        /// <summary>
        /// Every frame, moves the reticle if it's been told to follow the pointer
        /// </summary>
        protected virtual void MoveReticle()
        {
            if (_reticle == null) { return; }

            // if we're not supposed to rotate the reticle, we force its rotation, otherwise we apply the current look rotation
            if (!RotateReticle)
            {
                _reticle.transform.rotation = Quaternion.identity;
            }
            else
            {
                if (ReticleAtMousePosition)
                {
                    _reticle.transform.rotation = _lookRotation;
                }
            }

            // if we're in follow mouse mode and the current control scheme is mouse, we move the reticle to the mouse's position
            if (ReticleAtMousePosition && AimControl == AimControls.Mouse)
            {
                _reticlePosition = _mainCamera.ScreenToWorldPoint(_mousePosition);
                _reticlePosition.z = ReticleZPosition;
                _reticle.transform.position = _reticlePosition;
            }
        }

        /// <summary>
        /// Handles the hiding of the reticle and cursor
        /// </summary>
        protected virtual void HideReticle()
        {
            if (DisableReticleOnDeath && (_reticle != null))
            {
                if (_weapon.Owner != null)
                {
                    if (_weapon.Owner.ConditionState.CurrentState == CharacterStates.CharacterConditions.Dead)
                    {
                        _reticle.gameObject.SetActive(false);
                    }
                    else
                    {
                        _reticle.gameObject.SetActive(true);
                    }
                }
            }

            if (GameManager.Instance.Paused)
            {
                Cursor.visible = true;
                return;
            }
            if (ReplaceMousePointer)
            {
                Cursor.visible = false;
            }
            else
            {
                Cursor.visible = true;
            }
        }

        public virtual void AddAdditionalAngle(float addedAngle)
        {
            if (IgnoreSlopeRotation)
            {
                return;
            }

            _additionalAngle += addedAngle;
        }

        public virtual void ResetAdditionalAngle()
        {
            _additionalAngle = 0;
        }
    }
}