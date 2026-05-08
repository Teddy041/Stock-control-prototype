using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockControlPrototype.Data;
using StockControlPrototype.Models;
using StockControlPrototype.ViewModels;

namespace StockControlPrototype.Controllers;

public class ProductsController(
    AppDbContext context,
    IWebHostEnvironment environment,
    ILogger<ProductsController> logger) : Controller
{
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var products = await context.Products
            .OrderBy(p => p.Name)
            .ToListAsync();

        return View(products);
    }

    [Authorize]
    public async Task<IActionResult> Catalog(string? q)
    {
        var query = context.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Code.ToLower().Contains(term) ||
                p.Category.ToLower().Contains(term));
        }

        var grouped = await query
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync();

        var vm = grouped
            .GroupBy(p => p.Category)
            .ToDictionary(g => g.Key, g => g.ToList());

        ViewBag.Query = q;
        return View(vm);
    }

    [Authorize]
    public async Task<IActionResult> Details(int id)
    {
        var product = await context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        var vm = new ProductDetailsVm
        {
            Product = product,
            Movements = await context.StockMovements
                .Where(m => m.ProductId == id)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync()
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        ViewBag.Categories = context.Categories
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .ToArray();
        return View(new Product { Category = context.Categories.OrderBy(c => c.Name).Select(c => c.Name).FirstOrDefault() ?? "Diger" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = context.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToArray();
            return View(product);
        }

        try
        {
            product.ImagePath = await SaveImageAsync(imageFile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Image upload failed while creating product.");
            ModelState.AddModelError(string.Empty, ex is InvalidOperationException
                ? ex.Message
                : "Gorsel yuklenirken beklenmeyen bir hata olustu. Lutfen tekrar deneyin.");
            ViewBag.Categories = context.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToArray();
            return View(product);
        }

        context.Products.Add(product);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        ViewBag.Categories = context.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToArray();
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile)
    {
        if (id != product.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = context.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToArray();
            return View(product);
        }

        var existing = await context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (existing is null)
        {
            return NotFound();
        }

        string? newImagePath;
        try
        {
            newImagePath = await SaveImageAsync(imageFile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Image upload failed while editing product {ProductId}.", id);
            ModelState.AddModelError(string.Empty, ex is InvalidOperationException
                ? ex.Message
                : "Gorsel yuklenirken beklenmeyen bir hata olustu. Lutfen tekrar deneyin.");
            ViewBag.Categories = context.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToArray();
            return View(product);
        }

        product.ImagePath = string.IsNullOrWhiteSpace(newImagePath) ? existing.ImagePath : newImagePath;

        context.Products.Update(product);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        context.Products.Remove(product);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddMovement(int productId, MovementType type, int amount, string? description)
    {
        var product = await context.Products.FindAsync(productId);
        if (product is null)
        {
            return NotFound();
        }

        if (amount <= 0)
        {
            TempData["Error"] = "Miktar 0'dan buyuk olmali.";
            return RedirectToAction(nameof(Details), new { id = productId });
        }

        if (type == MovementType.Exit && product.Quantity < amount)
        {
            TempData["Error"] = "Yetersiz stok.";
            return RedirectToAction(nameof(Details), new { id = productId });
        }

        product.Quantity += type == MovementType.Entry ? amount : -amount;

        var movement = new StockMovement
        {
            ProductId = productId,
            Type = type,
            Amount = amount,
            Description = description
        };

        context.StockMovements.Add(movement);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = productId });
    }

    private async Task<string?> SaveImageAsync(IFormFile? imageFile)
    {
        if (imageFile is null || imageFile.Length == 0)
        {
            return null;
        }

        const long maxFileSize = 2 * 1024 * 1024;
        if (imageFile.Length > maxFileSize)
        {
            throw new InvalidOperationException("Gorsel boyutu en fazla 2 MB olabilir.");
        }

        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        var allowedExtensions = new HashSet<string> { ".png", ".jpg", ".jpeg", ".webp" };
        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Sadece .png, .jpg, .jpeg veya .webp dosyalari yuklenebilir.");
        }

        var webRootPath = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(environment.ContentRootPath, "wwwroot");
        }

        var uploadsPath = Path.Combine(webRootPath, "uploads");
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsPath, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await imageFile.CopyToAsync(stream);

        return $"/uploads/{fileName}";
    }
}
