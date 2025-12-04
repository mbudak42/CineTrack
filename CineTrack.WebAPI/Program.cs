using CineTrack.DataAccess;
using Microsoft.EntityFrameworkCore;
using CineTrack.WebAPI.Helpers;
using CineTrack.WebAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1) DbContext
builder.Services.AddDbContext<CineTrackDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("CineTrackDb")));

// 2) Servis kayıtları
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ContentService>();
builder.Services.AddScoped<RatingService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<FeedService>();
builder.Services.AddScoped<UserListService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

// 3) JWT kimlik doğrulama
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = builder.Configuration["Jwt:Issuer"],
			ValidAudience = builder.Configuration["Jwt:Audience"],
			IssuerSigningKey = new SymmetricSecurityKey(
				Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
			)
		};
	});

// 4) Controller ve Swagger/OpenAPI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 5) Middleware sırası
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
