using Kawa.Abstractions;
using Kawa.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using VrcWebMap.Backend.Contracts.Areas;
using VrcWebMap.Backend.Contracts.Comments;
using VrcWebMap.Backend.Contracts.Portal;
using VrcWebMap.Backend.Contracts.PlaceInfos;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Contracts.Users;
using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.Contracts.WebLinks;
using VrcWebMap.Backend.Endpoints.Web;
using VrcWebMap.Backend.Options;
using VrcWebMap.Backend.Serialization;
using VrcWebMap.Backend.Services;
using VrcWebMap.Backend.Stores;
using VrcWebMap.Backend.UseCases.Areas;
using VrcWebMap.Backend.UseCases.Comments;
using VrcWebMap.Backend.UseCases.Portal;
using VrcWebMap.Backend.UseCases.PlaceInfos;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;
using VrcWebMap.Backend.UseCases.VRChatWorlds;
using VrcWebMap.Backend.UseCases.WebLinks;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});
builder.Services.Configure<DiscordOptions>(builder.Configuration.GetSection("Discord"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActorAccessor, HttpCurrentActorAccessor>();
builder.Services.AddHttpClient<DiscordApiClient>();
builder.Services.AddHttpClient<IOpenGraphPreviewProvider, OpenGraphPreviewClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "vrcwebmap.auth";
        options.LoginPath = "/auth/discord/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/auth/forbidden";
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (IsWriteEndpoint(context.Request.Path))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                if (IsWriteEndpoint(context.Request.Path))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var databaseProvider = builder.Configuration.GetValue("Database:Provider", "InMemory");
var connectionString = builder.Configuration.GetConnectionString("Postgres");

if (string.Equals(databaseProvider, "PostgreSQL", StringComparison.OrdinalIgnoreCase) &&
    !string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
    builder.Services.AddScoped<ISpotRepository, PostgreSqlSpotRepository>();
    builder.Services.AddScoped<IDiscordUserRepository, PostgreSqlDiscordUserRepository>();
}
else
{
    builder.Services.AddSingleton<ISpotRepository, InMemorySpotRepository>();
    builder.Services.AddSingleton<IDiscordUserRepository, InMemoryDiscordUserRepository>();
}

builder.Services
    .AddKawa()
    .AddKawaWeb();
builder.Services.Configure<OpenApiOptions>(
    KawaOpenApiDefaults.DocumentName,
    options =>
    {
        options.CreateSchemaReferenceId = CreateOpenApiSchemaReferenceId;
        options.AddSchemaTransformer<AppOpenApiXmlCommentsSchemaTransformer>();
    });

AddUseCases(builder.Services);

var app = builder.Build();

if (string.Equals(databaseProvider, "PostgreSQL", StringComparison.OrdinalIgnoreCase) &&
    !string.IsNullOrWhiteSpace(connectionString))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    PostgreSqlSchemaInitializer.EnsureCreated(db);
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    DevelopmentSampleDataSeeder.Seed(scope.ServiceProvider.GetRequiredService<ISpotRepository>());
}

app.Use(async (context, next) =>
{
    context.Response.Headers.ContentSecurityPolicy =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'none'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "script-src 'self'; " +
        "script-src-elem 'self'; " +
        "connect-src 'self'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "form-action 'self'; " +
        "upgrade-insecure-requests";
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapDiscordAuth();
app.MapUsers();
app.MapAreas();
app.MapSpots();
app.MapSpotContent();
app.MapPortal();
app.MapKawaApiCatalog();
app.MapKawaOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapKawaSwagger(
        routePrefix: "openapi/swagger",
        documentUrl: "/openapi/v1.json",
        documentName: "VrcWebMap.Backend v1");
    app.MapKawaReDoc(
        routePrefix: "openapi/redoc",
        documentUrl: "/openapi/v1.json",
        documentName: "VrcWebMap.Backend v1");
}

app.Run();

static void AddUseCases(IServiceCollection services)
{
    services.AddScoped<IUseCase<ListAreas.Request, ListAreas.Response>, ListAreasUseCase>();
    services.AddScoped<IUseCase<CreateComment.Request, CreateComment.Response>, CreateCommentUseCase>();
    services.AddScoped<IUseCase<DeleteComment.Request, DeleteComment.Response>, DeleteCommentUseCase>();
    services.AddScoped<IUseCase<UpdateComment.Request, UpdateComment.Response>, UpdateCommentUseCase>();
    services.AddScoped<IUseCase<GetWorldData.Request, GetWorldData.Response>, GetWorldDataUseCase>();
    services.AddScoped<IUseCase<CreatePlaceInfo.Request, CreatePlaceInfo.Response>, CreatePlaceInfoUseCase>();
    services.AddScoped<IUseCase<DeletePlaceInfo.Request, DeletePlaceInfo.Response>, DeletePlaceInfoUseCase>();
    services.AddScoped<IUseCase<UpdatePlaceInfo.Request, UpdatePlaceInfo.Response>, UpdatePlaceInfoUseCase>();
    services.AddScoped<IUseCase<CreateSpot.Request, CreateSpot.Response>, CreateSpotUseCase>();
    services.AddScoped<IUseCase<DeleteSpot.Request, DeleteSpot.Response>, DeleteSpotUseCase>();
    services.AddScoped<IUseCase<GetSpot.Request, GetSpot.Response>, GetSpotUseCase>();
    services.AddScoped<IUseCase<ImportKmlSpots.Request, ImportKmlSpots.Response>, ImportKmlSpotsUseCase>();
    services.AddScoped<IUseCase<ListSpots.Request, ListSpots.Response>, ListSpotsUseCase>();
    services.AddScoped<IUseCase<PreviewKmlImport.Request, PreviewKmlImport.Response>, PreviewKmlImportUseCase>();
    services.AddScoped<IUseCase<UpdateSpot.Request, UpdateSpot.Response>, UpdateSpotUseCase>();
    services.AddScoped<IUseCase<RegisterDiscordUser.Request, RegisterDiscordUser.Response>, RegisterDiscordUserUseCase>();
    services.AddScoped<IUseCase<UpdateVRChatDisplayName.Request, UpdateVRChatDisplayName.Response>, UpdateVRChatDisplayNameUseCase>();
    services.AddScoped<IUseCase<ListUsers.Request, ListUsers.Response>, ListUsersUseCase>();
    services.AddScoped<IUseCase<SetUserAdminStatus.Request, SetUserAdminStatus.Response>, SetUserAdminStatusUseCase>();
    services.AddScoped<IUseCase<CreateVRChatWorld.Request, CreateVRChatWorld.Response>, CreateVRChatWorldUseCase>();
    services.AddScoped<IUseCase<DeleteVRChatWorld.Request, DeleteVRChatWorld.Response>, DeleteVRChatWorldUseCase>();
    services.AddScoped<IUseCase<UpdateVRChatWorld.Request, UpdateVRChatWorld.Response>, UpdateVRChatWorldUseCase>();
    services.AddScoped<IUseCase<CreateWebLink.Request, CreateWebLink.Response>, CreateWebLinkUseCase>();
    services.AddScoped<IUseCase<DeleteWebLink.Request, DeleteWebLink.Response>, DeleteWebLinkUseCase>();
    services.AddScoped<IUseCase<GetWebLinkPreview.Request, GetWebLinkPreview.Response>, GetWebLinkPreviewUseCase>();
    services.AddScoped<IUseCase<UpdateWebLink.Request, UpdateWebLink.Response>, UpdateWebLinkUseCase>();
}

static bool IsWriteEndpoint(PathString path) =>
    path.StartsWithSegments("/spots/create") ||
    path.StartsWithSegments("/spots/import/kml") ||
    path.StartsWithSegments("/spots/update") ||
    path.StartsWithSegments("/spots/delete") ||
    path.StartsWithSegments("/vrchat-worlds/create") ||
    path.StartsWithSegments("/vrchat-worlds/update") ||
    path.StartsWithSegments("/vrchat-worlds/delete") ||
    path.StartsWithSegments("/place-infos/create") ||
    path.StartsWithSegments("/place-infos/update") ||
    path.StartsWithSegments("/place-infos/delete") ||
    path.StartsWithSegments("/web-links/create") ||
    path.StartsWithSegments("/web-links/update") ||
    path.StartsWithSegments("/web-links/delete") ||
    path.StartsWithSegments("/comments/create") ||
    path.StartsWithSegments("/comments/update") ||
    path.StartsWithSegments("/comments/delete") ||
    path.StartsWithSegments("/users/profile") ||
    path.StartsWithSegments("/users/list") ||
    path.StartsWithSegments("/users/admin-status");

static string? CreateOpenApiSchemaReferenceId(System.Text.Json.Serialization.Metadata.JsonTypeInfo typeInfo)
{
    var type = typeInfo.Type;
    if (type is { IsNested: true, DeclaringType: not null } &&
        type.Namespace?.StartsWith("VrcWebMap.Backend.Contracts.", StringComparison.Ordinal) == true)
    {
        return $"{type.DeclaringType.Name}{type.Name}";
    }

    return OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);
}
