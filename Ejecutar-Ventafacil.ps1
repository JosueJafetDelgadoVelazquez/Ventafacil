$ErrorActionPreference = "Stop"

$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
try {
    [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
}
catch {
}

$repoRoot = $PSScriptRoot
$project = Join-Path $repoRoot "src\Desktop\Ventafacil.Desktop\Ventafacil.Desktop.csproj"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "No se encontro el SDK de .NET." -ForegroundColor Red
    Write-Host "Instala el SDK de .NET 10 desde: https://dotnet.microsoft.com/download/dotnet/10.0"
    exit 1
}

$sdk10 = dotnet --list-sdks | Where-Object { $_ -match "^10\." }
if (-not $sdk10) {
    Write-Host "No tienes instalado un SDK de .NET 10." -ForegroundColor Red
    Write-Host "Instala el SDK de .NET 10 desde: https://dotnet.microsoft.com/download/dotnet/10.0"
    exit 1
}

if (-not (Test-Path $project)) {
    Write-Host "No se encontro el proyecto: $project" -ForegroundColor Red
    exit 1
}

Set-Location $repoRoot
dotnet build $project --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "La compilacion fallo. Corrige los errores anteriores y vuelve a ejecutar." -ForegroundColor Red
    exit $LASTEXITCODE
}

dotnet run --project $project --no-build
$exitCode = $LASTEXITCODE

if ($exitCode -eq -532462766 -or $exitCode -eq 3762504530) {
    Write-Host ""
    Write-Host "Windows bloqueo uno de los archivos de VentaFacil mediante Smart App Control." -ForegroundColor Red
    Write-Host "Para una distribucion duradera, firma los ejecutables con un certificado Authenticode reconocido." -ForegroundColor Yellow
    Write-Host "Mientras tanto, revisa: Seguridad de Windows > Seguridad de aplicaciones > Control de aplicaciones." -ForegroundColor Yellow
}

exit $exitCode
