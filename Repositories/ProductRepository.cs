using System.Data;
using CrudApi.DTOs;
using CrudApi.Models;
using Microsoft.Data.SqlClient;

namespace CrudApi.Repositories;

public sealed class ProductRepository : IProductRepository
{
  private readonly string _connectionString;
  public ProductRepository(IConfiguration configuration)
  {
    _connectionString = configuration.GetConnectionString("ecommercedb") ?? throw new InvalidOperationException("Connection lost");
  }

  public async Task<bool> Add(Product product, CancellationToken cancellationToken)
  {
    const string sql = """
        INSERT INTO dbo.Product(Code, Name, Price, Stock, Category, CustomerId) VALUES
        (@code, @name, @price, @stock, @category, @customerId);
        """;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);

        AddProductParameter(command, product);

        try
        {
          var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
          return affectedRows == 1;
        }
        catch (SqlException e) when (e.Number is 2601 or 2627)
        {
          return false;
        }
        catch (SqlException e)
        {
          Console.WriteLine($"SQL Error tidak terduga: {e.Number} - {e.Message}");
          return false;
        }
  }

  public async Task<bool> Delete(string code, CancellationToken cancellationToken)
  {
    const string sql = """
      DELETE FROM dbo.Product
      WHERE Code = @code;
    """;

    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync(cancellationToken);
    await using var command = new SqlCommand(sql, connection);
    AddProductCode(command, code);
    var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
    return affectedRows == 1;
  }

  public async Task<IReadOnlyList<Product>> GetAll(CancellationToken cancellationToken)
  {
    const string sql = """
      SELECT Code, Name, Price, Stock, Category, CustomerId
      FROM dbo.Product
      ORDER BY Code DESC;
    """;

    var products = new List<Product>();
    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync(cancellationToken);
    await using var command = new SqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
    while(await reader.ReadAsync(cancellationToken))
    {
      products.Add(ReadProduct(reader));
    }
    return products;
  }

  public async Task<Product?> GetByCode(string code, CancellationToken cancellationToken)
  {
    const string sql = """
      SELECT Code, Name, Price, Stock, Category, CustomerId FROM dbo.Product WHERE Code = @code;
    """;

    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync(cancellationToken);
    await using var command = new SqlCommand(sql, connection);
    AddProductCode(command, code);
    await using var reader = await command.ExecuteReaderAsync(cancellationToken);

    if (!await reader.ReadAsync(cancellationToken))
    {
        return null;
    }
    return ReadProduct(reader);
  }

  public async Task<bool> Update(Product product, CancellationToken cancellationToken)
  {
    const string sql = """
        UPDATE dbo.Product SET 
            Name = @name,
            Price = @price,
            Stock = @stock,
            Category = @category
    WHERE Code = @code;
    """;

    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync(cancellationToken);
    await using var command = new SqlCommand(sql, connection);
    AddProductParameter(command, product);
    var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

    return affectedRows == 1;
  }


  private static void AddProductParameter(SqlCommand command, Product product)
  {
    AddProductCode(command, product.Code);
    command.Parameters.Add("@name", SqlDbType.NVarChar, 120).Value = product.Name;
    command.Parameters.Add("@stock", SqlDbType.Int).Value = product.Stock;
    var price = command.Parameters.Add("@price", SqlDbType.Decimal);
    command.Parameters.Add("@category", SqlDbType.NVarChar, 80).Value = product.Category;
    command.Parameters.Add("@customerId", SqlDbType.Int).Value = product.CustomerId;

    price.Precision = 18;
    price.Scale = 2;
    price.Value = product.Price;
  }

  public async Task<IReadOnlyList<CustomerProductResponse>> GetByCustomerCode(int id, CancellationToken cancellationToken)
  {
    const string sql = """
      SELECT c.Name AS CustomerName, p.Code AS ProductCode, p.Name AS ProductName, p.Price, p.Stock, p.Category
      FROM dbo.Product p
      INNER JOIN dbo.Customer c ON p.CustomerId = c.Id
      WHERE p.CustomerId = @customerId
      ORDER BY p.Code DESC;
    """;

    var products = new List<CustomerProductResponse>();
    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync(cancellationToken);
    await using var command = new SqlCommand(sql, connection);
    AddCustomerId(command, id);
    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
    while(await reader.ReadAsync(cancellationToken))
    {
      products.Add(ReadProductDto(reader));
    }
    return products;
  }

  private static void AddProductCode(SqlCommand command, string productCode)
  {
    command.Parameters.Add("@code", SqlDbType.NVarChar, 30).Value = productCode.Trim().ToUpperInvariant();
  }

  private static void AddCustomerId(SqlCommand command, int customerId)
  {
    command.Parameters.Add("@customerId", SqlDbType.Int).Value = customerId;
  }

  private static Product ReadProduct(SqlDataReader reader)
  {
    var customerId = reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader.GetValue(5));

    return new Product(
      reader.GetString(0),
      reader.GetString(1),
      reader.GetDecimal(2),
      reader.GetInt32(3),
      reader.GetString(4),
      customerId
    );
  }

  private static CustomerProductResponse ReadProductDto(SqlDataReader reader)
  {
    return new CustomerProductResponse
    {
        CustomerName = reader["CustomerName"].ToString(),
        ProductCode = reader["ProductCode"].ToString(),
        ProductName = reader["ProductName"].ToString(),
        Price = Convert.ToDecimal(reader["Price"]),
        Stock = Convert.ToInt32(reader["Stock"]),
        Category = reader["Category"].ToString()
    };
  }
}