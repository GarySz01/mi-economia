# Mi Economía

App web para registrar ingresos y egresos personales con categorías propias de cada usuario.
Más adelante habrá una app móvil que va a usar la misma API, por eso el backend es una API independiente del frontend.

El usuario habla español: responder, comentar el código y escribir los commits en español.

## Stack

- **Backend:** ASP.NET Core Web API (.NET 8), controladores, EF Core 8 + Npgsql, ASP.NET Core Identity + JWT.
- **Base de datos:** PostgreSQL 17 en Docker (`docker-compose.yml`).
- **Frontend (pendiente):** React + TypeScript con Vite, en `frontend/`. Íconos con `lucide-react`.

## Estructura

```
docker-compose.yml          PostgreSQL (lee credenciales de .env)
.env / .env.example         .env tiene la contraseña real y NO se commitea
backend/
  MiEconomia.sln
  .config/dotnet-tools.json dotnet-ef como herramienta local
  src/MiEconomia.Api/
    Domain/                 Entidades: Usuario (IdentityUser<Guid>), Categoria, Movimiento, TipoMovimiento
    Data/                   AppDbContext y Migrations/
    Auth/                   JWT, DTOs de auth, mensajes de Identity en español, rate limit
    Categorias/ Movimientos/ Resumen/   DTOs de cada módulo (+ CategoriasPorDefecto)
    Controllers/            Auth, Categorias, Movimientos, Resumen
    ProblemDetailsEs.cs     Traduce al español los errores que genera ASP.NET Core
```

## Comandos

Desde la raíz del repo:

```bash
docker compose up -d                                   # levantar PostgreSQL
dotnet build backend
dotnet run --project backend/src/MiEconomia.Api        # http://localhost:5170/swagger

# Migraciones (ejecutar desde backend/; la primera vez: dotnet tool restore)
dotnet ef migrations add <Nombre> -p src/MiEconomia.Api -o Data/Migrations
dotnet ef database update -p src/MiEconomia.Api
```

Secretos en **user-secrets** del proyecto API, nunca en `appsettings*.json`:
- `ConnectionStrings:Default`: `Host=localhost;Port=5432;Database=mieconomia;Username=mieconomia;Password=<la de .env>`
- `Jwt:Key`: clave aleatoria de al menos 32 caracteres (en producción, variable de entorno `Jwt__Key`).

## API

Todo bajo `/api`, y todo pide `Authorization: Bearer <token>` salvo registro y login.

| Endpoint | Notas |
|---|---|
| `POST auth/registro`, `POST auth/login`, `GET auth/yo` | Devuelven `{ token, expiraEn, usuario }`. El token dura 60 min, todavía sin refresh token |
| `categorias` (GET con `?tipo=`, GET/POST/PUT/DELETE `{id}`) | El tipo no se puede editar. Borrar con movimientos da 409 |
| `movimientos` (GET con `desde, hasta, categoriaId, tipo, pagina, tamanio`) | Devuelve `{ items, total, pagina, tamanio }` |
| `resumen?desde=&hasta=` | Totales y desglose por categoría; sin fechas usa el mes actual |
| `resumen/mensual?meses=&hasta=` | Un elemento por mes, incluidos los meses vacíos |

Formato JSON: camelCase, enums como texto (`"Ingreso"`/`"Egreso"`), fechas `DateOnly` como `"2026-10-01"`, montos `decimal(18,2)`.

## Convenciones

- **Nombres del dominio en español** (`Categoria`, `Movimiento`, `Monto`, `Fecha`), sin tildes en los identificadores. Los términos técnicos van en inglés (`Controller`, `DbContext`, `Request`/`Response`).
- **Aislamiento por usuario:** toda consulta filtra por `User.GetUsuarioId()`. Si algo es de otro usuario se responde 404, nunca 403, para no revelar que existe. Al recibir ids relacionados (por ejemplo `categoriaId`), verificar que también sean del usuario.
- **Monto siempre positivo:** si es ingreso o egreso lo define la categoría, no el signo. La base tiene un check `Monto > 0`.
- **Errores:** validaciones con DataAnnotations y `ErrorMessage` en español; las reglas de negocio con `ModelState.AddModelError(campo, mensaje)` + `ValidationProblem(ModelState)`; los conflictos con `Problem(title: ..., statusCode: 409)`. Los mensajes se escriben para que el frontend los muestre tal cual.
- **DTOs:** como `record` en la carpeta de su módulo, con un método estático `Desde(entidad)` para mapear. Las entidades nunca se devuelven directamente.
- **Agregaciones** (sumas, conteos) en la base con `GroupBy` + `Select`, no trayendo todas las filas a memoria.

## Flujo de trabajo

- Probar cada endpoint nuevo de punta a punta con `curl`: casos correctos, validaciones y acceso con un segundo usuario. Después borrar los usuarios de prueba (`delete from "AspNetUsers" where "Email" in (...)`; el borrado en cascada elimina sus datos).
- Un commit por funcionalidad terminada, con un mensaje en español, y solo cuando el usuario lo confirma.

## Entorno (Windows 10)

- No hay Python ni winget. Para tareas con scripts usar PowerShell o bash con curl/sed/grep.
- Si `dotnet` no está en el PATH de la shell: `export PATH="$PATH:/c/Program Files/dotnet"`.
- Docker Desktop tarda unos minutos en arrancar después de encender el equipo; esperar a que el motor esté corriendo.
- En PowerShell 5.1, `Invoke-WebRequest` manda mal el body JSON. Para probar la API usar `curl` desde bash.
- Los heredocs de bash con mucho código C# pueden fallar al parsearse; para crear archivos usar la herramienta Write.
