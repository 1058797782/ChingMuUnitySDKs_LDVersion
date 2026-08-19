using ChingMU;

internal static class ChingMuAddress
{
    internal static string Build(string serverAddress, int port)
    {
        string value = string.IsNullOrWhiteSpace(serverAddress)
            ? string.Empty
            : serverAddress.Trim();

        if (value.Length == 0 || port <= 0)
        {
            return value;
        }

        int atIndex = value.LastIndexOf('@');
        int colonIndex = value.LastIndexOf(':');
        if (colonIndex > atIndex && colonIndex >= 0)
        {
            int existingPort;
            if (int.TryParse(value.Substring(colonIndex + 1), out existingPort))
            {
                return value.Substring(0, colonIndex + 1) + port;
            }
        }

        return value + ":" + port;
    }

    internal static string Host(string serverAddress)
    {
        if (string.IsNullOrWhiteSpace(serverAddress))
        {
            return string.Empty;
        }

        string value = serverAddress.Trim();
        int atIndex = value.LastIndexOf('@');
        int startIndex = atIndex >= 0 ? atIndex + 1 : 0;
        int colonIndex = value.LastIndexOf(':');
        int length = colonIndex > startIndex ? colonIndex - startIndex : value.Length - startIndex;
        return length > 0 ? value.Substring(startIndex, length) : string.Empty;
    }

    internal static CMPluginAPI.CMServerType ServerType(string serverAddress)
    {
        return !string.IsNullOrEmpty(serverAddress) &&
               serverAddress.StartsWith("MCAvatar@", System.StringComparison.OrdinalIgnoreCase)
            ? CMPluginAPI.CMServerType.MCAvatar
            : CMPluginAPI.CMServerType.MCServer;
    }

    internal static string ApplyConfiguredHost(string currentAddress, string configuredAddress)
    {
        if (string.IsNullOrWhiteSpace(configuredAddress))
        {
            return currentAddress ?? string.Empty;
        }

        string configured = configuredAddress.Trim();
        if (configured.IndexOf('@') >= 0)
        {
            return configured;
        }

        if (!string.IsNullOrEmpty(currentAddress))
        {
            int atIndex = currentAddress.IndexOf('@');
            if (atIndex >= 0)
            {
                return currentAddress.Substring(0, atIndex + 1) + configured;
            }
        }

        return configured;
    }
}
