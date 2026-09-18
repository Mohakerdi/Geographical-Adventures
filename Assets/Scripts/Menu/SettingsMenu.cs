using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;
using GeoGame.Localization;

public class SettingsMenu : Menu
{
	public enum SettingsTab { Graphics, Audio, Controls }
	public SettingsTab defaultTab;

	[Header("Graphics Settings")]
	public Vector2Int[] supportedRatios;
	public ValueWheel aspectRatioWheel;
	public ValueWheel resolutionWheel;
	public Toggle fullscreenToggle;
	public Toggle vsyncToggle;
	public ValueWheel terrainQuality;
	public ValueWheel shadowQuality;

	[Header("Audio/Language Settings")]
	public ValueWheel languageWheel;
	public Slider masterVolumeSlider;
	public Slider musicVolumeSlider;
	public Slider sfxVolumeSlider;
	[Space()]
	public AudioMixer audioMixer;
	public LocalizationManager localizationManager;


	[Header("Other References")]
	public TabGroup tabGroup;
	public Button applyButton;

	// Private stuff
	Dictionary<Vector2Int, List<Vector2Int>> supportedResolutions;
	Settings lastAppliedSettings;


	protected override void Awake()
	{
		base.Awake();
	}

	void Start()
	{
		AddListeners();
		ApplySettings(Settings.LoadSavedSettings());
		SetUpScreen();
		SetupMobileSettingsUI();
		LocalizationManager.onLanguageChanged += UpdateMobileSettingsLabels;
		if (tabGroup != null && tabGroup.tabs != null && tabGroup.tabs.Length > 2 && tabGroup.tabs[2].button != null)
		{
			tabGroup.tabs[2].button.onClick.AddListener(SetupMobileSettingsUI);
		}
	}

	void OnDestroy()
	{
		LocalizationManager.onLanguageChanged -= UpdateMobileSettingsLabels;
	}

	void AddListeners()
	{
		applyButton.onClick.AddListener(ApplyCurrentSettings);
		languageWheel.onValueChanged += OnLanguageChanged;
		masterVolumeSlider.onValueChanged.AddListener((volume) => {
			UpdateAudioVolume();
			lastAppliedSettings.masterVolume = volume;
			PlayerPrefs.SetFloat("masterVolume", volume);
			PlayerPrefs.Save();
		});
		musicVolumeSlider.onValueChanged.AddListener((volume) => {
			UpdateAudioVolume();
			lastAppliedSettings.musicVolume = volume;
			PlayerPrefs.SetFloat("musicVolume", volume);
			PlayerPrefs.Save();
		});
		sfxVolumeSlider.onValueChanged.AddListener((volume) => {
			UpdateAudioVolume();
			lastAppliedSettings.sfxVolume = volume;
			PlayerPrefs.SetFloat("sfxVolume", volume);
			PlayerPrefs.Save();
		});
	}

	void OnLanguageChanged(int index)
	{
		if (localizationManager != null && localizationManager.languages != null && index >= 0 && index < localizationManager.languages.Length)
		{
			string langId = localizationManager.languages[index].languageID;
			localizationManager.ChangeLanguage(localizationManager.languages[index]);
			lastAppliedSettings.languageID = langId;
			PlayerPrefs.SetString("languageID", langId);
			PlayerPrefs.Save();
		}
	}

	// Set UI state from loaded settings
	void SetUIFromSettings(Settings settings)
	{
		// Graphics
		fullscreenToggle.SetIsOnWithoutNotify(settings.isFullscreen);
		vsyncToggle.SetIsOnWithoutNotify(settings.vsyncEnabled);
		InitResolutionSettings(settings.screenSize);
		terrainQuality.SetActiveIndex((int)settings.terrainQuality, notify: false);
		shadowQuality.SetActiveIndex((int)settings.shadowQuality, notify: false);

		// Audio / Language
		InitLanguageUI();
		languageWheel.SetActiveIndex(localizationManager.GetIndexFromID(settings.languageID), notify: false);
		masterVolumeSlider.SetValueWithoutNotify(settings.masterVolume);
		musicVolumeSlider.SetValueWithoutNotify(settings.musicVolume);
		sfxVolumeSlider.SetValueWithoutNotify(settings.sfxVolume);

		// Mobile Controls Opacity
		float op = Mathf.Clamp(settings.mobileControlsOpacity, 0.20f, 1.00f);
		activeOpacityIndex = 3;
		for (int i = 0; i < OpacityLevels.Length; i++)
		{
			if (Mathf.Abs(OpacityLevels[i] - op) < 0.05f)
			{
				activeOpacityIndex = i;
				break;
			}
		}
		if (mobileOpacityValueText != null)
		{
			mobileOpacityValueText.text = OpacityNames[activeOpacityIndex];
		}
	}

	// Construct settings struct from user's chosen settings
	Settings GetSettingsFromUI()
	{
		Settings settings = new Settings();
		// Graphics
		settings.isFullscreen = fullscreenToggle.isOn;
		Vector2Int[] resOptions = GetCurrentResolutionOptions();
		if (!Application.isEditor && resOptions != null && resOptions.Length > 0 && resolutionWheel.activeValueIndex >= 0 && resolutionWheel.activeValueIndex < resOptions.Length)
		{
			settings.screenSize = resOptions[resolutionWheel.activeValueIndex];
		}
		else
		{
			settings.screenSize = new Vector2Int(Screen.width, Screen.height);
		}
		settings.vsyncEnabled = vsyncToggle.isOn;
		settings.terrainQuality = (Settings.TerrainQuality)terrainQuality.activeValueIndex;
		settings.shadowQuality = (Settings.ShadowQuality)shadowQuality.activeValueIndex;

		// Audio / Language
		if (localizationManager != null && localizationManager.languages != null && languageWheel.activeValueIndex >= 0 && languageWheel.activeValueIndex < localizationManager.languages.Length)
		{
			settings.languageID = localizationManager.languages[languageWheel.activeValueIndex].languageID;
		}
		else
		{
			settings.languageID = lastAppliedSettings.languageID;
		}
		settings.masterVolume = masterVolumeSlider.value;
		settings.sfxVolume = sfxVolumeSlider.value;
		settings.musicVolume = musicVolumeSlider.value;

		// Mobile Controls Opacity
		if (activeOpacityIndex >= 0 && activeOpacityIndex < OpacityLevels.Length)
		{
			settings.mobileControlsOpacity = OpacityLevels[activeOpacityIndex];
		}
		else
		{
			settings.mobileControlsOpacity = 0.80f;
		}
		return settings;
	}

