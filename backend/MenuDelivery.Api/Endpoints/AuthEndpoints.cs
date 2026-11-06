using MenuDelivery.Api.Data;
using MenuDelivery.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MenuDelivery.Api.Endpoints;

public record RegisterDto(string Email, string Password);

public record LoginDto(string Email, string Password);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        // Регистрация
        group.MapPost("/register", async (
            RegisterDto dto,
            ApplicationDbContext db,
            IPasswordHasher<User> hasher) =>
        {
            var email = dto.Email.Trim().ToLower();

            if (await db.Users.AnyAsync(u => u.Email == email))
                return Results.Conflict("Пользователь с таким email уже существует.");

            var user = new User
            {
                Email = email
            };

            user.PasswordHash = hasher.HashPassword(user, dto.Password);

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/auth/{user.Id}",
                new { user.Id, user.Email });
        });

        // Вход
        group.MapPost("/login", async (
            LoginDto dto,
            ApplicationDbContext db,
            IPasswordHasher<User> hasher,
            IConfiguration configuration) =>
        {
            var email = dto.Email.Trim().ToLower();

            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return Results.Unauthorized();

            var result = hasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                dto.Password);

            if (result == PasswordVerificationResult.Failed)
                return Results.Unauthorized();

            var key = configuration["Jwt:Key"]!;
            var issuer = configuration["Jwt:Issuer"]!;

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key));

            var credentials = new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler()
                .WriteToken(token);

            return Results.Ok(new
            {
                token = tokenString,
                userId = user.Id,
                email = user.Email
            });
        });
    }
}
