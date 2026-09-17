using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class TypeEventSystemInheritEventExample : MonoBehaviour
{
        public interface IEventA
        {

        }

        public struct EventB : IEventA
        {

        }

        private void Start()
        {
            TypeEventSystem.Global.Register<IEventA>(e =>
            {
                Debug.Log(e.GetType().Name);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                TypeEventSystem.Global.Send<IEventA>(new EventB());

                // 无效写法
                //TypeEventSystem.Global.Send<EventB>();
            }
        }
}
