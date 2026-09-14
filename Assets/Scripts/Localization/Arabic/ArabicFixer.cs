using System;
using System.Collections.Generic;
using System.Text;

namespace GeoGame.Localization.Arabic
{
	/// <summary>
	/// Pure C# Arabic text shaper for Unity TextMeshPro.
	/// Converts raw Arabic Unicode letters into contextual presentation forms (isolated, initial, medial, final)
	/// and handles Lam-Alef ligatures, numbers, punctuation, and proper BiDi visual ordering.
	/// </summary>
	public static class ArabicFixer
	{
		private struct GlyphForms
		{
			public char isolated;
			public char final;
			public char initial;
			public char medial;

			public GlyphForms(int isolated, int final, int initial, int medial)
			{
				this.isolated = (char)isolated;
				this.final = (char)final;
				this.initial = (char)initial;
				this.medial = (char)medial;
			}
		}

		private static readonly Dictionary<char, GlyphForms> arabicGlyphs = new Dictionary<char, GlyphForms>()
		{
			{ '\u0621', new GlyphForms(0xFE80, 0xFE80, 0xFE80, 0xFE80) }, // Hamza
			{ '\u0622', new GlyphForms(0xFE81, 0xFE82, 0xFE81, 0xFE82) }, // Alef Madda
			{ '\u0623', new GlyphForms(0xFE83, 0xFE84, 0xFE83, 0xFE84) }, // Alef Hamza Above
			{ '\u0624', new GlyphForms(0xFE85, 0xFE86, 0xFE85, 0xFE86) }, // Waw Hamza
			{ '\u0625', new GlyphForms(0xFE87, 0xFE88, 0xFE87, 0xFE88) }, // Alef Hamza Below
			{ '\u0626', new GlyphForms(0xFE89, 0xFE8A, 0xFE8B, 0xFE8C) }, // Yeh Hamza
			{ '\u0627', new GlyphForms(0xFE8D, 0xFE8E, 0xFE8D, 0xFE8E) }, // Alef
			{ '\u0628', new GlyphForms(0xFE8F, 0xFE90, 0xFE91, 0xFE92) }, // Beh
			{ '\u0629', new GlyphForms(0xFE93, 0xFE94, 0xFE93, 0xFE94) }, // Teh Marbuta
			{ '\u062A', new GlyphForms(0xFE95, 0xFE96, 0xFE97, 0xFE98) }, // Teh
			{ '\u062B', new GlyphForms(0xFE99, 0xFE9A, 0xFE9B, 0xFE9C) }, // Theh
			{ '\u062C', new GlyphForms(0xFE9D, 0xFE9E, 0xFE9F, 0xFEA0) }, // Jeem
			{ '\u062D', new GlyphForms(0xFEA1, 0xFEA2, 0xFEA3, 0xFEA4) }, // Hah
			{ '\u062E', new GlyphForms(0xFEA5, 0xFEA6, 0xFEA7, 0xFEA8) }, // Khah
			{ '\u062F', new GlyphForms(0xFEA9, 0xFEAA, 0xFEA9, 0xFEAA) }, // Dal
			{ '\u0630', new GlyphForms(0xFEAB, 0xFEAC, 0xFEAB, 0xFEAC) }, // Thal
			{ '\u0631', new GlyphForms(0xFEAD, 0xFEAE, 0xFEAD, 0xFEAE) }, // Reh
			{ '\u0632', new GlyphForms(0xFEAF, 0xFEB0, 0xFEAF, 0xFEB0) }, // Zain
			{ '\u0633', new GlyphForms(0xFEB1, 0xFEB2, 0xFEB3, 0xFEB4) }, // Seen
			{ '\u0634', new GlyphForms(0xFEB5, 0xFEB6, 0xFEB7, 0xFEB8) }, // Sheen
			{ '\u0635', new GlyphForms(0xFEB9, 0xFEBA, 0xFEBB, 0xFEBC) }, // Sad
			{ '\u0636', new GlyphForms(0xFEBD, 0xFEBE, 0xFEBF, 0xFEC0) }, // Dad
			{ '\u0637', new GlyphForms(0xFEC1, 0xFEC2, 0xFEC3, 0xFEC4) }, // Tah
			{ '\u0638', new GlyphForms(0xFEC5, 0xFEC6, 0xFEC7, 0xFEC8) }, // Zah
			{ '\u0639', new GlyphForms(0xFEC9, 0xFECA, 0xFECB, 0xFECC) }, // Ain
			{ '\u063A', new GlyphForms(0xFECD, 0xFECE, 0xFECF, 0xFED0) }, // Ghain
			{ '\u0640', new GlyphForms(0x0640, 0x0640, 0x0640, 0x0640) }, // Tatweel
			{ '\u0641', new GlyphForms(0xFED1, 0xFED2, 0xFED3, 0xFED4) }, // Feh
			{ '\u0642', new GlyphForms(0xFED5, 0xFED6, 0xFED7, 0xFED8) }, // Qaf
			{ '\u0643', new GlyphForms(0xFED9, 0xFEDA, 0xFEDB, 0xFEDC) }, // Kaf
			{ '\u0644', new GlyphForms(0xFEDD, 0xFEDE, 0xFEDF, 0xFEE0) }, // Lam
			{ '\u0645', new GlyphForms(0xFEE1, 0xFEE2, 0xFEE3, 0xFEE4) }, // Meem
			{ '\u0646', new GlyphForms(0xFEE5, 0xFEE6, 0xFEE7, 0xFEE8) }, // Noon
			{ '\u0647', new GlyphForms(0xFEE9, 0xFEEA, 0xFEEB, 0xFEEC) }, // Heh
			{ '\u0648', new GlyphForms(0xFEED, 0xFEEE, 0xFEED, 0xFEEE) }, // Waw
			{ '\u0649', new GlyphForms(0xFEEF, 0xFEF0, 0xFBE8, 0xFBE9) }, // Alef Maksura
			{ '\u064A', new GlyphForms(0xFEF1, 0xFEF2, 0xFEF3, 0xFEF4) }, // Yeh
			{ '\u0671', new GlyphForms(0xFB50, 0xFB51, 0xFB50, 0xFB51) }, // Alef Wasla
			{ '\u067E', new GlyphForms(0xFB56, 0xFB57, 0xFB58, 0xFB59) }, // Peh
			{ '\u0686', new GlyphForms(0xFB7A, 0xFB7B, 0xFB7C, 0xFB7D) }, // Tcheh
			{ '\u0698', new GlyphForms(0xFB8A, 0xFB8B, 0xFB8A, 0xFB8B) }, // Jeh
			{ '\u06AF', new GlyphForms(0xFB92, 0xFB93, 0xFB94, 0xFB95) }, // Gaf
		};

