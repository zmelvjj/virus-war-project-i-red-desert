public static class SpecialStageAddressKeys
{
    public const string GroupName = "SpecialStageDefinitions";
    public const string Label = "SpecialStageData";

    public static string GetAddress(string specialStageId)
    {
        return $"SpecialStage_{specialStageId}";
    }
}
