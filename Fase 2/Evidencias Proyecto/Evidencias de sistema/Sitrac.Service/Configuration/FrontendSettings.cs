namespace Sitrac.Service.Configuration
{
    public class FrontendSettings
    {
        public const string SectionName = "FrontendSettings";
        public string BaseUrl { get; set; } = "http://127.0.0.1:5500";
    }
}