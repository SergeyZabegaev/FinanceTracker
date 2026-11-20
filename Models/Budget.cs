using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Models;

public class Budget
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    [Required(ErrorMessage = "Категория обязательна")]
    [Display(Name = "Категория")]
    public int CategoryId { get; set; }

    public Category? Category { get; set; }

    [Required(ErrorMessage = "Лимит обязателен")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Лимит должен быть больше нуля")]
    [Display(Name = "Лимит на месяц")]
    public decimal MonthlyLimit { get; set; }

    [Required]
    [Range(2000, 2100, ErrorMessage = "Год должен быть в разумных пределах")]
    [Display(Name = "Год")]
    public int Year { get; set; }

    [Required]
    [Range(1, 12, ErrorMessage = "Месяц должен быть от 1 до 12")]
    [Display(Name = "Месяц")]
    public int Month { get; set; }
}