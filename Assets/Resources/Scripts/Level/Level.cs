using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

class Level : MonoBehaviour
{
    // Start is called before the first frame update
    public void Start()
    {
        GameObject[] gameObjectsToDestroy = GameObject.FindGameObjectsWithTag("Destroy");
        foreach (GameObject gameObjectToDestroy in gameObjectsToDestroy)
        {
            Destroy(gameObjectToDestroy);
        }
    }
}
