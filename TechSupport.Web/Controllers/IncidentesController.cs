using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechSupport.Web.Models;
using TechSupport.Web.Services;

namespace TechSupport.Web.Controllers;

[Authorize]
public class IncidentesController(ApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        var resultado = await api.ListarAsync();
        if (resultado.NoAutenticado)
            return await SesionExpirada();

        if (!resultado.Exito)
        {
            TempData["Error"] = resultado.Mensaje;
            return View(new List<Incidente>());
        }

        return View(resultado.Datos ?? []);
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var resultado = await api.ObtenerAsync(id);
        if (resultado.NoAutenticado)
            return await SesionExpirada();

        if (!resultado.Exito || resultado.Datos is null)
        {
            TempData["Error"] = resultado.Mensaje;
            return RedirectToAction(nameof(Index));
        }

        var esAdmin = User.IsInRole("Administrador");
        IReadOnlyList<Tecnico> tecnicos = [];
        if (esAdmin)
        {
            var lista = await api.TecnicosAsync();
            tecnicos = lista.Datos ?? [];
        }

        return View(new DetalleIncidenteViewModel
        {
            Incidente = resultado.Datos,
            Tecnicos = tecnicos,
            EsAdministrador = esAdmin,
            EsTecnico = User.IsInRole("Tecnico")
        });
    }

    [HttpGet]
    public IActionResult Crear() => View(new CrearIncidenteViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearIncidenteViewModel vm)
    {
        var resultado = await api.CrearAsync(vm);
        if (resultado.NoAutenticado)
            return await SesionExpirada();

        if (!resultado.Exito || resultado.Datos is null)
        {
            ModelState.AddModelError(string.Empty, resultado.Mensaje ?? "No se pudo crear el incidente.");
            foreach (var error in resultado.Errores)
                ModelState.AddModelError(string.Empty, error);
            return View(vm);
        }

        TempData["Exito"] = $"Incidente #{resultado.Datos.Id} creado.";
        return RedirectToAction(nameof(Detalle), new { id = resultado.Datos.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Asignar(int id, int tecnicoId)
    {
        var resultado = await api.AsignarAsync(id, tecnicoId);
        return await Resultado(resultado, id, $"Técnico {resultado.Datos?.Tecnico} asignado.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, string nuevoEstado)
    {
        var resultado = await api.CambiarEstadoAsync(id, nuevoEstado);
        return await Resultado(resultado, id, $"Estado actualizado a {Formato.Estado(nuevoEstado)}.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await api.EliminarAsync(id);
        if (resultado.NoAutenticado)
            return await SesionExpirada();

        if (!resultado.Exito)
        {
            TempData["Error"] = resultado.Mensaje;
            return RedirectToAction(nameof(Detalle), new { id });
        }

        TempData["Exito"] = $"Incidente #{id} eliminado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Resultado<T>(ApiResultado<T> resultado, int id, string mensajeExito)
    {
        if (resultado.NoAutenticado)
            return await SesionExpirada();

        if (resultado.Exito)
            TempData["Exito"] = mensajeExito;
        else
            TempData["Error"] = resultado.Mensaje;

        return RedirectToAction(nameof(Detalle), new { id });
    }

    private async Task<IActionResult> SesionExpirada()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Error"] = "Tu sesión expiró. Inicia sesión de nuevo.";
        return RedirectToAction("Login", "Account");
    }
}
