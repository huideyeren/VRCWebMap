using Kawa.Web;
using VRCWebMapBackend.Endpoints.Web;
using VRCWebMapBackend.Serialization;
using VRCWebMapBackend.Stores;
using VRCWebMapBackend.UseCases.Spots;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddSingleton<ISpotRepository, InMemorySpotRepository>();
builder.Services
    .AddKawa()
    .AddKawaUseCasesFromAssemblies(typeof(CreateSpotUseCase).Assembly)
    .AddKawaWeb();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapSpots();
app.MapKawaApiCatalog();
app.MapKawaOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapKawaSwagger();
    app.MapKawaReDoc();
}

app.Run();
