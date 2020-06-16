using System.Collections;
using UnityEngine;

public class SeperatedVoxel : MonoBehaviour
{
    private Game game;

    public Rigidbody rigidBody;
    private Material material;
    public BoxCollider boxCollider;

    public const float pickSpeedMinSquared = 0.10f * 0.10f;

    private float triggeredMovementDuration;
    private const float cameraGoalPositionUpOffset = -0.45f;
    private Vector3 triggeredMovementFinalScale = new Vector3(0.60f, 0.60f, 0.60f);

    public bool active;
    public bool triggered;
    public bool hasHadEnoughVelocity;

    private float hasHadEnoughVelocityTimeoutDelay;
    private float hasHadEnoughVelocityTimeout = 0.0f;

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

        boxCollider = Utility.VoxelCreateBoxCollider(gameObject);

        triggeredMovementDuration = Random.Range(0.30f, 0.55f);
        hasHadEnoughVelocityTimeoutDelay = Random.Range(0.10f, 0.20f);
    }

    private void Update()
    {
        if (active && ((Time.time >= hasHadEnoughVelocityTimeout) || (!hasHadEnoughVelocity && rigidBody.velocity.sqrMagnitude >= pickSpeedMinSquared)))
        {
            hasHadEnoughVelocity = true;
        }
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

    public void SetActive(VoxelStruct voxelStruct, Transform parentTransform)
    {
        active = true;

        voxelTransform.position = parentTransform.TransformPoint(voxelStruct.localPosition);
        voxelTransform.rotation = parentTransform.rotation;

        material.color = voxelStruct.color;

        rigidBody = gameObject.AddComponent<Rigidbody>();
        rigidBody.mass = Voxel.MASS;

        hasHadEnoughVelocityTimeout = Time.time + hasHadEnoughVelocityTimeoutDelay;
    }

    private void SetInactive()
    {
        DestroyImmediate(rigidBody);

        voxelTransform.position = seperatedVoxelsParentTransform.position;
        voxelTransform.localScale = Vector3.one;

        hasHadEnoughVelocity = false;
        active = false;
        triggered = false;
    }
}
