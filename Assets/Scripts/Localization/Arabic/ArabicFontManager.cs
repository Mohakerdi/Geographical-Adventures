using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace GeoGame.Localization.Arabic
{
	/// <summary>
	/// Manages the game-wide Arabic gaming font (Cairo-Bold), ensuring all Arabic
	/// text across menus, HUD, country tooltips, and dialogs renders with crisp
	/// neo-grotesque gaming typography.
	/// </summary>
	public static class ArabicFontManager
	{
		private static TMP_FontAsset arabicFontAsset;
		private static bool initialized = false;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void Initialize()
		{
			if (initialized && arabicFontAsset != null) return;

			Font ttf = Resources.Load<Font>("Fonts/Cairo-Bold");
			if (ttf == null) ttf = Resources.Load<Font>("Fonts/Cairo-Black");
			if (ttf == null) ttf = Resources.Load<Font>("Fonts/NotoSansArabic-Bold");

			if (ttf == null)
			{
				Debug.LogWarning("ArabicFontManager: Could not load Cairo or Arabic font from Resources.");
				return;
			}

			// Create dynamic TMP font asset with gaming-oriented SDFAA rendering
			arabicFontAsset = TMP_FontAsset.CreateFontAsset(ttf, 44, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
			if (arabicFontAsset != null)
			{
				arabicFontAsset.name = "Cairo-Bold SDF Dynamic";

				RegisterFallbacks();

				SceneManager.sceneLoaded += (scene, mode) =>
				{
					RegisterFallbacks();
					if (LocalizationManager.IsRightToLeftWritingSystem)
					{
						ApplyFontToAllActiveText();
					}
				};

				LocalizationManager.onLanguageChanged += () =>
				{
					if (LocalizationManager.IsRightToLeftWritingSystem)
					{
						ApplyFontToAllActiveText();
					}
				};

				initialized = true;
				Debug.Log("ArabicFontManager: Successfully initialized Cairo gaming font asset as TMP fallback.");
			}
		}

		public static void RegisterFallbacks()
		{
			if (arabicFontAsset == null) return;

			if (TMP_Settings.defaultFontAsset != null)
			{
				AddToFallback(TMP_Settings.defaultFontAsset, arabicFontAsset);
			}

			TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
			foreach (var f in allFonts)
			{
				if (f != arabicFontAsset)
				{
					AddToFallback(f, arabicFontAsset);
				}
			}
		}

		private static void AddToFallback(TMP_FontAsset target, TMP_FontAsset fallback)
		{
			if (target == null || fallback == null) return;
			if (target.fallbackFontAssetTable == null)
			{
				target.fallbackFontAssetTable = new List<TMP_FontAsset>();
			}
			target.fallbackFontAssetTable.Remove(fallback);
			target.fallbackFontAssetTable.Insert(0, fallback);
		}

		public static void ApplyFontToAllActiveText()
		{
			if (arabicFontAsset == null) return;
			TMP_Text[] texts = Object.FindObjectsOfType<TMP_Text>(includeInactive: true);
			foreach (var txt in texts)
			{
				if (txt != null && (LocalizationManager.IsRightToLeftWritingSystem || ArabicFixer.ContainsArabic(txt.text)))
				{
					txt.font = arabicFontAsset;
				}
			}
		}

		public static TMP_FontAsset ArabicFontAsset => arabicFontAsset;
	}
}
