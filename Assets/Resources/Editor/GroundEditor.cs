using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class GroundEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Build Object"))
        {

        }
    }
}
