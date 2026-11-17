using UniversityPortal.API.Integrations.BusinessCentral;

namespace UniversityPortal.API.Tests;

public class BcValueTests
{
    [Theory]
    [InlineData("In_x0020_Review", "In Review")]
    [InlineData("Non_x002D_Inventory", "Non-Inventory")]
    [InlineData("_x0020_", " ")]
    [InlineData("Open", "Open")]
    public void Decodes_encoded_characters(string input, string expected) =>
        Assert.Equal(expected, BcValue.Decode(input));
}
