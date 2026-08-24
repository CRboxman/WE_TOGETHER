using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EthanAnim3D : MonoBehaviour
{
    private Animator anim;

    [SerializeField]private float roundSpeed = 30;
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
    }

    private void JumpOver()
    {
        anim.SetBool("DoJump",false);
    }
}
