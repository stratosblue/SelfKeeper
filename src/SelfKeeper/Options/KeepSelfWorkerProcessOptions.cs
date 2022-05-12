using System.Diagnostics.CodeAnalysis;

namespace SelfKeeper;

/// <summary>
/// SelfKeeper的工作进程选项
/// </summary>
internal class KeepSelfWorkerProcessOptions
{
    public KeepSelfFeatureFlag Features { get; set; }

    public int ParentProcessId { get; init; }

    public uint SessionId { get; }

    public KeepSelfWorkerProcessOptions(uint sessionId)
    {
        SessionId = sessionId;
    }

    public static bool TryParseFromCommandLineArgumentValue(string value, [NotNullWhen(true)] out KeepSelfWorkerProcessOptions? options)
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

        if (br.ReadByte() != 0)
        {
            return false;
        }

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

        //首字节为0，避免错误的解析普通字符串
        bw.Write((byte)0);
        bw.Write(SessionId);
        bw.Write(ParentProcessId);
        bw.Write((int)Features);

        return Convert.ToBase64String(ms.ToArray());
    }
}
