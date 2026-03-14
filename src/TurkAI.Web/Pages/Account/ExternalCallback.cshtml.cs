using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TurkAI.Web.Models;
using TurkAI.Web.Services;

namespace TurkAI.Web.Pages.Account;

public class ExternalCallbackModel : PageModel
{
    private readonly IUserService _users;

    public ExternalCallbackModel(IUserService users)
    {
        _users = users;
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = "/")
    {
        var result = await HttpContext.AuthenticateAsync("ExternalCookie");
        if (!result.Succeeded) return Redirect("/login?error=external_auth_failed");

        var externalClaims = result.Principal!;
        var provider = result.Properties!.Items[".AuthScheme"] ?? "External";
        var providerId = externalClaims.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var email = externalClaims.FindFirstValue(ClaimTypes.Email) ?? "";
        var displayName = externalClaims.FindFirstValue(ClaimTypes.Name) ?? email;
        var avatar = externalClaims.FindFirstValue("urn:google:picture")
                  ?? externalClaims.FindFirstValue("picture")
                  ?? "";

        // Look up existing user by external provider
        var user = await _users.FindByExternalProviderAsync(provider, providerId);

        if (user is null)
        {
            // Check if email is already registered
            user = await _users.FindByEmailAsync(email);
            if (user is null)
            {
                // Create new account
                user = await _users.CreateAsync(new AppUser
                {
                    Email = email,
                    DisplayName = displayName,
                    AvatarUrl = avatar,
                    ExternalProvider = provider,
                    ExternalProviderId = providerId,
                    Plan = SubscriptionPlan.Free,
                });
            }
            else
            {
                // Link external provider to existing account
                user.ExternalProvider = provider;
                user.ExternalProviderId = providerId;
                if (string.IsNullOrEmpty(user.AvatarUrl)) user.AvatarUrl = avatar;
                await _users.UpdateAsync(user);
            }
        }

        await HttpContext.SignOutAsync("ExternalCookie");

        var claims = LoginModel.BuildClaims(user);
        var identity = new ClaimsIdentity(claims, "TurkAI");
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync("TurkAI.Cookie", principal,
            new AuthenticationProperties { IsPersistent = true });

        return LocalRedirect(returnUrl ?? "/");
    }
}
