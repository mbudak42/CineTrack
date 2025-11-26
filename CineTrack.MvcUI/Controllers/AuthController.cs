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

		// 1. API'ye istek at (adresin başında "api/" olduğundan emin olun)
		var json = await _api.PostAsync("api/auth/register", model);

		if (json == null)
		{
			ModelState.AddModelError("", "Kayıt başarısız. E-posta zaten kullanılıyor olabilir.");
			return View(model);
		}

		// 2. Gelen JSON yanıtını işle
		using var doc = JsonDocument.Parse(json);
		var root = doc.RootElement;

		// Token'ı al ve kaydet
		var token = root.GetProperty("token").GetString();

		if (string.IsNullOrEmpty(token))
		{
			ModelState.AddModelError("", "Token alınamadı.");
			return View(model);
		}

		HttpContext.Session.SetString("token", token);
		_api.SetToken(token);

		// 3. Kullanıcı bilgilerini (username, avatarUrl) Session'a kaydet
		if (root.TryGetProperty("user", out var userElement))
		{
			var username = userElement.GetProperty("username").GetString();
			var avatarUrl = userElement.GetProperty("avatarUrl").GetString(); // Kayıtta genelde null döner

			HttpContext.Session.SetString("username", username ?? "");

			// AvatarUrl null olsa bile boş string olarak kaydediyoruz
			// _Layout.cshtml tarafında boşsa "none.png" gösterilecek
			HttpContext.Session.SetString("avatarUrl", avatarUrl ?? "");
		}

		// Ana sayfaya yönlendir
		return RedirectToAction("Index", "Home");
	}

	[HttpGet]
	public IActionResult Login() => View();

	// AuthController.cs

	[HttpPost]
	public async Task<IActionResult> Login(LoginViewModel model)
	{
		if (!ModelState.IsValid) return View(model);

		// API isteği (Burada "api/" düzeltmesini zaten yaptınız)
		var json = await _api.PostAsync("api/auth/login", model);

		if (json == null)
		{
			ModelState.AddModelError("", "E-posta veya şifre hatalı.");
			return View(model);
		}

		using var doc = JsonDocument.Parse(json);
		var root = doc.RootElement; // Root element'i al

		// 1. Token'ı al ve kaydet
		var token = root.GetProperty("token").GetString();
		if (string.IsNullOrEmpty(token))
		{
			ModelState.AddModelError("", "Token alınamadı.");
			return View(model);
		}
		HttpContext.Session.SetString("token", token);
		_api.SetToken(token);

		// 2. Kullanıcı bilgilerini al ve Session'a kaydet
		if (root.TryGetProperty("user", out var userElement))
		{
			var username = userElement.GetProperty("username").GetString();
			var avatarUrl = userElement.GetProperty("avatarUrl").GetString(); // Null olabilir

			// Session'a yaz
			HttpContext.Session.SetString("username", username ?? "");

			// Eğer avatarUrl null ise boş string kaydet, kontrolü View'da yapacağız
			HttpContext.Session.SetString("avatarUrl", avatarUrl ?? "");
		}

		return RedirectToAction("Index", "Home");
	}

	public IActionResult Logout()
	{
		HttpContext.Session.Clear();
		return RedirectToAction("Index", "Home");
	}
}