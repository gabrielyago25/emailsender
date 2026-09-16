using EmailSender.Infrastructure.Services;

namespace EmailSender.Infrastructure.Tests;

public class EmailContentSanitizerTests
{
    [Theory]
    [InlineData("<p><br></p>", false)]
    [InlineData("<ol><li> </li></ol>", false)]
    [InlineData("<p>&nbsp;&#8203;&#xfeff;</p>", false)]
    [InlineData("<script>alert(1)</script>", false)]
    [InlineData("<p>Olá</p>", true)]
    [InlineData("<p>😀</p>", true)]
    [InlineData("<p>•</p>", true)]
    [InlineData("<ul><li>Item</li></ul>", true)]
    public void DetectsActualTextAfterSanitizing(string html, bool expected)
    {
        var sanitizer = new EmailContentSanitizer();

        Assert.Equal(expected, sanitizer.PossuiTexto(sanitizer.Sanitizar(html)));
    }
}
