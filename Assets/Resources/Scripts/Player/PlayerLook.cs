using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    private Game game;

    private Camera cameraComponent;
    private Animator playerAnimator;

    private bool inverted = false;

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

    // Start is called before the first frame update
    void Start()
    {
        game = Game.GetGame();

        cameraComponent = game.mainCamera.GetComponent<Camera>();
        playerAnimator = game.mainCamera.GetComponent<Animator>();

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

        wasGrounded = game.characterController.isGrounded;
        previousStance = game.playerMovement.currentStance;
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
        float newMousePitch = AngleClamp180(game.playerEye.localEulerAngles.x + mousePitchAddition);
        newMousePitch = Mathf.Clamp(newMousePitch, mousePitchMin, mousePitchMax);

        game.playerEye.localEulerAngles = new Vector3(newMousePitch, game.playerEye.localEulerAngles.y, game.playerEye.localEulerAngles.z);

        float mouseYawAddition = (mouseInput.y * mouseSensitivity);
        float newMouseYaw = AngleClamp180(game.player.transform.localEulerAngles.y + mouseYawAddition);

        game.player.transform.localEulerAngles = new Vector3(game.player.transform.localEulerAngles.x, newMouseYaw, game.player.transform.localEulerAngles.z);
    }

    private void ApplyCameraFoV()
    {
        if(game.playerInput.ZoomPressed())
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
        if ((game.characterController.isGrounded && !wasGrounded))
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
        if (game.playerMovement.PlayerStanding() && !game.playerMovement.PlayerWasStanding(previousStance))
        {
            playerAnimator.CrossFade("CameraToStand", 0.4f, -1 );
        }

        if (game.playerMovement.PlayerProne() && !game.playerMovement.PlayerWasProne(previousStance))
        {
            playerAnimator.CrossFade("CameraProneImpact", 0.4f, -1);
        }

        if (game.playerMovement.PlayerCrouching() && game.playerMovement.PlayerWasStanding(previousStance))
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
