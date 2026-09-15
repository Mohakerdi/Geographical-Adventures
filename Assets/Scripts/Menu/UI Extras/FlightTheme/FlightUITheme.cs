using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

namespace GeoGame.UI
{
	/// <summary>
	/// Flight-Themed UI Manager that elevates the visual presentation of the game
	/// into a polished flight simulator experience.
	/// Replaces generic grey rectangular buttons with cockpit avionics glass buttons,
	/// HUD corner framing, aeronautical telemetry accents, and tactical indicators.
	/// </summary>
	public class FlightUITheme : MonoBehaviour
	{
		public static FlightUITheme Instance { get; private set; }

		// Flight Sprites (Procedurally generated in memory with 9-slice support for 100% build reliability)
		private static Sprite _buttonNormalSprite;
		private static Sprite _buttonHighlightSprite;
		private static Sprite _buttonPressedSprite;
		private static Sprite _buttonPrimarySprite;
		private static Sprite _buttonDangerSprite;
		private static Sprite _panelHUDSprite;
		private static Sprite _tabActiveSprite;
		private static Sprite _tabInactiveSprite;
		private static Sprite _attitudeIndicatorSprite;

		public static Sprite ButtonNormalSprite { get { EnsureSprites(); return _buttonNormalSprite; } }
		public static Sprite ButtonHighlightSprite { get { EnsureSprites(); return _buttonHighlightSprite; } }
		public static Sprite ButtonPressedSprite { get { EnsureSprites(); return _buttonPressedSprite; } }
		public static Sprite ButtonPrimarySprite { get { EnsureSprites(); return _buttonPrimarySprite; } }
		public static Sprite ButtonDangerSprite { get { EnsureSprites(); return _buttonDangerSprite; } }
		public static Sprite PanelHUDSprite { get { EnsureSprites(); return _panelHUDSprite; } }
		public static Sprite TabActiveSprite { get { EnsureSprites(); return _tabActiveSprite; } }
		public static Sprite TabInactiveSprite { get { EnsureSprites(); return _tabInactiveSprite; } }
		public static Sprite AttitudeIndicatorSprite { get { EnsureSprites(); return _attitudeIndicatorSprite; } }

		private static readonly HashSet<int> themedObjects = new HashSet<int>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void AutoInitialize()
		{
			EnsureSprites();
			if (Instance == null)
			{
				GameObject go = new GameObject("FlightUIThemeManager");
				Instance = go.AddComponent<FlightUITheme>();
				DontDestroyOnLoad(go);
			}
		}

