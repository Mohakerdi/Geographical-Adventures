using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GeoGame.Quest;
using GeoGame.Localization;
using GeoGame.Localization.Arabic;

public class StatsMenu : Menu
{
	public Color extraInfoCol;
	public Color perfectCol;
	public Color goodCol;
	public Color okCol;
	public Color badCol;

	public Player player;
	public QuestSystem questSystem;
	public TMP_Text labels;
	public TMP_Text values;

	public GameObject gameOverHolder;
	public GameObject pausedHolder;

	public Button quitToMainMenuButton;
	public Button continueInEndlessModeButton;

	protected override void Awake()
	{
		base.Awake();
		quitToMainMenuButton.onClick.AddListener(GameController.ExitToMainMenu);
		continueInEndlessModeButton.onClick.AddListener(SwitchToEndlessMode);
		LocalizationManager.onLanguageChanged += Refresh;
	}

	void OnDestroy()
	{
		LocalizationManager.onLanguageChanged -= Refresh;
	}

	void SwitchToEndlessMode()
	{
		questSystem.ContinueInEndlessMode();
		CloseMenu();
	}

	void Update()
	{
		//OnMenuOpened();
	}

	protected override void OnMenuOpened()
	{
		Refresh();
	}

	[NaughtyAttributes.Button()]
	void Refresh()
	{
		gameOverHolder.SetActive(GameController.IsState(GameState.GameOver));
		pausedHolder.SetActive(!GameController.IsState(GameState.GameOver));

		bool isRTL = LocalizationManager.IsRightToLeftWritingSystem;

		// Localize buttons and header titles
		if (gameOverHolder != null)
		{
			TMP_Text title = gameOverHolder.GetComponentInChildren<TMP_Text>();
			if (title != null)
			{
				title.text = isRTL ? ArabicFixer.Fix("انتهى الوقت!") : "Time's Up!";
				title.isRightToLeftText = false;
			}
		}

		if (continueInEndlessModeButton != null)
		{
			TMP_Text btnText = continueInEndlessModeButton.GetComponentInChildren<TMP_Text>();
			if (btnText != null)
			{
				btnText.text = isRTL ? ArabicFixer.Fix("المتابعة في وضع اللعب اللانهائي") : "Continue in Endless Mode";
				btnText.isRightToLeftText = false;
			}
		}

		if (quitToMainMenuButton != null)
		{
			TMP_Text btnText = quitToMainMenuButton.GetComponentInChildren<TMP_Text>();
			if (btnText != null)
			{
				btnText.text = isRTL ? ArabicFixer.Fix("العودة إلى القائمة الرئيسية") : "Quit to Main Menu";
				btnText.isRightToLeftText = false;
			}
		}

		if (pausedHolder != null)
		{
			TMP_Text backText = pausedHolder.GetComponentInChildren<TMP_Text>();
			if (backText != null)
			{
				backText.text = isRTL ? ArabicFixer.Fix("رجوع") : "Back";
				backText.isRightToLeftText = false;
			}
		}

		if (labels != null && values != null)
		{
			labels.isRightToLeftText = false;
			values.isRightToLeftText = false;
			labels.alignment = isRTL ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
			values.alignment = isRTL ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.TopRight;
		}

		labels.text = "";
		values.text = "";

		// Travel distance
		float numTimesAroundGlobe = player.distanceTravelledKM / GeoMaths.EarthCircumferenceKM;
		string dstTravelledString = DistanceString((int)player.distanceTravelledKM);
		string timesAroundGlobeString = isRTL
			? ArabicFixer.Fix($"({numTimesAroundGlobe:0.0} مرة حول الأرض)")
			: $"({numTimesAroundGlobe:0.0} times around the globe)";

		Add(isRTL ? ArabicFixer.Fix("المسافة المقطوعة") : "Distance travelled", CreateString(dstTravelledString, timesAroundGlobeString));
		Add(isRTL ? ArabicFixer.Fix("الوقت") : "Timer", GetTimeString(questSystem.TimeSinceGameStart));
		AddSpace();

		var results = questSystem.GetResults();

		int numTotal = results.Length;
		int numPerfect = 0;
		int numGood = 0;
		int numOk = 0;
		int numBad = 0;
		float totalDistanceError = 0;
		QuestSystem.DeliveryResult bestResult = default;
		bestResult.distanceKM = float.MaxValue;
		QuestSystem.DeliveryResult worstResult = default;
		worstResult.distanceKM = float.MinValue;

		foreach (var result in results)
		{
			if (result.distanceKM <= QuestSystem.perfectRadius)
			{
				numPerfect++;
			}
			else if (result.distanceKM < QuestSystem.goodRadius)
			{
				numGood++;
			}
			else if (result.distanceKM < QuestSystem.okRadius)
			{
				numOk++;
			}
			else
			{
				numBad++;
			}

			if (result.distanceKM < bestResult.distanceKM)
			{
				bestResult = result;
			}
			if (result.distanceKM > worstResult.distanceKM)
			{
				worstResult = result;
			}
			totalDistanceError += result.distanceKM;
		}

		Add(isRTL ? ArabicFixer.Fix("الطرود المسلّمة") : "Packages delivered", results.Length.ToString());

		string perfectLabel = isRTL ? ArabicFixer.Fix("تسليم مثالي") : "Perfect deliveries";
		string perfectRad = isRTL ? ArabicFixer.Fix($"(< {QuestSystem.perfectRadius} كم)") : $"(< {QuestSystem.perfectRadius} km)";
		Add(CreateString(SetColour(perfectLabel, perfectCol), perfectRad), DeliveryResultString(numPerfect, numTotal));

		string goodLabel = isRTL ? ArabicFixer.Fix("تسليم جيد جداً") : "Good deliveries";
		string goodRad = isRTL ? ArabicFixer.Fix($"(< {QuestSystem.goodRadius} كم)") : $"(< {QuestSystem.goodRadius} km)";
		Add(CreateString(SetColour(goodLabel, goodCol), goodRad), DeliveryResultString(numGood, numTotal));

		string okLabel = isRTL ? ArabicFixer.Fix("تسليم مقبول") : "OK deliveries";
		string okRad = isRTL ? ArabicFixer.Fix($"(< {QuestSystem.okRadius} كم)") : $"(< {QuestSystem.okRadius} km)";
		Add(CreateString(SetColour(okLabel, okCol), okRad), DeliveryResultString(numOk, numTotal));

		string badLabel = isRTL ? ArabicFixer.Fix("تسليم سيئ") : "Bad deliveries";
		Add(SetColour(badLabel, badCol), DeliveryResultString(numBad, numTotal));

		string bestDeliveryString = DistanceString(0);
		string worstDeliveryString = DistanceString(0);
		if (numTotal > 0)
		{
			bestDeliveryString = ResultInfoString(bestResult);
			worstDeliveryString = ResultInfoString(worstResult);
		}
		AddSpace();
		Add(isRTL ? ArabicFixer.Fix("أفضل تسليم") : "Best delivery", bestDeliveryString);
		Add(isRTL ? ArabicFixer.Fix("أسوأ تسليم") : "Worst delivery", worstDeliveryString);

		string averageErrorString = (numTotal == 0) ? DistanceString(0) : DistanceString(totalDistanceError / numTotal);
		Add(isRTL ? ArabicFixer.Fix("متوسط الخطأ") : "Average error", averageErrorString);

		// Score
		if (!questSystem.InEndlessMode)
		{
			AddSpace();
			int score = questSystem.CalculateScore();
			int prevPersonalBest = questSystem.PreviousPersonalBestTimedScore;
			bool isNewBestScore = score > prevPersonalBest;
			string scoreLabel = GameController.IsState(GameState.GameOver)
				? (isRTL ? "النتيجة النهائية" : "Final score")
				: (isRTL ? "النتيجة الحالية" : "Current score");
			string pbLabel = isNewBestScore
				? (isRTL ? "أفضل نتيجة سابقة" : "Previous personal best")
				: (isRTL ? "أفضل نتيجة شخصية" : "Personal best");
			Add(MakeBold(isRTL ? ArabicFixer.Fix(scoreLabel) : scoreLabel), score.ToString());
			Add(MakeBold(isRTL ? ArabicFixer.Fix(pbLabel) : pbLabel), prevPersonalBest.ToString());
		}
	}

