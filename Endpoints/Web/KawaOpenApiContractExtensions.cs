namespace VrcWebMap.Backend.Endpoints.Web;

/// <summary>
/// Kawa endpoint の OpenAPI schema を central contract の body 型に合わせます。
/// </summary>
public static class KawaOpenApiContractExtensions
{
    /// <summary>
    /// Kawa.Web の runtime mapping を維持したまま、OpenAPI の request / success response body を契約型として明示します。
    /// </summary>
    /// <typeparam name="TRequest">UseCase request contract type.</typeparam>
    /// <typeparam name="TResponse">UseCase success response contract type.</typeparam>
    /// <param name="builder">対象 endpoint の route handler builder です。</param>
    /// <returns>metadata を追加した route handler builder です。</returns>
    public static RouteHandlerBuilder WithContractOpenApi<TRequest, TResponse>(this RouteHandlerBuilder builder)
        where TRequest : notnull
    {
        return builder
            .Accepts<TRequest>("application/json")
            .Produces<TResponse>(StatusCodes.Status200OK, "application/json");
    }
}
