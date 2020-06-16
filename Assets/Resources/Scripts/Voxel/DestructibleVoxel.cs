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

    public void SetActive(VoxelStruct parentVoxelStruct, Destructible parentDestructible, Transform parentTransform)
    {
        voxelTransform.SetParent(parentTransform, false);
        voxelTransform.localPosition = parentVoxelStruct.localPosition;

        if (voxelStruct != null)
        {
            voxelStruct.destructibleVoxel = null;
        }

        voxelStruct = parentVoxelStruct;
        destructible = parentDestructible;
        voxelStruct.destructibleVoxel = thisDestructibleVoxel;
        active = true;
    }

    public void SetInactive()
    {
        voxelTransform.SetParent(destructibleVoxelsParentTransform, false);

        if (voxelStruct != null)
        {
            voxelStruct.destructibleVoxel = null;
        }
        voxelStruct = null;
        destructible = null;

        active = false;
    }
}
