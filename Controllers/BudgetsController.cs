using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using FinanceTracker.Data;
using FinanceTracker.Models;
using FinanceTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Controllers;

[Authorize]
public class BudgetsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public BudgetsController(ApplicationDbContext context,
                             UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Budgets?year=2026&month=11
    public async Task<IActionResult> Index(int? year, int? month)
    {
        var today = DateTime.Today;
        var y = year ?? today.Year;
        var m = month ?? today.Month;
        if (m < 1) m = 1;
        if (m > 12) m = 12;

        var userId = _userManager.GetUserId(User);

        var budgets = await _context.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId && b.Year == y && b.Month == m)
            .OrderBy(b => b.Category!.Name)
            .ToListAsync();

        var start = new DateTime(y, m, 1);
        var end = start.AddMonths(1);
        var categoryIds = budgets.Select(b => b.CategoryId).ToList();

        var rawExpenses = await _context.Transactions
            .Where(t => t.UserId == userId
                        && t.Type == TransactionType.Expense
                        && t.Date >= start && t.Date < end
                        && categoryIds.Contains(t.CategoryId))
            .Select(t => new { t.CategoryId, t.Amount })
            .ToListAsync();

        var spentByCategory = rawExpenses
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var items = budgets.Select(b =>
        {
            var spent = spentByCategory.GetValueOrDefault(b.CategoryId, 0m);
            return new BudgetItemViewModel
            {
                BudgetId = b.Id,
                CategoryId = b.CategoryId,
                CategoryName = b.Category?.Name ?? "Без категории",
                MonthlyLimit = b.MonthlyLimit,
                Spent = spent
            };
        }).ToList();

        var vm = new BudgetsIndexViewModel
        {
            Year = y,
            Month = m,
            Items = items,
            TotalLimit = items.Sum(i => i.MonthlyLimit),
            TotalSpent = items.Sum(i => i.Spent)
        };

        return View(vm);
    }

    // GET: Budgets/Create
    public async Task<IActionResult> Create(int? year, int? month)
    {
        var today = DateTime.Today;
        await PopulateCategoriesAsync();
        return View(new Budget
        {
            Year = year ?? today.Year,
            Month = month ?? today.Month
        });
    }

    // POST: Budgets/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("CategoryId,MonthlyLimit,Year,Month")] Budget budget)
    {
        var userId = _userManager.GetUserId(User);

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == budget.CategoryId
                                   && (c.UserId == userId || c.UserId == null));
        if (category == null)
            ModelState.AddModelError(nameof(Budget.CategoryId), "Категория не найдена");
        else if (category.Type != TransactionType.Expense)
            ModelState.AddModelError(nameof(Budget.CategoryId),
                "Бюджет можно задать только для расходной категории");

        var exists = await _context.Budgets.AnyAsync(b =>
            b.UserId == userId &&
            b.CategoryId == budget.CategoryId &&
            b.Year == budget.Year &&
            b.Month == budget.Month);

        if (exists)
            ModelState.AddModelError("",
                "Бюджет для этой категории на выбранный месяц уже существует");

        if (ModelState.IsValid)
        {
            budget.UserId = userId;
            _context.Add(budget);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index),
                new { year = budget.Year, month = budget.Month });
        }

        await PopulateCategoriesAsync();
        return View(budget);
    }

    // GET: Budgets/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (budget == null) return NotFound();

        await PopulateCategoriesAsync();
        return View(budget);
    }

    // POST: Budgets/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("Id,CategoryId,MonthlyLimit,Year,Month")] Budget budget)
    {
        if (id != budget.Id) return NotFound();

        var userId = _userManager.GetUserId(User);

        var existing = await _context.Budgets
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (existing == null) return NotFound();

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == budget.CategoryId
                                   && (c.UserId == userId || c.UserId == null));
        if (category == null)
            ModelState.AddModelError(nameof(Budget.CategoryId), "Категория не найдена");
        else if (category.Type != TransactionType.Expense)
            ModelState.AddModelError(nameof(Budget.CategoryId),
                "Бюджет можно задать только для расходной категории");

        var exists = await _context.Budgets.AnyAsync(b =>
            b.Id != budget.Id &&
            b.UserId == userId &&
            b.CategoryId == budget.CategoryId &&
            b.Year == budget.Year &&
            b.Month == budget.Month);

        if (exists)
            ModelState.AddModelError("",
                "Бюджет для этой категории на выбранный месяц уже существует");

        if (ModelState.IsValid)
        {
            budget.UserId = userId;
            _context.Update(budget);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index),
                new { year = budget.Year, month = budget.Month });
        }

        await PopulateCategoriesAsync();
        return View(budget);
    }

    // POST: Budgets/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = _userManager.GetUserId(User);

        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

        if (budget == null) return RedirectToAction(nameof(Index));

        var y = budget.Year;
        var m = budget.Month;
        _context.Budgets.Remove(budget);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { year = y, month = m });
    }

    private async Task PopulateCategoriesAsync()
    {
        var userId = _userManager.GetUserId(User);
        var categories = await _context.Categories
            .Where(c => (c.UserId == userId || c.UserId == null)
                        && c.Type == TransactionType.Expense)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Categories = new SelectList(categories, "Id", "Name");
    }
}