using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace GeoGame.Localization.Arabic
{
	public static class ArabicFontManager
	{
		private static TMP_FontAsset arabicFontAsset;
		private static bool initialized = false;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void Initialize()
		{
			if (initialized) return;

			Font ttf = Resources.Load<Font>("Fonts/NotoSansArabic-Bold");
			if (ttf == null)
			{
				Debug.LogWarning("ArabicFontManager: Could not load Fonts/NotoSansArabic-Bold from Resources.");
				return;
			}

			// Create dynamic TMP font asset with multi-character support
			arabicFontAsset = TMP_FontAsset.CreateFontAsset(ttf, 36, 4, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 512, 512);
			if (arabicFontAsset != null)
			{
				arabicFontAsset.name = "NotoSansArabic-Bold SDF Dynamic";

				// Register in TMP Settings default font fallback
				if (TMP_Settings.defaultFontAsset != null)
				{
					AddToFallback(TMP_Settings.defaultFontAsset, arabicFontAsset);
				}

				// Also check all loaded font assets
				TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
				foreach (var f in allFonts)
				{
					if (f != arabicFontAsset)
					{
						AddToFallback(f, arabicFontAsset);
					}
				}

				initialized = true;
				Debug.Log("ArabicFontManager: Successfully initialized Arabic dynamic font asset as TMP fallback.");
			}
		}

		private static void AddToFallback(TMP_FontAsset target, TMP_FontAsset fallback)
		{
			if (target == null || fallback == null) return;
			if (target.fallbackFontAssetTable == null)
			{
				target.fallbackFontAssetTable = new List<TMP_FontAsset>();
			}
			if (!target.fallbackFontAssetTable.Contains(fallback))
			{
				target.fallbackFontAssetTable.Add(fallback);
			}
		}

		public static TMP_FontAsset ArabicFontAsset => arabicFontAsset;
	}
}
