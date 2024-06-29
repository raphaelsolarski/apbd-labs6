using Microsoft.Data.SqlClient;
using Microsoft.OpenApi.Extensions;
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

        var productPrice = FetchProductPrice(con, warehouseProduct.IdProduct);
        if (productPrice == null)
        {
            throw new NotExistingProductException(warehouseProduct.IdProduct);
        }

        if (!WarehouseExists(con, warehouseProduct.IdWarehouse))
        {
            throw new NotExistingWarehouseException(warehouseProduct.IdWarehouse);
        }

        var matchingOrderId = FindMatchingOrder(con, warehouseProduct.IdProduct, warehouseProduct.Amount,
            warehouseProduct.CreatedAt);
        if (matchingOrderId == null)
        {
            throw new NotExistingMatchingOrderException();
        }

        if (OrderFullfiled(con, matchingOrderId.Value))
        {
            throw new OrderAlreadyFullfilledException(matchingOrderId.Value);
        }

        SetOrderFullfiledDate(con, matchingOrderId.Value, DateTime.Now);


        var id =  InsertWarehouseProduct(con, matchingOrderId.Value, productPrice.Value, warehouseProduct);
        transaction.Commit();
        return id;
    }

    private double? FetchProductPrice(SqlConnection con, int productId)
    {
        using var cmd = new SqlCommand();
        cmd.Connection = con;
        cmd.CommandText = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
        cmd.Parameters.AddWithValue("@IdProduct", productId);

        var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            return dr.GetDouble(0);
        }
        return null;
    }

    private bool WarehouseExists(SqlConnection con, int warehouseId)
    {
        using var cmd = new SqlCommand();
        cmd.Connection = con;
        cmd.CommandText = "SELECT 1 FROM Warehouse WHERE IdProduct = @IdWarehouse";
        cmd.Parameters.AddWithValue("@IdWarehouse", warehouseId);

        var dr = cmd.ExecuteReader();
        return dr.Read();
    }

    private int? FindMatchingOrder(SqlConnection con, int idProduct, int amount, DateTime requestDateTime)
    {
        using var cmd = new SqlCommand();
        cmd.Connection = con;
        cmd.CommandText =
            "SELECT IdOrder FROM Order WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @RequestDateTime";
        cmd.Parameters.AddWithValue("@IdProduct", idProduct);
        cmd.Parameters.AddWithValue("@Amount", amount);
        cmd.Parameters.AddWithValue("@RequestDateTime", requestDateTime);

        var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            return dr.GetInt32(0);
        }

        return null;
    }


    private bool OrderFullfiled(SqlConnection con, int idOrder)
    {
        using var cmd = new SqlCommand();
        cmd.Connection = con;
        cmd.CommandText = "SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder";
        cmd.Parameters.AddWithValue("@IdOrder", idOrder);
        var dr = cmd.ExecuteReader();
        return dr.Read();
    }

    private void SetOrderFullfiledDate(SqlConnection con, int idOrder, DateTime dateTime)
    {
        using var cmd = new SqlCommand();
        cmd.Connection = con;
        cmd.CommandText = "UPDATE Order SET FulfilledAt=@FulfilledDate WHERE IdOrder = @IdOrder";
        cmd.Parameters.AddWithValue("@IdOrder", idOrder);
        cmd.Parameters.AddWithValue("@FulfilledDate", dateTime);

        cmd.ExecuteNonQuery();
    }

    private int InsertWarehouseProduct(SqlConnection con, int idOrder, double productPrice,
        WarehouseProduct warehouseProduct)
    {
        using var cmd = new SqlCommand();
        cmd.Connection = con;
        cmd.CommandText =
            "INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) VALUES(@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt)";
        cmd.Parameters.AddWithValue("@IdWarehouse", warehouseProduct.IdWarehouse);
        cmd.Parameters.AddWithValue("@IdProduct", warehouseProduct.IdProduct);
        cmd.Parameters.AddWithValue("@IdOrder", idOrder);
        cmd.Parameters.AddWithValue("@Amount", warehouseProduct.Amount);
        cmd.Parameters.AddWithValue("@Price", productPrice * warehouseProduct.Amount);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

        return (int)cmd.ExecuteScalar();
    }
}