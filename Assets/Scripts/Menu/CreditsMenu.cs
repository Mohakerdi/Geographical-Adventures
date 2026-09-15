using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GeoGame.Localization;
using GeoGame.Localization.Arabic;

public class CreditsMenu : Menu
{
	public TextAsset contributersFile;
	public TMP_Text text;
	public Color linkCol;
	public Color linkHoverCol;
	public OpenHyperlinks hyperlinkOpener;

	const string youtubeVideoLink = "https://www.youtube.com/watch?v=sLqXFF8mlEU&list=PLFt_AvWsXl0dT82XMtKATYPcVIhpu2fh6&index=1";
	const string githubLink = "https://github.com/SebLague/Geographical-Adventures";
	const string naturalEarthDataLink = "https://www.naturalearthdata.com/downloads/50m-cultural-vectors/";
	const string nasaVisibleEarthLink = "https://visibleearth.nasa.gov/images/73934/topography";
	const string flagsLink = "https://flagpedia.net/";
	const string starDataLink = "https://github.com/astronexus/HYG-Database";
	const string earthObservatoryLink = "https://earthobservatory.nasa.gov/features/NightLights";

	void Start()
	{
		Refresh();
		LocalizationManager.onLanguageChanged += Refresh;
	}

	void OnDestroy()
	{
		LocalizationManager.onLanguageChanged -= Refresh;
	}

	[NaughtyAttributes.Button()]
	void Refresh()
	{
		hyperlinkOpener.hoverColor = linkHoverCol;
		text.text = "";

		bool isRTL = LocalizationManager.IsRightToLeftWritingSystem;
		text.isRightToLeftText = false;
		text.alignment = isRTL ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;

		if (isRTL)
		{
			AddText(SetColour(ArabicFixer.Fix("تم التطوير الأصلي بواسطة سيباستيان لاغي (Sebastian Lague)."), Color.white));
			AddLineBreak();
			AddText(SetColour(ArabicFixer.Fix("التطوير والتحسينات، ودعم الأجهزة المحمولة، والتعريب بواسطة محمد كردي (Mohammad Kerdi)."), new Color(0.15f, 0.95f, 1.0f)));
			AddLineBreak();
			AddText(ArabicFixer.Fix("إذا كنت مهتماً بكيفية صنع هذه اللعبة، يمكنك العثور على سلسلة فيديوهات حول تطويرها على "));
			AddText(CreateHyperlink("YouTube", youtubeVideoLink));
			AddText(ArabicFixer.Fix("، كما أن كود المشروع متاح أيضاً على "));
			AddText(CreateHyperlink("GitHub", githubLink) + ".");
			AddLineBreak(2);

			AddLine(SetColour(ArabicFixer.Fix("صُنعت اللعبة باستخدام بيانات من المصادر التالية:"), Color.white));
			AddLine(CreateHyperlink("Natural Earth Data", naturalEarthDataLink) + " " + ArabicFixer.Fix("بيانات حدود الدول ومواقع المدن."));
			AddLine(CreateHyperlink("NASA Visible Earth", nasaVisibleEarthLink) + " " + ArabicFixer.Fix("خرائط التضاريس وألوان اليابسة."));
			AddLine(CreateHyperlink("NASA Earth Observatory", earthObservatoryLink) + " " + ArabicFixer.Fix("خريطة أضواء المدن الليلية."));
			AddLine(CreateHyperlink("Flagpedia", flagsLink) + " " + ArabicFixer.Fix("صور أعلام الدول."));
			AddLine(CreateHyperlink("Astronexus", starDataLink) + " " + ArabicFixer.Fix("بيانات مواقع النجوم."));
			AddLineBreak();

			AddLine(SetColour(ArabicFixer.Fix("شكر خاص للفنانين على الموسيقى (عبر Artlist و SoundStripe):"), Color.white));
			AddLine("Veaceslav Draganov");
			AddLine("Gray North");
			AddLine("Jan Baars");
			AddLine("The Stolen Orchestra");
			AddLineBreak();

			AddText(SetColour(ArabicFixer.Fix("شكر جزيل لكل من ساهم في إصلاح الأخطاء وإضافة الميزات والترجمات للمشروع على GitHub:"), Color.white));
			AddLineBreak();
		}
		else
		{
			AddText(SetColour("Original Game Created by Sebastian Lague.", Color.white));
			AddLineBreak();
			AddText(SetColour("Enhanced, Mobile & Multiplatform Port by Mohammad Kerdi.", new Color(0.15f, 0.95f, 1.0f)));
			AddLineBreak();
			AddText("If you're interested in how the game was made, you can find a series of videos about its development on ");
			AddText(CreateHyperlink("YouTube", youtubeVideoLink));
			AddText(". The code for this project is also available on " + CreateHyperlink("GitHub", githubLink) + ".");
			AddLineBreak(2);

			AddLine(SetColour("The game was made with data from the following sources:", Color.white));
			AddLine(CreateHyperlink("Natural Earth Data", naturalEarthDataLink) + " country shape data and city locations.");
			AddLine(CreateHyperlink("NASA Visible Earth", nasaVisibleEarthLink) + " topography and land colour maps.");
			AddLine(CreateHyperlink("NASA Earth Observatory", earthObservatoryLink) + " night-time city light map.");
			AddLine(CreateHyperlink("Flagpedia", flagsLink) + " country flag images.");
			AddLine(CreateHyperlink("Astronexus", starDataLink) + " star data.");
			AddLineBreak();

			AddLine(SetColour("Thanks to the following artists for the music (from Artlist and SoundStripe):", Color.white));
			AddLine("Veaceslav Draganov");
			AddLine("Gray North");
			AddLine("Jan Baars");
			AddLine("The Stolen Orchestra");
			AddLineBreak();

			AddText(SetColour("A huge thanks to the following people for contributing various bug fixes, features, and translations to the project on GitHub:", Color.white));
			AddLineBreak();
		}

		string[] contributorNames = contributersFile.text.Split(',');
		for (int i = 0; i < contributorNames.Length; i++)
		{
			bool isLast = i == contributorNames.Length - 1;
			string contributorName = contributorNames[i].Trim();
			AddText(contributorName + (isLast ? "." : ", "));
		}
	}

	string CreateHyperlink(string displayText, string link)
	{
		return SetColour($"<link=\"{link}\">{displayText}</link>", linkCol);
	}

	void AddLine(string textString)
	{
		AddText(textString + "\n");
	}

	void AddText(string textString)
	{
		text.text += textString;
	}

	void AddLineBreak(int num = 1)
	{
		for (int i = 0; i < num; i++)
		{
			text.text += "\n";
		}
	}

	string SetColour(string text, Color colour)
	{
		return $"<color=#{ColorUtility.ToHtmlStringRGB(colour)}>{text}</color>";
	}

	void OnValidate()
	{
		if (!Application.isPlaying)
		{
			Refresh();
		}
	}
}
