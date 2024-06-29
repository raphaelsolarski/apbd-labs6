using WebApplication1.Models;

namespace WebApplication1.Repository;

public interface IWarehouseRepository
{
    int AddProductToWarehouse(WarehouseProduct warehouseProduct);
}