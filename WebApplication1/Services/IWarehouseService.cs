using WebApplication1.Models;
using WebApplication1.Repository;

namespace WebApplication1.Services;

public interface IWarehouseService
{
    int AddProductToWarehouse(WarehouseProduct warehouseProduct);
}