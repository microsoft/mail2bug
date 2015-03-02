namespace Mail2Bug.Helpers
{
    public class TextUtils
	{
		// Valid characters are: 0x09 | 0x0A | 0x0D | [0x20-0xFFFD]
		public static string FixLineBreaks(string s)
		{
//			for (int i = 0; i < s.Length; i++)
//			{
//				char c = s[i];
//				if (c < 0x20 && c != 0x09 && c != 0x0A && c != 0x0D)
//				{
//					s[i] = ' ';
//				}
//			}
			return s.Replace("\r\n", "\n").Replace("\n\n", "\n");
		}
    }
}
