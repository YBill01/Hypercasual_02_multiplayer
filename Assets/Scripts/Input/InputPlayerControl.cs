using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

public class InputPlayerControl : MonoBehaviour
{
	private NPlayerController _playerController;
	private VCamera _vCamera;

	private InputSystem_Actions _inputActions;

	[Inject]
	public void Construct(
		VCamera vCamera)
	{
		_vCamera = vCamera;
	}

	private void Awake()
	{
		_inputActions = new InputSystem_Actions();
	}

	private void OnEnable()
	{
		//_inputActions.Enable();

		_inputActions.Player.Move.performed += OnMoveDelta;
		_inputActions.Player.Move.canceled += OnMoveDelta;

		_inputActions.Player.Jump.performed += OnJumpButton;
		_inputActions.Player.Jump.canceled += OnJumpButton;

		_inputActions.Player.Attack.performed += OnAttackButton;
		_inputActions.Player.Attack.canceled += OnAttackButton;

		_inputActions.Player.Zoom.performed += OnZoomDelta;
	}
	private void OnDisable()
	{
		_inputActions.Player.Move.performed -= OnMoveDelta;
		_inputActions.Player.Move.canceled -= OnMoveDelta;

		_inputActions.Player.Jump.performed -= OnJumpButton;
		_inputActions.Player.Jump.canceled -= OnJumpButton;

		_inputActions.Player.Attack.performed -= OnAttackButton;
		_inputActions.Player.Attack.canceled -= OnAttackButton;

		_inputActions.Player.Zoom.performed -= OnZoomDelta;

		//_inputActions.Disable();
	}

	public void SetPlayerController(NPlayerController playerController)
	{
		_playerController = playerController;

		if (_playerController != null)
		{
			_inputActions.Enable();
		}
		else
		{
			_inputActions.Disable();
		}
	}

	private void OnMoveDelta(InputAction.CallbackContext context)
	{
		Vector2 value = context.ReadValue<Vector2>();

		Vector3 valueOnCamera = Quaternion.AngleAxis(_vCamera.transform.eulerAngles.y, Vector3.up) * new Vector3(value.x, 0.0f, value.y);

		_playerController.Move(valueOnCamera);
	}
	private void OnJumpButton(InputAction.CallbackContext context)
	{
		bool value = context.ReadValueAsButton();

		_playerController.Jump(value);
	}
	private void OnAttackButton(InputAction.CallbackContext context)
	{
		bool value = context.ReadValueAsButton();

		_playerController.Kick(value);
	}
	
	private void OnZoomDelta(InputAction.CallbackContext context)
	{
		Vector2 value = context.ReadValue<Vector2>();

		_vCamera.distanceValue = Math.Clamp(_vCamera.distanceValue - value.y * 0.5f, 0, 1);
	}
}