using WebApplication1.Models;
using WebApplication1.Repository;

namespace WebApplication1.Services;

public class WarehouseService(
    IWarehouseRepository warehouseRepository
)
    : IWarehouseService
{
    public int AddProductToWarehouse(WarehouseProduct warehouseProduct)
    {
        return warehouseRepository.AddProductToWarehouse(warehouseProduct);
    }
    
}

public class NotExistingProductException(int id) : Exception($"Product with id {id} does not exist");

public class NotExistingWarehouseException(int id) : Exception($"Warehouse with id {id} does not exist");

public class NotExistingMatchingOrderException() : Exception("There is no matching order for given request");

public class OrderAlreadyFulfilledException(int orderId) : Exception($"Order with id {orderId} is already fullfilled");