	// Apply current settings to the game
	void ApplyCurrentSettings()
	{
		Settings currentSettings = GetSettingsFromUI();
		if (RebindManager.Instance != null)
		{
			RebindManager.Instance.SaveAndApplyBindings();
		}
		ApplySettings(currentSettings);
	}

	// Applies the settings and saves them to disc
	void ApplySettings(Settings settings)
	{
		// Apply audio / language settings
		if (!string.IsNullOrEmpty(settings.languageID) && localizationManager != null)
		{
			localizationManager.ChangeLanguage(settings.languageID);
		}
		UpdateAudioVolume(settings.masterVolume, settings.musicVolume, settings.sfxVolume);

		// Apply graphics settings
		if (!Application.isMobilePlatform)
		{
			FullScreenMode mode = (settings.isFullscreen) ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
			Screen.SetResolution(settings.screenSize.x, settings.screenSize.y, mode);
			QualitySettings.vSyncCount = (settings.vsyncEnabled) ? 1 : 0;
		}

		RenderSettingsController.SetTerrainQuality(settings.terrainQuality);
		RenderSettingsController.SetShadowQuality(settings.shadowQuality);

		// Mobile controls
		float op = Mathf.Clamp(settings.mobileControlsOpacity, 0.20f, 1.00f);
		if (GeoGame.InputMobile.MobileControls.Instance != null)
		{
			GeoGame.InputMobile.MobileControls.Instance.SetOpacity(op);
		}

		// Save
		lastAppliedSettings = settings;
		Settings.Save(settings);
	}


	void InitLanguageUI()
	{
		string[] languageDisplayNames = new string[localizationManager.languages.Length];
		for (int i = 0; i < languageDisplayNames.Length; i++)
		{
			languageDisplayNames[i] = localizationManager.languages[i].languageDisplayName;
		}
		languageWheel.SetPossibleValues(languageDisplayNames, 0);
	}

	void SetUpScreen()
	{
		const string initialScreenSizeDeterminedKey = "initialScreenSizeDetermined";
		bool isFirstScreenSetup = PlayerPrefs.GetInt(initialScreenSizeDeterminedKey, 0) == 0;

		// The game currently struggles with rendering at high resolutions.
		// As a (hopefully temporary) work-around, we'll restrict the game's resolution the first time it's launched.
		// The player can then change it in the settings if they want, and we won't intefere again.
		if (isFirstScreenSetup)
		{
			Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
			const int initialResolutionThresholdX = 2560;
			const int initialResolutionThresholdY = 1440;

			// Check if screen size has exceeded the startup threshold, and if so, try find a lower resolution to switch to
			if (screenSize.x * screenSize.y > initialResolutionThresholdX * initialResolutionThresholdY)
			{
				Vector2Int screenRatio = ResolutionSettingsHelper.GetRatio(Screen.currentResolution.width, Screen.currentResolution.height);
				int n = Mathf.Max(1, Mathf.CeilToInt(initialResolutionThresholdX / screenRatio.x));
				Vector2Int newInitialResolution = new Vector2Int(n * screenRatio.x, n * screenRatio.y);
				Screen.SetResolution(newInitialResolution.x, newInitialResolution.y, Screen.fullScreenMode);
				//Debug.Log("Setting from " + screenSize + "   to   " + newInitialResolution + "  aspect = " + screenRatio);
			}


			// Flag that initial screen size has been determined so this doesn't run on subsequent launches
			PlayerPrefs.SetInt(initialScreenSizeDeterminedKey, 1);
			PlayerPrefs.Save();
		}
	}

	void InitResolutionSettings(Vector2Int currentScreenSize)
	{
		// Create dictionary of supported resolutions for each supported aspect ratio
		supportedResolutions = new Dictionary<Vector2Int, List<Vector2Int>>();

		foreach (Vector2Int supportedRatio in supportedRatios)
		{
			supportedResolutions.Add(supportedRatio, new List<Vector2Int>());
		}

		Resolution[] monitorResolutions = Screen.resolutions;

		foreach (Resolution resolution in monitorResolutions)
		{
			Vector2Int size = new Vector2Int(resolution.width, resolution.height);
			Vector2Int aspectRatio;
			if (IsSupportedAspectRatio(size, out aspectRatio))
			{
				if (!supportedResolutions[aspectRatio].Contains(size))
				{
					supportedResolutions[aspectRatio].Add(size);
				}
			}
		}

		// Ensure every ratio has at least one resolution (e.g. on mobile devices with non-standard aspect ratios)
		foreach (Vector2Int supportedRatio in supportedRatios)
		{
			if (supportedResolutions[supportedRatio].Count == 0)
			{
				supportedResolutions[supportedRatio].Add(new Vector2Int(Screen.width, Screen.height));
			}
		}

		// Set up ratio display
		string[] supportedRatioStrings = new string[supportedRatios.Length];
		for (int i = 0; i < supportedRatios.Length; i++)
		{
			supportedRatioStrings[i] = $"{supportedRatios[i].x} : {supportedRatios[i].y}";
		}

		int currentAspectRatioIndex;
		if (!IsSupportedAspectRatio(currentScreenSize, out currentAspectRatioIndex))
		{
			// No exact match, so find closest ratio
			float ratio = currentScreenSize.x / (float)currentScreenSize.y;
			float bestMatch = float.MaxValue;
			for (int i = 0; i < supportedRatios.Length; i++)
			{
				float supportedRatio = supportedRatios[i].x / (float)supportedRatios[i].y;
				if (Mathf.Abs(ratio - supportedRatio) < bestMatch)
				{
					bestMatch = Mathf.Abs(ratio - supportedRatio);
					currentAspectRatioIndex = i;
				}
			}
		}

		aspectRatioWheel.SetPossibleValues(supportedRatioStrings, currentAspectRatioIndex);
		SetResolutionOptions(currentAspectRatioIndex);
		aspectRatioWheel.onValueChanged -= SetResolutionOptions;
		aspectRatioWheel.onValueChanged += SetResolutionOptions;

		// Init fullscreen toggle
		fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
	}

