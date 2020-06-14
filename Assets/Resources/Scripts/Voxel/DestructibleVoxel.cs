using UnityEngine;

public class DestructibleVoxel : MonoBehaviour
{
    private Game game;

    public BoxCollider boxCollider;

    public bool active;
    public VoxelStruct voxelStruct;
    public Destructible destructible;
    private DestructibleVoxel thisDestructibleVoxel;

    private Transform destructibleVoxelsParentTransform;
    private Transform voxelTransform;

    void Start()
    {
        game = Game.GetGame();
        voxelTransform = transform;

        destructibleVoxelsParentTransform = game.destructibleVoxelsParentTransform;

        boxCollider = Utility.VoxelCreateBoxCollider(gameObject);

        thisDestructibleVoxel = this;
    }

    public void SetActive(VoxelStruct parentVoxelStruct, Destructible parentDestructible)
    {
        Transform parentTransform = parentDestructible.transform;
        voxelTransform.position = parentTransform.TransformPoint(parentVoxelStruct.localPosition);
        voxelTransform.rotation = parentTransform.rotation;
        if (parentVoxelStruct != null)
        {
            parentVoxelStruct.destructibleVoxel = null;
        }

        voxelStruct = parentVoxelStruct;
        destructible = parentDestructible;
        voxelStruct.destructibleVoxel = thisDestructibleVoxel;
        active = true;
    }

    public void SetInactive()
    {
        voxelTransform.position = destructibleVoxelsParentTransform.position;

        destructible = null;
        if (voxelStruct != null)
        {
            voxelStruct.destructibleVoxel = null;
        }
        voxelStruct = null;
        active = false;
    }
}
