using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using TechSupport.Web.Models;

namespace TechSupport.Web.Services;

public class ApiResultado<T>
{
    public HttpStatusCode Status { get; init; }
    public T? Datos { get; init; }
    public string? Mensaje { get; init; }
    public List<string> Errores { get; init; } = [];

    public bool Exito => (int)Status is >= 200 and < 300;
    public bool NoAutenticado => Status == HttpStatusCode.Unauthorized;
}

public class TokenHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    public const string ClaimToken = "access_token";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = accessor.HttpContext?.User.FindFirst(ClaimToken)?.Value;
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return base.SendAsync(request, cancellationToken);
    }
}

public class ApiClient(HttpClient http, ILogger<ApiClient> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<ApiResultado<LoginRespuesta>> LoginAsync(string usuario, string password) =>
        EnviarAsync<LoginRespuesta>(HttpMethod.Post, "api/auth/login", new { nombreUsuario = usuario, password });

    public Task<ApiResultado<List<Incidente>>> ListarAsync() =>
        EnviarAsync<List<Incidente>>(HttpMethod.Get, "api/incidentes");

    public Task<ApiResultado<Incidente>> ObtenerAsync(int id) =>
        EnviarAsync<Incidente>(HttpMethod.Get, $"api/incidentes/{id}");

    public Task<ApiResultado<Incidente>> CrearAsync(CrearIncidenteViewModel vm) =>
        EnviarAsync<Incidente>(HttpMethod.Post, "api/incidentes",
            new { titulo = vm.Titulo, descripcion = vm.Descripcion, prioridad = vm.Prioridad });

    public Task<ApiResultado<Incidente>> AsignarAsync(int id, int tecnicoId) =>
        EnviarAsync<Incidente>(HttpMethod.Put, $"api/incidentes/{id}/asignar", new { tecnicoId });

    public Task<ApiResultado<Incidente>> CambiarEstadoAsync(int id, string nuevoEstado) =>
        EnviarAsync<Incidente>(HttpMethod.Put, $"api/incidentes/{id}/estado", new { nuevoEstado });

    public Task<ApiResultado<object>> EliminarAsync(int id) =>
        EnviarAsync<object>(HttpMethod.Delete, $"api/incidentes/{id}");

    public Task<ApiResultado<List<Tecnico>>> TecnicosAsync() =>
        EnviarAsync<List<Tecnico>>(HttpMethod.Get, "api/usuarios/tecnicos");

    private async Task<ApiResultado<T>> EnviarAsync<T>(HttpMethod metodo, string ruta, object? cuerpo = null)
    {
        using var request = new HttpRequestMessage(metodo, ruta);
        if (cuerpo is not null)
            request.Content = JsonContent.Create(cuerpo, options: Json);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "No se pudo conectar con la API en {BaseAddress}", http.BaseAddress);
            return new ApiResultado<T>
            {
                Status = HttpStatusCode.ServiceUnavailable,
                Mensaje = "No se pudo conectar con la API. Verifica que esté en ejecución."
            };
        }

        using (response)
        {
            var contenido = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var datos = string.IsNullOrWhiteSpace(contenido) ? default : JsonSerializer.Deserialize<T>(contenido, Json);
                return new ApiResultado<T> { Status = response.StatusCode, Datos = datos };
            }

            var (mensaje, errores) = LeerError(contenido);
            return new ApiResultado<T>
            {
                Status = response.StatusCode,
                Mensaje = mensaje ?? MensajePorDefecto(response.StatusCode),
                Errores = errores
            };
        }
    }

    private static (string? Mensaje, List<string> Errores) LeerError(string contenido)
    {
        var errores = new List<string>();
        if (string.IsNullOrWhiteSpace(contenido))
            return (null, errores);

        try
        {
            using var doc = JsonDocument.Parse(contenido);
            var raiz = doc.RootElement;
            string? mensaje = raiz.TryGetProperty("message", out var m) ? m.GetString() : null;

            if (raiz.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Object)
                foreach (var campo in e.EnumerateObject())
                    errores.AddRange(campo.Value.EnumerateArray().Select(x => x.GetString() ?? string.Empty));

            return (mensaje, errores);
        }
        catch (JsonException)
        {
            return (null, errores);
        }
    }

    private static string MensajePorDefecto(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => "Tu sesión expiró. Inicia sesión de nuevo.",
        HttpStatusCode.Forbidden => "No tienes permiso para realizar esta operación.",
        HttpStatusCode.NotFound => "Incidente no encontrado.",
        _ => "Ocurrió un error al procesar la solicitud."
    };
}
