using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestAnimBehaviour : StateMachineBehaviour
{
    // 当转换开始并且状态机开始评估此状态时，将调用OnStateEnter
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log(stateInfo.IsName("HumanoidIdle"));
    }

    // 在OnStateEnter和OnStateExit回调之间的每个Update帧上调用OnStateUpdate
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // 在状态机评估此状态并且任何过渡都已完成时调用OnStateExit
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // 在状态机评估此状态并且任何过渡都已完成时调用OnStateMove
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // 在状态机评估此状态并且任何过渡都已完成时调用OnStateIK
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
