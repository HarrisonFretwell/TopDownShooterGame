using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Interface for living entities, controls health and death
public class LivingEntity : MonoBehaviour, IDamageable {

    public float startingHealth;
    public float health {get; protected set;}
    protected bool dead;

    public event System.Action OnDeath;

    protected virtual void Start(){
        health = startingHealth;
    }

    public virtual void TakeHit(float damage, Vector3 hitPoint, Vector3 hitDirection){
        //Do some stuff here
        TakeDamage(damage);
    }

    public virtual void TakeDamage(float damage){
        health -= damage;
        if(health <= 0 && !dead){
            Die();
        }
    }

    [ContextMenu("Self Destruct")]
    public virtual void Die(){
        if(OnDeath != null)
            OnDeath();
        dead = true;
        Destroy(gameObject);
    }
}
