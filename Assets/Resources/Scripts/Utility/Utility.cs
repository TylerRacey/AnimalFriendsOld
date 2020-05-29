using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utility
{
    public static GameObject[] GetAllItems()
    {
        return GameObject.FindGameObjectsWithTag("Item");
    }

    public static GameObject[] GetAllDestructibleTrees()
    {
        return GameObject.FindGameObjectsWithTag("Destructible_Tree");
    }

    public static GameObject[] SortByDistance(GameObject[] gameObjects, Vector3 position)
    {
        for (int primary = 0; primary <= gameObjects.Length - 2; primary++)
        {
            for (int secondary = 0; secondary <= gameObjects.Length - 2; secondary++)
            {
                if(Vector3.SqrMagnitude(gameObjects[secondary].transform.position - position) > Vector3.SqrMagnitude(gameObjects[secondary + 1].transform.position - position) )
                {
                    GameObject tempGameObject = gameObjects[secondary + 1];
                    gameObjects[secondary + 1] = gameObjects[secondary];
                    gameObjects[secondary] = tempGameObject;
                }
            }
        }

        return gameObjects;
    }

    public static List<GameObject> SortByDistance(List<GameObject> gameObjects, Vector3 position)
    {
        for (int primary = 0; primary <= gameObjects.Count - 2; primary++)
        {
            for (int secondary = 0; secondary <= gameObjects.Count - 2; secondary++)
            {
                if (Vector3.SqrMagnitude(gameObjects[secondary].transform.position - position) > Vector3.SqrMagnitude(gameObjects[secondary + 1].transform.position - position))
                {
                    GameObject tempGameObject = gameObjects[secondary + 1];
                    gameObjects[secondary + 1] = gameObjects[secondary];
                    gameObjects[secondary] = tempGameObject;
                }
            }
        }

        return gameObjects;
    }

    public static List<GameObject> GetChildrenWithTag(GameObject parent, string tag)
    {
        List<GameObject> children = new List<GameObject>();

        for (int index = 0; index < parent.transform.childCount; index++)
        {
            GameObject child = parent.transform.GetChild(index).gameObject;

            if (child.tag == tag)
            {
                children.Add(child);
            }
         }

        return children;
    }

    public static float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}
