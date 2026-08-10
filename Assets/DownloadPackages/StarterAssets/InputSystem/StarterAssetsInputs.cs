using System;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		[Header("Ability Events")]
		public UnityEvent OnLightAttackEvent;
		public UnityEvent OnDashEvent;
		public UnityEvent OnParryPressedEvent;
		public UnityEvent OnParryReleasedEvent;

		public UnityEvent OnAbilityOneEvent;
        public UnityEvent OnAbilityTwoEvent;
        
        [Header("UI Events")]
		public UnityEvent OnPauseEvent;
		public UnityEvent OnAbilityMenuEvent;
		public UnityEvent OnBackEvent;
		
		private LockOn lockOnSystem;

		private void Awake()
		{
			lockOnSystem = GetComponent<LockOn>();
		}

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if (cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}
		
		public void OnLockOn(InputValue value)
		{
			if (value.isPressed) lockOnSystem.ToggleLockOn();
		}

		public void OnSwitchTarget(InputValue value)
		{
			float v = value.Get<float>();
			if (Mathf.Abs(v) > 0.5f)
				lockOnSystem.SwitchTarget(v);
		}

		public void OnLightAttack(InputValue value)
		{
			if (value.isPressed) OnLightAttackEvent?.Invoke();
		}

		public void OnDash(InputValue value)
		{
			if (value.isPressed) OnDashEvent?.Invoke();
		}
		
		public void OnParry(InputValue value)
		{
			if (value.isPressed)
				OnParryPressedEvent?.Invoke();
			else
				OnParryReleasedEvent?.Invoke();
		}

		public void OnAbilityOne(InputValue value)
		{
			if (value.isPressed) OnAbilityOneEvent?.Invoke();
		}

		public void OnAbilityTwo(InputValue value)
		{
			if (value.isPressed) OnAbilityTwoEvent?.Invoke();
		}
		
		public void OnPause(InputValue value)
		{
			if (value.isPressed) OnPauseEvent?.Invoke();
		}

		public void OnAbilityMenu(InputValue value)
		{
			if(value.isPressed) OnAbilityMenuEvent?.Invoke();
		}

		public void OnBack(InputValue value)
		{
			if(value.isPressed) OnBackEvent?.Invoke();
		}
#endif


        public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		}

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}

}