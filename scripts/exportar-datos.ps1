# Guarda todos los datos de la base local en datos/mieconomia.sql para subirlos a git.
# Uso (desde la raíz del repo): powershell -ExecutionPolicy Bypass -File scripts\exportar-datos.ps1

. (Join-Path $PSScriptRoot 'comun.ps1')

Verificar-Base
Volcar-Base (Leer-Env) $ArchivoDatos

Write-Host "Datos exportados a $ArchivoDatos" -ForegroundColor Green
Write-Host 'Ahora: git add datos; git commit -m "Actualizar datos"; git push'
