using System.Net;
using System.Net.Mail;

namespace CineTrack.WebAPI.Services;

public class EmailService
{
	private readonly IConfiguration _config;

	public EmailService(IConfiguration config)
	{
		_config = config;
	}

	public async Task SendEmailAsync(string toEmail, string subject, string body)
	{
		var smtpServer = _config["EmailSettings:SmtpServer"];
		var port = int.Parse(_config["EmailSettings:Port"] ?? "587");
		var senderEmail = _config["EmailSettings:SenderEmail"];
		var password = _config["EmailSettings:Password"];

		using var client = new SmtpClient(smtpServer, port)
		{
			Credentials = new NetworkCredential(senderEmail, password),
			EnableSsl = true
		};

		var mailMessage = new MailMessage(senderEmail!, toEmail, subject, body)
		{
			IsBodyHtml = true
		};

		await client.SendMailAsync(mailMessage);
	}
}