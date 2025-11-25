using CineTrack.MvcUI.Models;
using CineTrack.MvcUI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CineTrack.MvcUI.Controllers;

public class AuthController : Controller
{
    private readonly ApiService _api;

    public AuthController(ApiService api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var json = await _api.PostAsync("auth/register", model);
        
        if (json == null)
        {
            ModelState.AddModelError("", "Kayıt başarısız. E-posta zaten kullanılıyor olabilir.");
            return View(model);
        }

        // JSON'u parse et
        using var doc = JsonDocument.Parse(json);
        var token = doc.RootElement.GetProperty("token").GetString();

        if (string.IsNullOrEmpty(token))
        {
            ModelState.AddModelError("", "Token alınamadı.");
            return View(model);
        }

        HttpContext.Session.SetString("token", token);
        _api.SetToken(token);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var json = await _api.PostAsync("auth/login", model);
        
        if (json == null)
        {
            ModelState.AddModelError("", "E-posta veya şifre hatalı.");
            return View(model);
        }

        using var doc = JsonDocument.Parse(json);
        var token = doc.RootElement.GetProperty("token").GetString();

        if (string.IsNullOrEmpty(token))
        {
            ModelState.AddModelError("", "Token alınamadı.");
            return View(model);
        }

        HttpContext.Session.SetString("token", token);
        _api.SetToken(token);

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}