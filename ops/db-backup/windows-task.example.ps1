$RepositoryPath = "C:\VrcWebMap.Backend"

Set-Location $RepositoryPath
& docker compose `
    --env-file .env.backup `
    --profile backup `
    run --rm db-backup backup

exit $LASTEXITCODE
