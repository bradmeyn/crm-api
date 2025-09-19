using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using CrmApi.Data;
using CrmApi.Models;
using CrmApi.DTOs.Auth;
using CrmApi.Services; 

namespace CrmApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AuthController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<User> userManager,
        ILogger<AuthController> logger,
        ApplicationDbContext context,
        IJwtTokenService jwtTokenService) 
    {
        _userManager = userManager;
        _logger = logger;
        _context = context;
        _jwtTokenService = jwtTokenService; 
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto data)
    {
        _logger.LogInformation("Register attempt for email: {Email}", data.Email);

        // 1. Check for duplicate email
        var existingUser = await _userManager.FindByEmailAsync(data.Email);
        if (existingUser != null)
        {
            return BadRequest(new { error = "Email is already registered" });
        }

        // 2. Create business first
        var business = new Business
        {
            Name = data.BusinessName,
            Email = data.Email
        };

        _context.Businesses.Add(business);
        await _context.SaveChangesAsync();

        // 3. Create user with BusinessId
        var user = new User
        {
            UserName = data.Email,
            Email = data.Email,
            FirstName = data.FirstName,
            LastName = data.LastName,
            BusinessId = business.Id, 
        };

        var result = await _userManager.CreateAsync(user, data.Password);
        if (!result.Succeeded)
        {

            // Clean up the business if user creation failed
            _context.Businesses.Remove(business);
            await _context.SaveChangesAsync();
            
            return BadRequest(new
            {
                message = "User creation failed",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        // 4. Assign role
        await _userManager.AddToRoleAsync(user, "Admin");

        // 5. Generate JWT token for immediate login
        try
        {
            var tokenResponse = await _jwtTokenService.GenerateTokenAsync(user);

   

            // Return token response with user data
            return Ok(new AuthResponseDto
            {
                Token = tokenResponse.Token,
                RefreshToken = tokenResponse.RefreshToken,
                ExpiresIn = tokenResponse.ExpiresIn,
                TokenType = tokenResponse.TokenType,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    BusinessId = user.BusinessId
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT generation failed for newly registered user {Email}", user.Email);

            return Ok(new
            {
                message = "Registration successful but login failed",
                userId = user.Id,
                businessId = business.Id,
                error = "Please log in manually"
            });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto data)
    {
        _logger.LogInformation("Login attempt for email: {Email}", data.Email);

        var user = await _userManager.FindByEmailAsync(data.Email);

        if(user == null)
        {
            _logger.LogWarning("Login failed for {Email}: user not found", data.Email);
            return BadRequest(new { error = "Invalid email or password" });
        }

        // Check user exists and password is correct
        if (!await _userManager.CheckPasswordAsync(user, data.Password))
        {
            _logger.LogWarning("Invalid login attempt for {Email}", data.Email);
            return BadRequest(new { error = "Invalid email or password" });
        }

        // Generate JWT token
        try
        {
            var tokenResponse = await _jwtTokenService.GenerateTokenAsync(user);
            _logger.LogInformation("User {Email} logged in successfully", user.Email);
            
            // Return token response with user data
            return Ok(new AuthResponseDto
            {
                Token = tokenResponse.Token,
                RefreshToken = tokenResponse.RefreshToken,
                ExpiresIn = tokenResponse.ExpiresIn,
                TokenType = tokenResponse.TokenType,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    BusinessId = user.BusinessId
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT generation failed for {Email}", user.Email);
            return StatusCode(500, new { error = "Authentication failed" });
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // For JWT, logout is handled client-side by discarding the token
        // You could implement token blacklisting here if needed
        _logger.LogInformation("Logout endpoint called");
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken(RefreshTokenDto data)
    {
        try
        {
            var tokenResponse = await _jwtTokenService.RefreshTokenAsync(data.RefreshToken);
            
            // For refresh, you might want to include updated user data too
            // Get the user ID from the token response or decode from refresh token
            return Ok(tokenResponse);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token refresh failed");
            return BadRequest(new { error = "Invalid refresh token" });
        }
    }
}