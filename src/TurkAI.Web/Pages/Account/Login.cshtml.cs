using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TurkAI.Web.Models;
using TurkAI.Web.Services;

namespace TurkAI.Web.Pages.Account;

public class LoginModel : PageModel
{
    private readonly IUserService _users;

    public LoginModel(IUserService users)
    {
        _users = users;
    }

    // Called by social-login buttons: /Account/Login?provider=Google&returnUrl=/chat
    public IActionResult OnGet(string provider, string? returnUrl = "/")
    {
        var redirectUrl = Url.Page("/Account/ExternalCallback", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, provider);
    }

    // Called by the email/password Blazor form via query string
    public async Task<IActionResult> OnPostAsync(string email, string password, bool rememberMe, string? returnUrl = "/")
    {
        var user = await _users.ValidatePasswordAsync(email, password);
        if (user is null)
        {
            TempData["LoginError"] = "Invalid email or password.";
            return LocalRedirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
        }

        await SignInUserAsync(user, rememberMe);
        return LocalRedirect(returnUrl ?? "/");
    }

    private async Task SignInUserAsync(AppUser user, bool persistent)
    {
        var claims = BuildClaims(user);
        var identity = new ClaimsIdentity(claims, "TurkAI");
        var principal = new ClaimsPrincipal(identity);
        var props = new AuthenticationProperties { IsPersistent = persistent };
        await HttpContext.SignInAsync("TurkAI.Cookie", principal, props);
    }

    internal static List<Claim> BuildClaims(AppUser user) =>
    [
        new(ClaimTypes.NameIdentifier, user.Id),
        new(ClaimTypes.Email,          user.Email),
        new(ClaimTypes.Name,           user.DisplayName),
        new("plan",                    user.Plan.ToString()),
        new("avatar",                  user.AvatarUrl ?? ""),
    ];
}