		private static readonly HashSet<char> nonConnectingLetters = new HashSet<char>()
		{
			'\u0621', '\u0622', '\u0623', '\u0624', '\u0625', '\u0627',
			'\u0629', '\u062F', '\u0630', '\u0631', '\u0632', '\u0648',
			'\u0649', '\u0671', '\u0698'
		};

		public static bool ContainsArabic(string text)
		{
			if (string.IsNullOrEmpty(text)) return false;
			foreach (char c in text)
			{
				if (c >= 0x0600 && c <= 0x06FF) return true;
				if (c >= 0xFB50 && c <= 0xFDFF) return true;
				if (c >= 0xFE70 && c <= 0xFEFC) return true;
			}
			return false;
		}

		/// <summary>
		/// Fixes an Arabic string by shaping characters and reversing text for LTR rendering.
		/// </summary>
		public static string Fix(string text)
		{
			if (string.IsNullOrEmpty(text) || !ContainsArabic(text))
			{
				return text;
			}

			string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
			for (int l = 0; l < lines.Length; l++)
			{
				lines[l] = FixLine(lines[l]);
			}
			return string.Join("\n", lines);
		}

		private static string FixLine(string line)
		{
			if (string.IsNullOrEmpty(line) || !ContainsArabic(line))
			{
				return line;
			}

			// First, shape the Arabic words and handle Lam-Alef ligatures
			StringBuilder shapedLine = new StringBuilder();
			int i = 0;
			int len = line.Length;

			while (i < len)
			{
				char c = line[i];

				// Check for XML/RichText tags like <color=#FFFFFF> or </color>
				if (c == '<')
				{
					int closeTag = line.IndexOf('>', i);
					if (closeTag != -1)
					{
						shapedLine.Append(line.Substring(i, closeTag - i + 1));
						i = closeTag + 1;
						continue;
					}
				}

				if (arabicGlyphs.ContainsKey(c))
				{
					// Check for Lam-Alef ligature
					if (c == '\u0644' && i + 1 < len)
					{
						char nextC = line[i + 1];
						char ligature = '\0';
						bool prevConnects = (i > 0 && arabicGlyphs.ContainsKey(line[i - 1]) && !nonConnectingLetters.Contains(line[i - 1]));

						if (nextC == '\u0622') ligature = prevConnects ? '\uFEF6' : '\uFEF5';
						else if (nextC == '\u0623') ligature = prevConnects ? '\uFEF8' : '\uFEF7';
						else if (nextC == '\u0625') ligature = prevConnects ? '\uFEFA' : '\uFEF9';
						else if (nextC == '\u0627') ligature = prevConnects ? '\uFEFC' : '\uFEFB';

						if (ligature != '\0')
						{
							shapedLine.Append(ligature);
							i += 2;
							continue;
						}
					}

					bool connectsToPrev = false;
					if (i > 0 && arabicGlyphs.ContainsKey(line[i - 1]) && !nonConnectingLetters.Contains(line[i - 1]))
					{
						connectsToPrev = true;
					}

					bool connectsToNext = false;
					if (!nonConnectingLetters.Contains(c) && i + 1 < len && arabicGlyphs.ContainsKey(line[i + 1]))
					{
						connectsToNext = true;
					}

					GlyphForms forms = arabicGlyphs[c];
					char shapedChar;

					if (connectsToPrev && connectsToNext)
						shapedChar = forms.medial;
					else if (connectsToPrev)
						shapedChar = forms.final;
					else if (connectsToNext)
						shapedChar = forms.initial;
					else
						shapedChar = forms.isolated;

					shapedLine.Append(shapedChar);
					i++;
				}
				else
				{
					shapedLine.Append(c);
					i++;
				}
			}

			// Reverse line for LTR rendering engines while keeping Latin words and numbers in natural direction
			return ReverseForLTR(shapedLine.ToString());
		}