	string ResultInfoString(QuestSystem.DeliveryResult result)
	{
		string resultString = DistanceString(result.distanceKM);
		bool isRTL = LocalizationManager.IsRightToLeftWritingSystem;
		string countryName = isRTL
			? LocalizationManager.Localize($"countryCode3.{result.targetCountry.alpha3Code}")
			: result.targetCountry.GetPreferredDisplayName(15);
		string info = isRTL
			? ArabicFixer.Fix($"({result.targetCity.name}، {countryName})")
			: $" ({result.targetCity.name}, {countryName})";
		resultString += FormatExtraInfo(info);
		return resultString;
	}

	string DeliveryResultString(int numInCategory, int total)
	{
		string resultString = numInCategory.ToString();
		if (total > 0)
		{
			int percent = Mathf.RoundToInt(numInCategory / (float)total * 100);
			resultString = CreateString(resultString, $"({percent}%)");
		}
		return resultString;
	}

	string SetColour(string text, Color colour)
	{
		string colHex = ColorUtility.ToHtmlStringRGB(colour);
		return $"<color=#{colHex}>{text}</color>";
	}

	string MakeBold(string text)
	{
		return $"<b>{text}</b>";
	}

	string CreateString(string mainString, string infoString)
	{
		return $"{mainString} {FormatExtraInfo(infoString)}";
	}

