using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeoGame.Localization
{
	public class StringLocalizer : MonoBehaviour
	{
		public string id;
		public TMPro.TMP_Text textElement;

		public string currentValue { get; private set; }
		public bool controlRectTransformWidth;
		public float padding = 50;

		void Start()
		{
			Localize();
			LocalizationManager.onLanguageChanged += Localize;
		}

		private TMPro.TMP_FontAsset originalFont;

		void Localize()
		{
			if (textElement == null) return;
			if (originalFont == null) originalFont = textElement.font;

			currentValue = LocalizationManager.Localize(id);
			textElement.text = currentValue;
			textElement.isRightToLeftText = false;

			if (LocalizationManager.IsRightToLeftWritingSystem && Arabic.ArabicFontManager.ArabicFontAsset != null)
			{
				textElement.font = Arabic.ArabicFontManager.ArabicFontAsset;
			}
			else if (originalFont != null)
			{
				textElement.font = originalFont;
			}

			if (controlRectTransformWidth)
			{
				textElement.ForceMeshUpdate();
				RectTransform rectTransform = GetComponent<RectTransform>();
				if (rectTransform != null)
				{
					GetComponent<RectTransform>().sizeDelta = new Vector2(textElement.bounds.size.x + padding, rectTransform.sizeDelta.y);
				}
			}
		}

#if UNITY_EDITOR
		void OnValidate()
		{
			if (textElement == null)
			{
				textElement = GetComponent<TMPro.TMP_Text>();
				if (textElement == null)
				{
					textElement = GetComponentInChildren<TMPro.TMP_Text>();
				}
			}
		}
#endif
	}
}