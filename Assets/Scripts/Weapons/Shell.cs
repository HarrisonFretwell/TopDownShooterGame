using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shell : MonoBehaviour
{
    public Rigidbody rigidbody;
    public float forceMin;
    public float forceMax;
    public float fadeTime = 1;

    float lifetime_s = 10;

    void Start(){
        float force = Random.Range(forceMin,forceMax);
        rigidbody.AddForce(transform.right * force);
        rigidbody.AddTorque(Random.insideUnitSphere * force);
        StartCoroutine(Fade());
    }

    IEnumerator Fade(){
        yield return new WaitForSeconds(lifetime_s);

        float percent = 0;
        float fadeSpeed = 1/fadeTime;
        Material material = GetComponent<Renderer>().material;
        Color initialColor = material.color;

        while(percent < 1){
            percent += Time.deltaTime*fadeSpeed;
            material.color = Color.Lerp(initialColor, Color.clear, percent);
            yield return null;
        }

        Destroy(gameObject);
    }
}
