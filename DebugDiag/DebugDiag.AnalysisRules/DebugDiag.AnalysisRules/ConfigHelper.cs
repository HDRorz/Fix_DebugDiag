using System.Configuration;
using System.IO;
using System.Reflection;

namespace DebugDiag.AnalysisRules;

public class ConfigHelper
{
	private static bool configLoadAttempted;

	private static KeyValueConfigurationCollection appSettings;

	public static string GetSetting(string name)
	{
		string text = ConfigurationManager.AppSettings[name];
		if (text == null)
		{
			if (!configLoadAttempted)
			{
				configLoadAttempted = true;
				string text2 = Assembly.GetExecutingAssembly().Location + ".config";
				if (File.Exists(text2))
				{
					appSettings = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap
					{
						ExeConfigFilename = text2
					}, ConfigurationUserLevel.None).AppSettings.Settings;
				}
				else
				{
					appSettings = null;
				}
			}
			if (appSettings != null)
			{
				KeyValueConfigurationElement keyValueConfigurationElement = appSettings[name];
				if (keyValueConfigurationElement != null)
				{
					return keyValueConfigurationElement.Value;
				}
			}
		}
		return text;
	}
}
