using System.Collections;
using UnityEngine;

public class SeperatedVoxel : MonoBehaviour
{
    private Game game;

    public Rigidbody rigidBody;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    private float triggeredMovementDuration;
    private const float cameraGoalPositionUpOffset = -0.45f;
    private Vector3 triggeredMovementFinalScale = new Vector3(0.60f, 0.60f, 0.60f);

    public bool active;
    public bool triggered;

    void Start()
    {
        game = Game.GetGame();

        rigidBody = gameObject.AddComponent<Rigidbody>();
        rigidBody.Sleep();

        meshFilter = gameObject.GetComponent<MeshFilter>();
        meshRenderer = gameObject.GetComponent<MeshRenderer>();

        Utility.VoxelCreateBoxCollider(gameObject);

        triggeredMovementDuration = Random.Range(0.30f, 0.55f);
    }

    public IEnumerator TriggeredMovementToInventory()
    {
        triggered = true;

        Vector3 originalScale = transform.localScale;
        Vector3 originalPosition = transform.position;
        Quaternion originalRotation = transform.rotation;

        for (float fraction = 0.0f; fraction < 1.0f; fraction += Time.deltaTime / triggeredMovementDuration)
        {
            transform.position = Vector3.Lerp(originalPosition, game.playerEye.transform.position + game.playerEye.transform.up * cameraGoalPositionUpOffset, fraction);

            transform.rotation = Quaternion.Lerp(originalRotation, Quaternion.Euler(game.player.transform.eulerAngles), fraction);

            transform.localScale = Vector3.Lerp(originalScale, triggeredMovementFinalScale, fraction);

            yield return null;
        }

        SetInactive();

        yield return null;
    }

    private void SetInactive()
    {
        gameObject.transform.position = game.seperatedVoxelsParentTransform.position;
        gameObject.transform.localScale = Vector3.one;

        rigidBody.velocity = Vector3.zero;
        rigidBody.Sleep();

        triggered = false;
        active = false;
    }
}
