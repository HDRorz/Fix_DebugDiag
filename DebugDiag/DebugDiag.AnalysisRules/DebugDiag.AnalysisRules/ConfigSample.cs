namespace DebugDiag.AnalysisRules;

internal class ConfigSample
{
	private static int sampleInt;

	private static string sampleString;

	static ConfigSample()
	{
		LoadConfig();
	}

	private static void LoadConfig()
	{
		sampleString = ConfigHelper.GetSetting("sampleString") ?? "This is the default value when not speicified in config";
		if (!int.TryParse(ConfigHelper.GetSetting("sampleInt"), out sampleInt))
		{
			sampleInt = 123;
		}
	}
}
