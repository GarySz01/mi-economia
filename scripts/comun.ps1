# Funciones que usan exportar-datos.ps1 e importar-datos.ps1.

$ErrorActionPreference = 'Stop'

$Raiz = Split-Path $PSScriptRoot -Parent
$Contenedor = 'mieconomia-db'
$ArchivoDatos = Join-Path $Raiz 'datos\mieconomia.sql'

# Usuario y base de PostgreSQL, leídos del .env (el mismo que usa docker-compose).
function Leer-Env {
    $archivo = Join-Path $Raiz '.env'
    if (-not (Test-Path $archivo)) { throw "No existe $archivo. Cópialo de .env.example y completa la contraseña." }
    $valores = @{}
    foreach ($linea in Get-Content $archivo) {
        if ($linea -match '^\s*([A-Z_]+)\s*=\s*(.*?)\s*$') { $valores[$Matches[1]] = $Matches[2] }
    }
    return $valores
}

function Invocar-Docker {
    & docker @args
    if ($LASTEXITCODE -ne 0) { throw "Falló el comando: docker $($args -join ' ')" }
}

function Verificar-Base {
    $estado = & docker inspect -f '{{.State.Running}}' $Contenedor
    if ($LASTEXITCODE -ne 0 -or $estado -ne 'true') {
        throw "La base no está corriendo. Abre Docker Desktop y ejecuta: docker compose up -d"
    }
}

# Vuelca la base completa (estructura y datos) a un archivo de la PC.
function Volcar-Base([hashtable]$envs, [string]$destino) {
    New-Item -ItemType Directory -Force (Split-Path $destino) | Out-Null
    Invocar-Docker exec $Contenedor pg_dump -U $envs.POSTGRES_USER -d $envs.POSTGRES_DB --no-owner --no-privileges -f /tmp/volcado.sql
    Invocar-Docker cp "${Contenedor}:/tmp/volcado.sql" $destino
    Invocar-Docker exec $Contenedor rm /tmp/volcado.sql
}