	Vector2Int[] GetCurrentResolutionOptions()
	{
		if (aspectRatioWheel == null || supportedRatios == null || supportedRatios.Length == 0)
		{
			return new Vector2Int[] { new Vector2Int(Screen.width, Screen.height) };
		}
		int ratioIndex = Mathf.Clamp(aspectRatioWheel.activeValueIndex, 0, supportedRatios.Length - 1);
		Vector2Int aspectRatio = supportedRatios[ratioIndex];
		if (supportedResolutions != null && supportedResolutions.ContainsKey(aspectRatio) && supportedResolutions[aspectRatio].Count > 0)
		{
			return supportedResolutions[aspectRatio].ToArray();
		}
		return new Vector2Int[] { new Vector2Int(Screen.width, Screen.height) };
	}

	// Called when the selected aspect ratio changes
	void SetResolutionOptions(int aspectRatioIndex)
	{
		// Create resolution names
		Vector2Int[] resolutions = GetCurrentResolutionOptions();
		string[] resolutionNames = new string[resolutions.Length];
		for (int i = 0; i < resolutions.Length; i++)
		{
			resolutionNames[i] = $"{resolutions[i].x} x {resolutions[i].y}";
		}

		// Find resolution that most closely matches current resolution to display by default
		Vector2Int currentScreenSize = new Vector2Int(Screen.width, Screen.height);
		int resolutionIndex = 0;
		int closestResolutionMatch = int.MaxValue;

		for (int i = 0; i < resolutions.Length; i++)
		{
			int dst = (currentScreenSize - resolutions[i]).sqrMagnitude;
			if (dst < closestResolutionMatch)
			{
				closestResolutionMatch = dst;
				resolutionIndex = i;
			}
		}
		resolutionWheel.SetPossibleValues(resolutionNames, resolutionIndex);
	}


	void OnFullscreenChanged(bool isFullscreen)
	{
		FullScreenMode mode = (isFullscreen) ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
		Screen.SetResolution(Screen.width, Screen.height, mode);
	}

	public bool IsSupportedAspectRatio(Vector2Int res, out Vector2Int ratio)
	{
		int index;
		if (IsSupportedAspectRatio(res, out index))
		{
			ratio = supportedRatios[index];
			return true;
		}

		ratio = Vector2Int.zero;
		return false;
	}

	public bool IsSupportedAspectRatio(Vector2Int res, out int ratioIndex)
	{
		for (int i = 0; i < supportedRatios.Length; i++)
		{
			Vector2Int supportedRatio = supportedRatios[i];
			if (res.x % supportedRatio.x == 0 && res.y % supportedRatio.y == 0)
			{
				if (res.x / supportedRatio.x == res.y / supportedRatio.y)
				{
					ratioIndex = i;
					return true;
				}
			}
		}
		ratioIndex = -1;
		return false;
	}


	protected override void OnMenuOpened()
	{
		if (Application.isPlaying)
		{
			lastAppliedSettings = Settings.LoadSavedSettings();
			if (RebindManager.Instance != null)
			{
				RebindManager.Instance.OnSettingsOpened();
			}
			SetUIFromSettings(lastAppliedSettings);
			SetupMobileSettingsUI();
		}
	}

	protected override void OnMenuClosed()
	{
		if (Application.isPlaying)
		{
			// On mobile, auto-apply current UI selections when closing so users don't lose changes
			if (Application.isMobilePlatform)
			{
				ApplyCurrentSettings();
			}
			else
			{
				ApplySettings(lastAppliedSettings);
			}
		}
	}

	void UpdateAudioVolume()
	{
		UpdateAudioVolume(masterVolumeSlider.value, musicVolumeSlider.value, sfxVolumeSlider.value);
	}

	void UpdateAudioVolume(float masterVolumeT, float musicVolumeT, float sfxVolumeT)
	{
		audioMixer.SetFloat("Master Volume", CalculateVolumeDB(masterVolumeT));
		audioMixer.SetFloat("Music Volume", CalculateVolumeDB(musicVolumeT));
		audioMixer.SetFloat("SFX Volume", CalculateVolumeDB(sfxVolumeT));
	}

	float CalculateVolumeDB(float volumeT)
	{
		// See https://www.dr-lex.be/info-stuff/volumecontrols.html
		return Mathf.Log10(Mathf.Lerp(0.0001f, 1, volumeT)) * 20;
	}

	void OnValidate()
	{
		if (!Application.isPlaying)
		{
			if (tabGroup != null)
			{
				tabGroup.ShowTab((int)defaultTab);
			}
		}
	}

