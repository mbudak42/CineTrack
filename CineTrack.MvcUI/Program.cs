using CineTrack.MvcUI.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// DataProtection - session cookie hatalarını önler
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "DataProtectionKeys")))
    .SetApplicationName("CineTrack.MvcUI");

// Session
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".CineTrack.Session";
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// HttpClient + ApiService
builder.Services.AddHttpClient<ApiService>(client =>
{
    // WebAPI'nin çalıştığı portu buraya yaz
    client.BaseAddress = new Uri("http://localhost:5011/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession(); // ← önemli: routing'den sonra, endpoint'lerden önce
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();