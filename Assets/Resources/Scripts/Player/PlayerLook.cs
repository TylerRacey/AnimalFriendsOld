using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    private CharacterController characterController;
    private PlayerMovement playerMovement;
    private Animator playerAnimator;
    private PlayerInput playerInput;
    private Camera cameraComponent;

    [SerializeField]
    private Transform player, playerEye, mainCamera;

    [SerializeField]
    private bool inverted = false;

    [SerializeField]
    private float mouseSensitivity = 5.0f;

    private float mousePitchMin = -70.0f;
    private float mousePitchMax = 80.0f;

    private int groundImpactAnimationIndex = 0;

    private float cameraDefaultFoV = 65.0f;
    private float cameraZoomedFoV = 50.0f;
    private float cameraZoomInRate = 5.0f;
    private float cameraZoomOutRate = 5.0f;

    private bool wasGrounded = true;

    private PlayerMovement.Stance previousStance;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        playerAnimator = mainCamera.GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        cameraComponent = mainCamera.GetComponent<Camera>();
    }

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        LockAndUnlockCursor();

        if( Cursor.lockState == CursorLockMode.Locked)
        {
            Look();

            ApplyCameraFoV();

            // ApplyGroundImpactShake();
            // ApplyStanceChangeShake();
        }

        wasGrounded = characterController.isGrounded;
        previousStance = playerMovement.currentStance;
    }

    private void LockAndUnlockCursor()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if( Cursor.lockState == CursorLockMode.Locked )
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void Look()
    {
        Vector2 mouseInput = new Vector2(Input.GetAxis(MouseAxis.MOUSE_Y), Input.GetAxis(MouseAxis.MOUSE_X));

        float mousePitchAddition = (mouseInput.x * mouseSensitivity * invertValue());
        float newMousePitch = AngleClamp180(playerEye.localEulerAngles.x + mousePitchAddition);
        newMousePitch = Mathf.Clamp(newMousePitch, mousePitchMin, mousePitchMax);

        playerEye.localEulerAngles = new Vector3(newMousePitch, playerEye.localEulerAngles.y, playerEye.localEulerAngles.z);

        float mouseYawAddition = (mouseInput.y * mouseSensitivity);
        float newMouseYaw = AngleClamp180(player.localEulerAngles.y + mouseYawAddition);

        player.localEulerAngles = new Vector3(player.localEulerAngles.x, newMouseYaw, player.localEulerAngles.z);
    }

    private void ApplyCameraFoV()
    {
        if(playerInput.ZoomPressed())
        {
            cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, cameraZoomedFoV, cameraZoomInRate * Time.deltaTime);
            cameraComponent.cullingMask |= 1 << LayerMask.NameToLayer("UnderWorld");
        }
        else
        {
            cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, cameraDefaultFoV, cameraZoomOutRate * Time.deltaTime);
            cameraComponent.cullingMask &= ~(1 << LayerMask.NameToLayer("UnderWorld"));
        }
    }

    private void ApplyGroundImpactShake()
    {
        if ((characterController.isGrounded && !wasGrounded))
        {
            string[] animations = GroundImpactAnimations();
            string animation = animations[groundImpactAnimationIndex];

            playerAnimator.CrossFade(animation, 0.4f, -1);

            groundImpactAnimationIndex = (int)Mathf.Repeat(groundImpactAnimationIndex + 1, animations.Length);
        }
    }

    private string[] GroundImpactAnimations()
    {
        return new string[] { "CameraGroundImpactA", "CameraGroundImpactB" };
    }

    private void ApplyStanceChangeShake()
    {
        if (playerMovement.PlayerStanding() && !playerMovement.PlayerWasStanding(previousStance))
        {
            playerAnimator.CrossFade("CameraToStand", 0.4f, -1 );
        }

        if (playerMovement.PlayerProne() && !playerMovement.PlayerWasProne(previousStance))
        {
            playerAnimator.CrossFade("CameraProneImpact", 0.4f, -1);
        }

        if (playerMovement.PlayerCrouching() && playerMovement.PlayerWasStanding(previousStance))
        {
            playerAnimator.CrossFade("CameraStandToCrouch", 0.4f, -1);
        }
    }

    private int invertValue()
    {
        if( inverted )
        {
            return 1;
        }
        else
        {
            return -1;
        }
    }

    private float AngleClamp180(float angle)
    {
        if (angle > 180)
        {
            angle -= 360;
        }

        return angle;
    }
}
