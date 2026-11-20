using FinanceTracker.Data;
using FinanceTracker.Models;
using FinanceTracker.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace FinanceTracker.Controllers;

[Authorize]
public class TransactionsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TransactionsController(ApplicationDbContext context,
                                  UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Transactions
    public async Task<IActionResult> Index(
        DateTime? startDate,
        DateTime? endDate,
        string? typeFilter,
        int page = 1,
        int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        startDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        endDate ??= DateTime.Now;

        var userId = _userManager.GetUserId(User);

        var baseQuery = _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId)
            .Where(t => t.Date >= startDate && t.Date <= endDate);

        if (!string.IsNullOrEmpty(typeFilter) &&
            Enum.TryParse<TransactionType>(typeFilter, out var parsedType))
        {
            baseQuery = baseQuery.Where(t => t.Type == parsedType);
        }

        // SQLite не умеет Sum по decimal — считаем в памяти
        var rawData = await baseQuery
            .Select(t => new { t.Type, t.Amount })
            .ToListAsync();

        var totalIncome = rawData
            .Where(x => x.Type == TransactionType.Income)
            .Sum(x => x.Amount);

        var totalExpense = rawData
            .Where(x => x.Type == TransactionType.Expense)
            .Sum(x => x.Amount);

        var totalCount = await baseQuery.CountAsync();

        var transactions = await baseQuery
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var viewModel = new TransactionsIndexViewModel
        {
            Transactions = transactions,
            Categories = await _context.Categories
                .Where(c => c.UserId == userId || c.UserId == null)
                .ToListAsync(),
            StartDate = startDate.Value,
            EndDate = endDate.Value,
            TypeFilter = typeFilter,
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return View(viewModel);
    }

    // GET: Transactions/Create
    public async Task<IActionResult> Create()
    {
        var userId = _userManager.GetUserId(User);
        ViewBag.Categories = await _context.Categories
            .Where(c => c.UserId == userId || c.UserId == null)
            .ToListAsync();
        return View();
    }

    // POST: Transactions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Amount,Date,Type,CategoryId,Description")] Transaction transaction)
    {
        var userId = _userManager.GetUserId(User);

        // Проверка: категория должна принадлежать пользователю или быть системной
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == transaction.CategoryId
                                   && (c.UserId == userId || c.UserId == null));
        if (category == null)
        {
            ModelState.AddModelError(nameof(Transaction.CategoryId), "Категория недоступна");
        }

        if (ModelState.IsValid)
        {
            transaction.UserId = userId;
            _context.Add(transaction);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Categories = await _context.Categories
            .Where(c => c.UserId == userId || c.UserId == null)
            .ToListAsync();
        return View(transaction);
    }

    // GET: Transactions/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var userId = _userManager.GetUserId(User);

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (transaction == null) return NotFound();

        ViewBag.Categories = await _context.Categories
            .Where(c => c.UserId == userId || c.UserId == null)
            .ToListAsync();
        return View(transaction);
    }

    // POST: Transactions/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("Id,Amount,Date,Type,CategoryId,Description")] Transaction transaction)
    {
        if (id != transaction.Id) return NotFound();

        var userId = _userManager.GetUserId(User);

        // Проверка владельца
        var existing = await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (existing == null) return NotFound();

        // Проверка категории
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == transaction.CategoryId
                                   && (c.UserId == userId || c.UserId == null));
        if (category == null)
            ModelState.AddModelError(nameof(Transaction.CategoryId), "Категория недоступна");

        if (ModelState.IsValid)
        {
            try
            {
                transaction.UserId = userId;
                _context.Update(transaction);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TransactionExists(transaction.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Categories = await _context.Categories
            .Where(c => c.UserId == userId || c.UserId == null)
            .ToListAsync();
        return View(transaction);
    }

    // POST: Transactions/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = _userManager.GetUserId(User);

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (transaction != null)
        {
            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
    // GET: Transactions/ExportCsv?startDate=...&endDate=...&typeFilter=...
    public async Task<IActionResult> ExportCsv(
        DateTime? startDate,
        DateTime? endDate,
        string? typeFilter)
    {
        // Те же значения по умолчанию, что и в Index
        startDate ??= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        endDate ??= DateTime.Now;

        var userId = _userManager.GetUserId(User);

        var query = _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId)
            .Where(t => t.Date >= startDate && t.Date <= endDate);

        if (!string.IsNullOrEmpty(typeFilter) &&
            Enum.TryParse<TransactionType>(typeFilter, out var parsedType))
        {
            query = query.Where(t => t.Type == parsedType);
        }

        var transactions = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToListAsync();

        // Формируем CSV
        var sb = new StringBuilder();
        sb.AppendLine("Дата;Тип;Категория;Описание;Сумма");

        foreach (var t in transactions)
        {
            var type = t.Type == TransactionType.Income ? "Доход" : "Расход";
            var category = t.Category?.Name ?? "Без категории";
            var description = (t.Description ?? string.Empty)
                .Replace(";", ",")          // защита от поломки CSV
                .Replace("\r", " ")
                .Replace("\n", " ");
            var amount = t.Amount.ToString("0.00",
                System.Globalization.CultureInfo.InvariantCulture);

            sb.AppendLine($"{t.Date:yyyy-MM-dd};{type};{category};{description};{amount}");
        }

        // BOM (EF BB BF) нужен, чтобы Excel корректно открыл кириллицу
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var bytes = preamble.Concat(body).ToArray();

        var fileName = $"transactions_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";

        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
    private bool TransactionExists(int id) =>
        _context.Transactions.Any(e => e.Id == id);
}
