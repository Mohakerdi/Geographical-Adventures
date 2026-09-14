using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GeoGame.InputMobile
{
	/// <summary>
	/// On-screen touch controls for Android phone and mobile devices.
	/// Includes virtual steering joystick, throttle (accelerate/decelerate), boost button,
	/// drop package button, camera toggle, map toggle, and pause toggle.
	/// Automatically adapts to screen resolution and DPI.
	/// </summary>
	public class MobileControls : MonoBehaviour
	{
		public static MobileControls Instance { get; private set; }

		[Header("Settings")]
		public bool forceEnableInEditor = false;
		public float joystickDeadzone = 0.05f;

		// Current input states
		public Vector2 MovementInput { get; private set; }
		public float SpeedInput { get; private set; }
		public bool IsBoosting { get; private set; }

		private bool dropPackageTriggered;
		private bool cycleCameraTriggered;
		private bool toggleMapTriggered;
		private bool togglePauseTriggered;

		// UI Elements
		private GameObject canvasObj;
		private Canvas canvas;
		private CanvasGroup flightControlsGroup;
		private CanvasGroup mapControlsGroup;

		// Joystick
		private RectTransform joystickBaseRect;
		private RectTransform joystickKnobRect;
		private Vector2 joystickBasePos;
		private float joystickRadius = 90f;
		private int joystickPointerId = -1;

		// Cached procedural textures
		private static Sprite circleSprite;
		private static Sprite circleHollowSprite;
		private static Sprite roundedRectSprite;

		public bool IsActive
		{
			get
			{
				bool isMobile = Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
				int userSetting = PlayerPrefs.GetInt("MobileControls_Enabled", isMobile ? 1 : 0);
				return (userSetting == 1) || forceEnableInEditor;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void AutoInstantiate()
		{
			if (Instance == null)
			{
				GameObject go = new GameObject("MobileControlsManager");
				Instance = go.AddComponent<MobileControls>();
				DontDestroyOnLoad(go);
			}
		}

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
				DontDestroyOnLoad(gameObject);
			}
			else if (Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			// If first run on Android/mobile, enable touch controls by default and force landscape
			if (Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld)
			{
				Screen.autorotateToPortrait = false;
				Screen.autorotateToPortraitUpsideDown = false;
				Screen.autorotateToLandscapeLeft = true;
				Screen.autorotateToLandscapeRight = true;
				Screen.orientation = ScreenOrientation.LandscapeLeft;

				if (!PlayerPrefs.HasKey("MobileControls_Enabled"))
				{
					PlayerPrefs.SetInt("MobileControls_Enabled", 1);
					PlayerPrefs.Save();
				}
			}

			BuildUI();
			GeoGame.Localization.LocalizationManager.onLanguageChanged += RebuildUI;
		}

		private void OnDestroy()
		{
			GeoGame.Localization.LocalizationManager.onLanguageChanged -= RebuildUI;
		}

		private void RebuildUI()
		{
			if (canvasObj != null)
			{
				Destroy(canvasObj);
				canvasObj = null;
			}
			BuildUI();
		}

		private void Update()
		{
			if (canvasObj == null) return;

			bool active = IsActive;
			if (canvasObj.activeSelf != active)
			{
				canvasObj.SetActive(active);
			}

			if (!active)
			{
				MovementInput = Vector2.zero;
				SpeedInput = 0;
				IsBoosting = false;
				return;
			}

			// Show/hide controls depending on game state
			bool isPlaying = GameController.IsState(GameState.Playing);
			bool isMap = GameController.IsState(GameState.ViewingMap);

			if (flightControlsGroup != null)
			{
				flightControlsGroup.alpha = isPlaying ? 1f : 0f;
				flightControlsGroup.interactable = isPlaying;
				flightControlsGroup.blocksRaycasts = isPlaying;
			}

			if (mapControlsGroup != null)
			{
				mapControlsGroup.alpha = isMap ? 1f : 0f;
				mapControlsGroup.interactable = isMap;
				mapControlsGroup.blocksRaycasts = isMap;
			}
		}

		public bool ConsumeDropPackage()
		{
			if (dropPackageTriggered)
			{
				dropPackageTriggered = false;
				return true;
			}
			return false;
		}

		public bool ConsumeCycleCamera()
		{
			if (cycleCameraTriggered)
			{
				cycleCameraTriggered = false;
				return true;
			}
			return false;
		}

		public bool ConsumeToggleMap()
		{
			if (toggleMapTriggered)
			{
				toggleMapTriggered = false;
				return true;
			}
			return false;
		}

		public bool ConsumeTogglePause()
		{
			if (togglePauseTriggered)
			{
				togglePauseTriggered = false;
				return true;
			}
			return false;
		}

		// ----------------------------------------------------
		// UI Construction
		// ----------------------------------------------------

		private void BuildUI()
		{
			if (canvasObj != null) return;

			// Ensure EventSystem exists
			if (FindObjectOfType<EventSystem>() == null)
			{
				GameObject es = new GameObject("EventSystem");
				es.AddComponent<EventSystem>();
				es.AddComponent<StandaloneInputModule>();
			}

			GenerateSprites();

			// Main Canvas
			canvasObj = new GameObject("MobileControlsCanvas");
			canvasObj.transform.SetParent(transform, false);

			canvas = canvasObj.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 500;

			CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920, 1080);
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			scaler.matchWidthOrHeight = 0.5f;

			canvasObj.AddComponent<GraphicRaycaster>();

			// 1. Flight Controls Container
			GameObject flightGo = new GameObject("FlightControls", typeof(RectTransform), typeof(CanvasGroup));
			flightGo.transform.SetParent(canvasObj.transform, false);
			StretchFull(flightGo.GetComponent<RectTransform>());
			flightControlsGroup = flightGo.GetComponent<CanvasGroup>();

			// 2. Map Controls Container (Shown when viewing map)
			GameObject mapGo = new GameObject("MapControls", typeof(RectTransform), typeof(CanvasGroup));
			mapGo.transform.SetParent(canvasObj.transform, false);
			StretchFull(mapGo.GetComponent<RectTransform>());
			mapControlsGroup = mapGo.GetComponent<CanvasGroup>();

			CreateFlightControls(flightGo.transform);
			CreateTopBar(canvasObj.transform);
			CreateMapControls(mapGo.transform);

			canvasObj.SetActive(IsActive);
		}

		private void CreateFlightControls(Transform parent)
		{
			// Left Joystick Area
			GameObject joyZone = new GameObject("JoystickZone", typeof(RectTransform), typeof(Image));
			joyZone.transform.SetParent(parent, false);
			RectTransform joyZoneRect = joyZone.GetComponent<RectTransform>();
			joyZoneRect.anchorMin = new Vector2(0, 0);
			joyZoneRect.anchorMax = new Vector2(0.45f, 0.7f);
			joyZoneRect.pivot = new Vector2(0, 0);
			joyZoneRect.offsetMin = Vector2.zero;
			joyZoneRect.offsetMax = Vector2.zero;
			Image joyZoneImg = joyZone.GetComponent<Image>();
			joyZoneImg.color = new Color(0, 0, 0, 0.001f); // Invisible raycast target

			// Joystick Base
			GameObject joyBase = new GameObject("JoystickBase", typeof(RectTransform), typeof(Image));
			joyBase.transform.SetParent(joyZone.transform, false);
			joystickBaseRect = joyBase.GetComponent<RectTransform>();
			joystickBaseRect.anchorMin = new Vector2(0, 0);
			joystickBaseRect.anchorMax = new Vector2(0, 0);
			joystickBaseRect.pivot = new Vector2(0.5f, 0.5f);
			joystickBasePos = new Vector2(180, 180);
			joystickBaseRect.anchoredPosition = joystickBasePos;
			joystickBaseRect.sizeDelta = new Vector2(joystickRadius * 2, joystickRadius * 2);

			Image baseImg = joyBase.GetComponent<Image>();
			baseImg.sprite = circleSprite;
			baseImg.color = new Color(0.06f, 0.10f, 0.16f, 0.75f);
			baseImg.raycastTarget = false;

			// Outer ring decoration (Cockpit compass / heading rim)
			GameObject joyRing = new GameObject("JoystickRing", typeof(RectTransform), typeof(Image));
			joyRing.transform.SetParent(joyBase.transform, false);
			RectTransform ringRect = joyRing.GetComponent<RectTransform>();
			ringRect.anchorMin = Vector2.zero;
			ringRect.anchorMax = Vector2.one;
			ringRect.offsetMin = Vector2.zero;
			ringRect.offsetMax = Vector2.zero;
			Image ringImg = joyRing.GetComponent<Image>();
			ringImg.sprite = circleHollowSprite;
			ringImg.color = new Color(0.00f, 0.85f, 1.00f, 0.60f); // Aviation cyan rim
			ringImg.raycastTarget = false;

			// Joystick Knob (Attitude Indicator with Wings Reticle)
			GameObject joyKnob = new GameObject("JoystickKnob", typeof(RectTransform), typeof(Image));
			joyKnob.transform.SetParent(joyBase.transform, false);
			joystickKnobRect = joyKnob.GetComponent<RectTransform>();
			joystickKnobRect.anchoredPosition = Vector2.zero;
			joystickKnobRect.sizeDelta = new Vector2(76, 76);
			Image knobImg = joyKnob.GetComponent<Image>();
			knobImg.sprite = GeoGame.UI.FlightUITheme.AttitudeIndicatorSprite != null ? GeoGame.UI.FlightUITheme.AttitudeIndicatorSprite : circleSprite;
			knobImg.color = Color.white;
			knobImg.raycastTarget = false;

			// Joystick touch handler
			TouchTrigger joyTrigger = joyZone.AddComponent<TouchTrigger>();
			joyTrigger.onPointerDown = (ped) =>
			{
				joystickPointerId = ped.pointerId;
				// In ScreenSpaceOverlay canvas, Camera MUST be null for ScreenPointToLocalPointInRectangle
				if (RectTransformUtility.ScreenPointToLocalPointInRectangle(joyZoneRect, ped.position, null, out Vector2 localPos))
				{
					float minX = joystickRadius;
					float maxX = Mathf.Max(minX, joyZoneRect.rect.width - joystickRadius);
					float minY = joystickRadius;
					float maxY = Mathf.Max(minY, joyZoneRect.rect.height - joystickRadius);
					localPos.x = Mathf.Clamp(localPos.x, minX, maxX);
					localPos.y = Mathf.Clamp(localPos.y, minY, maxY);
					joystickBaseRect.anchoredPosition = localPos;
				}
				UpdateJoystick(ped.position);
			};
			joyTrigger.onDrag = (ped) =>
			{
				if (ped.pointerId == joystickPointerId)
				{
					UpdateJoystick(ped.position);
				}
			};
			joyTrigger.onPointerUp = (ped) =>
			{
				if (ped.pointerId == joystickPointerId)
				{
					joystickPointerId = -1;
					joystickKnobRect.anchoredPosition = Vector2.zero;
					joystickBaseRect.anchoredPosition = joystickBasePos;
					MovementInput = Vector2.zero;
				}
			};

			// Right Action Buttons Area (Flight HUD Themed)
			bool isRTL = GeoGame.Localization.LocalizationManager.IsRightToLeftWritingSystem;
			string dropLabel = isRTL ? "✈ " + GeoGame.Localization.Arabic.ArabicFixer.Fix("إسقاط") + "\n\u25BC" : "✈ DROP\n\u25BC";
			string boostLabel = isRTL ? "\u26A1 " + GeoGame.Localization.Arabic.ArabicFixer.Fix("تعزيز") + "\nMACH" : "\u26A1 BOOST\nMACH";
			string spdUpLabel = isRTL ? "\u25B2\n" + GeoGame.Localization.Arabic.ArabicFixer.Fix("دفع+") : "\u25B2\n+THRUST";
			string spdDownLabel = isRTL ? "\u25BC\n" + GeoGame.Localization.Arabic.ArabicFixer.Fix("دفع-") : "\u25BC\n-THRUST";

			// 1. DROP PACKAGE BUTTON (Cockpit payload release gold)
			CreateActionButton(parent, "DropButton", new Vector2(-120, 130), new Vector2(110, 110), dropLabel,
				new Color(0.95f, 0.68f, 0.12f, 0.90f),
				onDown: () => { dropPackageTriggered = true; });

			// 2. BOOST BUTTON (Afterburner Mach cyan)
			CreateActionButton(parent, "BoostButton", new Vector2(-250, 110), new Vector2(85, 85), boostLabel,
				new Color(0.05f, 0.78f, 1.00f, 0.90f),
				onDown: () => { IsBoosting = true; },
				onUp: () => { IsBoosting = false; });

			// 3. SPEED UP BUTTON (Airspeed increase emerald)
			CreateActionButton(parent, "SpeedUpButton", new Vector2(-120, 270), new Vector2(75, 75), spdUpLabel,
				new Color(0.12f, 0.85f, 0.50f, 0.85f),
				onDown: () => { SpeedInput = 1f; },
				onUp: () => { SpeedInput = 0f; });

			// 4. SPEED DOWN BUTTON (Airspeed decrease amber)
			CreateActionButton(parent, "SpeedDownButton", new Vector2(-220, 220), new Vector2(75, 75), spdDownLabel,
				new Color(0.92f, 0.35f, 0.25f, 0.85f),
				onDown: () => { SpeedInput = -1f; },
				onUp: () => { SpeedInput = 0f; });
		}

		private void CreateTopBar(Transform parent)
		{
			bool isRTL = GeoGame.Localization.LocalizationManager.IsRightToLeftWritingSystem;
			string mapLabel = isRTL ? GeoGame.Localization.Arabic.ArabicFixer.Fix("خريطة") + "\n\uD83C\uDF0D NAV" : "NAV\n\uD83C\uDF0D";
			string camLabel = isRTL ? GeoGame.Localization.Arabic.ArabicFixer.Fix("كاميرا") + "\n\uD83D\uDCF7 HUD" : "HUD\n\uD83D\uDCF7";

			// Top-Left: Pause / Systems Button (⏸ SYS)
			CreateActionButton(parent, "PauseButton", new Vector2(70, -60), new Vector2(70, 70), "\u2759\u2759\nSYS",
				new Color(0.12f, 0.18f, 0.26f, 0.85f),
				onDown: () => { togglePauseTriggered = true; },
				anchorMin: new Vector2(0, 1), anchorMax: new Vector2(0, 1));

			// Top-Right: Map Button (🗺)
			CreateActionButton(parent, "MapButton", new Vector2(-70, -60), new Vector2(70, 70), mapLabel,
				new Color(0.2f, 0.5f, 0.85f, 0.85f),
				onDown: () => { toggleMapTriggered = true; },
				anchorMin: new Vector2(1, 1), anchorMax: new Vector2(1, 1));

			// Top-Right: Camera View Button (📷)
			CreateActionButton(parent, "CamButton", new Vector2(-160, -60), new Vector2(70, 70), camLabel,
				new Color(0.3f, 0.35f, 0.4f, 0.8f),
				onDown: () => { cycleCameraTriggered = true; },
				anchorMin: new Vector2(1, 1), anchorMax: new Vector2(1, 1));
		}

		private void CreateMapControls(Transform parent)
		{
			bool isRTL = GeoGame.Localization.LocalizationManager.IsRightToLeftWritingSystem;
			string backFlightLabel = isRTL ? "\u2716 " + GeoGame.Localization.Arabic.ArabicFixer.Fix("العودة للطيران") : "\u2716 BACK TO FLIGHT";

			// "CLOSE MAP / RETURN TO FLIGHT" button in top-center
			CreateActionButton(parent, "CloseMapButton", new Vector2(0, -65), new Vector2(230, 60), backFlightLabel,
				new Color(0.85f, 0.25f, 0.25f, 0.9f),
				onDown: () => { toggleMapTriggered = true; },
				anchorMin: new Vector2(0.5f, 1), anchorMax: new Vector2(0.5f, 1));
		}

		private void UpdateJoystick(Vector2 screenPos)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickBaseRect, screenPos, null, out Vector2 localPoint);
			float dst = localPoint.magnitude;
			Vector2 dir = dst > 0.001f ? localPoint / dst : Vector2.zero;

			float clampedDst = Mathf.Min(dst, joystickRadius);
			joystickKnobRect.anchoredPosition = dir * clampedDst;

			float normalizedDst = clampedDst / joystickRadius;
			if (normalizedDst < joystickDeadzone)
			{
				MovementInput = Vector2.zero;
			}
			else
			{
				float remappedDst = (normalizedDst - joystickDeadzone) / (1f - joystickDeadzone);
				MovementInput = dir * remappedDst;
			}
		}

		private void CreateActionButton(Transform parent, string name, Vector2 pos, Vector2 size, string labelText,
			Color color, System.Action onDown = null, System.Action onUp = null,
			Vector2? anchorMin = null, Vector2? anchorMax = null)
		{
			Vector2 aMin = anchorMin ?? new Vector2(1, 0);
			Vector2 aMax = anchorMax ?? new Vector2(1, 0);

			GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TouchTrigger));
			btnObj.transform.SetParent(parent, false);

			RectTransform rt = btnObj.GetComponent<RectTransform>();
			rt.anchorMin = aMin;
			rt.anchorMax = aMax;
			rt.anchoredPosition = pos;
			rt.sizeDelta = size;

			Image img = btnObj.GetComponent<Image>();
			img.sprite = circleSprite;
			img.color = color;

			// Label using TextMeshProUGUI for proper Arabic font fallback and crisp SDF rendering
			GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
			labelObj.transform.SetParent(btnObj.transform, false);
			StretchFull(labelObj.GetComponent<RectTransform>());

			TMPro.TextMeshProUGUI txt = labelObj.GetComponent<TMPro.TextMeshProUGUI>();
			txt.text = labelText;
			txt.fontSize = Mathf.Min(size.x, size.y) * 0.28f;
			txt.alignment = TMPro.TextAlignmentOptions.Center;
			txt.color = Color.white;
			txt.raycastTarget = false;
			txt.isRightToLeftText = false;

			// Touch triggers with visual bounce
			TouchTrigger trigger = btnObj.GetComponent<TouchTrigger>();
			Vector3 origScale = Vector3.one;

			trigger.onPointerDown = (ped) =>
			{
				btnObj.transform.localScale = origScale * 0.9f;
				img.color = new Color(color.r * 1.2f, color.g * 1.2f, color.b * 1.2f, 1f);
				onDown?.Invoke();
			};

			trigger.onPointerUp = (ped) =>
			{
				btnObj.transform.localScale = origScale;
				img.color = color;
				onUp?.Invoke();
			};
		}

		private void StretchFull(RectTransform rt)
		{
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;
		}

		// ----------------------------------------------------
		// Procedural Sprite Generation (No assets required!)
		// ----------------------------------------------------

		private static void GenerateSprites()
		{
			if (circleSprite != null) return;

			// Solid Circle
			int sz = 128;
			Texture2D circleTex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
			circleTex.filterMode = FilterMode.Bilinear;
			float r = (sz - 4) / 2f;
			Vector2 center = new Vector2(sz / 2f, sz / 2f);

			for (int y = 0; y < sz; y++)
			{
				for (int x = 0; x < sz; x++)
				{
					float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
					float alpha = Mathf.Clamp01(r - d + 1f);
					circleTex.SetPixel(x, y, new Color(1, 1, 1, alpha));
				}
			}
			circleTex.Apply();
			circleSprite = Sprite.Create(circleTex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));

			// Hollow Ring
			Texture2D hollowTex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
			hollowTex.filterMode = FilterMode.Bilinear;
			float outerR = (sz - 4) / 2f;
			float innerR = outerR - 6f;

			for (int y = 0; y < sz; y++)
			{
				for (int x = 0; x < sz; x++)
				{
					float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
					float aOuter = Mathf.Clamp01(outerR - d + 1f);
					float aInner = Mathf.Clamp01(d - innerR + 1f);
					float alpha = Mathf.Min(aOuter, aInner);
					hollowTex.SetPixel(x, y, new Color(1, 1, 1, alpha));
				}
			}
			hollowTex.Apply();
			circleHollowSprite = Sprite.Create(hollowTex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
		}
	}

	/// <summary>
	/// Helper for pointer event forwarding.
	/// </summary>
	public class TouchTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
	{
		public System.Action<PointerEventData> onPointerDown;
		public System.Action<PointerEventData> onPointerUp;
		public System.Action<PointerEventData> onDrag;

		public void OnPointerDown(PointerEventData eventData) => onPointerDown?.Invoke(eventData);
		public void OnPointerUp(PointerEventData eventData) => onPointerUp?.Invoke(eventData);
		public void OnDrag(PointerEventData eventData) => onDrag?.Invoke(eventData);
	}
}
