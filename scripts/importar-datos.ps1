# Reemplaza la base local por los datos de datos/mieconomia.sql (los que trajo git pull)
# y después aplica las migraciones que falten.
# Antes guarda un respaldo de lo que había en datos/respaldos/ (no se sube a git).
# Uso (desde la raíz del repo, con la API detenida): powershell -ExecutionPolicy Bypass -File scripts\importar-datos.ps1

. (Join-Path $PSScriptRoot 'comun.ps1')

if (-not (Test-Path $ArchivoDatos)) { throw "No existe $ArchivoDatos. Haz git pull primero." }
if (Get-NetTCPConnection -LocalPort 5170 -State Listen -ErrorAction SilentlyContinue) {
    throw 'La API está corriendo (puerto 5170). Detenla antes de importar.'
}
Verificar-Base
$envs = Leer-Env

$respaldo = Join-Path $Raiz ("datos\respaldos\antes-de-importar-{0:yyyy-MM-dd_HH-mm-ss}.sql" -f (Get-Date))
Volcar-Base $envs $respaldo
Write-Host "Respaldo de la base anterior: $respaldo"

# Se vacía la base entera para que no queden tablas de una versión distinta.
Invocar-Docker cp $ArchivoDatos "${Contenedor}:/tmp/importar.sql"
Invocar-Docker exec $Contenedor psql -q -v ON_ERROR_STOP=1 -U $envs.POSTGRES_USER -d $envs.POSTGRES_DB -c 'DROP SCHEMA public CASCADE; CREATE SCHEMA public;'
Invocar-Docker exec $Contenedor psql -q -v ON_ERROR_STOP=1 --single-transaction -U $envs.POSTGRES_USER -d $envs.POSTGRES_DB -f /tmp/importar.sql
Invocar-Docker exec $Contenedor rm /tmp/importar.sql

# Si el código trae migraciones más nuevas que los datos exportados, se aplican ahora.
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { $env:PATH += ';C:\Program Files\dotnet' }
Push-Location (Join-Path $Raiz 'backend')
try {
    & dotnet tool restore | Out-Null
    & dotnet ef database update -p src/MiEconomia.Api
    if ($LASTEXITCODE -ne 0) { throw 'Falló dotnet ef database update.' }
} finally {
    Pop-Location
}

Write-Host 'Datos importados.' -ForegroundColor Green
