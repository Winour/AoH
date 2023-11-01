using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Colliders")]
        public GameObject LeftPunch;
        public GameObject RightLeg;
        [SerializeField] private Image _energyImage;
        [SerializeField] private EnemyController[] _enemies;
        private EnemyController _ultiableEnemy;

        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;
        public float AttackTimeOut;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDBasicAttack;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;
        private Dictionary<string, GameObject> _colliders = new Dictionary<string, GameObject>();

        private float _currentEnergy;
        private float _stunTimeOut;
        private bool _isGameOver;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }


        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

            _colliders.Add("LeftPunch", LeftPunch);
            _colliders.Add("RightLeg", RightLeg);
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();
            ResetAllColliders();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            UpdateEnergyDisplay();
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            Move();
            BasicAttack();
            UpdateUltiableEnemy();
            SpecialAttack();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDBasicAttack = Animator.StringToHash("BasicAttack");
        }

        public void GainEnergy()
        {
            _currentEnergy += 17.5f;
            _currentEnergy = Mathf.Clamp(_currentEnergy, 0, 100f);
            UpdateEnergyDisplay();
        }

        public void LooseEnergy()
        {
            _currentEnergy -= 10f;
            _currentEnergy = Mathf.Clamp(_currentEnergy, 0, 100f);
            UpdateEnergyDisplay();
        }

        private void UpdateEnergyDisplay()
        {
            _energyImage.DOFillAmount(_currentEnergy / 100f, 0.3f);
        }

        private void UpdateUltiableEnemy()
        {
            var prevEnemy = _ultiableEnemy;

            _ultiableEnemy = null;

            if(_currentEnergy >= 100)
            {
                var closestEnemyDistance = float.MaxValue;

                foreach(var enemy in _enemies)
                {
                    var distanceToEnemy = Vector3.Distance(this.transform.position, enemy.transform.position);
                    if(distanceToEnemy < 30f) 
                    {
                        if(distanceToEnemy > closestEnemyDistance)
                        {
                            continue;
                        }

                        var angle = Vector3.Angle(Camera.main.transform.forward, enemy.transform.position - this.transform.position);
                        if(Mathf.Abs(angle) < 70f)
                        {
                            closestEnemyDistance = distanceToEnemy;
                            _ultiableEnemy = enemy;
                        }
                    }
                }
            }

            if(_ultiableEnemy != prevEnemy)
            {
                if(prevEnemy != null)
                    prevEnemy.SetUltimateTargetState(false);

                if(_ultiableEnemy != null)
                    _ultiableEnemy.SetUltimateTargetState(true);
            }
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            if(IsAttacking() || IsStunned())
            {
                _controller.enabled = false;
                return;
            }
            else
            {
                _controller.enabled = true;
            }

            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    70f);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        public void ResetAllColliders()
        {
            foreach(var collider in _colliders)
            {
                collider.Value.SetActive(false);
            }
        }

        public void StartAttack(string attack)
        {
            if(!IsAttacking())
            {
                return;
            }

            if(_colliders.TryGetValue(attack, out var collider))
            {
                collider.SetActive(true);
            }
        }

        public void FinishAttack(string attack)
        {
            if(_colliders.TryGetValue(attack, out var collider))
            {
                collider.SetActive(false);
            }
        }

        private void BasicAttack()
        {
            if(_isGameOver || !_input.basicAttack || IsAttacking() || IsStunned())
            {
                _input.basicAttack = false;
                return;
            }

            AttackTimeOut = Time.time + 0.5f;
            _animator.SetTrigger(_animIDBasicAttack);
            _input.basicAttack = false;
        }

        private int _attacksDone;

        private void SpecialAttack()
        {
            if(!_input.specialAttack || IsAttacking() || IsStunned())
            {
                _input.specialAttack = false;
                return;
            }

            if(_currentEnergy < 100f || _ultiableEnemy == null)
            {
                return;
            }

            _currentEnergy = 0f;
            UpdateEnergyDisplay();

            var attackToDo = _attacksDone % 2 == 0 ? "SpecialAttack_01" : "SpecialAttack_02";
            var targetLookAt = _ultiableEnemy.transform.position;
            targetLookAt.y = this.transform.position.y;
            this.transform.LookAt(targetLookAt);
            AttackTimeOut = Time.time + 0.5f;
            _ultiableEnemy.ReceiveSpecialAttack(attackToDo, _attacksDone % 2);
            _animator.Play(attackToDo);
            _input.specialAttack = false;
            _attacksDone++;

            DelayedActions.DelayedAction(CheckGameIsFinished, 4f);
        }

        private bool IsAttacking()
        {
            return AttackTimeOut > Time.time;
        }

        [SerializeField] private GameObject _gameOverMenu;

        private void CheckGameIsFinished()
        {
            foreach(var enemy in _enemies)
            {
                if(!enemy.IsDead)
                {
                    return;
                }
            }
            _input.cursorLocked = false;
            Cursor.lockState = CursorLockMode.None;
            _gameOverMenu.SetActive(true);
            _isGameOver = true;
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if(other.CompareTag("EnemyAttack"))
            {
                GetHit();
                other.gameObject.SetActive(false);
            }
        }

        private bool IsStunned()
        {
            return _stunTimeOut > Time.time;
        }

        private void GetHit()
        {
            if(IsAttacking() || IsStunned())
            {
                return;
            }

            ResetAllColliders();
            LooseEnergy();
            ResetAllColliders();
            AttackTimeOut = 0f;
            _stunTimeOut = Time.time + 0.8f;
            _animator.Rebind();
            _animator.Play("GetHit");
        }
    }
}