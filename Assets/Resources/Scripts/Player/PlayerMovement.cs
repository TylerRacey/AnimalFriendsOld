using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Game game;

    private Vector3 worldMoveVector;
    public Vector3 localMoveVector;
    private Vector3 previousLocalMoveVector;

    private const float gravity = 35.0f;

    public float maxSpeed;
    private const float sprintSpeed = 8.0f;
    private const float walkSpeed = 5.0f;
    private const float crouchSpeed = 2.0f;
    private const float proneSpeed = 0.85f;

    private const float standEyeHeight = 1.6f;
    private const float crouchEyeHeight = 1.0f;
    private const float slideEyeHeight = 0.5f;
    private const float proneEyeHeight = 0.2f;
    private const float stanceChangeRate = 8.0f;

    private const float standCapsuleHeight = 2.0f;
    private const float crouchCapsuleHeight = 1.4f;
    private const float slideCapsuleHeight = 0.9f;
    private const float proneCapsuleHeight = 0.6f;

    public enum Stance
    {
        STAND,
        CROUCH,
        SLIDE,
        PRONE
    }

    public bool isSprinting;
    public bool sprintKeyUpRequired;
    private const float sprintNormalizedForwardMin = 0.5f;

    public Stance currentStance;
    public Stance previousStance;

    private const float jumpStandVerticalForce = 10.0f;
    private const float jumpSlideVerticalForce = 10.0f;
    private float verticalSpeed;

    private const float slideForce = 0.37f;
    private Vector3 currentSlideVelocity;
    private const float slideFrictionRate = 3.1f;
    private const float slideCompleteSpeed = 0.01f;

    // Start is called before the first frame update
    void Awake()
    {
        game = Game.GetGame();

        PlayerStanceSetStand();
    }

    // Update is called once per frame
    void Update()
    {
        PlayerStanceUpdate();

        PlayerMoveUpdate();

        PlayerEyeUpdate();

        previousStance = currentStance;
    }

    private void PlayerStanceUpdate()
    {
        if (game.playerInput.JumpPressed())
        {
            PlayerStanceSetStand();
        }

        //if (playerInput.PronePressed())
        //{
        //    if (PlayerProne())
        //    {
        //        PlayerStanceSetStand();
        //    }
        //    else
        //    {
        //        PlayerStanceSetProne();
        //    }
        //}

        if (game.playerInput.CrouchPressed() && !game.playerInput.SprintPressed())
        {
            if (PlayerCrouching())
            {
                PlayerStanceSetStand();
            }
            else if(playerCanCrouch())
            {
                PlayerStanceSetCrouch();
            }
        }

        if (PlayerSliding())
        {
            if (Vector3.Magnitude(currentSlideVelocity) <= slideCompleteSpeed)
            {
                PlayerStanceSetCrouch();
                currentSlideVelocity = Vector3.zero;
            }

            if (PlayerFacingCollision())
            {
                PlayerStanceSetCrouch();
                currentSlideVelocity = Vector3.zero;
            }
        }

        if (game.playerInput.SprintPressed() && PlayerCanSprint())
        {
            if (game.playerInput.CrouchPressed())
            {
                PlayerStanceSetSlide();

                PlayerSetSprinting(false);
                sprintKeyUpRequired = true;
            }
            else if(!sprintKeyUpRequired)
            {
                PlayerSetSprinting(true);
                PlayerStanceSetStand();
            }
        } 
        else
        {
            if (sprintKeyUpRequired && game.playerInput.SprintReleased())
            {
                sprintKeyUpRequired = false;
            }

            PlayerSetSprinting(false);
        }
    }

    private bool playerCanCrouch()
    {
        if (PlayerSliding())
            return false;

        return true;
    }

    private bool PlayerCanJump()
    {
        if (game.playerInput.PronePressed())
            return false;

        if (game.playerInput.CrouchPressed())
            return false;

        if (!game.characterController.isGrounded)
            return false;

        return true;
    }

    private bool PlayerCanSprint()
    {
        if (PlayerSliding())
            return false;

        if (game.playerInput.ForwardMovementNormalized() < sprintNormalizedForwardMin)
            return false;

        return true;
    }

    private void PlayerEyeUpdate()
    {
        if (PlayerStanding())
        {
            game.playerEye.localPosition = Vector3.Lerp(game.playerEye.localPosition, new Vector3(0.0f, standEyeHeight, 0.0f), Time.deltaTime * stanceChangeRate);
            game.characterController.height = standCapsuleHeight;
        }
        else if (PlayerCrouching())
        {
            game.playerEye.localPosition = Vector3.Lerp(game.playerEye.localPosition, new Vector3(0.0f, crouchEyeHeight, 0.0f), Time.deltaTime * stanceChangeRate);
            game.characterController.height = crouchCapsuleHeight;
        }
        else if (PlayerSliding())
        {
            game.playerEye.localPosition = Vector3.Lerp(game.playerEye.localPosition, new Vector3(0.0f, slideEyeHeight, 0.0f), Time.deltaTime * stanceChangeRate);
            game.characterController.height = slideCapsuleHeight;
        }
        else if (PlayerProne())
        {
            game.playerEye.localPosition = Vector3.Lerp(game.playerEye.localPosition, new Vector3(0.0f, proneEyeHeight, 0.0f), Time.deltaTime * stanceChangeRate);
            game.characterController.height = proneCapsuleHeight;
        }

        game.characterController.center = new Vector3(0.0f, game.characterController.height * 0.5f, 0.0f);
    }

    private void PlayerSetSprinting(bool boolean)
    {
        isSprinting = boolean;
    }

    private void PlayerStanceSetStand()
    {
        currentStance = Stance.STAND;
    }

    private void PlayerStanceSetCrouch()
    {
        currentStance = Stance.CROUCH;
    }

    private void PlayerStanceSetSlide()
    {
        currentStance = Stance.SLIDE;
    }

    private void PlayerStanceSetProne()
    {
        currentStance = Stance.PRONE;
    }

    public bool PlayerStanding()
    {
        return (currentStance == Stance.STAND);
    }

    public bool PlayerCrouching()
    {
        return (currentStance == Stance.CROUCH);
    }

    public bool PlayerSliding()
    {
        return (currentStance == Stance.SLIDE);
    }

    public bool PlayerProne()
    {
        return (currentStance == Stance.PRONE);
    }

    public bool PlayerWasStanding(Stance lastStance)
    {
        return (lastStance == Stance.STAND);
    }

    public bool PlayerWasCrouching(Stance lastStance)
    {
        return (lastStance == Stance.CROUCH);
    }

    public bool PlayerWasSliding(Stance lastStance)
    {
        return (lastStance == Stance.SLIDE);
    }

    public bool PlayerWasProne(Stance lastStance)
    {
        return (lastStance == Stance.PRONE);
    }

    public bool PlayerSprinting()
    {
        return isSprinting;
    }

    private void PlayerMoveUpdate()
    {
        if (PlayerSprinting())
        {
            maxSpeed = sprintSpeed;
        }
        else if (PlayerStanding())
        {
            maxSpeed = walkSpeed;
        }
        else if (PlayerCrouching())
        {
            maxSpeed = crouchSpeed;
        }
        else if (PlayerProne())
        {
            maxSpeed = proneSpeed;
        }

        if (PlayerSliding())
        {
            ApplySlide();
        }
        else if (game.characterController.isGrounded)
        {
            localMoveVector = PlayerLocalMovement();
        }
        else
        {
            localMoveVector = previousLocalMoveVector;
        }

        worldMoveVector = transform.TransformDirection(localMoveVector);

        ApplyGravity();

        game.characterController.Move(worldMoveVector);
  
        previousLocalMoveVector = localMoveVector;
    }

    public Vector3 PlayerLocalMovement()
    {
        return (game.playerInput.NormalizedMovement() * (maxSpeed * Time.deltaTime));
    }

    public float PlayerSpeed()
    {
        return Vector3.Magnitude(PlayerLocalMovement());
    }

    public float PlayerSpeedNormalized()
    {
        return Vector3.Magnitude(game.playerInput.NormalizedMovement());
    }

    public bool PlayerFacingCollision()
    {
        float traceCapsuleRadius = 0.10f;
        float traceCapsuleForwardDistance = (game.characterController.radius + traceCapsuleRadius);

        Vector3 capsulePointBottom = (transform.position + game.characterController.center);
        capsulePointBottom += (transform.up * ((game.characterController.height * -0.5f) + game.characterController.radius + traceCapsuleRadius));

        Vector3 capsulePointTop = (capsulePointBottom + (transform.up * (game.characterController.height - ((game.characterController.radius * -2.0f) + (traceCapsuleRadius * -2.0f)))));

        return Physics.CapsuleCast(capsulePointBottom, capsulePointTop, traceCapsuleRadius, transform.forward, traceCapsuleForwardDistance);
    }

    private void ApplyGravity()
    {
        verticalSpeed -= (gravity * Time.deltaTime);

        if (game.playerInput.JumpPressed() && PlayerCanJump())
        {
            ApplyJump();
        }

        worldMoveVector.y = (verticalSpeed * Time.deltaTime);
    }

    private void ApplyJump()
    {
        if (PlayerWasStanding(previousStance))
        {
            verticalSpeed = jumpStandVerticalForce;
        }
        else if (PlayerWasSliding(previousStance))
        {
            verticalSpeed = jumpSlideVerticalForce;
        }
    }

    private void ApplySlide()
    {
        if (PlayerWasSliding(previousStance))
        {
            currentSlideVelocity = Vector3.Lerp(currentSlideVelocity, Vector3.zero, Time.deltaTime * slideFrictionRate);

            localMoveVector = currentSlideVelocity;
        }
        else
        {
            currentSlideVelocity = localMoveVector;
            currentSlideVelocity.z += slideForce;
        }
    }
}
