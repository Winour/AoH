using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private NavMeshAgent _navAgent;
    [SerializeField] private ThirdPersonController _target;
    [SerializeField] private Animator _animator;
    [SerializeField] private Collider _collider;
    [SerializeField] private Rigidbody _hipsRigidBody;

    private float _currentSpeedAnimator;
    private float _stunTimeOut;

    private bool _isDead;

    void Start()
    {
        _isDead = true;
    }

    void Update()
    {
        if(_isDead)
        {
            return;
        }

        if(IsStunned())
        {
            _navAgent.speed = 0;
            _animator.SetFloat("Speed", 0);
        }

        NavMeshPath path = new NavMeshPath();
        if(_navAgent.CalculatePath(_target.transform.position, path))
        {
            _navAgent.SetPath(path);

            if(_navAgent.remainingDistance < 10f)
            {
                _navAgent.speed = 0;
                _currentSpeedAnimator = Mathf.Lerp(_currentSpeedAnimator, 0f, 0.5f);
            }
            else
            {
                _navAgent.speed = 15f;
                _currentSpeedAnimator = Mathf.Lerp(_currentSpeedAnimator, 1f, 0.5f);
            }

            _animator.SetFloat("Speed", _currentSpeedAnimator);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("PlayerAttack"))
        {
            Stun();
        }
    }

    private void Stun()
    {
        _stunTimeOut = Time.time + 0.3f;
        _animator.Rebind();
        _animator.SetTrigger("GetHit");
    }

    private bool IsStunned()
    {
        return _stunTimeOut > Time.time;
    }

    public void ReceiveSpecialAttack(string attack, int id)
    {
        _isDead = true;
        _navAgent.enabled = false;

        this.transform.position = _target.transform.position + _target.transform.forward * 24f;

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
}
