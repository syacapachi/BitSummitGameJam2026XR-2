public static class JobSettingHolder
{
    public static JobSetting Current;

    public static void Set(JobSetting setting)
    {
        Current = setting;

        if (Current == null)
        {
            UnityEngine.Debug.LogError("JobSettingHolder: Current is NULL!");
        }
        else
        {
            UnityEngine.Debug.Log($"JobSetting switched to: {Current.name}");
        }
    }
}