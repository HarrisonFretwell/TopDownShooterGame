using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public LayerMask mask;
    public GameObject onHitEffect;
    ParticleSystem onHitParticleEffect;
    float projSpeed;
    float damage = 1;
    float lifetime = 3;
    float collisionBias = .1f;

    public void setSpeed(float projSpeed){
        this.projSpeed = projSpeed;
    }

    void Start(){
        onHitParticleEffect = onHitEffect.GetComponent<ParticleSystem>();
        //Destroy after lifetime has expired
        Destroy(gameObject,lifetime);
        //Check if inside any colliders
        Collider[] initialCollisions = Physics.OverlapSphere(transform.position,0.1f,mask);
        if(initialCollisions.Length > 0){
            OnHitObject(initialCollisions[0], transform.position);
        }
        
    }

    void Update()
    {
        float moveDistance = Time.deltaTime * projSpeed;
        CheckCollisions(moveDistance);
        transform.Translate(Vector3.forward * moveDistance);
        
    }

    //Check short ray in front of projectile to see if collides with enemy
    void CheckCollisions(float moveDistance){
        Ray ray = new Ray(transform.position,transform.forward);
        RaycastHit hit;
        if(Physics.Raycast(ray,out hit,moveDistance+collisionBias,mask,QueryTriggerInteraction.Collide)){
            OnHitObject(hit.collider, hit.point);
        }

    }

    void OnHitObject(Collider c,Vector3 hitPoint){

        //Get damageable interface of hit object
        IDamageable damageableObject = c.GetComponent<IDamageable>();
        if(damageableObject != null){
            damageableObject.TakeHit(damage,hitPoint, transform.forward);
        }
        
        Color hitColor = c.gameObject.GetComponent<Renderer>().material.color;
        GameObject particleEffect = Instantiate(onHitEffect,hitPoint,Quaternion.FromToRotation(Vector3.forward, transform.forward)) as GameObject;
        particleEffect.GetComponent<ParticleSystemRenderer>().material.color = hitColor;
        
        Destroy(particleEffect,0.8f);
        GameObject.Destroy(gameObject);
    }
}
