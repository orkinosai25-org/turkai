using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TurkAI.Web.Models;
using TurkAI.Web.Services;

namespace TurkAI.Web.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly IUserService _users;

    public RegisterModel(IUserService users)
    {
        _users = users;
    }

    public async Task<IActionResult> OnPostAsync(
        string displayName, string email, string password, string plan, string? returnUrl = "/")
    {
        if (await _users.FindByEmailAsync(email) is not null)
        {
            TempData["RegisterError"] = "An account with this email already exists.";
            return LocalRedirect($"/register?plan={Uri.EscapeDataString(plan)}&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
        }

        if (!Enum.TryParse<SubscriptionPlan>(plan, true, out var subscriptionPlan))
            subscriptionPlan = SubscriptionPlan.Free;

        var user = await _users.CreateAsync(new AppUser
        {
            DisplayName = displayName,
            Email = email,
            PasswordHash = _users.HashPassword(password),
            Plan = subscriptionPlan,
            ApiKey = subscriptionPlan == SubscriptionPlan.Enterprise
                ? _users.GenerateApiKey()
                : null,
        });

        var claims = LoginModel.BuildClaims(user);
        var identity = new ClaimsIdentity(claims, "TurkAI");
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync("TurkAI.Cookie", principal,
            new AuthenticationProperties { IsPersistent = true });

        return LocalRedirect(returnUrl ?? "/");
    }
}
