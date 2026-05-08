using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockControlPrototype.Data;
using StockControlPrototype.Models;

namespace StockControlPrototype.Controllers;

[Authorize(Roles = "Admin")]
public class CategoriesController(AppDbContext context) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categories = await context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();
        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            TempData["CategoryError"] = "Kategori adi bos olamaz.";
            return RedirectToAction(nameof(Index));
        }

        var exists = await context.Categories.AnyAsync(c => c.Name.ToLower() == trimmedName.ToLower());
        if (exists)
        {
            TempData["CategoryError"] = "Bu kategori zaten mevcut.";
            return RedirectToAction(nameof(Index));
        }

        context.Categories.Add(new Category { Name = trimmedName });
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await context.Categories.FindAsync(id);
        if (category is null)
        {
            return RedirectToAction(nameof(Index));
        }

        var isUsed = await context.Products.AnyAsync(p => p.Category == category.Name);
        if (isUsed)
        {
            TempData["CategoryError"] = "Bu kategori urunlerde kullanildigi icin silinemez.";
            return RedirectToAction(nameof(Index));
        }

        context.Categories.Remove(category);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
