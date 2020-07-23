using UnityEditor;
using UnityEngine;

//Declare what type class/script this is an editor for
[CustomEditor (typeof(MapGenerator))]
public class MapEditor : Editor
{
    public override void OnInspectorGUI(){
        MapGenerator map = target as MapGenerator;
        if(DrawDefaultInspector()){
            //target is of class as defined above
            map.GenerateMap();
        }

        if(GUILayout.Button("Generate Map")){
            map.GenerateMap();
        }

    }
}
