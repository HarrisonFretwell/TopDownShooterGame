using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

//Ensure that player controller object also attached to parent game object
[RequireComponent (typeof (PlayerController))]
[RequireComponent (typeof (GunController))]
public class Player : LivingEntity
{
    public Crosshairs crosshairs;
    
    public float moveSpeed = 5;
    PlayerController pController;
    GunController gController;
    Camera viewCamera;

    protected override void Start(){
        base.Start();
        
    }
    
    void Awake(){
        pController = GetComponent<PlayerController>();
        gController = GetComponent<GunController>();
        viewCamera = Camera.main;
        FindObjectOfType<Spawner>().OnNewWave += OnNewWave;
    }

    void Update(){
        //*Movement Input
        //GetAxisRaw removes smoothing of movement
        Vector3 moveInput = new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical"));
        Vector3 moveVelocity = Vector3.Normalize(moveInput) * moveSpeed;
        pController.Move(moveVelocity);
        //Kill player if fall off 
        if(transform.position.y < -10){
            TakeDamage(health);
        }

        //*Look Input
        //Cast ray from camera to mouse position
        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up,Vector3.up * gController.GunHeight);
        float rayDistance;

        //Intersect ray with plane, returns true if intersected, outputs ray distance
        if(groundPlane.Raycast(ray,out rayDistance)){
            //Get point along ray with rayDistance
            Vector3 pointOfIntersection = ray.GetPoint(rayDistance);
            //Debug.DrawLine(ray.origin, pointOfIntersection, Color.red);
            pController.LookAt(pointOfIntersection);
            crosshairs.transform.position = pointOfIntersection;
            crosshairs.DetectTargets(ray);
            if((new Vector2(pointOfIntersection.x,pointOfIntersection.z)-new Vector2(transform.position.x,transform.position.y)).sqrMagnitude > 1){
                gController.Aim(pointOfIntersection);
            }
            
        }
        //*Weapon Input
        if(Input.GetMouseButton(0)){
            gController.OnTriggerHold();
        }
        if(Input.GetMouseButtonUp(0)){
            gController.OnTriggerRelease();
        }
        if(Input.GetKeyDown(KeyCode.R)){
            gController.Reload();
        }
        if(Input.GetKeyDown(KeyCode.Escape)){
            SceneManager.LoadScene ("Menu");
        }
    }
    
    void OnNewWave(int waveNumber){
        health = startingHealth;
        gController.EquipWeapon(waveNumber-1);
    }
    
    public override void Die(){
        AudioManager.instance.PlaySound("Player Death", transform.position);
        base.Die();
    }
}
