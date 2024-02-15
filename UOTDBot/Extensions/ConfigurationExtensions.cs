using Microsoft.Extensions.Configuration;

namespace UOTDBot.Extensions;

internal static class ConfigurationExtensions
{
    public static string GetRequiredValue(this IConfiguration configuration, string key)
    {
        var value = configuration[key];

        if (string.IsNullOrEmpty(value))
        {
            throw new Exception($"{key} is required");
        }

        return value;
    }
}
