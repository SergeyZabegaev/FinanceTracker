using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using FinanceTracker.Data;
using FinanceTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CategoriesController(ApplicationDbContext context,
                                UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Categories
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var categories = await _context.Categories
            .Where(c => c.UserId == userId || c.UserId == null)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .ToListAsync();
        return View(categories);
    }

    // GET: Categories/Create
    public IActionResult Create() => View();

    // POST: Categories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Type")] Category category)
    {
        if (ModelState.IsValid)
        {
            category.UserId = _userManager.GetUserId(User);
            _context.Add(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    // POST: Categories/Delete
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = _userManager.GetUserId(User);

        // Удалять можно только свои категории; системные (UserId == null) — нельзя
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (category == null)
        {
            TempData["ErrorMessage"] = "Категория не найдена или является системной.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] =
                "Невозможно удалить категорию: она используется в операциях или бюджетах.";
        }

        return RedirectToAction(nameof(Index));
    }
}