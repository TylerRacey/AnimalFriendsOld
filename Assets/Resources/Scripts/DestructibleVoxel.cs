using System.Collections;
using UnityEngine;

public class DestructibleVoxel : MonoBehaviour
{
    private Game game;

    public BoxCollider boxCollider;

    public bool active;
    public VoxelStruct voxelStruct;
    public Destructible destructible;

    void Start()
    {
        game = Game.GetGame();

        boxCollider = Utility.VoxelCreateBoxCollider(gameObject);
    }

    public void SetInactive()
    {
        gameObject.transform.position = game.destructibleVoxelsParentTransform.position;

        active = false;
        destructible = null;
        voxelStruct.destructibleVoxel = null;
    }
}
