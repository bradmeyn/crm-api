using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using CrmApi.Data;
using CrmApi.Models;
using CrmApi.DTOs.Auth;
using CrmApi.Services; 
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CrmApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AuthController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    public AuthController(
        UserManager<User> userManager,
        ILogger<AuthController> logger,
        ApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        IConfiguration config)
    {
        _userManager = userManager;
        _logger = logger;
        _context = context;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _config = config;
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
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, data.Password);
        if (!result.Succeeded)
        {
            _logger.LogError("User creation failed: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));

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

        // 5. Generate email confirmation token
        var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // 6. Create confirmation link
        var confirmationLink = Url.Action(
            nameof(ConfirmEmail),
            "Auth",
            new { userId = user.Id, token = emailToken },
            Request.Scheme);

        // 7. Send confirmation email
        var emailSent = await _emailService.SendEmailConfirmationAsync(user, confirmationLink);

        _logger.LogInformation("User {Email} registered successfully. Email confirmation sent: {EmailSent}",
            user.Email, emailSent);

        return Ok(new
        {
            message = "Registration successful! Please check your email to confirm your account.",
            userId = user.Id,
            businessId = business.Id,
            emailSent = emailSent,
            requiresEmailConfirmation = true
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto data)
    {
        _logger.LogInformation("Login attempt for email: {Email}", data.Email);

        var user = await _userManager.FindByEmailAsync(data.Email);

        if (user == null)
        {
            _logger.LogWarning("Login failed for {Email}: user not found", data.Email);
            return BadRequest(new { error = "Invalid email or password" });
        }

        // Check password
        if (!await _userManager.CheckPasswordAsync(user, data.Password))
        {
            _logger.LogWarning("Invalid login attempt for {Email}", data.Email);
            return BadRequest(new { error = "Invalid email or password" });
        }

        // Check email confirmation
        if (!user.EmailConfirmed)
        {
            return BadRequest(new
            {
                error = "Email not confirmed",
                emailConfirmationRequired = true,
                message = "Please check your email and confirm your account before logging in."
            });
        }

        // Generate JWT token
        try
        {
            var tokenResponse = await _jwtTokenService.GenerateTokenAsync(user);
            _logger.LogInformation("User {Email} logged in successfully", user.Email);

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

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        var frontendUrl = _config["Frontend:BaseUrl"];

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            return Redirect($"{frontendUrl}/email-confirmation?error=invalid-link");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Redirect($"{frontendUrl}/email-confirmation?error=user-not-found");
        }

        if (user.EmailConfirmed)
        {
            return Redirect($"{frontendUrl}/email-confirmed?status=already-confirmed");
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
        {
            try
            {

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendWelcomeEmailAsync(user);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
                    }
                });

                return Redirect($"{frontendUrl}/email-confirmed?status=success");       

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email confirmed but failed to generate login token for {Email}", user.Email);
                return Redirect($"{frontendUrl}/login?emailConfirmed=true");
            }
        }

     
        return Redirect($"{frontendUrl}/email-confirmation?error=confirmation-failed");
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationDto data)
    {
        var user = await _userManager.FindByEmailAsync(data.Email);
        if (user == null)
        {
            // Don't reveal if email exists or not for security
            return Ok(new { message = "If the email exists, a confirmation email has been sent." });
        }

        if (user.EmailConfirmed)
        {
            return BadRequest(new { error = "Email is already confirmed" });
        }

        var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = Url.Action(
            nameof(ConfirmEmail),
            "Auth",
            new { userId = user.Id, token = emailToken },
            Request.Scheme);

        var emailSent = await _emailService.SendEmailConfirmationAsync(user, confirmationLink);

        return Ok(new { message = "Confirmation email sent", emailSent });
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
            if (string.IsNullOrEmpty(data.RefreshToken))
            {
                return BadRequest(new { error = "Refresh token is required" });
            }
            var tokenResponse = await _jwtTokenService.RefreshTokenAsync(data.RefreshToken);

            return Ok(tokenResponse);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token refresh failed");
            return BadRequest(new { error = "Invalid refresh token" });
        }
    }
    
    [HttpGet("user")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId);
        
        if (user == null)
            return NotFound();
            
        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            BusinessId = user.BusinessId
        });
    }   
}