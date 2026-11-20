namespace FinanceTracker.ViewModels;

public class BudgetsIndexViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<BudgetItemViewModel> Items { get; set; } = new();
    public decimal TotalLimit { get; set; }
    public decimal TotalSpent { get; set; }
}

public class BudgetItemViewModel
{
    public int BudgetId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal MonthlyLimit { get; set; }
    public decimal Spent { get; set; }

    // Проценты выполнения (может быть > 100 при перерасходе)
    public int Percent => MonthlyLimit <= 0 ? 0
        : (int)Math.Round(Spent / MonthlyLimit * 100);

    public bool IsOverBudget => Spent > MonthlyLimit;
    public decimal Remaining => MonthlyLimit - Spent;

    // Цвет прогресс-бара
    public string ProgressClass
    {
        get
        {
            if (IsOverBudget) return "bg-danger";
            if (Percent >= 80) return "bg-warning";
            return "bg-success";
        }
    }
}