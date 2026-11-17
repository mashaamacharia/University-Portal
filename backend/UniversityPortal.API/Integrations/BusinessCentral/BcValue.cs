using System.Text.RegularExpressions;

namespace UniversityPortal.API.Integrations.BusinessCentral;

public static partial class BcValue
{
    public static string Decode(string value) =>
        EncodedChar().Replace(
            value,
            match => ((char)Convert.ToInt32(match.Groups[1].Value, 16)).ToString());

    [GeneratedRegex("_x([0-9A-Fa-f]{4})_")]
    private static partial Regex EncodedChar();
}
