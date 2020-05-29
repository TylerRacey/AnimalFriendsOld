using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructibleTree : MonoBehaviour
{
    List<GameObject> destructibleVoxels;
    // Start is called before the first frame update
    void Start()
    {
        destructibleVoxels = Utility.GetChildrenWithTag(gameObject, "Destructible_Tree_Voxel");
        for (int index = 0; index < destructibleVoxels.Count; index++)
        {
            Rigidbody rigidBody = destructibleVoxels[index].GetComponent<Rigidbody>();
            rigidBody.Sleep();
            rigidBody.detectCollisions = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TakeDamage(Vector3 position)
    {
        destructibleVoxels = Utility.SortByDistance(destructibleVoxels, position);
        for(int index = 0; index < 5;index++)
        {
            LaunchVoxel(destructibleVoxels[index]);
        }
    }

    private void LaunchVoxel(GameObject voxel)
    {
        Rigidbody rigidBody = voxel.GetComponent<Rigidbody>();
        rigidBody.WakeUp();
        rigidBody.detectCollisions = true;
        rigidBody.AddForce(new Vector3(Random.Range(-30, 30), Random.Range(80, 130), Random.Range(-30, 30)));
    }
}
