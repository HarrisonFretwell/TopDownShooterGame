using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    public GameObject[] flashHolders;
    public Sprite[] flashSprites;
    public SpriteRenderer[] spriteRenderers;

    public float flashTime;

    void Start(){
        Deactivate();
    }

    public void Activate() {
        foreach(GameObject flashHolder in flashHolders){
            flashHolder.SetActive(true);

            int flashSpriteIndex = Random.Range(0,flashSprites.Length);
            for(int i = 0; i < spriteRenderers.Length; i++){
                spriteRenderers[i].sprite = flashSprites[flashSpriteIndex];
            }

            Invoke("Deactivate",flashTime);
        }

    }
    public void Deactivate() {
        foreach(GameObject flashHolder in flashHolders){
            flashHolder.SetActive(false);
        }
    }

}
