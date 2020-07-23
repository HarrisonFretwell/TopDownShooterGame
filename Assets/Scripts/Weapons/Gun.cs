using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public enum FireMode {Auto, Burst, Single};
    public FireMode fireMode;
    
    [Header("Characteristics")]
    public Transform[] projectileSpawns;
    public Projectile projectile;
    public float msBetweenShots = 100;
    public float muzzleVelocity = 35;
    public int burstCount;
    public int projectilesPerMag;
    public float reloadTime = .4f;
    [Header("Recoil")]
    public float maxRecoilAngle = 20;
    public Vector2 kickkMinMax = new Vector2(.05f,.2f);
    public Vector2 recoilAngleMinMax = new Vector2(3,8);
    public float recoilKickbackReturnTime = .1f;
    public float recoilRotationReturnTime = .1f;
    float nextShotTime;
    [Header("Effects")]
    public Transform shell;
    public Transform shellEjection;
    public AudioClip shootAudio;
    public AudioClip reloadAudio;
    MuzzleFlash muzzleFlash;

    bool triggerReleasedSinceLastShot;
    int shotsRemainingInBurst;
    int projectilesRemainingInMag;
    bool isReloading; 
    
    Vector3 recoilSmoothDampVelocity;
    float recoilRotSmoothDampVelocity;
    float recoilAngle;

    void Start(){
        muzzleFlash = GetComponent<MuzzleFlash>();
        shotsRemainingInBurst = burstCount;
        projectilesRemainingInMag = projectilesPerMag;
    }
    void LateUpdate(){
        //Animate recoil
        transform.localPosition = Vector3.SmoothDamp(transform.localPosition, Vector3.zero,ref recoilSmoothDampVelocity, recoilKickbackReturnTime);
        recoilAngle = Mathf.SmoothDamp(recoilAngle,0,ref recoilRotSmoothDampVelocity,recoilRotationReturnTime);
        transform.localEulerAngles = transform.localEulerAngles + Vector3.left * recoilAngle;
        if(!isReloading && projectilesRemainingInMag < 1){
            Reload();
        }
    }

    void Shoot(){
        //Check if shooting allowed
        if(!isReloading && Time.time > nextShotTime && projectilesRemainingInMag > 0){
            //Deal with different fire modes
            if (fireMode == FireMode.Burst){
                if(shotsRemainingInBurst == 0){
                    return;
                }
                shotsRemainingInBurst--;
                
            }
            else if(fireMode == FireMode.Single){
                if(!triggerReleasedSinceLastShot){
                    return;
                }
            }
            //Shoot projectiles
            foreach (Transform projectileSpawn in projectileSpawns)
            {
                if(projectilesRemainingInMag == 0){
                    break;
                }
                projectilesRemainingInMag--;
                Projectile newProjectile = Instantiate(projectile, projectileSpawn.position, projectileSpawn.rotation) as Projectile;
                newProjectile.setSpeed(muzzleVelocity);
                nextShotTime = Time.time + msBetweenShots/1000;

                
            }
            Instantiate(shell,shellEjection.position, shellEjection.rotation);
            muzzleFlash.Activate();
            
            AudioManager.instance.PlaySound(shootAudio,transform.position);
            
            //Recoil
            transform.localPosition -= Vector3.forward *Random.Range(kickkMinMax.x,kickkMinMax.y);
            recoilAngle += Random.Range(recoilAngleMinMax.x,recoilAngleMinMax.y);
            recoilAngle = Mathf.Clamp(recoilAngle,0,maxRecoilAngle);
            
        }
    }
    
    public void Aim(Vector3 aimPoint){
        transform.LookAt(aimPoint);
    }
    
    public void Reload(){
        if(!isReloading && projectilesRemainingInMag < projectilesPerMag){
            StartCoroutine(AnimateReload());
            AudioManager.instance.PlaySound(reloadAudio,transform.position);
        }
    }
    
    IEnumerator AnimateReload(){
        isReloading = true;
        yield return new WaitForSeconds(.2f);
        
        const float maxReloadAngle = 45;
        Vector3 initialRot = transform.localEulerAngles;
        float reloadSpeed = 1/reloadTime;
        float percent = 0;
        while(percent < 1){
            percent += Time.deltaTime * reloadSpeed;
            //Animate gun rotating
            float interpolation = (-Mathf.Pow(percent,2)+percent)*4;
            float reloadAngle = Mathf.Lerp(0,maxReloadAngle,interpolation);
            transform.localEulerAngles = initialRot + Vector3.left * reloadAngle;
            yield return null;
        }
        
        projectilesRemainingInMag = projectilesPerMag;
        isReloading = false;
        
    }

    public void OnTriggerHold(){
        Shoot();
        triggerReleasedSinceLastShot = false;
    }

    public void OnTriggerRelease(){
        triggerReleasedSinceLastShot = true;
        shotsRemainingInBurst = burstCount;
    }
}
