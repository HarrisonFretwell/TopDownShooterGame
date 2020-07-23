using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Rigidbody))]
public class PlayerController : MonoBehaviour
{
    Rigidbody rigidbody;
    Vector3 velocity;
    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

   

    void FixedUpdate(){
        rigidbody.MovePosition(rigidbody.position + Time.fixedDeltaTime*velocity);
    }

     public void Move(Vector3 velocity){
        this.velocity = velocity;
    }

    public void LookAt(Vector3 point){
        point.y = transform.position.y;
        transform.LookAt(point);
    }
}
