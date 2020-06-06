using System.Collections;
using UnityEngine;

public class SeperatedVoxel : MonoBehaviour
{
    private GameObject player;
    private Transform playerEye;

    private Rigidbody rigidBody;
    private BoxCollider boxCollider;

    private float triggeredMovementDuration;
    private const float minTriggerSpeedSquared = 0.10f * 0.10f;
    private const float cameraGoalPositionUpOffset = -0.45f;
    private static Vector3 triggeredMovementFinalScale = new Vector3(0.60f, 0.60f, 0.60f);

    [HideInInspector]
    public bool canBeTriggered;

    void Start()
    {
        gameObject.name = "SeperatedVoxel";
        gameObject.layer = LayerMask.NameToLayer("SeperatedVoxel");

        player = GameObject.FindWithTag("Player");
        playerEye = player.transform.Find("Eye");

        rigidBody = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();

        triggeredMovementDuration = Random.Range(0.30f, 0.55f);
    }

    void Update()
    {
        if(rigidBody.velocity.sqrMagnitude <= minTriggerSpeedSquared)
        {
            canBeTriggered = true;
            Destroy(rigidBody);

            enabled = false;
        }
    }

    public IEnumerator TriggeredMovementToInventory()
    {
        Destroy(boxCollider);

        Vector3 originalScale = transform.localScale;
        Vector3 originalPosition = transform.position;
        Quaternion originalRotation = transform.rotation;

        for (float fraction = 0.0f; fraction < 1.0f; fraction += Time.deltaTime / triggeredMovementDuration)
        {
            transform.position = Vector3.Lerp(originalPosition, playerEye.transform.position + playerEye.transform.up * cameraGoalPositionUpOffset, fraction);

            transform.rotation = Quaternion.Lerp(originalRotation, Quaternion.Euler(player.transform.eulerAngles), fraction);

            transform.localScale = Vector3.Lerp(originalScale, triggeredMovementFinalScale, fraction);

            yield return null;
        }

        Destroy(this.gameObject);

        yield return null;
    }
}
