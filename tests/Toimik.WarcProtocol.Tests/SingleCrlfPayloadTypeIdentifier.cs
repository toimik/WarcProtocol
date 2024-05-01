namespace Toimik.WarcProtocol.Tests;

using System.Text;

public class SingleCrlfPayloadTypeIdentifier : PayloadTypeIdentifier
{
    public const string PayloadType = "foobar";

    public static readonly int[] GeminiDelimiter =
    [
        WarcParser.CarriageReturn,
        WarcParser.LineFeed,
    ];

    public SingleCrlfPayloadTypeIdentifier()
        : base(GeminiDelimiter)
    {
    }

    public static string CreateDelimiterText(int[] delimiter)
    {
        var builder = new StringBuilder();
        foreach (int character in delimiter)
        {
            builder.Append((char)character);
        }

        var text = builder.ToString();
        return text;
    }

    public override string? Identify(byte[] payload) => PayloadType;
}