using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace VrcWebMap.Backend.Serialization;

/// <summary>
/// アプリケーションの XML documentation comment を OpenAPI schema description へ反映します。
/// </summary>
public sealed partial class AppOpenApiXmlCommentsSchemaTransformer : IOpenApiSchemaTransformer
{
    private static readonly Lazy<IReadOnlyDictionary<string, string>> Summaries = new(LoadSummaries);

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(schema.Description))
        {
            return Task.CompletedTask;
        }

        var memberName = context.JsonPropertyInfo?.AttributeProvider is PropertyInfo property
            ? GetPropertyMemberName(property)
            : GetTypeMemberName(context.JsonTypeInfo.Type);

        if (memberName is not null && Summaries.Value.TryGetValue(memberName, out var summary))
        {
            schema.Description = summary;
        }

        return Task.CompletedTask;
    }

    private static IReadOnlyDictionary<string, string> LoadSummaries()
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, "VrcWebMap.Backend.xml");
        if (!File.Exists(xmlPath))
        {
            return new Dictionary<string, string>();
        }

        var document = XDocument.Load(xmlPath);
        return document
            .Descendants("member")
            .Select(member => new
            {
                Name = member.Attribute("name")?.Value,
                Summary = NormalizeSummary(member.Element("summary")?.Value)
            })
            .Where(member => !string.IsNullOrWhiteSpace(member.Name) && !string.IsNullOrWhiteSpace(member.Summary))
            .ToDictionary(member => member.Name!, member => member.Summary!);
    }

    private static string? GetTypeMemberName(Type type)
    {
        if (type.FullName is null)
        {
            return null;
        }

        return $"T:{type.FullName.Replace('+', '.')}";
    }

    private static string? GetPropertyMemberName(PropertyInfo property)
    {
        var declaringTypeName = property.DeclaringType?.FullName;
        if (declaringTypeName is null)
        {
            return null;
        }

        return $"P:{declaringTypeName.Replace('+', '.')}.{property.Name}";
    }

    private static string? NormalizeSummary(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return WhitespaceRegex().Replace(value.Trim(), " ");
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
