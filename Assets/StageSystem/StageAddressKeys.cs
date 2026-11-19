public static class StageAddressKeys
{
    public const string GroupName = "StageDefinitions";
    public const string Label = "StageData";

    public static string GetAddress(int stageIndex)
    {
        return $"Stage_{stageIndex}";
    }
}
