using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EthanAnim3D : MonoBehaviour
{
    private Animator anim;
    //[SerializeField]Transform lookAtTarget;
    [SerializeField]private float roundSpeed = 30;

    [SerializeField] private Transform Head;
    private Vector3 getMouseXY;
    private Vector3 getMouseXY1;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        //WS，上下，对应着前后移动
        anim.SetInteger("Speed",(int)Input.GetAxisRaw("Vertical"));
        if (Input.GetKeyDown(KeyCode.Space))
        {
            anim.SetBool("DoJump",true);
        }
        //AD，水平，对应左转右转
        transform.Rotate(Vector3.up,Input.GetAxisRaw(("Horizontal")) * roundSpeed * Time.deltaTime);
        //
        // Debug.Log(Vector3.Distance(lookAtTarget.position,transform.position));
        //
        // var dir = Vector3.Angle(lookAtTarget.position - transform.position,transform.forward);
        // Debug.Log(dir);
        // Debug.Log( transform.eulerAngles);
        // Debug.Log( transform.rotation);
        // Vector3 a=Quaternion.AngleAxis(dir,Vector3.up)*transform.forward;

        if (Head != null)
        {
            var posx = Input.GetAxis("Mouse X");
            var posy = Input.GetAxis("Mouse Y");
            getMouseXY.x += posx*10;
            getMouseXY.y += posy*10;
            Debug.Log(posx);
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
    }

    // private void OnAnimatorMove()
    // {
    //     Debug.Log("这是OnAnimatorMove");
    // }

    private void OnAnimatorIK(int layerIndex)
    {
        anim.SetLookAtWeight(1,0,1);
        //anim.SetLookAtPosition(lookAtTarget.position);
        anim.SetLookAtPosition(getMouseXY1);
    }
}
