using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{

	public Player player;
	public GeoGame.Quest.QuestSystem questSystem;
	public GameCamera gameCamera;
	public UIManager uIManager;
	public SolarSystem.SolarSystemManager solarSystemManager;
	PlayerAction playerActions;


	void Start()
	{
		playerActions = RebindManager.Instance.activePlayerActions;

		playerActions.PlayerControls.Enable();
		playerActions.CameraControls.Enable();
		playerActions.UIControls.Enable();
	}


	void Update()
	{
		if (GameController.IsState(GameState.Playing))
		{
			PlayerControls();
			CameraControls();
			SolarSystemControls();
		}

		UIControls();
	}

	void PlayerControls()
	{
		Vector2 movementInput = playerActions.PlayerControls.Movement.ReadValue<Vector2>();
		float accelerateDir = playerActions.PlayerControls.Speed.ReadValue<float>();
		bool boosting = playerActions.PlayerControls.Boost.IsPressed();
		bool dropPackage = playerActions.PlayerControls.DropPackage.WasPressedThisFrame();

		if (GeoGame.InputMobile.MobileControls.Instance != null && GeoGame.InputMobile.MobileControls.Instance.IsActive)
		{
			var mobile = GeoGame.InputMobile.MobileControls.Instance;
			if (mobile.MovementInput.sqrMagnitude > 0.001f)
			{
				movementInput = mobile.MovementInput;
			}
			if (Mathf.Abs(mobile.SpeedInput) > 0.001f)
			{
				accelerateDir = mobile.SpeedInput;
			}
			if (mobile.IsBoosting)
			{
				boosting = true;
			}
			if (mobile.ConsumeDropPackage())
			{
				dropPackage = true;
			}
		}

		player.UpdateMovementInput(movementInput, accelerateDir, boosting);

		if (dropPackage)
		{
			questSystem.TryDropPackage();
		}
	}

	void SolarSystemControls()
	{
		if (playerActions.PlayerControls.MakeDaytime.WasPressedThisFrame())
		{
			solarSystemManager.FastForward(toDaytime: true);
		}
		if (playerActions.PlayerControls.MakeNighttime.WasPressedThisFrame())
		{
			solarSystemManager.FastForward(toDaytime: false);
		}
	}

	void CameraControls()
	{
		bool forward = playerActions.CameraControls.ForwardCameraView.WasPressedThisFrame();
		bool backward = playerActions.CameraControls.BackwardCameraView.WasPressedThisFrame();
		bool top = playerActions.CameraControls.TopCameraView.WasPressedThisFrame();
		bool cycle = GeoGame.InputMobile.MobileControls.Instance != null && GeoGame.InputMobile.MobileControls.Instance.ConsumeCycleCamera();

		if (cycle)
		{
			if (gameCamera.activeView == GameCamera.ViewMode.LookingForward)
				gameCamera.SetActiveView(GameCamera.ViewMode.LookingBehind);
			else if (gameCamera.activeView == GameCamera.ViewMode.LookingBehind)
				gameCamera.SetActiveView(GameCamera.ViewMode.TopDown);
			else
				gameCamera.SetActiveView(GameCamera.ViewMode.LookingForward);
		}
		else if (forward)
		{
			gameCamera.SetActiveView(GameCamera.ViewMode.LookingForward);
		}
		else if (backward)
		{
			gameCamera.SetActiveView(GameCamera.ViewMode.LookingBehind);
		}
		else if (top)
		{
			gameCamera.SetActiveView(GameCamera.ViewMode.TopDown);
		}
	}

	void UIControls()
	{
		bool pause = playerActions.UIControls.TogglePause.WasPressedThisFrame();
		bool map = playerActions.UIControls.ToggleMap.WasPressedThisFrame();

		if (GeoGame.InputMobile.MobileControls.Instance != null)
		{
			if (GeoGame.InputMobile.MobileControls.Instance.ConsumeTogglePause()) pause = true;
			if (GeoGame.InputMobile.MobileControls.Instance.ConsumeToggleMap()) map = true;
		}

		if (pause)
		{
			uIManager.TogglePause();
		}

		if (map)
		{
			uIManager.ToggleMap();
		}
	}

}
