namespace Toimik.WarcProtocol.Tests;

using System;
using Xunit;

public class UtilsTest
{
    [Fact]
    public void AddBracketsToUriWithUriThatIsNull()
    {
        Uri? uri = null;
        var expectedUri = Utils.AddBracketsToUri(uri);

        Assert.Null(expectedUri);
    }
}