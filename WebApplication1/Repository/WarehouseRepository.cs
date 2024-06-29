using Microsoft.Data.SqlClient;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Repository;

public class WarehouseRepository : IWarehouseRepository
{
    private IConfiguration _configuration;

    public WarehouseRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public int AddProductToWarehouse(WarehouseProduct warehouseProduct)
    {
        using var con = new SqlConnection(_configuration["ConnectionStrings:DefaultConnection"]);
        con.Open();
        var transaction = con.BeginTransaction();

        var productPrice = FetchProductPrice(transaction, warehouseProduct.IdProduct);
        if (productPrice == null)
        {
            throw new NotExistingProductException(warehouseProduct.IdProduct);
        }

        if (!WarehouseExists(transaction, warehouseProduct.IdWarehouse))
        {
            throw new NotExistingWarehouseException(warehouseProduct.IdWarehouse);
        }

        var matchingOrderId = FindMatchingOrder(transaction, warehouseProduct.IdProduct, warehouseProduct.Amount,
            warehouseProduct.CreatedAt);
        if (matchingOrderId == null)
        {
            throw new NotExistingMatchingOrderException();
        }

        if (OrderFullfiled(transaction, matchingOrderId.Value))
        {
            throw new OrderAlreadyFulfilledException(matchingOrderId.Value);
        }

        SetOrderFullfiledDate(transaction, matchingOrderId.Value, DateTime.Now);


        var id = InsertWarehouseProduct(transaction, matchingOrderId.Value, productPrice.Value, warehouseProduct);
        transaction.Commit();
        return id;
    }

    private decimal? FetchProductPrice(SqlTransaction transaction, int productId)
    {
        using var cmd = new SqlCommand();
        cmd.Connection = transaction.Connection;
        cmd.Transaction = transaction;
        cmd.CommandText = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
        cmd.Parameters.AddWithValue("@IdProduct", productId);

        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            return dr.GetDecimal(0);
        }

        return null;
    }

    private bool WarehouseExists(SqlTransaction transaction, int warehouseId)
    {
        using var cmd = new SqlCommand();
        cmd.Connection = transaction.Connection;
        cmd.Transaction = transaction;
        cmd.CommandText = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
        cmd.Parameters.AddWithValue("@IdWarehouse", warehouseId);
        using var dr = cmd.ExecuteReader();
        return dr.Read();
    }

    private int? FindMatchingOrder(SqlTransaction transaction, int idProduct, int amount, DateTime requestDateTime)
    {
        using var cmd = new SqlCommand();
        cmd.Connection = transaction.Connection;
        cmd.Transaction = transaction;
        cmd.CommandText =
            "SELECT IdOrder FROM Orders WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @RequestDateTime";
        cmd.Parameters.AddWithValue("@IdProduct", idProduct);
        cmd.Parameters.AddWithValue("@Amount", amount);
        cmd.Parameters.AddWithValue("@RequestDateTime", requestDateTime);

        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            return dr.GetInt32(0);
        }

        return null;
    }


    private bool OrderFullfiled(SqlTransaction transaction, int idOrder)
    {
        using var cmd = new SqlCommand();
        cmd.Connection = transaction.Connection;
        cmd.Transaction = transaction;
        cmd.CommandText = "SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder";
        cmd.Parameters.AddWithValue("@IdOrder", idOrder);
        using var dr = cmd.ExecuteReader();
        return dr.Read();
    }

    private void SetOrderFullfiledDate(SqlTransaction transaction, int idOrder, DateTime dateTime)
    {
        using var cmd = new SqlCommand();
        cmd.Connection = transaction.Connection;
        cmd.Transaction = transaction;
        cmd.CommandText = "UPDATE Orders SET FulfilledAt=@FulfilledDate WHERE IdOrder = @IdOrder";
        cmd.Parameters.AddWithValue("@IdOrder", idOrder);
        cmd.Parameters.AddWithValue("@FulfilledDate", dateTime);

        cmd.ExecuteNonQuery();
    }

    private int InsertWarehouseProduct(SqlTransaction transaction, int idOrder, decimal productPrice,
        WarehouseProduct warehouseProduct)
    {
        using var cmd = new SqlCommand();
        cmd.Connection = transaction.Connection;
        cmd.Transaction = transaction;
        cmd.CommandText =
            "INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) OUTPUT INSERTED.IdProductWarehouse VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt)";
        cmd.Parameters.AddWithValue("@IdWarehouse", warehouseProduct.IdWarehouse);
        cmd.Parameters.AddWithValue("@IdProduct", warehouseProduct.IdProduct);
        cmd.Parameters.AddWithValue("@IdOrder", idOrder);
        cmd.Parameters.AddWithValue("@Amount", warehouseProduct.Amount);
        cmd.Parameters.AddWithValue("@Price", productPrice * warehouseProduct.Amount);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}