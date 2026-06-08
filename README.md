# VRCWebMapBackend

VRCWebMapBackend is a prototype backend for managing map spots related to
VRChat worlds, areas, restaurants, and free-form comments.

The application follows Kawa's contract-first and usecase-first style:

- `Contracts/` contains Kawa request and response contracts.
- `UseCases/` contains transport-independent application flows.
- `Endpoints/Web/` exposes use cases through Kawa.Web endpoints.
- `Models/` contains simple C# records for the current data shape.
- `Stores/` contains the current in-memory repository implementation.

## Status

This project is experimental. The current storage implementation is in-memory
and is intended for prototyping only.

## Requirements

- .NET 10 SDK

## Restore, Build, and Test

In this Codex workspace, NuGet package writes are routed to `/private/tmp` to
avoid sandbox write restrictions.

```bash
dotnet restore \
  --source /private/tmp/nuget-local \
  --packages /private/tmp/nuget-packages \
  -p:NuGetAudit=false

dotnet build --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages
```

Run tests:

```bash
dotnet test VRCWebMapBackend.Tests/VRCWebMapBackend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages
```

Outside this sandbox, standard `dotnet restore`, `dotnet build`, and
`dotnet test` should be usable if the local NuGet cache is writable.

## API

Spot management endpoints are exposed through Kawa use cases:

- `POST /spots/list`
- `POST /spots/get`
- `POST /spots/create`
- `POST /spots/update`
- `POST /spots/delete`

Kawa API catalog and OpenAPI endpoints are also mapped by the application.
Swagger UI and ReDoc are enabled in development.

## License

This project is licensed under the MIT License. See `LICENSE`.

Dependency license notes are listed in `NOTICE`.

## Data and Trademark Notes

This repository's MIT License applies to the source code in this project.

VRChat names, identifiers, trademarks, logos, and any data obtained from VRChat
or third-party services are not granted by this repository's license. If this
project later imports or republishes external data, confirm the applicable
terms of service and data licensing separately.

Restaurant data and links from external services such as Gurunavi, Tabelog,
Retty, X, or Instagram may be subject to their own terms. Treat those rights
and usage conditions separately from this source-code license.
