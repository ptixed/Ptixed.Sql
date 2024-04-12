using Microsoft.Extensions.Configuration;

namespace Ptixed.Sql.Tests
{
    internal static class AppSettings
    {
        public static readonly IConfigurationRoot Instance = new ConfigurationBuilder()
           .AddJsonFile("appsettings.json")
           .Build();
    }
}
