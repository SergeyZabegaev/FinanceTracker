using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Models;

public class Category
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    [Required(ErrorMessage = "Название категории обязательно")]
    [Display(Name = "Название категории")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Тип")]
    public TransactionType Type { get; set; }
}