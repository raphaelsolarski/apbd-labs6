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
        try
        {
            var warehouseProductId = warehouseService.AddProductToWarehouse(warehouseProduct);
            return StatusCode(StatusCodes.Status201Created, new { Id = warehouseProductId });
        }
        catch (Exception e)
        {
            if (e is NotExistingProductException || e is NotExistingWarehouseException ||
                e is NotExistingMatchingOrderException || e is OrderAlreadyFulfilledException)
            {
                return BadRequest(e.Message);
            }
            //rethrow without specified response code - it should be handled globaly
            throw new Exception("Unexpected exception during adding product to warehouse", e);
        }
    }
}