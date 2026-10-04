using System.Net;
using System.Net.Http.Headers;
using MiEconomia.Api.Auth;
using MiEconomia.Api.Categorias;
using MiEconomia.Api.Domain;
using MiEconomia.Api.Tests.Infraestructura;

namespace MiEconomia.Api.Tests;

[Collection(ApiCollection.Nombre)]
public class AuthTests(ApiFactory api)
{
    private readonly HttpClient _anonimo = api.CreateClient();

    [Fact]
    public async Task Registro_devuelve_token_y_datos_del_usuario()
    {
        var email = $"{Guid.NewGuid():N}@test.com";

        var respuesta = await _anonimo.PostJsonAsync("/api/auth/registro",
            new { nombre = "  Ana Pérez  ", email, password = "clave1234" });

        var auth = await respuesta.LeerAsync<AuthResponse>(HttpStatusCode.Created);
        Assert.False(string.IsNullOrEmpty(auth.Token));
        Assert.Equal("Ana Pérez", auth.Usuario.Nombre);
        Assert.Equal(email, auth.Usuario.Email);
        Assert.True(auth.ExpiraEn > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Registro_crea_las_categorias_por_defecto()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var categorias = await (await sesion.Cliente.GetAsync("/api/categorias"))
            .LeerAsync<List<CategoriaResponse>>();

        Assert.Equal(13, categorias.Count);
        Assert.Equal(4, categorias.Count(c => c.Tipo == TipoMovimiento.Ingreso));
        Assert.Contains(categorias, c => c is { Nombre: "Sueldo", Tipo: TipoMovimiento.Ingreso, Icono: "briefcase" });
        Assert.Contains(categorias, c => c is { Nombre: "Supermercado", Tipo: TipoMovimiento.Egreso, Color: "#EA580C" });
    }

    [Fact]
    public async Task Registro_con_email_duplicado_devuelve_un_solo_error_en_espanol()
    {
        var existente = await api.NuevoUsuarioAsync();

        var respuesta = await _anonimo.PostJsonAsync("/api/auth/registro",
            new { nombre = "Otro", email = existente.Email, password = "clave1234" });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal("Hay errores en los datos enviados.", problema.Title);
        var error = Assert.Single(problema.De("Email"));
        Assert.Equal($"El email '{existente.Email}' ya está registrado.", error);
    }

    [Fact]
    public async Task Registro_con_password_debil_explica_cada_regla()
    {
        var respuesta = await _anonimo.PostJsonAsync("/api/auth/registro",
            new { nombre = "Ana", email = $"{Guid.NewGuid():N}@test.com", password = "abc" });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Contains("La contraseña debe tener al menos 8 caracteres.", problema.De("Password"));
        Assert.Contains("La contraseña debe tener al menos un número.", problema.De("Password"));
    }

    [Fact]
    public async Task Registro_con_campos_vacios_valida_cada_campo()
    {
        var respuesta = await _anonimo.PostJsonAsync("/api/auth/registro",
            new { nombre = "", email = "no-es-un-email", password = "" });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["El nombre es obligatorio."], problema.De("Nombre"));
        Assert.Equal(["El email no es válido."], problema.De("Email"));
        Assert.Equal(["La contraseña es obligatoria."], problema.De("Password"));
    }

    [Fact]
    public async Task Login_correcto_devuelve_token_que_sirve_para_consultar_yo()
    {
        var sesion = await api.NuevoUsuarioAsync("Beto");

        var login = await (await _anonimo.PostJsonAsync("/api/auth/login",
            new { email = sesion.Email, password = Sesion.Password })).LeerAsync<AuthResponse>();

        var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        var yo = await (await cliente.GetAsync("/api/auth/yo")).LeerAsync<UsuarioResponse>();
        Assert.Equal(sesion.Auth.Usuario.Id, yo.Id);
        Assert.Equal("Beto", yo.Nombre);
    }

    [Fact]
    public async Task Login_no_revela_si_el_email_existe()
    {
        var sesion = await api.NuevoUsuarioAsync();

        var passwordMal = await (await _anonimo.PostJsonAsync("/api/auth/login",
            new { email = sesion.Email, password = "incorrecta1" })).LeerProblemaAsync(HttpStatusCode.Unauthorized);
        var emailInexistente = await (await _anonimo.PostJsonAsync("/api/auth/login",
            new { email = "nadie@test.com", password = "incorrecta1" })).LeerProblemaAsync(HttpStatusCode.Unauthorized);

        Assert.Equal("Email o contraseña incorrectos.", passwordMal.Title);
        Assert.Equal(passwordMal.Title, emailInexistente.Title);
    }

    [Fact]
    public async Task Login_bloquea_la_cuenta_tras_5_intentos_fallidos()
    {
        var sesion = await api.NuevoUsuarioAsync();
        for (var i = 0; i < 5; i++)
            await _anonimo.PostJsonAsync("/api/auth/login", new { email = sesion.Email, password = "incorrecta1" });

        // Incluso con la contraseña correcta queda bloqueada.
        var respuesta = await _anonimo.PostJsonAsync("/api/auth/login",
            new { email = sesion.Email, password = Sesion.Password });

        var problema = await respuesta.LeerProblemaAsync(HttpStatusCode.Unauthorized);
        Assert.StartsWith("Cuenta bloqueada temporalmente", problema.Title);
    }

    [Theory]
    [InlineData("/api/auth/yo")]
    [InlineData("/api/categorias")]
    [InlineData("/api/movimientos")]
    [InlineData("/api/resumen")]
    [InlineData("/api/resumen/mensual")]
    public async Task Endpoints_protegidos_sin_token_devuelven_401(string url)
    {
        var respuesta = await _anonimo.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Token_alterado_devuelve_401()
    {
        var sesion = await api.NuevoUsuarioAsync();
        var cliente = api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", sesion.Auth.Token[..^3] + "abc");

        var respuesta = await cliente.GetAsync("/api/auth/yo");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }
}
