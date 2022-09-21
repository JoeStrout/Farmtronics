using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Farmtronics.Utils
{
	static class StringUtils {

		public const string _newline = "\n";

		public static string Escape(string txt) {
			string result = txt.Replace("\\", "\\\\")
				.Replace("\n", "\\n")
				.Replace("\r", "\\r")
				.Replace("\t", "\\t");
			return result;
		}

		/// <summary>
		/// Unescape common control sequences in a string.
		/// </summary>
		/// <param name="txt">string that may contain \n, \r, \t, or \\</param>
		/// <returns>string with those things replaced with actual control codes</returns>
		public static string Unescape(string txt) {
			if (string.IsNullOrEmpty(txt)) return txt;
			StringBuilder retval = new StringBuilder(txt.Length);
			for (int ix = 0; ix < txt.Length;) {
				int jx = txt.IndexOf('\\', ix);
				if (jx < 0 || jx == txt.Length - 1) jx = txt.Length;
				retval.Append(txt, ix, jx - ix);
				if (jx >= txt.Length) break;
				switch (txt[jx + 1]) {
				case 'n': retval.Append('\n'); break;  // Line feed
				case 'r': retval.Append('\r'); break;  // Carriage return
				case 't': retval.Append('\t'); break;  // Tab
				case '\\': retval.Append('\\'); break; // Don't escape
				default:                                 // Unrecognized, copy as-is
					retval.Append('\\').Append(txt[jx + 1]); break;
				}
				ix = jx + 2;
			}
			return retval.ToString();
		}

		/// <summary>
		/// Wrap the given text to a given character width.
		/// Adapted from: https://stackoverflow.com/questions/3961278
		/// </summary>
		/// <param name="the_string">text to wrap</param>
		/// <param name="width">maximum width per line</param>
		/// <returns>text with newlines inserted as needed</returns>
		public static string WordWrap(string the_string, int width) {
			int pos, next;
			StringBuilder sb = new StringBuilder();

			// Lucidity check
			if (width < 1) return the_string;
			//Debug.Log("WordWrap(" + the_string + ", " + width + ")");


			// Parse each line of text
			for (pos = 0; pos < the_string.Length; pos = next) {
				// Find end of line
				int eol = the_string.IndexOf(_newline, pos);

				if (eol == -1) next = eol = the_string.Length;
				else next = eol + _newline.Length;
				//Debug.Log("next newline is at " + next);

				// Copy this line of text, breaking into smaller lines as needed
				if (eol > pos) {
					do {
						int len = eol - pos;

						if (len > width) len = BreakLine(the_string, pos, width);
						//Debug.Log("BreakLine returned " + len);
						//Debug.Log("Line is [" + the_string.Substring(pos, len) + "]");

						sb.Append(the_string, pos, len);
						sb.Append(_newline);

						// Skip past line, plus any whitespace following break
						pos += len;
						//Debug.Log("Skipped line to " + pos);
						if (pos < the_string.Length && the_string[pos] == '\n') {
							pos++;  // special case: skip linebreak in exactly the right spot
						} else {
							// normal case: skip trailing whitespace up to (but not including) the line break
							while (pos < eol && char.IsWhiteSpace(the_string[pos])) pos++;
						}
						//Debug.Log("Skipped whitespace to " + pos);
					} while (eol > pos);
				} else sb.Append(_newline); // Empty line
			}

			string result = sb.ToString();
			//Debug.Log("Final result: " + Escape(result));
			return result;
		}

		/// <summary>
		/// Locates position to break the given line so as to avoid
		/// breaking words.
		/// </summary>
		/// <param name="text">String that contains line of text</param>
		/// <param name="pos">Index where line of text starts</param>
		/// <param name="max">Maximum line length</param>
		/// <returns>The modified line length</returns>
		public static int BreakLine(string text, int pos, int max) {
			// Find last whitespace in line
			int i = max;
			while (i >= 0 && !char.IsWhiteSpace(text[pos + i])) i--;
			if (i < 0) return max; // No whitespace found; break at maximum length

			// Find start of whitespace
			while (i >= 0 && char.IsWhiteSpace(text[pos + i])) i--;

			// Return length of text before whitespace
			return i + 1;
		}

		/// <summary>
		/// Simple version of string.Split that's shockingly absent from .NET.
		/// </summary>
		/// <param name="s">string to split</param>
		/// <param name="delimiter">substring to split on</param>
		/// <returns></returns>
		public static string[] Split(this string s, string delimiter) {
			return s.Split(new string[] { delimiter }, System.StringSplitOptions.None);
		}

		/// <summary>
		/// Split into fields by a regular expression.
		/// </summary>
		/// <param name="s">string to split</param>
		/// <param name="delimiter">RegEx pattern matching delimiter</param>
		/// <returns>fields (substrings)</returns>
		public static string[] SplitByRegEx(string s, string delimiter) {
			Regex re = new Regex(delimiter);
			MatchCollection matches = re.Matches(s);
			var result = new string[matches.Count + 1];
			int startPos = 0;
			int i = 0;
			foreach (Match m in matches) {
				result[i] = s.Substring(startPos, m.Index - startPos);
				startPos = m.Index + m.Length;
				i++;
			}
			result[i] = s.Substring(startPos);
			return result;
		}

		/// <summary>
		/// Replace just the first occurrence of a substring.
		/// </summary>
		/// <param name="s">string to search</param>
		/// <param name="search">substring to search for</param>
		/// <param name="replace">what to replace it with</param>
		public static string ReplaceFirst(this string text, string search, string replace) {
			int pos = text.IndexOf(search);
			if (pos < 0) return text;
			return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
		}

		/// <summary>
		/// This is like the standard Substring, but safe: it doesn't throw
		/// exceptions if you ask for more than is available (it just returns
		/// a shorter string).
		/// </summary>
		public static string Mid(this string s, int start, int length = -1) {
			if (length == -1 || start + length > s.Length) length = s.Length - start;
			if (start >= s.Length) return null;
			return s.Substring(start, length);
		}

		public static string Left(this string s, int count) {
			if (count >= s.Length) return s;
			return s.Substring(0, count);
		}

		public static string Right(this string s, int count) {
			if (count >= s.Length) return s;
			return s.Substring(s.Length - count);
		}

		// Return the value of the integer at the start of this string
		// (after any whitespace, and before any non-digit characters).
		// You can think of this as "int.TryHarderParse".  
		// But call it as s.ToInt().
		public static int ToInt(this string s) {
			if (s == null) return 0;
			int val = 0;
			int i = 0;
			while (i < s.Length && s[i] <= ' ') i++;
			while (i < s.Length && s[i] >= '0' && s[i] <= '9') {
				val = val * 10 + (s[i] - '0');
				i++;
			}
			return val;
		}

		// A better version of String.Replace that takes an option to
		// allow it to be case insensitive (using, for example,
		// System.StringComparison.InvariantCultureIgnoreCase).
		// Reference: https://stackoverflow.com/questions/244531/
		public static string Replace(this string str, string oldValue, string newValue, StringComparison comparison) {
			if (oldValue == null) throw new ArgumentNullException("oldValue");
			if (oldValue == "") throw new ArgumentException("String cannot be of zero length.", "oldValue");

			int index = str.IndexOf(oldValue, comparison);
			if (index < 0) return str;

			StringBuilder sb = new StringBuilder();

			int previousIndex = 0;
			while (index != -1) {
				sb.Append(str.Substring(previousIndex, index - previousIndex));
				sb.Append(newValue);
				index += oldValue.Length;

				previousIndex = index;
				index = str.IndexOf(oldValue, index, comparison);
			}
			sb.Append(str.Substring(previousIndex));

			return sb.ToString();
		}

		/*
		public static void RunUnitTests() {
			string s = "Foo bar baz";
			UnitTests.AssertEqual(s.Split(" "), new string[]{"Foo", "bar", "baz"});
			UnitTests.AssertEqual(SplitByRegEx(s, "\\s"), new string[]{"Foo", "bar", "baz"});
			UnitTests.AssertEqual(SplitByRegEx(s, "[aeiou]+"), new string[]{"F", " b", "r b", "z"});
			UnitTests.AssertEqual(s.Left(3), "Foo");
			UnitTests.AssertEqual(s.Right(3), "baz");
			UnitTests.AssertEqual(" 42foo".ToInt(), 42);		
			UnitTests.AssertEqual(s.Replace("BAR", "xxx", StringComparison.InvariantCultureIgnoreCase), "Foo xxx baz");
		}
		*/
	}	
}
