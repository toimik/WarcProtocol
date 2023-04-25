namespace Toimik.WarcProtocol.Tests;

using System;
using Xunit;

public class UtilsTest
{
    [Fact]
    public void AddBracketsToUriWithUriThatIsNull()
    {
        Uri? uri = null;
        var actualUri = Utils.AddBracketsToUri(uri);

        Assert.Null(actualUri);
    }
}