using System.Diagnostics.CodeAnalysis;

namespace SelfKeeper;

/// <summary>
/// SelfKeeper的子进程选项
/// </summary>
internal class KeepSelfChildProcessOptions
{
    public KeepSelfFeatureFlag Features { get; set; }

    public int ParentProcessId { get; init; }

    public uint SessionId { get; }

    public KeepSelfChildProcessOptions(uint sessionId)
    {
        SessionId = sessionId;
    }

    public static bool TryParseFromCommandLineArgumentValue(string value, [NotNullWhen(true)] out KeepSelfChildProcessOptions? options)
    {
        options = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var buffer = new byte[(value.Length >> 2) * 3];

        if (!Convert.TryFromBase64String(value, buffer, out var _))
        {
            return false;
        }

        using var ms = new MemoryStream(buffer);
        using var br = new BinaryReader(ms);

        try
        {
            options = new(br.ReadUInt32())
            {
                ParentProcessId = br.ReadInt32(),
                Features = (KeepSelfFeatureFlag)br.ReadInt32(),
            };

            return true;
        }
        catch
        {
            // ignore all exception
            return false;
        }
    }

    public string ToCommandLineArgumentValue()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        bw.Write(SessionId);
        bw.Write(ParentProcessId);
        bw.Write((int)Features);

        return Convert.ToBase64String(ms.ToArray());
    }
}
