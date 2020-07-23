using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent (typeof (NavMeshAgent))]
public class Enemy : LivingEntity
{
    //Basic FSM
    public enum State {Idle, Chasing, Attacking}
    State currentState;

    public ParticleSystem deathEffect;
    public static event System.Action OnDeathStatic;

    LivingEntity targetEntity;
    NavMeshAgent pathFinder;
    Material skinMaterial;
    Transform target;
    Color originalColor;

    public float attackDistanceThreshold = .5f;
    float timeBetweenAttacks = 1;
    float nextAvailableAttackTime = float.MinValue;
    float damage = 1;

    float myCollisionRadius;
    float targetCollisionRadius;

    bool hasTarget;
    
    void Awake(){
        pathFinder = GetComponent<NavMeshAgent> ();
		
		if (GameObject.FindGameObjectWithTag ("Player") != null) {
			hasTarget = true;
			
			target = GameObject.FindGameObjectWithTag ("Player").transform;
			targetEntity = target.GetComponent<LivingEntity> ();
			
			myCollisionRadius = GetComponent<CapsuleCollider> ().radius;
			targetCollisionRadius = target.GetComponent<CapsuleCollider> ().radius;
		}
    }

    protected override void Start()
    {
        //Run interface class' start method first
        base.Start();
        if (hasTarget) {
			currentState = State.Chasing;
			targetEntity.OnDeath += OnTargetDeath;

			StartCoroutine (UpdatePath ());
		}
        
    }

    void Update(){
        //Attack code, checks if attack is possible
        if(hasTarget && nextAvailableAttackTime < Time.time){
            float SquareDistanceToTarget = (target.position - transform.position).sqrMagnitude;
            if(SquareDistanceToTarget < Mathf.Pow(attackDistanceThreshold + myCollisionRadius + targetCollisionRadius, 2)){
                nextAvailableAttackTime = Time.time + timeBetweenAttacks;
                Debug.Log("Attacking!");
                StartCoroutine(Attack());
            }
        }
    }

    public override void TakeHit(float damage, Vector3 hitPoint, Vector3 hitDirection){
        AudioManager.instance.PlaySound("Impact",transform.position);
        if(damage >= health){
            if(OnDeathStatic != null)
                OnDeathStatic();
            AudioManager.instance.PlaySound("Enemy Death",transform.position);
        Destroy(Instantiate(deathEffect.gameObject, hitPoint, Quaternion.FromToRotation(Vector3.forward, hitDirection)) as GameObject, 8);        }
        base.TakeHit(damage,hitPoint,hitDirection);
    }
    
    void OnTargetDeath(){
        hasTarget = false;
        currentState = State.Idle;
    }
    
    //Set characteristics of enemy for current wave
    public void SetCharacteristics(float moveSpeed, int hitsToKillPlayer, float enemyHealth, Color skinColour){
        pathFinder.speed = moveSpeed;

		if (hasTarget) {
			damage = Mathf.Ceil(targetEntity.startingHealth / hitsToKillPlayer);
		}
		startingHealth = enemyHealth;
        ParticleSystem.MainModule ps_main = deathEffect.main;
        ps_main.startColor = new ParticleSystem.MinMaxGradient(new Color(skinColour.r,skinColour.g,skinColour.b,1));
		skinMaterial = GetComponent<Renderer> ().material;
		skinMaterial.color = skinColour;
		originalColor = skinMaterial.color;
    }

    //Handles attack animation
    IEnumerator Attack(){
        //Don't want to pathfind while attacking
        currentState = State.Attacking;
        pathFinder.enabled = false;
        Vector3 originalPosition = transform.position;
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        Vector3 attackPosition = target.position - directionToTarget * (myCollisionRadius);

        float attackSpeed = 3f;
        float percent = 0;
        bool hasAppliedDamage = false;
        AudioManager.instance.PlaySound("Enemy Attack",transform.position);

        skinMaterial.color = Color.red;

        while(percent <= 1){
            
            //-4x*2+4x
            percent += Time.deltaTime * attackSpeed;
            float interpolation = (-Mathf.Pow(percent,2) +percent)*4;
            transform.position = Vector3.Lerp(originalPosition,attackPosition,interpolation);
            if(percent >= 0.5 && !hasAppliedDamage){
                hasAppliedDamage = true;
                targetEntity.TakeDamage(damage);
            }
            
            yield return null;
        }
        pathFinder.enabled = true;
        currentState = State.Chasing;
        skinMaterial.color = originalColor;
    }

    //Navigate to player
    IEnumerator UpdatePath(){
        float refreshRate = 0.25f;
        while(hasTarget){
            if(currentState == State.Chasing){
                Vector3 directionToTarget = (target.position - transform.position).normalized;
                Vector3 targetPosition = target.position - directionToTarget * (myCollisionRadius + targetCollisionRadius + attackDistanceThreshold/2);
                if(!dead){
                    pathFinder.SetDestination(targetPosition);
                }
                
            }
           yield return new WaitForSeconds(refreshRate);
        }
        
    }
}
