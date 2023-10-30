using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackTimeOutSetter : StateMachineBehaviour
{
    public bool IsBasicAttack;
    public float attackDelay;
    public float timeout;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var controller = animator.GetComponent<EnemyController>();
        if(controller == null)
        {
            return;
        }

        controller.AttackTimeOut = Time.time + timeout;

        if(IsBasicAttack)
        {
            DelayedActions.DelayedAction(() => controller.SetBasicAttackState(true), attackDelay);
        }
        else
        {
            DelayedActions.DelayedAction(() => controller.SetSpecialAttackState(true), attackDelay);
        }
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var controller = animator.GetComponent<EnemyController>();
        if(controller == null)
        {
            return;
        }

        if(IsBasicAttack)
        {
            controller.SetBasicAttackState(false);
        }
        else
        {
            controller.SetSpecialAttackState(false);
        }
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
