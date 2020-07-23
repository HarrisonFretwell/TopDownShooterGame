using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundLibrary : MonoBehaviour
{
    public SoundGroup[] soundGroups;
    
    Dictionary<string, AudioClip[]> groupDictionary = new Dictionary<string, AudioClip[]>();
    
    void Awake(){
        foreach(SoundGroup soundGroup in soundGroups){
            groupDictionary.Add(soundGroup.groupID,soundGroup.group);
        }
    }
    
    public AudioClip GetClipFromName(string name){
        //If is a group of sounds, return a random one
        if(groupDictionary.ContainsKey(name)){
            AudioClip[] sounds = groupDictionary [name];
            return sounds[Random.Range(0,sounds.Length)];
        }
        return null;
    }
    
    [System.Serializable]
    public class SoundGroup {
        public string groupID;
        public AudioClip[] group;
    }
}
