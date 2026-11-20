using FinanceTracker.Models;

namespace FinanceTracker.ViewModels;

public class TransactionsIndexViewModel
{
    // Данные
    public List<Transaction> Transactions { get; set; } = new();
    public List<Category> Categories { get; set; } = new();

    // Фильтры
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? TypeFilter { get; set; }

    // Итоги за весь период (не за страницу!)
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance => TotalIncome - TotalExpense;

    // Пагинация
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => TotalCount == 0
        ? 1
        : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
