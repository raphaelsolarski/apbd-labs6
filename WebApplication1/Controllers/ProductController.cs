using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[Route("/api/warehouseProducts")]
[ApiController]
public class ProductController(IWarehouseService warehouseService) : ControllerBase
{
    [HttpPost]
    public IActionResult AddProductToWarehouse(WarehouseProduct warehouseProduct)
    {
        var warehouseProductId = warehouseService.AddProductToWarehouse(warehouseProduct);
        return StatusCode(StatusCodes.Status201Created, new { Id = warehouseProductId });
    }
}