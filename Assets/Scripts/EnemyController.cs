using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private NavMeshAgent _navAgent;
    [SerializeField] private Transform _target;


    void Start()
    {
        
    }

    void Update()
    {
        NavMeshPath path = null;
        if(_navAgent.CalculatePath(_target.position, path))
        {
            _navAgent.SetPath(path);

            if(_navAgent.remainingDistance < 5f)
            {
                Debug.Log("STOP");
            }
            else
            {
                Debug.Log("MOVE");
            }
        }
    }
}
