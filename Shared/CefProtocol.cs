using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Huichuan.Protocol;

/// <summary>
/// MainClient 与 CefClient 之间的兼容协议。消息名和 JSON 字段名属于稳定接口，
/// 两端通过链接同一个源文件避免协议漂移。
/// </summary>
public static class CefProtocol
{
    public const int CopyDataId = 100;

    public static class Messages
    {
        public const string Register = "REG";
        public const string Load = "LOAD";
        public const string Stop = "STOP";
        public const string Show = "SHOW";
        public const string Hide = "HIDE";
        public const string TaskCount = "OnTaskCountHandler";
        public const string TaskDsp = "OnTaskDspHandler";
        public const string TaskLog = "OnTaskLogHandler";
    }

    public static JObject Create(string message, JToken? data = null, string? clientId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var envelope = new JObject { ["Msg"] = message };
        if (clientId is not null)
            envelope["ClientId"] = clientId;
        if (data is not null)
            envelope["Data"] = data;
        return envelope;
    }

    public static string Serialize(string message, JToken? data = null, string? clientId = null) =>
        Create(message, data, clientId).ToString(Formatting.None);

    public static bool TryParse(string? json, out JObject envelope, out string message)
    {
        envelope = null!;
        message = string.Empty;
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            envelope = JObject.Parse(json);
            message = envelope.Value<string>("Msg") ?? string.Empty;
            return message.Length != 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static string? GetArgument(IEnumerable<string> arguments, string name)
    {
        var prefix = name + "=";
        return arguments.FirstOrDefault(value =>
                value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            ?[prefix.Length..];
    }
}
