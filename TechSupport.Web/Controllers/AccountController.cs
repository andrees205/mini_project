using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechSupport.Web.Models;
using TechSupport.Web.Services;

namespace TechSupport.Web.Controllers;

public class AccountController(ApiClient api) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Incidentes");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var resultado = await api.LoginAsync(vm.NombreUsuario, vm.Password);
        if (!resultado.Exito || resultado.Datos is null)
        {
            ModelState.AddModelError(string.Empty, resultado.Mensaje ?? "No se pudo iniciar sesión.");
            vm.Password = string.Empty;
            return View(vm);
        }

        var datos = resultado.Datos;
        var identidad = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, datos.UsuarioId.ToString()),
            new Claim(ClaimTypes.Name, datos.NombreUsuario),
            new Claim(ClaimTypes.Role, datos.Rol),
            new Claim(TokenHandler.ClaimToken, datos.Token)
        ], CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            new AuthenticationProperties { ExpiresUtc = datos.Expira, IsPersistent = false });

        return Url.IsLocalUrl(vm.ReturnUrl)
            ? LocalRedirect(vm.ReturnUrl)
            : RedirectToAction("Index", "Incidentes");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccesoDenegado() => View();
}
