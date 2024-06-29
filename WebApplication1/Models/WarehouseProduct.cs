using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class WarehouseProduct
{
    [Required] public required int IdProduct { get; set; }

    [Required] public required int IdWarehouse { get; set; }

    [Required] [Range(1, int.MaxValue)] public required int Amount { get; set; }

    [Required] public required DateTime CreatedAt { get; set; }
}