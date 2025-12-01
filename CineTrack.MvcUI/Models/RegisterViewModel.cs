using System.ComponentModel.DataAnnotations; // Bu satırı en üste ekleyin

namespace CineTrack.MvcUI.Models;

public class RegisterViewModel
{
	[Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
	public string Username { get; set; } = string.Empty;

	[Required(ErrorMessage = "E-posta zorunludur.")]
	[EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
	public string Email { get; set; } = string.Empty;

	[Required(ErrorMessage = "Şifre zorunludur.")]
	[MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
	[DataType(DataType.Password)]
	public string Password { get; set; } = string.Empty;

	// YENİ EKLENEN ALAN
	[Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
	[DataType(DataType.Password)]
	[Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
	[Display(Name = "Şifre Tekrar")]
	public string ConfirmPassword { get; set; } = string.Empty;
}