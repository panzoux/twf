namespace TWF.Models
{
    /// <summary>
    /// Represents a custom user-defined function with macro support
    /// </summary>
    public class CustomFunction
    {
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Configuration for custom functions loaded from JSON
    /// </summary>
    public class CustomFunctionsConfig
    {
        public string Version { get; set; } = "1.0";
        public List<CustomFunction> Functions { get; set; } = new List<CustomFunction>();
    }
}
