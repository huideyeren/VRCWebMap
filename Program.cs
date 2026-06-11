using Kawa.Abstractions;
using Kawa.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using VrcWebMap.Backend.Contracts.Comments;
using VrcWebMap.Backend.Contracts.Portal;
using VrcWebMap.Backend.Contracts.Restaurants;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Contracts.Users;
using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.Endpoints.Web;
using VrcWebMap.Backend.Options;
using VrcWebMap.Backend.Serialization;
using VrcWebMap.Backend.Services;
using VrcWebMap.Backend.Stores;
using VrcWebMap.Backend.UseCases.Comments;
using VrcWebMap.Backend.UseCases.Portal;
using VrcWebMap.Backend.UseCases.Restaurants;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;
using VrcWebMap.Backend.UseCases.VRChatWorlds;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});
builder.Services.Configure<DiscordOptions>(builder.Configuration.GetSection("Discord"));
builder.Services.AddHttpClient<DiscordApiClient>();
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
    services.AddScoped<IUseCase<CreateRestaurant.Request, CreateRestaurant.Response>, CreateRestaurantUseCase>();
    services.AddScoped<IUseCase<DeleteRestaurant.Request, DeleteRestaurant.Response>, DeleteRestaurantUseCase>();
    services.AddScoped<IUseCase<UpdateRestaurant.Request, UpdateRestaurant.Response>, UpdateRestaurantUseCase>();
    services.AddScoped<IUseCase<CreateSpot.Request, CreateSpot.Response>, CreateSpotUseCase>();
    services.AddScoped<IUseCase<DeleteSpot.Request, DeleteSpot.Response>, DeleteSpotUseCase>();
    services.AddScoped<IUseCase<GetSpot.Request, GetSpot.Response>, GetSpotUseCase>();
    services.AddScoped<IUseCase<ListSpots.Request, ListSpots.Response>, ListSpotsUseCase>();
    services.AddScoped<IUseCase<UpdateSpot.Request, UpdateSpot.Response>, UpdateSpotUseCase>();
    services.AddScoped<IUseCase<RegisterDiscordUser.Request, RegisterDiscordUser.Response>, RegisterDiscordUserUseCase>();
    services.AddScoped<IUseCase<CreateVRChatWorld.Request, CreateVRChatWorld.Response>, CreateVRChatWorldUseCase>();
    services.AddScoped<IUseCase<DeleteVRChatWorld.Request, DeleteVRChatWorld.Response>, DeleteVRChatWorldUseCase>();
    services.AddScoped<IUseCase<UpdateVRChatWorld.Request, UpdateVRChatWorld.Response>, UpdateVRChatWorldUseCase>();
}
