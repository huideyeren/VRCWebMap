using System.Net;
using System.Text.RegularExpressions;
using VrcWebMap.Backend.Contracts.WebLinks;
using VrcWebMap.Backend.UseCases.WebLinks;

namespace VrcWebMap.Backend.Services;

public sealed class OpenGraphPreviewClient(HttpClient http) : IOpenGraphPreviewProvider
{
    private static readonly Regex MetaPattern = new(
        "<meta\\s+[^>]*?(?:property|name)\\s*=\\s*[\"'](?<key>og:[^\"']+|twitter:[^\"']+)[\"'][^>]*?>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ContentPattern = new(
        "content\\s*=\\s*[\"'](?<content>[^\"']*)[\"']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TitlePattern = new(
        "<title\\s*>\\s*(?<title>.*?)\\s*</title>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public async Task<GetWebLinkPreview.Preview?> TryGetPreviewAsync(Uri url, CancellationToken cancellationToken)
    {
        if (!await IsPublicHttpEndpointAsync(url, cancellationToken))
        {
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("VrcWebMap.Backend/0.1 OGPPreview");
        request.Headers.Accept.ParseAdd("text/html");

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode ||
            response.Content.Headers.ContentType?.MediaType?.Contains("html", StringComparison.OrdinalIgnoreCase) != true)
        {
            return null;
        }

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        if (html.Length > 256_000)
        {
            html = html[..256_000];
        }

        var values = ReadMetaValues(html);
        var title = First(values, "og:title", "twitter:title") ?? ReadTitle(html);
        var description = First(values, "og:description", "twitter:description");
        var siteName = First(values, "og:site_name");
        var image = First(values, "og:image", "twitter:image");
        var imageUrl = TryResolveUrl(url, image);

        return new GetWebLinkPreview.Preview(
            url,
            WebUtility.HtmlDecode(title),
            WebUtility.HtmlDecode(description),
            imageUrl,
            WebUtility.HtmlDecode(siteName));
    }

    private static Dictionary<string, string> ReadMetaValues(string html)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match meta in MetaPattern.Matches(html))
        {
            var content = ContentPattern.Match(meta.Value);
            if (!content.Success)
            {
                continue;
            }

            var key = meta.Groups["key"].Value;
            values.TryAdd(key, content.Groups["content"].Value);
        }

        return values;
    }

    private static string? First(Dictionary<string, string> values, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string? ReadTitle(string html)
    {
        var title = TitlePattern.Match(html);
        return title.Success ? title.Groups["title"].Value.Trim() : null;
    }

    private static Uri? TryResolveUrl(Uri baseUrl, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Uri.TryCreate(baseUrl, value.Trim(), out var url) &&
               url.Scheme is "http" or "https"
            ? url
            : null;
    }

    private static async Task<bool> IsPublicHttpEndpointAsync(Uri url, CancellationToken cancellationToken)
    {
        if (url.Scheme is not ("http" or "https") ||
            string.IsNullOrWhiteSpace(url.Host))
        {
            return false;
        }

        var addresses = await Dns.GetHostAddressesAsync(url.Host, cancellationToken);
        return addresses.Length > 0 && addresses.All(IsPublicAddress);
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) ||
            address.Equals(IPAddress.Any) ||
            address.Equals(IPAddress.IPv6Any) ||
            address.Equals(IPAddress.None) ||
            address.Equals(IPAddress.IPv6None))
        {
            return false;
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return !(address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast);
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] switch
        {
            10 => false,
            127 => false,
            169 when bytes[1] == 254 => false,
            172 when bytes[1] is >= 16 and <= 31 => false,
            192 when bytes[1] == 168 => false,
            _ => true
        };
    }
}
