using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Generates a JWT token using username and password
    /// </summary>
    [HttpPost("GenerateToken")]
    public IActionResult GenerateToken([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { Message = "Username and password are required." });
        }

        // Dummy authentication - Replace with actual user validation
        if (request.Username == "admin" && request.Password == "password")
        {
            var token = CreateToken(request.Username);
            return Ok(new { Token = token });
        }

        return Unauthorized(new { Message = "Invalid username or password" });
    }

    /// <summary>
    /// Creates the JWT token
    /// </summary>
    private string CreateToken(string username)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim("GeneratedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["DurationInMinutes"])),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>
/// Login Request Model
/// </summary>
public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}