		private static string ReverseForLTR(string text)
		{
			// Segment into tokens: Arabic runs, Number runs, Latin runs, Tags
			List<string> segments = new List<string>();
			int i = 0;
			int len = text.Length;

			while (i < len)
			{
				char c = text[i];

				// RichText tag segment
				if (c == '<')
				{
					int close = text.IndexOf('>', i);
					if (close != -1)
					{
						segments.Add(text.Substring(i, close - i + 1));
						i = close + 1;
						continue;
					}
				}

				// Numbers or Latin
				if (char.IsDigit(c) || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
				{
					int start = i;
					while (i < len && (char.IsLetterOrDigit(text[i]) || text[i] == '.' || text[i] == ':' || text[i] == '-' || text[i] == '%') && !ContainsArabicChar(text[i]) && text[i] != '<')
					{
						i++;
					}
					segments.Add(text.Substring(start, i - start));
					continue;
				}

				// Arabic or other characters (brackets, punctuation, space)
				if (c == '(') { segments.Add(")"); i++; continue; }
				if (c == ')') { segments.Add("("); i++; continue; }
				if (c == '[') { segments.Add("]"); i++; continue; }
				if (c == ']') { segments.Add("["); i++; continue; }
				if (c == '{') { segments.Add("}"); i++; continue; }
				if (c == '}') { segments.Add("{"); i++; continue; }
				if (c == '«') { segments.Add("»"); i++; continue; }
				if (c == '»') { segments.Add("«"); i++; continue; }

				segments.Add(c.ToString());
				i++;
			}

			segments.Reverse();
			StringBuilder sb = new StringBuilder();
			foreach (var seg in segments)
			{
				sb.Append(seg);
			}
			return sb.ToString();
		}

		private static bool ContainsArabicChar(char c)
		{
			return (c >= 0x0600 && c <= 0x06FF) || (c >= 0xFB50 && c <= 0xFDFF) || (c >= 0xFE70 && c <= 0xFEFC);
		}
	}
}
