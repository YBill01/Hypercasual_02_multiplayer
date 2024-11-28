using UnityEditor;
using UnityEngine;
using static VCameraTarget;

public class NPlayerController : MonoBehaviour, IUpdatable
{
	[Header("Moving")]
	[SerializeField]
	private float m_moveSpeed;
	[SerializeField]
	private float m_moveSpeedDamping = 8.0f;

	[Header("Jump")]
	[SerializeField]
	private float m_jumpHeight = 1.0f;

	[Header("Rotate")]
	[SerializeField]
	private float m_rotationSpeed = 8.0f;

	[Header("Gravity")]
	[SerializeField]
	private Vector3 m_gravity = Physics.gravity;

	private bool _isMoving;
	public bool IsMoving => _isMoving;

	private bool _isJumping;
	public bool IsJumping => _isJumping;

	private bool _isCollecting;
	public bool IsCollecting => _isCollecting;
	
	private bool _isKick;
	public bool IsKick => _isKick;

	public bool IsGraunded => _characterController.isGrounded;

	private NPlayer _player;
	private NAvatar _avatar;
	private CharacterController _characterController;

	private Vector3 _moveVelocity;
	private Vector3 _targetMoveVelocity;
	private Vector3 _moveDirection;
	private Vector3 _jumpVelocity;
	private Vector3 _impactVelocity;

	private void Awake()
	{
		_characterController = GetComponent<CharacterController>();
		_player = GetComponent<NPlayer>();
		_avatar = GetComponent<NAvatar>();
	}

	private void Start()
	{
		SetOrientation(transform.position, transform.rotation);

		_avatar.SetMoveSpeed(m_moveSpeed);
	}

	public void OnUpdate(float deltaTime)
	{
		UpdateGravity(deltaTime);
		UpdateImpact(deltaTime);

		ProcessMove();
		ProcessJump();
		ProcessCollect();
		
		ApplyRotation(deltaTime);
		ApplyMove(deltaTime);
	}

	/*public void SetPause(bool pause)
	{
		//_avatar.SetPause(pause);
	}*/

	public void SetOrientation(Vector3 position, Quaternion rotation)
	{
		transform.SetPositionAndRotation(position, rotation);
		_moveDirection = rotation * Vector3.forward;
	}

	public void Move(Vector3 velocity)
	{
		_targetMoveVelocity = Vector3.ClampMagnitude(velocity, 1.0f);
		if (!velocity.Equals(Vector3.zero))
		{
			_isMoving = true;
		}
		else
		{
			_isMoving = false;
		}
	}
	public void Jump(bool value)
	{
		_isJumping = value;
	}
	public void Collect()
	{
		_isCollecting = true;
	}
	public void Kick(bool value)
	{
		_isKick = value;

		_player.IsKick.Value = value;
	}
	
	public void Impact(Vector3 direction, float strength)
	{
		_impactVelocity = direction * strength;
		_impactVelocity.y = Mathf.Sqrt(m_jumpHeight * -16.0f * m_gravity.y);
	}

	private void ProcessMove()
	{
		_avatar.SetMoveVelocity(_moveVelocity.magnitude);
	}
	private void ProcessJump()
	{
		if (IsGraunded && _isJumping)
		{
			_jumpVelocity.y += Mathf.Sqrt(m_jumpHeight * -2.0f * m_gravity.y);

			_avatar.SetJump();
		}
	}
	private void ProcessCollect()
	{
		if (IsGraunded && _isCollecting && !_isJumping && !_isMoving)
		{
			_avatar.SetCollect();
		}

		_isCollecting = false;
	}
	
	private void UpdateGravity(float deltaTime)
	{
		if (IsGraunded && _jumpVelocity.y < 0.0f)
		{
			_jumpVelocity.y = -1.0f;
		}
		else
		{
			_jumpVelocity.y += m_gravity.y * Time.deltaTime;
		}
	}
	private void UpdateImpact(float deltaTime)
	{
		_impactVelocity = Vector3.Lerp(_impactVelocity, Vector3.zero, 8.0f * deltaTime);
	}

	private void ApplyRotation(float deltaTime)
	{
		Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
		transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, m_rotationSpeed * Time.deltaTime);
	}
	private void ApplyMove(float deltaTime)
	{
		_moveVelocity = Vector3.Lerp(_moveVelocity, _targetMoveVelocity, m_moveSpeedDamping * Time.deltaTime);

		if (_moveVelocity.sqrMagnitude >= 0.01f)
		{
			_moveDirection = _moveVelocity.normalized;
		}

		_characterController.Move(((_moveDirection * _moveVelocity.magnitude * m_moveSpeed) + _jumpVelocity + _impactVelocity) * Time.deltaTime);
	}

#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		if (Application.isPlaying)
		{
			Handles.color = Color.green;
			Handles.DrawLine(transform.position, transform.position + (_moveDirection * (_moveVelocity.magnitude * m_moveSpeed)), 1.0f);
			Handles.color = Color.white;
			Handles.DrawLine(transform.position, transform.position + _moveDirection, 1.0f);
		}
	}
#endif
}