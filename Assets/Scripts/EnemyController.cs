using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private NavMeshAgent _navAgent;
    [SerializeField] private ThirdPersonController _target;
    [SerializeField] private Animator _animator;
    [SerializeField] private Collider _collider;
    [SerializeField] private Rigidbody _hipsRigidBody;
    [SerializeField] private GameObject _ultimateDisplay;

    [Header("Attack Colliders")]
    public GameObject BasicAttack;
    public GameObject SpecialAttack;
    [HideInInspector] public float AttackTimeOut;

    private float _currentSpeedAnimator;
    private float _stunTimeOut;

    public bool IsDead;

    void Start()
    {
        IsDead = false;
        AttackTimeOut = float.MinValue;
        SetUltimateTargetState(false);
    }

    void Update()
    {
        if(IsDead)
        {
            return;
        }

        if(!IsAttacking() && CanAttack())
        {
            var distanceToPlayer = Vector3.Distance(this.transform.position, _target.transform.position);


            if(distanceToPlayer < 8f)
            {
                AttackTimeOut = Time.time + 0.5f;
                _animator.SetTrigger("BasicAttack");
            }
            else if(distanceToPlayer < 15f)
            {
                AttackTimeOut = Time.time + 0.5f;
                _animator.SetTrigger("SpecialAttack");
            }
        }

        if(IsAttacking())
        {
            _navAgent.enabled = false;
            return;
        }
        
        _navAgent.enabled = true;

        if(IsStunned())
        {
            _navAgent.speed = 0;
            _animator.SetFloat("Speed", 0);
        }


        NavMeshPath path = new NavMeshPath();
        if(_navAgent.CalculatePath(_target.transform.position, path))
        {
            _navAgent.SetPath(path);

            if(_navAgent.remainingDistance < 7f)
            {
                _navAgent.speed = 0;
                _currentSpeedAnimator = Mathf.Lerp(_currentSpeedAnimator, 0f, 0.5f);
                var targetLookAt = _target.transform.position;
                targetLookAt.y = this.transform.position.y;
                this.transform.LookAt(targetLookAt);
            }
            else
            {
                _navAgent.speed = 15f;
                _currentSpeedAnimator = Mathf.Lerp(_currentSpeedAnimator, 1f, 0.5f);
            }

            _animator.SetFloat("Speed", _currentSpeedAnimator);
        }
    }

    private bool CanAttack()
    {
        return AttackTimeOut + 2f < Time.time;
    }

    private bool IsAttacking()
    {
        return AttackTimeOut > Time.time;
    }

    public void SetBasicAttackState(bool state)
    {
        if(!IsAttacking() && state)
        {
            return;
        }
        BasicAttack.SetActive(state);
    }

    public void SetSpecialAttackState(bool state)
    {
        if(!IsAttacking() && state)
        {
            return;
        }
        SpecialAttack.SetActive(state);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("PlayerAttack"))
        {
            GetHit();
        }
    }

    private void GetHit()
    {
        if(SpecialAttack.activeInHierarchy)
        {
            return;
        }

        _target.GainEnergy();
        _stunTimeOut = Time.time + 0.3f;
        AttackTimeOut = 0f;
        SetBasicAttackState(false);
        SetSpecialAttackState(false);
        _animator.Rebind();
        _animator.SetTrigger("GetHit");
    }

    private bool IsStunned()
    {
        return _stunTimeOut > Time.time;
    }

    public void ReceiveSpecialAttack(string attack, int id)
    {
        IsDead = true;
        _navAgent.enabled = false;

        this.transform.DOMove(_target.transform.position + _target.transform.forward * 24f, 0.3f);

        var targetLookAt = _target.transform.position;
        targetLookAt.y = this.transform.position.y;
        this.transform.LookAt(targetLookAt);
        _animator.Rebind();
        _animator.Play($"Get{attack}");
        DelayedActions.DelayedAction(ActivateRagdoll, id == 0 ? 1.6f : 1.25f);
    }

    private void ActivateRagdoll()
    {
        _animator.enabled = false;
        _collider.enabled = false;
        _hipsRigidBody.AddExplosionForce(100000f, _target.transform.position, 5000f);
    }

    public void SetUltimateTargetState(bool state)
    {
        _ultimateDisplay.SetActive(state);
    }
}