		public static void EnsureSprites()
		{
			if (_buttonNormalSprite == null)
			{
				GenerateFlightSprites();
			}
		}

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
				DontDestroyOnLoad(gameObject);
				GenerateFlightSprites();
			}
			else if (Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private void OnDestroy()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		private void Start()
		{
			ThemeAllActiveUI();
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			themedObjects.Clear();
			StartCoroutine(ThemeWithDelay());
		}

		private IEnumerator ThemeWithDelay()
		{
			// Wait for menus to awaken and initialize
			yield return null;
			ThemeAllActiveUI();
			yield return new WaitForSeconds(0.25f);
			ThemeAllActiveUI();
		}

		private void Update()
		{
			// Periodically scan for dynamically activated menus (e.g. Pause, Settings, Stats)
			if (Time.frameCount % 45 == 0)
			{
				ThemeAllActiveUI();
			}
		}

		/// <summary>
		/// Scans all active Canvases and styles buttons, panels, tabs, and wheels with aviation aesthetics.
		/// </summary>
		public void ThemeAllActiveUI()
		{
			if (ButtonNormalSprite == null)
			{
				GenerateFlightSprites();
			}

			Canvas[] canvases = FindObjectsOfType<Canvas>(includeInactive: true);
			foreach (Canvas canvas in canvases)
			{
				// Skip mobile touch controls canvas (handled dedicatedly)
				if (canvas.name == "MobileControlsCanvas") continue;

				ThemeCanvas(canvas);
			}
		}

		private void ThemeCanvas(Canvas canvas)
		{
			Button[] buttons = canvas.GetComponentsInChildren<Button>(includeInactive: true);
			foreach (Button btn in buttons)
			{
				ThemeButton(btn);
			}

			// Theme panels / backgrounds
			Image[] images = canvas.GetComponentsInChildren<Image>(includeInactive: true);
			foreach (Image img in images)
			{
				ThemePanel(img);
			}
		}

		private void ThemeButton(Button btn)
		{
			if (btn == null) return;
			int id = btn.gameObject.GetInstanceID();

			Image img = btn.GetComponent<Image>();
			if (img == null) return;

			// Determine button category based on hierarchy and name
			string bName = btn.name.ToLowerInvariant();
			bool isPrimary = bName.Contains("play") || bName.Contains("resume") || bName.Contains("apply") || bName.Contains("start");
			bool isDanger = bName.Contains("quit") || bName.Contains("exit") || bName.Contains("close") || bName.Contains("abort");
			bool isTab = bName.Contains("tab") || (btn.transform.parent != null && btn.transform.parent.name.ToLowerInvariant().Contains("tab"));
			bool isArrow = bName.Contains("increase") || bName.Contains("decrease") || bName.Contains("left") || bName.Contains("right") || bName.Contains("arrow");

			// Choose appropriate flight sprite
			Sprite normalSpr = isPrimary ? ButtonPrimarySprite : (isDanger ? ButtonDangerSprite : ButtonNormalSprite);
			Sprite hoverSpr = ButtonHighlightSprite;
			Sprite pressSpr = ButtonPressedSprite;

			if (!themedObjects.Contains(id))
			{
				themedObjects.Add(id);

				// Apply 9-slice flight sprite
				img.sprite = normalSpr;
				img.type = Image.Type.Sliced;
				img.color = Color.white;

				// Configure button transitions for crisp avionics feedback
				btn.transition = Selectable.Transition.SpriteSwap;
				SpriteState ss = new SpriteState
				{
					highlightedSprite = hoverSpr,
					pressedSprite = pressSpr,
					selectedSprite = hoverSpr,
					disabledSprite = normalSpr
				};
				btn.spriteState = ss;

				// Add interactive scale bounce component
				if (btn.GetComponent<FlightButtonFeedback>() == null)
				{
					btn.gameObject.AddComponent<FlightButtonFeedback>();
				}

				// Enhance button label text
				EnhanceButtonLabel(btn, isPrimary, isDanger, isArrow);
			}
			else
			{
				// Keep sprite and color consistent if menu re-opened
				if (img.sprite != normalSpr && img.sprite != hoverSpr && img.sprite != pressSpr)
				{
					img.sprite = normalSpr;
					img.type = Image.Type.Sliced;
					img.color = Color.white;
				}
			}
		}

		private void EnhanceButtonLabel(Button btn, bool isPrimary, bool isDanger, bool isArrow)
		{
			if (isArrow) return;

			TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(includeInactive: true);
			if (tmp != null)
			{
				tmp.color = Color.white;
				string cur = tmp.text;
				// Strip any emoji glyphs that may corrupt rendering on standard TMP fonts
				string cleaned = cur.Replace("✈", "").Replace("⚙", "").Replace("📊", "").Replace("★", "")
				                    .Replace("⏻", "").Replace("▶", "").Replace("✔", "").Replace("✖", "")
				                    .Replace("🖥", "").Replace("🔊", "").Replace("🎮", "").Trim();
				if (cleaned != cur)
				{
					tmp.text = cleaned;
				}
				return;
			}

			Text uTxt = btn.GetComponentInChildren<Text>(includeInactive: true);
			if (uTxt != null)
			{
				uTxt.color = Color.white;
			}
		}

		private void ThemePanel(Image img)
		{
			if (img == null || img.GetComponent<Button>() != null) return;
			// Never restyle mobile controls, sliders, or elements within scroll views (tabs, settings rows)
			if (img.gameObject.name.Contains("MobileControls") || img.gameObject.name.Contains("ValBox") || img.gameObject.name.StartsWith("Btn")) return;
			if (img.GetComponentInParent<ScrollRect>() != null) return;
			if (img.GetComponentInParent<Slider>() != null) return;

			string name = img.gameObject.name.ToLowerInvariant();

			// Detect menu background holders / panels (top level only)
			if (name.Contains("panel") || name.Contains("menuholder") || name == "background")
			{
				// Avoid tiny images or icons
				RectTransform rt = img.rectTransform;
				if (rt.rect.width > 200 && rt.rect.height > 150)
				{
					int id = img.gameObject.GetInstanceID();
					if (!themedObjects.Contains(id))
					{
						themedObjects.Add(id);
						img.sprite = PanelHUDSprite;
						img.type = Image.Type.Sliced;
						img.color = new Color(1f, 1f, 1f, 0.95f);
					}
				}
			}
		}

		// ----------------------------------------------------
		// Procedural Generation of Aviation Cockpit Sprites
		// ----------------------------------------------------

		private static void GenerateFlightSprites()
		{
			if (_buttonNormalSprite != null) return;

			// 1. Normal Flight Button: Dark titanium slate (#101A26) + glowing cyan border (#00D2FF) + HUD corner ticks
			_buttonNormalSprite = CreateProceduralButtonSprite(
				new Color(0.10f, 0.16f, 0.23f, 0.95f), // Top slate
				new Color(0.06f, 0.09f, 0.14f, 0.98f), // Bottom deep slate
				new Color(0.00f, 0.82f, 1.00f, 0.92f), // Cyan border
				new Color(0.00f, 0.95f, 1.00f, 1.00f), // Corner HUD tick
				glow: true
			);

			// 2. Highlight Flight Button: Luminous electric cyan cockpit glow
			_buttonHighlightSprite = CreateProceduralButtonSprite(
				new Color(0.10f, 0.28f, 0.38f, 0.98f),
				new Color(0.05f, 0.18f, 0.26f, 1.00f),
				new Color(0.15f, 1.00f, 1.00f, 1.00f),
				Color.white,
				glow: true
			);

			// 3. Pressed Flight Button: Tactical afterburner amber (#FFA000)
			_buttonPressedSprite = CreateProceduralButtonSprite(
				new Color(0.35f, 0.20f, 0.05f, 1.00f),
				new Color(0.20f, 0.10f, 0.02f, 1.00f),
				new Color(1.00f, 0.70f, 0.00f, 1.00f),
				new Color(1.00f, 0.90f, 0.40f, 1.00f),
				glow: true
			);

			// 4. Primary Action Button: Emerald / Flight Engage green-cyan (#00E676)
			_buttonPrimarySprite = CreateProceduralButtonSprite(
				new Color(0.06f, 0.22f, 0.16f, 0.98f),
				new Color(0.03f, 0.13f, 0.10f, 1.00f),
				new Color(0.00f, 0.92f, 0.52f, 0.96f),
				new Color(0.40f, 1.00f, 0.70f, 1.00f),
				glow: true
			);

			// 5. Danger / Quit Button: Aviation warning coral (#FF453A)
			_buttonDangerSprite = CreateProceduralButtonSprite(
				new Color(0.24f, 0.08f, 0.10f, 0.98f),
				new Color(0.14f, 0.05f, 0.06f, 1.00f),
				new Color(1.00f, 0.30f, 0.28f, 0.96f),
				new Color(1.00f, 0.60f, 0.60f, 1.00f),
				glow: true
			);

			// 6. Glass Cockpit HUD Panel (Dark glass with cyan HUD corner brackets)
			_panelHUDSprite = CreateProceduralPanelSprite();

			// 7. Attitude Indicator (Horizon & Wings)
			_attitudeIndicatorSprite = CreateAttitudeIndicatorSprite();
		}

		private static Sprite CreateProceduralButtonSprite(Color bgTop, Color bgBot, Color borderCol, Color tickCol, bool glow)
		{
			int w = 128;
			int h = 64;
			int c = 10; // Chamfer cut size
			Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
			tex.filterMode = FilterMode.Bilinear;
			tex.wrapMode = TextureWrapMode.Clamp;

			Color clear = new Color(0, 0, 0, 0);
			Color[] pixels = new Color[w * h];

			for (int y = 0; y < h; y++)
			{
				float v = (float)y / (h - 1);
				Color baseBg = Color.Lerp(bgBot, bgTop, v);

				for (int x = 0; x < w; x++)
				{
					int idx = y * w + x;

					// Chamfer distance calculation
					int dLeft = x;
					int dRight = (w - 1) - x;
					int dBottom = y;
					int dTop = (h - 1) - y;

					// Corner cut tests
					bool cutBL = (dLeft + dBottom) < c;
					bool cutBR = (dRight + dBottom) < c;
					bool cutTL = (dLeft + dTop) < c;
					bool cutTR = (dRight + dTop) < c;

					if (cutBL || cutBR || cutTL || cutTR)
					{
						pixels[idx] = clear;
						continue;
					}

					// Border detection (outer 2 pixels or chamfer line)
					bool isOuterEdge = (dLeft <= 1 || dRight <= 1 || dBottom <= 1 || dTop <= 1);
					bool isChamferEdge = ((dLeft + dBottom) <= c + 1) || ((dRight + dBottom) <= c + 1) ||
					                     ((dLeft + dTop) <= c + 1) || ((dRight + dTop) <= c + 1);

					if (isOuterEdge || isChamferEdge)
					{
						pixels[idx] = borderCol;
						continue;
					}

					// Corner HUD tick brackets [ ]
					bool isBracketX = (dLeft >= 4 && dLeft <= 10) || (dRight >= 4 && dRight <= 10);
					bool isBracketY = (dBottom >= 4 && dBottom <= 10) || (dTop >= 4 && dTop <= 10);
					bool isCornerRegion = (dLeft <= 12 || dRight <= 12) && (dBottom <= 12 || dTop <= 12);

					if (isCornerRegion && (isBracketX || isBracketY) && ((dLeft == 4 || dRight == 4 || dBottom == 4 || dTop == 4)))
					{
						pixels[idx] = tickCol;
						continue;
					}

					// Side HUD ladder notches
					int midY = h / 2;
					bool isLadderY = Mathf.Abs(y - midY) <= 1 || Mathf.Abs(y - (midY - 10)) <= 1 || Mathf.Abs(y - (midY + 10)) <= 1;
					if (isLadderY && (dLeft <= 4 || dRight <= 4))
					{
						pixels[idx] = tickCol;
						continue;
					}

					// Upper half glass gloss sheen
					Color col = baseBg;
					if (y >= h / 2)
					{
						float gloss = (float)(y - h / 2) / (h / 2) * 0.12f;
						col += new Color(gloss, gloss, gloss, 0);
					}

					pixels[idx] = col;
				}
			}

			tex.SetPixels(pixels);
			tex.Apply();
			tex.hideFlags = HideFlags.DontSave;

			// 9-slice borders (16px left, 16px bottom, 16px right, 16px top)
			Sprite spr = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
			spr.hideFlags = HideFlags.DontSave;
			return spr;
		}

		private static Sprite CreateProceduralPanelSprite()
		{
			int w = 128;
			int h = 128;
			Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
			tex.filterMode = FilterMode.Bilinear;
			tex.wrapMode = TextureWrapMode.Clamp;

			Color[] pixels = new Color[w * h];
			Color glassBg = new Color(0.04f, 0.07f, 0.11f, 0.92f);
			Color cyanBorder = new Color(0.00f, 0.82f, 1.00f, 0.40f);
			Color cyanCorner = new Color(0.00f, 0.95f, 1.00f, 0.95f);

			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					int idx = y * w + x;
					int dL = x;
					int dR = (w - 1) - x;
					int dB = y;
					int dT = (h - 1) - y;

					// Outer 1px line
					if (dL == 0 || dR == 0 || dB == 0 || dT == 0)
					{
						pixels[idx] = cyanBorder;
						continue;
					}

					// HUD corner brackets (10px length, 2px thick)
					bool cornerZone = (dL <= 10 || dR <= 10) && (dB <= 10 || dT <= 10);
					bool cornerLine = (dL <= 1 || dR <= 1 || dB <= 1 || dT <= 1);
					if (cornerZone && cornerLine)
					{
						pixels[idx] = cyanCorner;
						continue;
					}

					pixels[idx] = glassBg;
				}
			}

