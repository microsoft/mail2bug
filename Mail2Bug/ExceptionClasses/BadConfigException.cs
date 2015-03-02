using System;
using System.Text;

namespace Mail2Bug.ExceptionClasses
{
	public class BadConfigException : Exception 
	{
		public BadConfigException(string configSetting)
			: base(BuildMessage(configSetting, null)) {}

		public BadConfigException(string configSetting, string details)
			:base(BuildMessage(configSetting, details)) {}

		private static string BuildMessage(string configSetting, string details)
		{
			var sb = new StringBuilder();
			sb.Append("Bad config setting for '").Append(configSetting).Append("'");

			if(!string.IsNullOrEmpty(details))
			{
				sb.AppendLine().Append(details);
			}

			return sb.ToString();
		}
	}
}
