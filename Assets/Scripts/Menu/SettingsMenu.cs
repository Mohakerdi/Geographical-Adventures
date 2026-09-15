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

	// Mobile Controls Settings (Opacity / Transient Level)
	private GameObject mobileSettingsCard;
	private TMP_Text mobileOpacityValueText;
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

		ScrollRect scroll = controlsContainer.GetComponentInChildren<ScrollRect>(true);

		if (!isMobile)
		{
			// On PC / Desktop, show standard keyboard rebinding scroll view
			if (scroll != null) scroll.gameObject.SetActive(true);
			if (mobileSettingsCard != null) mobileSettingsCard.SetActive(false);
			return;
		}

		// On Mobile / Android APK: Completely hide PC keyboard rebind controls
		if (scroll != null)
		{
			scroll.gameObject.SetActive(false);
		}

		if (mobileSettingsCard != null)
		{
			mobileSettingsCard.SetActive(true);
			UpdateMobileSettingsLabels();
			return;
		}

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
		mobileSettingsCard = new GameObject("MobileControlsSettingsCard", typeof(RectTransform), typeof(VerticalLayoutGroup));
		mobileSettingsCard.transform.SetParent(controlsContainer, false);

		RectTransform cardRt = mobileSettingsCard.GetComponent<RectTransform>();
		cardRt.anchorMin = Vector2.zero;
		cardRt.anchorMax = Vector2.one;
		cardRt.offsetMin = new Vector2(40, 20);
		cardRt.offsetMax = new Vector2(-40, -20);

		VerticalLayoutGroup vlg = mobileSettingsCard.GetComponent<VerticalLayoutGroup>();
		vlg.padding = new RectOffset(20, 20, 15, 15);
		vlg.spacing = 18;
		vlg.childAlignment = TextAnchor.UpperCenter;
		vlg.childControlWidth = true;
		vlg.childControlHeight = false;
		vlg.childForceExpandWidth = true;
		vlg.childForceExpandHeight = false;

		// 1. Header
		GameObject headerGo = new GameObject("Header", typeof(RectTransform), typeof(TMP_Text));
		headerGo.transform.SetParent(mobileSettingsCard.transform, false);
		RectTransform hRt = headerGo.GetComponent<RectTransform>();
		hRt.sizeDelta = new Vector2(0, 40);
		TMP_Text hTxt = headerGo.GetComponent<TMP_Text>();
		hTxt.fontSize = 28;
		hTxt.fontStyle = FontStyles.Bold;
		hTxt.color = new Color(0f, 0.90f, 1f, 1f); // Neon Cyan HUD
		hTxt.alignment = TextAlignmentOptions.Center;

		// 2. Subheader
		GameObject subheaderGo = new GameObject("Subheader", typeof(RectTransform), typeof(TMP_Text));
		subheaderGo.transform.SetParent(mobileSettingsCard.transform, false);
		RectTransform subRt = subheaderGo.GetComponent<RectTransform>();
		subRt.sizeDelta = new Vector2(0, 30);
		TMP_Text subTxt = subheaderGo.GetComponent<TMP_Text>();
		subTxt.fontSize = 18;
		subTxt.color = new Color(0.85f, 0.90f, 0.95f, 0.9f);
		subTxt.alignment = TextAlignmentOptions.Center;

		// 3. Opacity / Transient Level Row
		GameObject rowGo = new GameObject("OpacityRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
		rowGo.transform.SetParent(mobileSettingsCard.transform, false);
		RectTransform rowRt = rowGo.GetComponent<RectTransform>();
		rowRt.sizeDelta = new Vector2(0, 60);
		HorizontalLayoutGroup rowHlg = rowGo.GetComponent<HorizontalLayoutGroup>();
		rowHlg.childControlWidth = false;
		rowHlg.childControlHeight = true;
		rowHlg.childForceExpandWidth = false;
		rowHlg.childForceExpandHeight = true;
		rowHlg.childAlignment = TextAnchor.MiddleCenter;
		rowHlg.spacing = 25;

		// Row Title Label
		GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TMP_Text));
		titleGo.transform.SetParent(rowGo.transform, false);
		RectTransform titleRt = titleGo.GetComponent<RectTransform>();
		titleRt.sizeDelta = new Vector2(420, 50);
		TMP_Text titleTxt = titleGo.GetComponent<TMP_Text>();
		titleTxt.fontSize = 22;
		titleTxt.color = Color.white;
		titleTxt.alignment = TextAlignmentOptions.MidlineLeft;

		// Stepper Container: [<] [ 80% ] [>]
		GameObject stepperGo = new GameObject("Stepper", typeof(RectTransform), typeof(HorizontalLayoutGroup));
		stepperGo.transform.SetParent(rowGo.transform, false);
		RectTransform stepperRt = stepperGo.GetComponent<RectTransform>();
		stepperRt.sizeDelta = new Vector2(280, 50);
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
		btnLeftGo.GetComponent<RectTransform>().sizeDelta = new Vector2(54, 50);
		btnLeftGo.GetComponent<Image>().sprite = GeoGame.UI.FlightUITheme.ButtonNormalSprite;
		btnLeftGo.GetComponent<Image>().type = Image.Type.Sliced;
		Button btnLeft = btnLeftGo.GetComponent<Button>();
		GameObject txtLeftGo = new GameObject("Txt", typeof(RectTransform), typeof(TMP_Text));
		txtLeftGo.transform.SetParent(btnLeftGo.transform, false);
		StretchFull(txtLeftGo.GetComponent<RectTransform>());
		TMP_Text lTxt = txtLeftGo.GetComponent<TMP_Text>();
		lTxt.text = "<";
		lTxt.fontSize = 26;
		lTxt.fontStyle = FontStyles.Bold;
		lTxt.alignment = TextAlignmentOptions.Center;
		lTxt.color = Color.white;

		// Value Box [ 80% ]
		GameObject valBoxGo = new GameObject("ValBox", typeof(RectTransform), typeof(Image));
		valBoxGo.transform.SetParent(stepperGo.transform, false);
		valBoxGo.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 50);
		valBoxGo.GetComponent<Image>().sprite = GeoGame.UI.FlightUITheme.ButtonNormalSprite;
		valBoxGo.GetComponent<Image>().type = Image.Type.Sliced;
		valBoxGo.GetComponent<Image>().color = new Color(0.12f, 0.18f, 0.25f, 0.85f);
		GameObject valTxtGo = new GameObject("ValTxt", typeof(RectTransform), typeof(TMP_Text));
		valTxtGo.transform.SetParent(valBoxGo.transform, false);
		StretchFull(valTxtGo.GetComponent<RectTransform>());
		mobileOpacityValueText = valTxtGo.GetComponent<TMP_Text>();
		mobileOpacityValueText.fontSize = 24;
		mobileOpacityValueText.fontStyle = FontStyles.Bold;
		mobileOpacityValueText.alignment = TextAlignmentOptions.Center;
		mobileOpacityValueText.color = new Color(0f, 0.9f, 1f, 1f); // Neon cyan
		mobileOpacityValueText.text = OpacityNames[activeOpacityIndex];

		// Right Button [>]
		GameObject btnRightGo = new GameObject("BtnIncrease", typeof(RectTransform), typeof(Image), typeof(Button));
		btnRightGo.transform.SetParent(stepperGo.transform, false);
		btnRightGo.GetComponent<RectTransform>().sizeDelta = new Vector2(54, 50);
		btnRightGo.GetComponent<Image>().sprite = GeoGame.UI.FlightUITheme.ButtonNormalSprite;
		btnRightGo.GetComponent<Image>().type = Image.Type.Sliced;
		Button btnRight = btnRightGo.GetComponent<Button>();
		GameObject txtRightGo = new GameObject("Txt", typeof(RectTransform), typeof(TMP_Text));
		txtRightGo.transform.SetParent(btnRightGo.transform, false);
		StretchFull(txtRightGo.GetComponent<RectTransform>());
		TMP_Text rTxt = txtRightGo.GetComponent<TMP_Text>();
		rTxt.text = ">";
		rTxt.fontSize = 26;
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
		GameObject guideGo = new GameObject("GuideBox", typeof(RectTransform), typeof(TMP_Text));
		guideGo.transform.SetParent(mobileSettingsCard.transform, false);
		RectTransform guideRt = guideGo.GetComponent<RectTransform>();
		guideRt.sizeDelta = new Vector2(0, 110);
		TMP_Text guideTxt = guideGo.GetComponent<TMP_Text>();
		guideTxt.fontSize = 17;
		guideTxt.color = new Color(0.70f, 0.85f, 1.0f, 0.90f);
		guideTxt.alignment = TextAlignmentOptions.Center;

		UpdateMobileSettingsLabels();
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

		TMP_Text hTxt = mobileSettingsCard.transform.Find("Header")?.GetComponent<TMP_Text>();
		if (hTxt != null)
		{
			if (isRTL && fontAsset != null) hTxt.font = fontAsset;
			hTxt.text = isRTL ? GeoGame.Localization.Arabic.ArabicFixer.Fix("أزرار التحكم باللمس (موبايل)") : "MOBILE TOUCH CONTROLS";
		}

		TMP_Text subTxt = mobileSettingsCard.transform.Find("Subheader")?.GetComponent<TMP_Text>();
		if (subTxt != null)
		{
			if (isRTL && fontAsset != null) subTxt.font = fontAsset;
			subTxt.text = isRTL ? GeoGame.Localization.Arabic.ArabicFixer.Fix("شاشة التحكم باللمس وعصا التوجيه الافتراضية") : "On-Screen Flight HUD & Virtual Joystick";
		}

		TMP_Text titleTxt = mobileSettingsCard.transform.Find("OpacityRow/Title")?.GetComponent<TMP_Text>();
		if (titleTxt != null)
		{
			if (isRTL && fontAsset != null) titleTxt.font = fontAsset;
			titleTxt.text = isRTL ? GeoGame.Localization.Arabic.ArabicFixer.Fix("شفافية الأزرار (Transient Level)") : "Button Opacity (Transient Level)";
		}

		TMP_Text guideTxt = mobileSettingsCard.transform.Find("GuideBox")?.GetComponent<TMP_Text>();
		if (guideTxt != null)
		{
			if (isRTL && fontAsset != null) guideTxt.font = fontAsset;
			guideTxt.text = isRTL
				? GeoGame.Localization.Arabic.ArabicFixer.Fix("• عصا توجيه ديناميكية: اسحب إصبعك في أي مكان على يسار الشاشة للتوجيه\n• أزرار الطيران: السرعة والتعزيز وإسقاط الصناديق على يمين الشاشة\n• شريط المهام: تبديل الكاميرا وخريطة العالم ثلاثية الأبعاد")
				: "• Dynamic Steering: Touch & drag anywhere on left screen to steer\n• Flight Actions: Throttle, Boost, and Package Drop on right screen\n• Nav Bar: Cockpit Camera toggle, 3D Globe Map, and Systems Menu";
		}

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