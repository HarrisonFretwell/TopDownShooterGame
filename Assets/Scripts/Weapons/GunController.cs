using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
    Gun equippedGun;
    public Transform weaponHold;
    public Gun[] guns;

    void Start(){
        
    }

    public void EquipWeapon(Gun gunToEquip){
        if(equippedGun != null){
            Destroy(equippedGun.gameObject);
        }
        equippedGun = Instantiate (gunToEquip, weaponHold.position, weaponHold.rotation) as Gun;
        equippedGun.transform.parent = weaponHold;
    }
    
    public void EquipWeapon(int weaponIndex){
        EquipWeapon(guns[weaponIndex]);
    }

    public void OnTriggerHold(){
        if(equippedGun != null){
            equippedGun.OnTriggerHold();
        }
    }

    public void OnTriggerRelease(){
        if(equippedGun != null){
            equippedGun.OnTriggerRelease();
        }
    }
    
    public void Aim(Vector3 aimPoint){
        if(equippedGun != null){
            equippedGun.OnTriggerRelease();
            equippedGun.Aim(aimPoint);
        }
    }
    
    public void Reload(){
        if(equippedGun != null){
            equippedGun.Reload();
        }
    }
    
    public float GunHeight{
        get {
            return weaponHold.position.y;
        }
    }

}
