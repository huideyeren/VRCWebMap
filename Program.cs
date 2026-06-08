using Kawa.Web;
using VrcWebMap.Backend.Endpoints.Web;
using VrcWebMap.Backend.Serialization;
using VrcWebMap.Backend.Stores;
using VrcWebMap.Backend.UseCases.Spots;

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
