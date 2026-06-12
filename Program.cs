using Kawa.Abstractions;
using Kawa.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
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

AddUseCases(builder.Services);

var app = builder.Build();

if (string.Equals(databaseProvider, "PostgreSQL", StringComparison.OrdinalIgnoreCase) &&
    !string.IsNullOrWhiteSpace(connectionString))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.ContentSecurityPolicy =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'none'; " +
        "style-src 'self' 'unsafe-inline' https://unpkg.com; " +
        "script-src 'self' https://unpkg.com https://esm.sh; " +
        "script-src-elem 'self' https://unpkg.com https://esm.sh; " +
        "connect-src 'self' https://esm.sh; " +
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
app.MapSpots();
app.MapSpotContent();
app.MapPortal();
app.MapKawaApiCatalog();
app.MapKawaOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapKawaSwagger();
    app.MapKawaReDoc();
}

app.Run();

static void AddUseCases(IServiceCollection services)
{
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
    services.AddScoped<IUseCase<ListSpots.Request, ListSpots.Response>, ListSpotsUseCase>();
    services.AddScoped<IUseCase<UpdateSpot.Request, UpdateSpot.Response>, UpdateSpotUseCase>();
    services.AddScoped<IUseCase<RegisterDiscordUser.Request, RegisterDiscordUser.Response>, RegisterDiscordUserUseCase>();
    services.AddScoped<IUseCase<CreateVRChatWorld.Request, CreateVRChatWorld.Response>, CreateVRChatWorldUseCase>();
    services.AddScoped<IUseCase<DeleteVRChatWorld.Request, DeleteVRChatWorld.Response>, DeleteVRChatWorldUseCase>();
    services.AddScoped<IUseCase<UpdateVRChatWorld.Request, UpdateVRChatWorld.Response>, UpdateVRChatWorldUseCase>();
    services.AddScoped<IUseCase<CreateWebLink.Request, CreateWebLink.Response>, CreateWebLinkUseCase>();
    services.AddScoped<IUseCase<DeleteWebLink.Request, DeleteWebLink.Response>, DeleteWebLinkUseCase>();
    services.AddScoped<IUseCase<GetWebLinkPreview.Request, GetWebLinkPreview.Response>, GetWebLinkPreviewUseCase>();
    services.AddScoped<IUseCase<UpdateWebLink.Request, UpdateWebLink.Response>, UpdateWebLinkUseCase>();
}
