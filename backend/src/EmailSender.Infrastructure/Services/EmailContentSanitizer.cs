using System.Text;
using System.Globalization;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using EmailSender.Core.Interfaces;
using Ganss.Xss;

namespace EmailSender.Infrastructure.Services;

public class EmailContentSanitizer : IEmailContentSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public EmailContentSanitizer()
    {
        _sanitizer = new HtmlSanitizer();

        //Aceitamos somente as tags utilizadas pelo editor.
        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedTags.UnionWith([
            "p",
            "br",
            "strong",
            "b",
            "em",
            "i",
            "u",
            "span",
            "ul",
            "ol",
            "li"
        ]);

        // O Tiptap usa style para tamanho da fonte.

        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.Add("style");

        // Por enquanto, a única propriedade CSS permitida é font-size.

        _sanitizer.AllowedCssProperties.Clear();
        _sanitizer.AllowedCssProperties.Add("font-size");
    }

    public string Sanitizar(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }
        return _sanitizer.Sanitize(html);
    }
    public bool PossuiTexto(string htmlSanitizado)
    {
        var document = new HtmlParser().ParseDocument(htmlSanitizado);
        var texto = document.Body?.TextContent ?? string.Empty;

        // Usa somente texto do documento, sem os marcadores gerados para listas.
        foreach (var rune in texto.EnumerateRunes())
        {
            if (!Rune.IsWhiteSpace(rune) && !Rune.IsControl(rune) &&
                Rune.GetUnicodeCategory(rune) != UnicodeCategory.Format)
            {
                return true;
            }
        }

        return false;
    }

    public string ConverterParaTexto(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        if (document.Body is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        foreach (var node in document.Body.ChildNodes)
        {
            AdicionarTexto(node, builder);
        }
        return builder.ToString().Trim();
    }

    private static void AdicionarTexto(INode node, StringBuilder builder)
    {
        if (node is IText text)
        {
            builder.Append(text.Data);
            return;
        }
        if (node is not IElement element)
        {
            return;
        }
        var tag = element.TagName.ToLowerInvariant();

        if (tag == "br")
        {
            builder.AppendLine();
            return;
        }
        if (tag == "li")
        {
            builder.Append("• ");
        }
        foreach (var child in element.ChildNodes)
        {
            AdicionarTexto(child, builder);
        }
        if (tag is "p" or "li")
        {
            builder.AppendLine();
        }
    }
}
