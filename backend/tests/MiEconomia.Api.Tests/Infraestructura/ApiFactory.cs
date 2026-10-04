using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiEconomia.Api.Auth;
using MiEconomia.Api.Data;
using Testcontainers.PostgreSql;

namespace MiEconomia.Api.Tests.Infraestructura;

/// <summary>
/// Levanta la API completa en memoria contra un PostgreSQL real en Docker, creado solo para los tests.
/// Se comparte entre todas las clases de tests (ver <see cref="ApiCollection"/>): cada test crea sus
/// propios usuarios, y como los datos están aislados por usuario no hace falta limpiar la base.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" en lugar de "Development" para no cargar los user-secrets de la máquina.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _db.GetConnectionString());
        builder.UseSetting("Jwt:Key", "clave-solo-para-tests-de-integracion-0123456789");
        // Los tests registran muchos usuarios desde la misma IP.
        builder.UseSetting("RateLimit:AuthPorMinuto", "100000");
    }

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
    }

    /// <summary>Registra un usuario nuevo (con email único) y devuelve un cliente ya autenticado.</summary>
    public async Task<Sesion> NuevoUsuarioAsync(string nombre = "Usuario de prueba")
    {
        var email = $"{Guid.NewGuid():N}@test.com";
        var cliente = CreateClient();
        var respuesta = await cliente.PostJsonAsync("/api/auth/registro", new { nombre, email, password = Sesion.Password });
        var auth = await respuesta.LeerAsync<AuthResponse>(System.Net.HttpStatusCode.Created);

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return new Sesion(cliente, auth, email);
    }
}

public record Sesion(HttpClient Cliente, AuthResponse Auth, string Email)
{
    public const string Password = "clave1234";
}

[CollectionDefinition(Nombre)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Nombre = "API";
}