	string FormatExtraInfo(string extraInfo)
	{
		string colHex = ColorUtility.ToHtmlStringRGB(extraInfoCol);
		return $"<size={75}%><color=#{colHex}>{extraInfo}</color></size>";
	}

	string AddRichTextToString(string original, string stringToAdd, int sizePercent, Color col)
	{
		string colHex = ColorUtility.ToHtmlStringRGB(col);
		return $"{original}<size={sizePercent}%><color=#{colHex}>{stringToAdd}</color></size>";
	}

	void AddSpace(int sizePercent = 100)
	{
		string lineBreak = $"<line-height={sizePercent}%>\n</line-height>";
		labels.text += lineBreak;
		values.text += lineBreak;
	}

	void Add(string label, string value)
	{
		if (!string.IsNullOrEmpty(labels.text))
		{
			labels.text += "\n";
		}
		if (!string.IsNullOrEmpty(values.text))
		{
			values.text += "\n";
		}

		labels.text += label;
		values.text += value;
	}

	public static string DistanceString(float dstKm)
	{
		if (LocalizationManager.IsRightToLeftWritingSystem)
		{
			return (int)dstKm + " " + ArabicFixer.Fix("كم");
		}
		return (int)dstKm + " km";
	}

	public static string GetTimeString(float time)
	{
		int seconds = (int)(time % 60);
		int minutes = (int)(time / 60) % 60;
		int hours = (int)(time / 60 / 60);

		if (LocalizationManager.IsRightToLeftWritingSystem)
		{
			List<string> parts = new List<string>();
			if (hours > 0)
			{
				string hUnit = (hours == 1) ? "ساعة" : (hours <= 10 ? "ساعات" : "ساعة");
				parts.Add($"{hours} {hUnit}");
			}
			if (minutes > 0 || hours > 0)
			{
				string mUnit = (minutes == 1) ? "دقيقة" : (minutes <= 10 ? "دقائق" : "دقيقة");
				parts.Add($"{minutes} {mUnit}");
			}
			string sUnit = (seconds == 1) ? "ثانية" : (seconds <= 10 ? "ثوانٍ" : "ثانية");
			parts.Add($"{seconds} {sUnit}");

			return ArabicFixer.Fix(string.Join("، ", parts));
		}

		string timeString = "";
		if (hours > 0)
		{
			timeString += hours + ((hours == 1) ? " hour, " : " hours, ");
		}
		if (minutes > 0 || hours > 0)
		{
			timeString += minutes + ((minutes == 1) ? " minute, " : " minutes, ");
		}
		timeString += seconds + ((seconds == 1) ? " second" : " seconds");
		return timeString;
	}
}