	// Mobile Controls Settings (Opacity / Transient Level / Guide / Toggle)
	private GameObject mobileSettingsCard;
	private GameObject touchSettingsContent;
	private ScrollRect pcControlsScroll;
	private TextMeshProUGUI mobileOpacityValueText;
	private TextMeshProUGUI touchToggleBtnText;
	private Image touchToggleBtnImg;
	private Button touchModeBtn;
	private Button physicalModeBtn;
	private TextMeshProUGUI touchModeTxt;
	private TextMeshProUGUI physicalModeTxt;
	private bool isTouchModeActive = true;

	private static readonly float[] OpacityLevels = new float[] { 0.20f, 0.40f, 0.60f, 0.80f, 1.00f };
	private static readonly string[] OpacityNames = new string[] { "20%", "40%", "60%", "80%", "100%" };
	private int activeOpacityIndex = 3; // default 80%

	void SetupMobileSettingsUI()
	{
		Transform controlsContainer = null;
		if (tabGroup != null && tabGroup.tabs != null && tabGroup.tabs.Length > 2 && tabGroup.tabs[2].holder != null)
		{
			controlsContainer = tabGroup.tabs[2].holder.transform;
		}
		if (controlsContainer == null) return;

		bool isMobile = Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld || (GeoGame.InputMobile.MobileControls.Instance != null && GeoGame.InputMobile.MobileControls.Instance.forceEnableInEditor);

		if (pcControlsScroll == null)
		{
			pcControlsScroll = controlsContainer.GetComponentInChildren<ScrollRect>(true);
		}

		if (mobileSettingsCard != null)
		{
			mobileSettingsCard.SetActive(true);
			SetControlsSubView(isTouchModeActive);
			UpdateMobileSettingsLabels();
			return;
		}

		// Initial subview state: touch mode for mobile devices, physical rebinds for PC
		isTouchModeActive = isMobile;

		// Load current opacity setting
		float currentOp = PlayerPrefs.GetFloat("MobileControls_Opacity", 0.80f);
		activeOpacityIndex = 3;
		for (int i = 0; i < OpacityLevels.Length; i++)
		{
			if (Mathf.Abs(OpacityLevels[i] - currentOp) < 0.05f)
			{
				activeOpacityIndex = i;
				break;
			}
		}

		// Create Dedicated Mobile Controls Container directly on the Controls Tab
		mobileSettingsCard = new GameObject("MobileControlsSettingsCard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
		mobileSettingsCard.transform.SetParent(controlsContainer, false);

		RectTransform cardRt = mobileSettingsCard.GetComponent<RectTransform>();
		cardRt.anchorMin = Vector2.zero;
		cardRt.anchorMax = Vector2.one;
		cardRt.offsetMin = new Vector2(20, 10);
		cardRt.offsetMax = new Vector2(-20, -10);

		Image cardBg = mobileSettingsCard.GetComponent<Image>();
		cardBg.sprite = GeoGame.UI.FlightUITheme.PanelHUDSprite;
		cardBg.type = Image.Type.Sliced;
		cardBg.color = new Color(0.06f, 0.10f, 0.16f, 0.95f);

		VerticalLayoutGroup vlg = mobileSettingsCard.GetComponent<VerticalLayoutGroup>();
		vlg.padding = new RectOffset(18, 18, 14, 14);
		vlg.spacing = 12;
		vlg.childAlignment = TextAnchor.UpperCenter;
		vlg.childControlWidth = true;
		vlg.childControlHeight = false;
		vlg.childForceExpandWidth = true;
		vlg.childForceExpandHeight = false;

		// 1. Top Sub-view Switcher Bar: [ 📱 Touch Controls ]  [ 🎮 Gamepad & Keys ]
		GameObject switcherRowGo = new GameObject("SwitcherRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
		switcherRowGo.transform.SetParent(mobileSettingsCard.transform, false);
		RectTransform switchRt = switcherRowGo.GetComponent<RectTransform>();
		switchRt.sizeDelta = new Vector2(0, 44);
		HorizontalLayoutGroup switchHlg = switcherRowGo.GetComponent<HorizontalLayoutGroup>();
		switchHlg.childControlWidth = true;
		switchHlg.childControlHeight = true;
		switchHlg.childForceExpandWidth = true;
		switchHlg.childForceExpandHeight = true;
		switchHlg.childAlignment = TextAnchor.MiddleCenter;
		switchHlg.spacing = 15;

		// Switcher: Touch Controls Tab Button
		GameObject touchBtnGo = new GameObject("BtnTouchMode", typeof(RectTransform), typeof(Image), typeof(Button));
		touchBtnGo.transform.SetParent(switcherRowGo.transform, false);
		touchBtnGo.GetComponent<Image>().sprite = GeoGame.UI.FlightUITheme.ButtonNormalSprite;
		touchBtnGo.GetComponent<Image>().type = Image.Type.Sliced;
		touchModeBtn = touchBtnGo.GetComponent<Button>();

		GameObject touchTxtGo = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
		touchTxtGo.transform.SetParent(touchBtnGo.transform, false);
		StretchFull(touchTxtGo.GetComponent<RectTransform>());
		touchModeTxt = touchTxtGo.GetComponent<TextMeshProUGUI>();
		touchModeTxt.fontSize = 20;
		touchModeTxt.fontStyle = FontStyles.Bold;
		touchModeTxt.alignment = TextAlignmentOptions.Center;
		touchModeTxt.color = Color.white;

		// Switcher: Physical Gamepad / Keys Tab Button
		GameObject physBtnGo = new GameObject("BtnPhysMode", typeof(RectTransform), typeof(Image), typeof(Button));
		physBtnGo.transform.SetParent(switcherRowGo.transform, false);
		physBtnGo.GetComponent<Image>().sprite = GeoGame.UI.FlightUITheme.ButtonNormalSprite;
		physBtnGo.GetComponent<Image>().type = Image.Type.Sliced;
		physicalModeBtn = physBtnGo.GetComponent<Button>();

		GameObject physTxtGo = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
		physTxtGo.transform.SetParent(physBtnGo.transform, false);
		StretchFull(physTxtGo.GetComponent<RectTransform>());
		physicalModeTxt = physTxtGo.GetComponent<TextMeshProUGUI>();
		physicalModeTxt.fontSize = 20;
		physicalModeTxt.fontStyle = FontStyles.Bold;
		physicalModeTxt.alignment = TextAlignmentOptions.Center;
		physicalModeTxt.color = Color.white;

		touchModeBtn.onClick.AddListener(() => SetControlsSubView(true));
		physicalModeBtn.onClick.AddListener(() => SetControlsSubView(false));

		// 2. Touch Controls Content Container
		touchSettingsContent = new GameObject("TouchSettingsContent", typeof(RectTransform), typeof(VerticalLayoutGroup));
		touchSettingsContent.transform.SetParent(mobileSettingsCard.transform, false);
		VerticalLayoutGroup tVlg = touchSettingsContent.GetComponent<VerticalLayoutGroup>();
		tVlg.padding = new RectOffset(0, 0, 0, 0);
		tVlg.spacing = 10;
		tVlg.childAlignment = TextAnchor.UpperCenter;
		tVlg.childControlWidth = true;
		tVlg.childControlHeight = false;
		tVlg.childForceExpandWidth = true;
		tVlg.childForceExpandHeight = false;

		// Header
		GameObject headerGo = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
		headerGo.transform.SetParent(touchSettingsContent.transform, false);
		RectTransform hRt = headerGo.GetComponent<RectTransform>();
		hRt.sizeDelta = new Vector2(0, 32);
		TextMeshProUGUI hTxt = headerGo.GetComponent<TextMeshProUGUI>();
		hTxt.fontSize = 24;
		hTxt.fontStyle = FontStyles.Bold;
		hTxt.color = new Color(0f, 0.90f, 1f, 1f); // Neon Cyan HUD
		hTxt.alignment = TextAlignmentOptions.Center;

		// Subheader
		GameObject subheaderGo = new GameObject("Subheader", typeof(RectTransform), typeof(TextMeshProUGUI));
		subheaderGo.transform.SetParent(touchSettingsContent.transform, false);
		RectTransform subRt = subheaderGo.GetComponent<RectTransform>();
		subRt.sizeDelta = new Vector2(0, 24);
		TextMeshProUGUI subTxt = subheaderGo.GetComponent<TextMeshProUGUI>();
		subTxt.fontSize = 16;
		subTxt.color = new Color(0.85f, 0.90f, 0.95f, 0.9f);
		subTxt.alignment = TextAlignmentOptions.Center;

		// Toggle Row: Touch HUD Active [ ON / OFF ]
		GameObject toggleRowGo = new GameObject("ToggleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
		toggleRowGo.transform.SetParent(touchSettingsContent.transform, false);
		RectTransform togRt = toggleRowGo.GetComponent<RectTransform>();
		togRt.sizeDelta = new Vector2(0, 48);
		HorizontalLayoutGroup togHlg = toggleRowGo.GetComponent<HorizontalLayoutGroup>();
		togHlg.childControlWidth = false;
		togHlg.childControlHeight = true;
		togHlg.childForceExpandWidth = false;
		togHlg.childForceExpandHeight = true;
		togHlg.childAlignment = TextAnchor.MiddleCenter;
		togHlg.spacing = 25;

		GameObject togTitleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
		togTitleGo.transform.SetParent(toggleRowGo.transform, false);
		RectTransform togTitleRt = togTitleGo.GetComponent<RectTransform>();
		togTitleRt.sizeDelta = new Vector2(420, 46);
		TextMeshProUGUI togTitleTxt = togTitleGo.GetComponent<TextMeshProUGUI>();
		togTitleTxt.fontSize = 20;
		togTitleTxt.color = Color.white;
		togTitleTxt.alignment = TextAlignmentOptions.MidlineLeft;

		GameObject togBtnGo = new GameObject("BtnToggle", typeof(RectTransform), typeof(Image), typeof(Button));
		togBtnGo.transform.SetParent(toggleRowGo.transform, false);
		togBtnGo.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 46);
		mobileToggleBtnImg = togBtnGo.GetComponent<Image>();
		mobileToggleBtnImg.sprite = GeoGame.UI.FlightUITheme.ButtonPrimarySprite;
		mobileToggleBtnImg.type = Image.Type.Sliced;
		Button togBtn = togBtnGo.GetComponent<Button>();

		GameObject togBtnTxtGo = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
		togBtnTxtGo.transform.SetParent(togBtnGo.transform, false);
		StretchFull(togBtnTxtGo.GetComponent<RectTransform>());
		touchToggleBtnText = togBtnTxtGo.GetComponent<TextMeshProUGUI>();
		touchToggleBtnText.fontSize = 20;
		touchToggleBtnText.fontStyle = FontStyles.Bold;
		touchToggleBtnText.alignment = TextAlignmentOptions.Center;
		touchToggleBtnText.color = Color.white;

		togBtn.onClick.AddListener(() =>
		{
			bool cur = PlayerPrefs.GetInt("MobileControls_Enabled", 1) == 1;
			bool next = !cur;
			if (GeoGame.InputMobile.MobileControls.Instance != null)
			{
				GeoGame.InputMobile.MobileControls.Instance.SetMobileControlsEnabled(next);
			}
			else
			{
				PlayerPrefs.SetInt("MobileControls_Enabled", next ? 1 : 0);
				PlayerPrefs.Save();
			}
			UpdateToggleVisuals(next);
		});

		// Opacity / Transient Level Row
		GameObject rowGo = new GameObject("OpacityRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
		rowGo.transform.SetParent(touchSettingsContent.transform, false);
		RectTransform rowRt = rowGo.GetComponent<RectTransform>();
		rowRt.sizeDelta = new Vector2(0, 50);
		HorizontalLayoutGroup rowHlg = rowGo.GetComponent<HorizontalLayoutGroup>();
		rowHlg.childControlWidth = false;
		rowHlg.childControlHeight = true;
		rowHlg.childForceExpandWidth = false;
		rowHlg.childForceExpandHeight = true;
		rowHlg.childAlignment = TextAnchor.MiddleCenter;
		rowHlg.spacing = 25;

		GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
		titleGo.transform.SetParent(rowGo.transform, false);
		RectTransform titleRt = titleGo.GetComponent<RectTransform>();
		titleRt.sizeDelta = new Vector2(420, 46);
		TextMeshProUGUI titleTxt = titleGo.GetComponent<TextMeshProUGUI>();
		titleTxt.fontSize = 20;
		titleTxt.color = Color.white;
		titleTxt.alignment = TextAlignmentOptions.MidlineLeft;

		// Stepper Container: [<] [ 80% ] [>]
		GameObject stepperGo = new GameObject("Stepper", typeof(RectTransform), typeof(HorizontalLayoutGroup));
		stepperGo.transform.SetParent(rowGo.transform, false);
		RectTransform stepperRt = stepperGo.GetComponent<RectTransform>();
		stepperRt.sizeDelta = new Vector2(280, 46);
		HorizontalLayoutGroup stepHlg = stepperGo.GetComponent<HorizontalLayoutGroup>();
		stepHlg.childControlWidth = false;
		stepHlg.childControlHeight = true;
		stepHlg.childForceExpandWidth = false;
		stepHlg.childForceExpandHeight = true;
		stepHlg.childAlignment = TextAnchor.MiddleCenter;
		stepHlg.spacing = 10;

		// Left Button [<]
		GameObject btnLeftGo = new GameObject("BtnDecrease", typeof(RectTransform), typeof(Image), typeof(Button));
		btnLeftGo.transform.SetParent(stepperGo.transform, false);
		btnLeftGo.GetComponent<RectTransform>().sizeDelta = new Vector2(54, 46);
		btnLeftGo.GetComponent<Image>().sprite = GeoGame.UI.FlightUITheme.ButtonNormalSprite;
		btnLeftGo.GetComponent<Image>().type = Image.Type.Sliced;
		Button btnLeft = btnLeftGo.GetComponent<Button>();
		GameObject txtLeftGo = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
		txtLeftGo.transform.SetParent(btnLeftGo.transform, false);
		StretchFull(txtLeftGo.GetComponent<RectTransform>());
		TextMeshProUGUI lTxt = txtLeftGo.GetComponent<TextMeshProUGUI>();
		lTxt.text = "<";
		lTxt.fontSize = 24;
		lTxt.fontStyle = FontStyles.Bold;
		lTxt.alignment = TextAlignmentOptions.Center;
		lTxt.color = Color.white;

		// Value Box [ 80% ]
		GameObject valBoxGo = new GameObject("ValBox", typeof(RectTransform), typeof(Image));
		valBoxGo.transform.SetParent(stepperGo.transform, false);
		valBoxGo.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 46);
		valBoxGo.GetComponent<Image>().sprite = GeoGame.UI.FlightUITheme.ButtonNormalSprite;
		valBoxGo.GetComponent<Image>().type = Image.Type.Sliced;
		valBoxGo.GetComponent<Image>().color = new Color(0.12f, 0.18f, 0.25f, 0.85f);
		GameObject valTxtGo = new GameObject("ValTxt", typeof(RectTransform), typeof(TextMeshProUGUI));
		valTxtGo.transform.SetParent(valBoxGo.transform, false);
		StretchFull(valTxtGo.GetComponent<RectTransform>());
		mobileOpacityValueText = valTxtGo.GetComponent<TextMeshProUGUI>();
		mobileOpacityValueText.fontSize = 22;
		mobileOpacityValueText.fontStyle = FontStyles.Bold;
		mobileOpacityValueText.alignment = TextAlignmentOptions.Center;
		mobileOpacityValueText.color = new Color(0f, 0.9f, 1f, 1f); // Neon cyan
		mobileOpacityValueText.text = OpacityNames[activeOpacityIndex];

		// Right Button [>]
		GameObject btnRightGo = new GameObject("BtnIncrease", typeof(RectTransform), typeof(Image), typeof(Button));
		btnRightGo.transform.SetParent(stepperGo.transform, false);
		btnRightGo.GetComponent<RectTransform>().sizeDelta = new Vector2(54, 46);
		btnRightGo.GetComponent<Image>().sprite = GeoGame.UI.FlightUITheme.ButtonNormalSprite;
		btnRightGo.GetComponent<Image>().type = Image.Type.Sliced;
		Button btnRight = btnRightGo.GetComponent<Button>();
		GameObject txtRightGo = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
		txtRightGo.transform.SetParent(btnRightGo.transform, false);
		StretchFull(txtRightGo.GetComponent<RectTransform>());
		TextMeshProUGUI rTxt = txtRightGo.GetComponent<TextMeshProUGUI>();
		rTxt.text = ">";
		rTxt.fontSize = 24;
		rTxt.fontStyle = FontStyles.Bold;
		rTxt.alignment = TextAlignmentOptions.Center;
		rTxt.color = Color.white;

		btnLeft.onClick.AddListener(() =>
		{
			if (activeOpacityIndex > 0)
			{
				activeOpacityIndex--;
				ApplyOpacityChange();
			}
		});

		btnRight.onClick.AddListener(() =>
		{
			if (activeOpacityIndex < OpacityLevels.Length - 1)
			{
				activeOpacityIndex++;
				ApplyOpacityChange();
			}
		});

		// 4. Controls Information & Layout Card
		GameObject guideGo = new GameObject("GuideBox", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
		guideGo.transform.SetParent(touchSettingsContent.transform, false);
		RectTransform guideRt = guideGo.GetComponent<RectTransform>();
		guideRt.sizeDelta = new Vector2(0, 115);
		Image guideBg = guideGo.GetComponent<Image>();
		guideBg.sprite = GeoGame.UI.FlightUITheme.ButtonNormalSprite;
		guideBg.type = Image.Type.Sliced;
		guideBg.color = new Color(0.04f, 0.08f, 0.14f, 0.85f);

		VerticalLayoutGroup gVlg = guideGo.GetComponent<VerticalLayoutGroup>();
		gVlg.padding = new RectOffset(16, 16, 10, 10);
		gVlg.childAlignment = TextAnchor.MiddleCenter;
		gVlg.childControlWidth = true;
		gVlg.childControlHeight = true;
		gVlg.childForceExpandWidth = true;
		gVlg.childForceExpandHeight = true;

		GameObject guideTxtGo = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
		guideTxtGo.transform.SetParent(guideGo.transform, false);
		StretchFull(guideTxtGo.GetComponent<RectTransform>());
		TextMeshProUGUI guideTxt = guideTxtGo.GetComponent<TextMeshProUGUI>();
		guideTxt.fontSize = 16;
		guideTxt.color = new Color(0.70f, 0.88f, 1.0f, 0.95f);
		guideTxt.alignment = TextAlignmentOptions.Center;

		SetControlsSubView(isTouchModeActive);
		UpdateToggleVisuals(PlayerPrefs.GetInt("MobileControls_Enabled", 1) == 1);
		UpdateMobileSettingsLabels();
	}

	void SetControlsSubView(bool showTouch)
	{
		isTouchModeActive = showTouch;

		if (touchSettingsContent != null)
		{
			touchSettingsContent.SetActive(showTouch);
		}

		if (pcControlsScroll != null)
		{
			pcControlsScroll.gameObject.SetActive(!showTouch);
		}

		if (touchModeBtn != null)
		{
			touchModeBtn.GetComponent<Image>().sprite = showTouch ? GeoGame.UI.FlightUITheme.ButtonPrimarySprite : GeoGame.UI.FlightUITheme.ButtonNormalSprite;
		}

		if (physicalModeBtn != null)
		{
			physicalModeBtn.GetComponent<Image>().sprite = !showTouch ? GeoGame.UI.FlightUITheme.ButtonPrimarySprite : GeoGame.UI.FlightUITheme.ButtonNormalSprite;
		}
	}

	void UpdateToggleVisuals(bool isEnabled)
	{
		bool isRTL = GeoGame.Localization.LocalizationManager.IsRightToLeftWritingSystem;
		if (touchToggleBtnText != null)
		{
			if (isEnabled)
			{
				touchToggleBtnText.text = isRTL ? GeoGame.Localization.Arabic.ArabicFixer.Fix("✔ مفعل (ACTIVE)") : "✔ ACTIVE";
				touchToggleBtnText.color = new Color(0.2f, 1f, 0.5f, 1f);
			}
			else
			{
				touchToggleBtnText.text = isRTL ? GeoGame.Localization.Arabic.ArabicFixer.Fix("✖ معطل (OFF)") : "✖ DISABLED";
				touchToggleBtnText.color = new Color(1f, 0.4f, 0.4f, 1f);
			}
		}

		if (mobileToggleBtnImg != null)
		{
			mobileToggleBtnImg.sprite = isEnabled ? GeoGame.UI.FlightUITheme.ButtonPrimarySprite : GeoGame.UI.FlightUITheme.ButtonDangerSprite;
		}
	}

	void ApplyOpacityChange()
	{
		float val = OpacityLevels[activeOpacityIndex];
		if (mobileOpacityValueText != null)
		{
			mobileOpacityValueText.text = OpacityNames[activeOpacityIndex];
		}
		if (GeoGame.InputMobile.MobileControls.Instance != null)
		{
			GeoGame.InputMobile.MobileControls.Instance.SetOpacity(val);
		}
		else
		{
			PlayerPrefs.SetFloat("MobileControls_Opacity", val);
			PlayerPrefs.Save();
		}
	}

	void UpdateMobileSettingsLabels()
	{
		if (mobileSettingsCard == null) return;
		bool isRTL = GeoGame.Localization.LocalizationManager.IsRightToLeftWritingSystem;
		var fontAsset = GeoGame.Localization.Arabic.ArabicFontManager.ArabicFontAsset;

		if (touchModeTxt != null)
		{
			if (isRTL && fontAsset != null) touchModeTxt.font = fontAsset;
			touchModeTxt.text = isRTL ? GeoGame.Localization.Arabic.ArabicFixer.Fix("📱 أزرار اللمس") : "📱 Touch Controls";
		}

		if (physicalModeTxt != null)
		{
			if (isRTL && fontAsset != null) physicalModeTxt.font = fontAsset;
			physicalModeTxt.text = isRTL ? GeoGame.Localization.Arabic.ArabicFixer.Fix("🎮 اليد ولوحة المفاتيح") : "🎮 Gamepad & Keys";
		}

		TextMeshProUGUI hTxt = mobileSettingsCard.transform.Find("TouchSettingsContent/Header")?.GetComponent<TextMeshProUGUI>();
		if (hTxt != null)
		{
			if (isRTL && fontAsset != null) hTxt.font = fontAsset;
			hTxt.text = isRTL ? GeoGame.Localization.Arabic.ArabicFixer.Fix("أزرار التحكم باللمس (موبايل)") : "MOBILE TOUCH CONTROLS";
		}

		TextMeshProUGUI subTxt = mobileSettingsCard.transform.Find("TouchSettingsContent/Subheader")?.GetComponent<TextMeshProUGUI>();
		if (subTxt != null)
		{
			if (isRTL && fontAsset != null) subTxt.font = fontAsset;
			subTxt.text = isRTL ? GeoGame.Localization.Arabic.ArabicFixer.Fix("شاشة التحكم باللمس وعصا التوجيه الافتراضية") : "On-Screen Flight HUD & Virtual Joystick";
		}

		TextMeshProUGUI togTitleTxt = mobileSettingsCard.transform.Find("TouchSettingsContent/ToggleRow/Title")?.GetComponent<TextMeshProUGUI>();
		if (togTitleTxt != null)
		{
			if (isRTL && fontAsset != null) togTitleTxt.font = fontAsset;
			togTitleTxt.text = isRTL ? GeoGame.Localization.Arabic.ArabicFixer.Fix("أزرار التحكم باللمس على الشاشة") : "Touch Controls HUD";
		}

		TextMeshProUGUI titleTxt = mobileSettingsCard.transform.Find("TouchSettingsContent/OpacityRow/Title")?.GetComponent<TextMeshProUGUI>();
		if (titleTxt != null)
		{
			if (isRTL && fontAsset != null) titleTxt.font = fontAsset;
			titleTxt.text = isRTL ? GeoGame.Localization.Arabic.ArabicFixer.Fix("شفافية الأزرار (Transient Level)") : "Button Opacity (Transient Level)";
		}

		TextMeshProUGUI guideTxt = mobileSettingsCard.transform.Find("TouchSettingsContent/GuideBox/Txt")?.GetComponent<TextMeshProUGUI>();
		if (guideTxt != null)
		{
			if (isRTL && fontAsset != null) guideTxt.font = fontAsset;
			guideTxt.text = isRTL
				? GeoGame.Localization.Arabic.ArabicFixer.Fix("• عصا توجيه ديناميكية: اسحب إصبعك في أي مكان على يسار الشاشة للتوجيه والارتفاع\n• أزرار الطيران: السرعة والتعزيز وإسقاط الصناديق على يمين الشاشة\n• شريط المهام: تبديل الكاميرا وخريطة العالم ثلاثية الأبعاد والقائمة")
				: "• Dynamic Steering: Touch & drag anywhere on left screen to steer\n• Flight Actions: Throttle, Boost, and Package Drop on right screen\n• Nav Bar: Cockpit Camera toggle, 3D Globe Map, and Systems Menu";
		}

		UpdateToggleVisuals(PlayerPrefs.GetInt("MobileControls_Enabled", 1) == 1);

		if (mobileOpacityValueText != null)
		{
			mobileOpacityValueText.text = OpacityNames[activeOpacityIndex];
		}
	}

	private void StretchFull(RectTransform rt)
	{
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.offsetMin = Vector2.zero;
		rt.offsetMax = Vector2.zero;
	}
}


public struct Settings
{
	public enum TerrainQuality { Low, High }
	public enum ShadowQuality { Disabled, Low, High }

	// Graphics
	public Vector2Int screenSize;
	public bool isFullscreen;
	public bool vsyncEnabled;
	public TerrainQuality terrainQuality;
	public ShadowQuality shadowQuality;

	// Audio / Language
	public string languageID;
	public float masterVolume;
	public float musicVolume;
	public float sfxVolume;

	// Controls
	public float mobileControlsOpacity;

	// Load settings from prefs
	public static Settings LoadSavedSettings()
	{
		Settings settings = new Settings();
		// Graphics
		settings.vsyncEnabled = PlayerPrefs.GetInt(nameof(vsyncEnabled), defaultValue: 1) == 1;
		// Note: since Unity remembers screen size / fullscreen mode automatically, just get current screen size
		settings.screenSize = new Vector2Int(Screen.width, Screen.height);
		settings.isFullscreen = Screen.fullScreen;
		settings.terrainQuality = (TerrainQuality)PlayerPrefs.GetInt(nameof(terrainQuality), defaultValue: (int)TerrainQuality.High);
		settings.shadowQuality = (ShadowQuality)PlayerPrefs.GetInt(nameof(shadowQuality), defaultValue: (int)ShadowQuality.High);

		// Audio / Language
		settings.languageID = PlayerPrefs.GetString(nameof(languageID));
		settings.masterVolume = PlayerPrefs.GetFloat(nameof(masterVolume), defaultValue: 0.75f);
		settings.musicVolume = PlayerPrefs.GetFloat(nameof(musicVolume), defaultValue: 0.75f);
		settings.sfxVolume = PlayerPrefs.GetFloat(nameof(sfxVolume), defaultValue: 0.75f);

		// Controls
		float op = PlayerPrefs.GetFloat("MobileControls_Opacity", defaultValue: 0.80f);
		if (op < 0.20f || op > 1.0f) op = 0.80f;
		settings.mobileControlsOpacity = op;
		return settings;
	}

	public static void Save(Settings settings)
	{
		// --- Graphics
		// Note: Unity remembers screen size / fullscreen mode automatically, so don't need to save these
		PlayerPrefs.SetInt(nameof(vsyncEnabled), (settings.vsyncEnabled) ? 1 : 0);
		PlayerPrefs.SetInt(nameof(terrainQuality), (int)settings.terrainQuality);
		PlayerPrefs.SetInt(nameof(shadowQuality), (int)settings.shadowQuality);

		// Audio / Language
		PlayerPrefs.SetString(nameof(languageID), settings.languageID);
		PlayerPrefs.SetFloat(nameof(masterVolume), settings.masterVolume);
		PlayerPrefs.SetFloat(nameof(musicVolume), settings.musicVolume);
		PlayerPrefs.SetFloat(nameof(sfxVolume), settings.sfxVolume);

		// Controls
		PlayerPrefs.SetFloat("MobileControls_Opacity", settings.mobileControlsOpacity);

		// Write
		PlayerPrefs.Save();
	}


}