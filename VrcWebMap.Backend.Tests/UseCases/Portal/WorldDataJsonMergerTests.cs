using System.Text.Json.Nodes;
using VrcWebMap.Backend.Contracts.Portal;
using VrcWebMap.Backend.UseCases.Portal;

namespace VrcWebMap.Backend.Tests.UseCases.Portal;

public sealed class WorldDataJsonMergerTests
{
    [Fact]
    public void Merge_PreservesUnknownPropertiesAndForcesPrivateVisibility()
    {
        const string source = """
            {
              "ReverseCategorys": true,
              "ShowPrivateWorld": false,
              "VendorExtension": {"Keep": true},
              "Categorys": [{
                "Category": "既存",
                "Worlds": [],
                "UnknownCategoryValue": 123
              }]
            }
            """;

        var result = WorldDataJsonMerger.Merge(source, SystemData("追加"));

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var root = JsonNode.Parse(result.Json!)!.AsObject();
        Assert.True(root["ReverseCategorys"]!.GetValue<bool>());
        Assert.True(root["ShowPrivateWorld"]!.GetValue<bool>());
        Assert.True(root["VendorExtension"]!["Keep"]!.GetValue<bool>());
        Assert.Equal(123, root["Categorys"]![0]!["UnknownCategoryValue"]!.GetValue<int>());
        Assert.Equal("追加", root["Categorys"]![1]!["Category"]!.GetValue<string>());
    }

    [Fact]
    public void Merge_AppendsRolesAndReusesIdenticalExistingRole()
    {
        const string source = """
            {
              "Categorys": [],
              "Roles": [{
                "RoleName": " Owner ",
                "DisplayNames": ["Owner"]
              }]
            }
            """;
        var system = SystemData(
            "個人",
            permittedRole: "Owner",
            roles: [new GetWorldData.Role("Owner", ["Owner"])]);

        var result = WorldDataJsonMerger.Merge(source, system);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var root = JsonNode.Parse(result.Json!)!.AsObject();
        Assert.Single(root["Roles"]!.AsArray());
        Assert.Equal("Owner", root["Categorys"]![0]!["PermittedRoles"]![0]!.GetValue<string>());
    }

    [Fact]
    public void Merge_DoesNotCreateRolesWithoutSystemRoles()
    {
        var result = WorldDataJsonMerger.Merge(
            """{"Categorys":[]}""",
            SystemData("公開"));

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Null(JsonNode.Parse(result.Json!)!["Roles"]);
    }

    [Theory]
    [InlineData("""{"Categorys":[{"Category":" duplicate ","Worlds":[]}]}""")]
    [InlineData("""{"Categorys":[],"Roles":[{"RoleName":"Owner","DisplayNames":["Different"]}]}""")]
    public void Merge_RejectsCategoryOrRoleCollision(string source)
    {
        var system = SystemData(
            "DUPLICATE",
            permittedRole: "Owner",
            roles: [new GetWorldData.Role("Owner", ["Owner"])]);

        var result = WorldDataJsonMerger.Merge(source, system);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("""{"Categorys":{}}""")]
    [InlineData("""{"Categorys":[],"Roles":{}}""")]
    [InlineData("""{"Categorys":[{"Category":1,"Worlds":[]}]}""")]
    public void Merge_RejectsInvalidShape(string source)
    {
        var result = WorldDataJsonMerger.Merge(source, SystemData("追加"));

        Assert.False(result.IsSuccess);
    }

    private static GetWorldData.Response SystemData(
        string categoryName,
        string? permittedRole = null,
        GetWorldData.Role[]? roles = null) =>
        new(
            ReverseCategorys: false,
            ShowPrivateWorld: true,
            Categorys:
            [
                new GetWorldData.Category(
                    categoryName,
                    [],
                    permittedRole is null ? null : [permittedRole])
            ],
            Roles: roles);
}
