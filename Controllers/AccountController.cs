using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StockControlPrototype.ViewModels;

namespace StockControlPrototype.Controllers;

public class AccountController(SignInManager<IdentityUser> signInManager) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["HideNavbar"] = true;
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["HideNavbar"] = true;
            return View(vm);
        }

        var result = await signInManager.PasswordSignInAsync(vm.UserName, vm.Password, true, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ViewData["HideNavbar"] = true;
            ModelState.AddModelError(string.Empty, "Kullanici adi veya sifre hatali.");
            return View(vm);
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Catalog", "Products");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        ViewData["HideNavbar"] = true;
        return View();
    }
}
