using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace GeoGame.Localization.Arabic
{
	/// <summary>
	/// Manages the game-wide Arabic gaming font (Cairo-Bold), ensuring all Arabic
	/// text across menus, HUD, country tooltips, and dialogs renders with crisp
	/// neo-grotesque gaming typography, with NotoSansArabic as resilient secondary fallback.
	/// </summary>
	public static class ArabicFontManager
	{
		private static TMP_FontAsset arabicFontAsset;
		private static TMP_FontAsset notoFontAsset;
		private static bool initialized = false;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void Initialize()
		{
			if (initialized && arabicFontAsset != null) return;

			Font cairoTTF = Resources.Load<Font>("Fonts/Cairo-Bold");
			if (cairoTTF == null) cairoTTF = Resources.Load<Font>("Fonts/Cairo-Black");

			Font notoTTF = Resources.Load<Font>("Fonts/NotoSansArabic-Bold");

			Font primaryTTF = cairoTTF != null ? cairoTTF : notoTTF;
			if (primaryTTF == null)
			{
				Debug.LogWarning("ArabicFontManager: Could not load Cairo or Arabic font from Resources.");
				return;
			}

			// Create primary dynamic TMP font asset with gaming-oriented SDFAA rendering
			arabicFontAsset = TMP_FontAsset.CreateFontAsset(primaryTTF, 44, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
			if (arabicFontAsset != null)
			{
				arabicFontAsset.name = "Cairo-Bold SDF Dynamic";

				// Secondary fallback (Noto Sans) in case any exotic glyph is requested
				if (notoTTF != null && notoTTF != primaryTTF)
				{
					notoFontAsset = TMP_FontAsset.CreateFontAsset(notoTTF, 44, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 512, 512);
					if (notoFontAsset != null)
					{
						notoFontAsset.name = "NotoSansArabic-Bold SDF Fallback";
						AddToFallback(arabicFontAsset, notoFontAsset);
					}
				}

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
				if (notoFontAsset != null) AddToFallback(TMP_Settings.defaultFontAsset, notoFontAsset);
				AddToFallback(TMP_Settings.defaultFontAsset, arabicFontAsset);
			}

			TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
			foreach (var f in allFonts)
			{
				if (f != arabicFontAsset && f != notoFontAsset)
				{
					if (notoFontAsset != null) AddToFallback(f, notoFontAsset);
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
