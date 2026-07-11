public static class JobSettingHolder
{
    public static JobSettingGenerator Current;

    public static void Set(JobSettingGenerator setting)
    {
        Current = setting;

        if (Current == null)
        {
            LogScope.Error("JobSettingHolder: ActionScope is NULL!");
        }
        else
        {
            LogScope.Log($"JobSetting switched to: {Current.name}");
        }
    }
}