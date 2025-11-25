using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CineTrack.MvcUI.Services;

public class ApiService
{
    private readonly HttpClient _client;
    private readonly IHttpContextAccessor _contextAccessor;

    public ApiService(HttpClient client, IHttpContextAccessor contextAccessor)
    {
        _client = client;
        _contextAccessor = contextAccessor;
    }

    public void SetToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    // Session'dan token'ı otomatik yükle
    private void LoadTokenFromSession()
    {
        var token = _contextAccessor.HttpContext?.Session.GetString("token");
        if (!string.IsNullOrEmpty(token))
        {
            SetToken(token);
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        LoadTokenFromSession();
        
        var response = await _client.GetAsync(endpoint);
        if (!response.IsSuccessStatusCode) return default;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
    }

    public async Task<string?> PostAsync<T>(string endpoint, T data)
{
    LoadTokenFromSession();
    
    var content = new StringContent(
        JsonSerializer.Serialize(data), 
        Encoding.UTF8, 
        "application/json"
    );
    
    var response = await _client.PostAsync(endpoint, content);
    
    // ✨ EKLEME: Hata detayını logla
    var responseBody = await response.Content.ReadAsStringAsync();
    
    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"❌ API Hatası: {response.StatusCode}");
        Console.WriteLine($"📄 Hata Detayı: {responseBody}");
        return null;
    }

    return responseBody;
}

    public async Task<bool> DeleteAsync(string endpoint)
    {
        LoadTokenFromSession();
        
        var response = await _client.DeleteAsync(endpoint);
        return response.IsSuccessStatusCode;
    }
}