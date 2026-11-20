using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using FinanceTracker.Data;
using FinanceTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext context,
                               UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public IActionResult Index() => View();

    // GET: Dashboard/GetMonthlyData
    [HttpGet]
    public async Task<IActionResult> GetMonthlyData()
    {
        var today = DateTime.Today;
        var start = new DateTime(today.Year, today.Month, 1).AddMonths(-11);
        var userId = _userManager.GetUserId(User);

        var raw = await _context.Transactions
            .Where(t => t.UserId == userId && t.Date >= start)
            .Select(t => new { t.Date, t.Type, t.Amount })
            .ToListAsync();

        var grouped = raw
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Income = g.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount),
                Expense = g.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        var labels = new List<string>();
        var incomes = new List<decimal>();
        var expenses = new List<decimal>();

        for (int i = 0; i < 12; i++)
        {
            var d = start.AddMonths(i);
            var item = grouped.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month);
            labels.Add(d.ToString("MMM yyyy"));
            incomes.Add(item?.Income ?? 0m);
            expenses.Add(item?.Expense ?? 0m);
        }

        return Json(new { labels, incomes, expenses });
    }

    // GET: Dashboard/GetExpensesByCategory
    [HttpGet]
    public async Task<IActionResult> GetExpensesByCategory()
    {
        var today = DateTime.Today;
        var start = new DateTime(today.Year, today.Month, 1).AddMonths(-11);
        var userId = _userManager.GetUserId(User);

        var raw = await _context.Transactions
            .Where(t => t.UserId == userId
                        && t.Date >= start
                        && t.Type == TransactionType.Expense)
            .Select(t => new { t.CategoryId, t.Amount, CategoryName = t.Category!.Name })
            .ToListAsync();

        var grouped = raw
            .GroupBy(t => new { t.CategoryId, t.CategoryName })
            .Select(g => new
            {
                Category = g.Key.CategoryName ?? "Без категории",
                Sum = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.Sum)
            .ToList();

        return Json(new
        {
            labels = grouped.Select(x => x.Category).ToList(),
            values = grouped.Select(x => x.Sum).ToList()
        });
    }

    // GET: Dashboard/GetTopExpenseCategories
    [HttpGet]
    public async Task<IActionResult> GetTopExpenseCategories()
    {
        var today = DateTime.Today;
        var start = new DateTime(today.Year, today.Month, 1).AddMonths(-11);
        var userId = _userManager.GetUserId(User);

        var raw = await _context.Transactions
            .Where(t => t.UserId == userId
                        && t.Date >= start
                        && t.Type == TransactionType.Expense)
            .Select(t => new { t.Amount, CategoryName = t.Category!.Name })
            .ToListAsync();

        var top5 = raw
            .GroupBy(t => t.CategoryName)
            .Select(g => new
            {
                Category = g.Key ?? "Без категории",
                Sum = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.Sum)
            .Take(5)
            .ToList();

        return Json(new
        {
            labels = top5.Select(x => x.Category).ToList(),
            values = top5.Select(x => x.Sum).ToList()
        });
    }
}