			tex.SetPixels(pixels);
			tex.Apply();
			tex.hideFlags = HideFlags.DontSave;
			Sprite pSpr = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(18, 18, 18, 18));
			pSpr.hideFlags = HideFlags.DontSave;
			return pSpr;
		}

		private static Sprite CreateAttitudeIndicatorSprite()
		{
			int sz = 128;
			Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
			tex.filterMode = FilterMode.Bilinear;
			tex.wrapMode = TextureWrapMode.Clamp;

			Color[] pixels = new Color[sz * sz];
			Color clear = new Color(0, 0, 0, 0);
			Vector2 center = new Vector2(sz / 2f, sz / 2f);
			float radius = sz / 2f - 4f;

			for (int y = 0; y < sz; y++)
			{
				for (int x = 0; x < sz; x++)
				{
					int idx = y * sz + x;
					float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);

					if (d > radius)
					{
						pixels[idx] = clear;
						continue;
					}

					// Dark translucent sphere body
					float alpha = Mathf.Clamp01(radius - d + 1f);
					Color col = new Color(0.08f, 0.14f, 0.20f, alpha * 0.85f);

					// Outer cyan rim
					if (d >= radius - 3f)
					{
						col = new Color(0.00f, 0.85f, 1.00f, alpha * 0.95f);
					}

					// Central Horizon and Aircraft Wings Reticle [─·┴·─]
					int dy = Mathf.Abs(y - sz / 2);
					int dx = Mathf.Abs(x - sz / 2);

					// Horizon center reference
					if (dy == 0 && dx > 8 && dx < 36)
					{
						col = new Color(1.00f, 0.80f, 0.20f, 1.00f); // Aviation Gold
					}
					// Center pip
					if (dx <= 2 && dy <= 2)
					{
						col = new Color(1.00f, 0.85f, 0.25f, 1.00f);
					}
					// Center vertical notch
					if (dx == 0 && y >= sz / 2 - 6 && y <= sz / 2 + 6)
					{
						col = new Color(1.00f, 0.80f, 0.20f, 1.00f);
					}

					// Pitch ladder notches (-10, +10)
					if ((Mathf.Abs(y - (sz / 2 - 14)) <= 1 || Mathf.Abs(y - (sz / 2 + 14)) <= 1) && dx <= 16)
					{
						col = new Color(0.00f, 0.90f, 1.00f, 0.70f);
					}

					pixels[idx] = col;
				}
			}

			tex.SetPixels(pixels);
			tex.Apply();
			tex.hideFlags = HideFlags.DontSave;
			Sprite aSpr = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
			aSpr.hideFlags = HideFlags.DontSave;
			return aSpr;
		}
	}

	/// <summary>
	/// Smooth tactile feedback on flight buttons (scale bounce on hover and press).
	/// </summary>
	public class FlightButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
	{
		private Vector3 originalScale;
		private Coroutine animRoutine;

		private void Awake()
		{
			originalScale = transform.localScale;
			if (originalScale == Vector3.zero) originalScale = Vector3.one;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			AnimateTo(originalScale * 1.035f, 0.1f);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			AnimateTo(originalScale, 0.1f);
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			AnimateTo(originalScale * 0.965f, 0.06f);
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			AnimateTo(originalScale * 1.035f, 0.08f);
		}

		private void AnimateTo(Vector3 target, float duration)
		{
			if (!gameObject.activeInHierarchy) return;
			if (animRoutine != null) StopCoroutine(animRoutine);
			animRoutine = StartCoroutine(ScaleRoutine(target, duration));
		}

		private IEnumerator ScaleRoutine(Vector3 target, float duration)
		{
			Vector3 start = transform.localScale;
			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				transform.localScale = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / duration));
				yield return null;
			}
			transform.localScale = target;
		}
	}
}
