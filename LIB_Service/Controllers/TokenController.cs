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
    public IActionResult GenerateToken()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Unauthorized(new { Message = "Authorization header is missing" });
        }

        var authHeader = Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Basic "))
        {
            return Unauthorized(new { Message = "Invalid authorization scheme" });
        }

        // Decode Base64-encoded credentials
        var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
        var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));

        // Split into username and password
        var parts = credentials.Split(':', 2);
        if (parts.Length != 2)
        {
            return Unauthorized(new { Message = "Invalid credentials format" });
        }

        string username = parts[0];
        string password = parts[1];

        // Dummy authentication - Replace with actual user validation
        if (username == "LibET_SecureUser_2024_XYZ!@#" && password == "LibET@2024!SuperSecure#X9%^&*A1b2C3d4E5")
        {
            var token = CreateToken(username);
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
