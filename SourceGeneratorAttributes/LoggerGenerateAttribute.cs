namespace SourceGeneratorAttributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class LoggerGenerateAttribute : Attribute
    {
        public string[] LogEvenLevels { get; }
        public int GenericOverrideCount { get; }

        public LoggerGenerateAttribute(int genericOverrideCount, string logEventLevels)
        {
            if (string.IsNullOrWhiteSpace(logEventLevels)) throw new ArgumentException("Must not be empty", nameof(logEventLevels));

            LogEvenLevels = logEventLevels.Split(',').Select(l => l.Trim()).ToArray();
            GenericOverrideCount = Math.Max(0, genericOverrideCount);
        }
    }
}
