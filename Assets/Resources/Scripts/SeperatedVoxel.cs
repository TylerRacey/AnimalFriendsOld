using System.Collections;
using UnityEngine;

public class SeperatedVoxel : MonoBehaviour
{
    private Game game;

    public Rigidbody rigidBody;
    private Material material;

    private float triggeredMovementDuration;
    private const float cameraGoalPositionUpOffset = -0.45f;
    private Vector3 triggeredMovementFinalScale = new Vector3(0.60f, 0.60f, 0.60f);

    private float canBeTriggeredDelay;

    public bool active;
    public bool triggered;
    public bool canBeTriggered = true;

    private Transform seperatedVoxelsParentTransform;
    private Transform voxelTransform;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private Transform playerEyeTransform;
    private Transform playerTransform;

    void Start()
    {
        game = Game.GetGame();
        playerEyeTransform = game.playerEye.transform;
        playerTransform = game.player.transform;

        seperatedVoxelsParentTransform = game.seperatedVoxelsParentTransform;
        voxelTransform = transform;

        material = gameObject.GetComponent<MeshRenderer>().material;

        Utility.VoxelCreateBoxCollider(gameObject);

        triggeredMovementDuration = Random.Range(0.30f, 0.55f);
        canBeTriggeredDelay = Random.Range(0.05f, 0.35f);
    }

    public IEnumerator TriggeredMovementToInventory()
    {
        triggered = true;

        originalPosition = voxelTransform.position;
        originalRotation = voxelTransform.rotation;

        for (float fraction = 0.0f; fraction < 1.0f; fraction += Time.deltaTime / triggeredMovementDuration)
        {
            voxelTransform.position = Vector3.Lerp(originalPosition, playerEyeTransform.position + playerEyeTransform.up * cameraGoalPositionUpOffset, fraction);

            voxelTransform.rotation = Quaternion.Lerp(originalRotation, Quaternion.Euler(playerTransform.eulerAngles), fraction);

            voxelTransform.localScale = Vector3.Lerp(Vector3.one, triggeredMovementFinalScale, fraction);

            yield return null;
        }

        SetInactive();

        yield return null;
    }

    public void SetActive(VoxelStruct voxelStruct, Destructible parentDestructible)
    {
        active = true;

        Transform parentDestructibleTransform = parentDestructible.transform;
        voxelTransform.position = parentDestructibleTransform.TransformPoint(voxelStruct.localPosition);
        voxelTransform.rotation = parentDestructibleTransform.rotation;

        material.color = voxelStruct.color;

        rigidBody = gameObject.AddComponent<Rigidbody>();

        // StartCoroutine(CanBeTriggeredLogic());
    }

    private IEnumerator CanBeTriggeredLogic()
    {
        canBeTriggered = false;

        yield return new WaitForSeconds(canBeTriggeredDelay);

        canBeTriggered = true;
    }

    private void SetInactive()
    {
        DestroyImmediate(rigidBody);

        voxelTransform.position = seperatedVoxelsParentTransform.position;
        voxelTransform.localScale = Vector3.one;

        active = false;
        triggered = false;
    }
}
