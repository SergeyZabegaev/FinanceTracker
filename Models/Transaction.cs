using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Models;

public class Transaction
{
    public int Id { get; set; }
    public string? UserId { get; set; }

    [Required(ErrorMessage = "Сумма обязательна")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Сумма должна быть больше нуля")]
    [Display(Name = "Сумма")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Дата обязательна")]
    [Display(Name = "Дата")]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Тип операции обязателен")]
    [Display(Name = "Тип операции")]
    public TransactionType Type { get; set; }

    [Required(ErrorMessage = "Категория обязательна")]
    [Display(Name = "Категория")]
    public int CategoryId { get; set; }

    // Навигационное свойство для EF Core
    public Category? Category { get; set; }

    [Display(Name = "Описание")]
    public string? Description { get; set; }
}