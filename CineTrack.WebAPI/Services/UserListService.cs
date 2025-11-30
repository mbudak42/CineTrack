using CineTrack.DataAccess;
using CineTrack.DataAccess.Entities;
using CineTrack.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CineTrack.WebAPI.Services;

public class UserListService
{
	private readonly CineTrackDbContext _context;
	private readonly IHttpContextAccessor _http;

	public UserListService(CineTrackDbContext context, IHttpContextAccessor http)
	{
		_context = context;
		_http = http;
	}

	private int GetUserId()
	{
		var id = _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		return id != null ? int.Parse(id) : 0;
	}

	// 1) Liste oluştur
	public async Task<UserListDto> CreateListAsync(string name, string? description)
	{
		var userId = GetUserId();

		var list = new UserList
		{
			UserId = userId,
			ListName = name,
		};

		_context.UserLists.Add(list);
		await _context.SaveChangesAsync();

		return new UserListDto
		{
			Id = list.Id,
			Name = list.ListName,
			Contents = new()
		};
	}

	// 2) Kullanıcının tüm listelerini getir
	public async Task<List<UserListDto>> GetUserListsAsync()
	{
		var userId = GetUserId();

		var lists = await _context.UserLists
			.Include(l => l.ListContents)
			.ThenInclude(lc => lc.Content)
			.Where(l => l.UserId == userId)
			.ToListAsync();

		return lists.Select(l => new UserListDto
		{
			Id = l.Id,
			Name = l.ListName,
			Contents = l.ListContents.Select(lc => new ContentDto
			{
				Id = lc.ContentId,
				Title = lc.Content.Title,
				ContentType = lc.Content.ContentType,
				CoverUrl = lc.Content.CoverUrl
			}).ToList()
		}).ToList();
	}

	// Mevcut GetUserListsAsync sadece giriş yapan kişiyi getiriyor.
	// Bu yeni metot, ID'si verilen herhangi bir kullanıcının listelerini getirir.
	public async Task<List<UserListDto>> GetListsByUserIdAsync(int targetUserId)
	{
		var lists = await _context.UserLists
			.Include(l => l.ListContents)
			.ThenInclude(lc => lc.Content)
			.Where(l => l.UserId == targetUserId)
			.ToListAsync();

		return lists.Select(l => new UserListDto
		{
			Id = l.Id,
			Name = l.ListName,
			// Açıklama alanı entity'de yoksa boş geçiyoruz, varsa ekleyebilirsiniz
			Description = null,
			Contents = l.ListContents.Select(lc => new ContentDto
			{
				Id = lc.ContentId,
				Title = lc.Content.Title,
				ContentType = lc.Content.ContentType,
				CoverUrl = lc.Content.CoverUrl
			}).ToList()
		}).ToList();
	}

	// 3) Liste detayını getir
	public async Task<UserListDto?> GetListDetailAsync(int listId)
	{
		var list = await _context.UserLists
			.Include(l => l.ListContents)
			.ThenInclude(lc => lc.Content)
			.FirstOrDefaultAsync(l => l.Id == listId);

		if (list == null) return null;

		return new UserListDto
		{
			Id = list.Id,
			Name = list.ListName,
			Contents = list.ListContents.Select(lc => new ContentDto
			{
				Id = lc.ContentId,
				Title = lc.Content.Title,
				ContentType = lc.Content.ContentType,
				CoverUrl = lc.Content.CoverUrl
			}).ToList()
		};
	}

	// 4) Listeye içerik ekle
	public async Task<bool> AddContentToListAsync(int listId, string contentId)
	{
		var userId = GetUserId();
		var list = await _context.UserLists.FirstOrDefaultAsync(l => l.Id == listId && l.UserId == userId);
		if (list == null) return false;

		var exists = await _context.ListContents.AnyAsync(lc => lc.ListId == listId && lc.ContentId == contentId);
		if (exists) return false;

		_context.ListContents.Add(new ListContent
		{
			ListId = listId,
			ContentId = contentId
		});
		await _context.SaveChangesAsync();

		// Feed logu
		_context.ActivityLogs.Add(new ActivityLog
		{
			UserId = userId,
			ActionType = "list_add",
			ContentId = contentId
		});
		await _context.SaveChangesAsync();

		return true;
	}

	// 5) Listeden içerik sil
	public async Task<bool> RemoveContentFromListAsync(int listId, string contentId)
	{
		var userId = GetUserId();
		var list = await _context.UserLists.FirstOrDefaultAsync(l => l.Id == listId && l.UserId == userId);
		if (list == null) return false;

		var lc = await _context.ListContents
			.FirstOrDefaultAsync(lc => lc.ListId == listId && lc.ContentId == contentId);
		if (lc == null) return false;

		_context.ListContents.Remove(lc);
		await _context.SaveChangesAsync();
		return true;
	}

	// 6) Listeyi Tamamen Sil
	public async Task<bool> DeleteListAsync(int listId)
	{
		var userId = GetUserId();
		// Sadece listenin sahibi silebilir
		var list = await _context.UserLists.FirstOrDefaultAsync(l => l.Id == listId && l.UserId == userId);

		if (list == null) return false;

		_context.UserLists.Remove(list);
		await _context.SaveChangesAsync();
		return true;
	}

	// 7) Listeyi Güncelle (Ad ve Açıklama)
	public async Task<UserListDto?> UpdateListAsync(int listId, string newName, string? newDesc)
	{
		var userId = GetUserId();
		var list = await _context.UserLists.FirstOrDefaultAsync(l => l.Id == listId && l.UserId == userId);

		if (list == null) return null;

		list.ListName = newName;
		// Description veritabanında kolon olarak yoksa burayı atlayabilirsiniz, 
		// ancak UserListDto'da Description var görünüyor, Entity'de yoksa Entity'ye de eklenmeli.
		// Mevcut entity yapısına göre sadece ismi güncelliyoruz:

		await _context.SaveChangesAsync();

		return new UserListDto
		{
			Id = list.Id,
			Name = list.ListName,
			// Description entity'de olmadığı için boş dönüyoruz veya client'tan geleni basıyoruz
			Description = newDesc
		};
	}
}
