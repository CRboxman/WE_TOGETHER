using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EthanAnim3D : MonoBehaviour
{
    private Animator anim;
    //[SerializeField]Transform lookAtTarget;
    [SerializeField]private float roundSpeed = 30;

    private float playerMoveSpeed = 0;
    private bool isRunning=false;
    private float currentTree = 0.5f;                       // 前进用的 Blend Tree 值：0.5=走，1=跑
    private float targetTree  = 0.5f;
    [SerializeField] private float blendSpeed = 3f; // 每秒变化量，越大越快
    [SerializeField] private Transform Head;
    [SerializeField]private Transform jumpTarget;
    private Vector3 getMouseXY;
    private Vector3 getMouseXY1;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        anim.SetBool("IsIdle",true);
        anim.SetBool("IsBack",false);
        anim.SetFloat("Tree", currentTree);
    }

    // Update is called once per frame
    void Update()
    {
        playerMoveSpeed = Input.GetAxisRaw("Vertical");
        isRunning = Input.GetKey(KeyCode.LeftShift);

        if (playerMoveSpeed != 0)
        {
            anim.SetBool("IsIdle", false);

            if (playerMoveSpeed < 0)
            {
                // 后退：走独立状态（Walk 倒放），不再让 Blend Tree 从 0 号倒放段起步，
                // 这样从站立切到前进时不会先"倒蹭"一下。
                anim.SetBool("IsBack", true);
            }
            else
            {
                anim.SetBool("IsBack", false);
                targetTree = isRunning ? 1f : 0.5f;          // 跑 / 走
                currentTree = Mathf.MoveTowards(currentTree, targetTree, blendSpeed * Time.deltaTime);
                anim.SetFloat("Tree", currentTree);
            }
        }
        else
        {
            anim.SetBool("IsIdle", true);
            anim.SetBool("IsBack", false);
            // 停下时不再把 Tree 拉回 0：Blend Tree 里已经没有倒放段，
            // 拉回 0 只会在下一次起步时多一次无意义过渡。
        }

        //AD，水平，对应左转右转
        transform.Rotate(Vector3.up,Input.GetAxisRaw(("Horizontal")) * roundSpeed * Time.deltaTime);

        //盯着看
        if (Head != null)
        {
            var posx = Input.GetAxis("Mouse X");
            var posy = Input.GetAxis("Mouse Y");
            getMouseXY.x += posx*10;
            getMouseXY.y += posy*10;

            getMouseXY.x = Mathf.Clamp(getMouseXY.x,-45,45);
            getMouseXY.y = Mathf.Clamp(getMouseXY.y,-45,45);
            Quaternion q=Quaternion.AngleAxis(getMouseXY.x,Vector3.up)*Quaternion.AngleAxis(getMouseXY.y,Vector3.left);
            getMouseXY1 =Head.position + q *  Head.forward * 10;
            Debug.DrawLine(Head.position,getMouseXY1,Color.red);
            if (Input.GetMouseButtonDown(0))
            {
                getMouseXY = Vector3.zero;
                getMouseXY1 = Head.position + Head.forward * 10;
            }
        }

        //跳跃
        if (Input.GetKeyDown(KeyCode.Space))
        {
            anim.SetTrigger("DoJump");
        }
    }
    

    private void OnAnimatorIK(int layerIndex)
    {
        anim.SetLookAtWeight(1,0,1);
        //anim.SetLookAtPosition(lookAtTarget.position);
        anim.SetLookAtPosition(getMouseXY1);
    }
    
    private void JumpOver()
    {
        anim.MatchTarget(jumpTarget.position,
            jumpTarget.rotation,
            AvatarTarget.LeftFoot,
            new MatchTargetWeightMask(Vector3.one,0),
            0.4f,
            0.64f);
    }
}
