using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using VrcWebMap.Backend.Contracts.Portal;

namespace VrcWebMap.Backend.UseCases.Portal;

/// <summary>
/// ベンダー拡張を失わないようJsonNode上でWorldData.jsonを合成します。
/// </summary>
public static class WorldDataJsonMerger
{
    private static readonly JsonSerializerOptions OutputJsonOptions = new()
    {
        WriteIndented = true,
        // WPPLSへ渡すUTF-8 JSONを人が読める状態にしつつ、JSONとして必要な文字はencoderに保護させます。
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public static MergeResult Merge(
        string existingJson,
        GetWorldData.Response systemData)
    {
        try
        {
            if (JsonNode.Parse(existingJson) is not JsonObject root)
            {
                return MergeResult.Failure("WorldData.jsonのルートはオブジェクトにしてください。");
            }

            if (root["Categorys"] is not JsonArray existingCategorys)
            {
                return MergeResult.Failure("Categorysは配列にしてください。");
            }

            if (root.TryGetPropertyValue("Roles", out var existingRolesNode) &&
                existingRolesNode is not JsonArray)
            {
                return MergeResult.Failure("Rolesは配列にしてください。");
            }

            var systemRoot = JsonSerializer.SerializeToNode(systemData)!.AsObject();
            var systemCategorys = systemRoot["Categorys"]!.AsArray();
            var categoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var categoryNode in existingCategorys)
            {
                var error = AddCategoryName(categoryNode, categoryNames);
                if (error is not null)
                {
                    return MergeResult.Failure(error);
                }
            }

            foreach (var categoryNode in systemCategorys)
            {
                var error = AddCategoryName(categoryNode, categoryNames);
                if (error is not null)
                {
                    return MergeResult.Failure(error);
                }

                existingCategorys.Add(categoryNode!.DeepClone());
            }

            var rolesResult = MergeRoles(root, systemRoot);
            if (rolesResult is not null)
            {
                return MergeResult.Failure(rolesResult);
            }

            // WPPLS側で非公開releaseのワールドも扱える方針は、マージ元に左右されません。
            root["ShowPrivateWorld"] = true;
            return MergeResult.Success(
                root.ToJsonString(OutputJsonOptions));
        }
        catch (JsonException)
        {
            return MergeResult.Failure("WorldData.jsonをJSONとして読み取れません。");
        }
    }

    private static string? AddCategoryName(
        JsonNode? categoryNode,
        HashSet<string> categoryNames)
    {
        if (categoryNode is not JsonObject category ||
            !TryGetRequiredString(category, "Category", out var name) ||
            category["Worlds"] is not JsonArray)
        {
            return "Categorysの各要素にはCategory文字列とWorlds配列が必要です。";
        }

        if (!categoryNames.Add(name.Trim()))
        {
            return $"カテゴリ名「{name.Trim()}」が重複しています。";
        }

        return null;
    }

    private static string? MergeRoles(
        JsonObject root,
        JsonObject systemRoot)
    {
        var existingRoles = root["Roles"] as JsonArray;
        var systemRoles = systemRoot["Roles"] as JsonArray;
        var parsedExisting = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        if (existingRoles is not null)
        {
            foreach (var roleNode in existingRoles)
            {
                if (!TryReadRole(roleNode, out var roleName, out var displayNames))
                {
                    return "Rolesの各要素にはRoleName文字列とDisplayNames文字列配列が必要です。";
                }

                if (parsedExisting.TryGetValue(roleName, out var duplicate) &&
                    !duplicate.SetEquals(displayNames))
                {
                    return $"ロール「{roleName}」の定義が競合しています。";
                }

                parsedExisting[roleName] = displayNames;
            }
        }

        if (systemRoles is null || systemRoles.Count == 0)
        {
            return null;
        }

        existingRoles ??= [];
        if (root["Roles"] is null)
        {
            root["Roles"] = existingRoles;
        }

        foreach (var roleNode in systemRoles)
        {
            if (!TryReadRole(roleNode, out var roleName, out var displayNames))
            {
                return "システムRoleの形式が不正です。";
            }

            if (parsedExisting.TryGetValue(roleName, out var existing))
            {
                if (!existing.SetEquals(displayNames))
                {
                    return $"ロール「{roleName}」の定義が競合しています。";
                }

                continue;
            }

            existingRoles.Add(roleNode!.DeepClone());
            parsedExisting[roleName] = displayNames;
        }

        return null;
    }

    private static bool TryReadRole(
        JsonNode? roleNode,
        out string roleName,
        out HashSet<string> displayNames)
    {
        roleName = string.Empty;
        displayNames = new HashSet<string>(StringComparer.Ordinal);
        if (roleNode is not JsonObject role ||
            !TryGetRequiredString(role, "RoleName", out var rawRoleName) ||
            role["DisplayNames"] is not JsonArray names)
        {
            return false;
        }

        foreach (var nameNode in names)
        {
            if (nameNode is not JsonValue value ||
                !value.TryGetValue<string>(out var name))
            {
                return false;
            }

            displayNames.Add(name);
        }

        roleName = rawRoleName.Trim();
        return true;
    }

    private static bool TryGetRequiredString(
        JsonObject value,
        string propertyName,
        out string result)
    {
        result = string.Empty;
        if (value[propertyName] is not JsonValue node ||
            !node.TryGetValue<string>(out var candidate) ||
            string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        result = candidate;
        return true;
    }
}

/// <summary>
/// 純粋マージ処理の成功JSONまたは安全な検証メッセージです。
/// </summary>
public sealed record MergeResult(string? Json, string? ErrorMessage)
{
    public bool IsSuccess => ErrorMessage is null;

    public static MergeResult Success(string json) => new(json, null);

    public static MergeResult Failure(string message) => new(null, message);
}
