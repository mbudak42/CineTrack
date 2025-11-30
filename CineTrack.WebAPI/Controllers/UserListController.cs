using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CineTrack.WebAPI.Services;

namespace CineTrack.WebAPI.Controllers;

// Parametreleri karşılamak için DTO'lar
public class CreateListDto
{
	public string Name { get; set; }
	public string? Description { get; set; }
}

public class UpdateListDto
{
	public string Name { get; set; }
	public string? Description { get; set; }
}

public class AddContentDto
{
	public string ContentId { get; set; }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserListController : ControllerBase
{
	private readonly UserListService _listService;

	public UserListController(UserListService listService)
	{
		_listService = listService;
	}

	// 1) Liste oluştur (GÜNCELLENDİ)
	[HttpPost]
	public async Task<IActionResult> CreateList([FromBody] CreateListDto body)
	{
		// Artık body.Name diyerek güvenle erişebiliriz
		var list = await _listService.CreateListAsync(body.Name, body.Description);
		return Ok(list);
	}

	// 2) Tüm listeleri getir
	[HttpGet]
	public async Task<IActionResult> GetLists()
	{
		var lists = await _listService.GetUserListsAsync();
		return Ok(lists);
	}

	// 3) Liste detay
	[HttpGet("{id}")]
	public async Task<IActionResult> GetListDetail(int id)
	{
		var list = await _listService.GetListDetailAsync(id);
		if (list == null)
			return NotFound(new { message = "Liste bulunamadi." });
		return Ok(list);
	}

	// 4) Listeye içerik ekle (GÜNCELLENDİ)
	[HttpPost("{id}/add")]
	public async Task<IActionResult> AddContent(int id, [FromBody] AddContentDto body)
	{
		// Artık body.ContentId diyerek güvenle erişebiliriz
		bool result = await _listService.AddContentToListAsync(id, body.ContentId);
		if (!result)
			return BadRequest(new { message = "Icerik eklenemedi veya zaten listede." });

		return Ok(new { message = "Icerik listeye eklendi." });
	}

	// 5) Listeden içerik sil
	[HttpDelete("{id}/remove/{contentId}")]
	public async Task<IActionResult> RemoveContent(int id, string contentId)
	{
		bool result = await _listService.RemoveContentFromListAsync(id, contentId);
		if (!result)
			return BadRequest(new { message = "Icerik kaldirilamadi." });

		return Ok(new { message = "Icerik listeden silindi." });
	}

	[HttpDelete("{id}")]
	public async Task<IActionResult> DeleteList(int id)
	{
		bool result = await _listService.DeleteListAsync(id);
		if (!result) return NotFound(new { message = "Liste bulunamadı veya silme yetkiniz yok." });

		return Ok(new { message = "Liste başarıyla silindi." });
	}

	// 7) Listeyi Güncelle
	[HttpPut("{id}")]
	public async Task<IActionResult> UpdateList(int id, [FromBody] UpdateListDto body)
	{
		var result = await _listService.UpdateListAsync(id, body.Name, body.Description);
		if (result == null) return NotFound(new { message = "Liste bulunamadı." });

		return Ok(new { success = true, message = "Liste güncellendi.", data = result });
	}
}
