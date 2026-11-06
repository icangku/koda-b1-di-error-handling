using CrudApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrudApi.Controllers;

[ApiController]
[Route("api/v1/products/{productCode}/image")]
public sealed class ProductImageController : ControllerBase
{
  private const long MaxFileBytes = 5L * 1024 * 1024;

  private IProductService _productService;
  private readonly IProductImageStorage _imageStorage;

  public ProductImageController(IProductService productService, IProductImageStorage imageStorage)
  {
    _productService = productService;
    _imageStorage = imageStorage;
  }

[HttpPost]
[Consumes("multipart/form-data")]
[RequestSizeLimit(6_291_456)]
[RequestFormLimits(MultipartBodyLengthLimit = 6_291_456)]
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> Upload(
[FromRoute] string productCode, [FromForm] IFormFile? file, CancellationToken ct
)
{
    var safeCode = productCode.Trim().ToUpperInvariant();

    var validCode = safeCode.Length is >= 1 and <= 30 && safeCode.All(character => (character >= 'A' && character <= 'Z') || (character >= '0' && character <= '9') || character is '-' or '_');

    if (!validCode)
    {
      return Problem(statusCode: 400, title: "Invalid ProductCode", detail: "Format Kode Product tidak valid");
    }

    if (file is null)
    {
      return Problem(statusCode: 400, title: "File is required", detail: "isi filenya cuy");
    }

    // Tolak File kosong atau lebih dari 5 mb
    if (file.Length <= 0 || file.Length > MaxFileBytes)
    {
      return Problem(statusCode: 400, title: "Invalid file size", detail: "ukuran filenya bang kegedean");
    }
    var extension = Path.GetExtension(file.FileName);

    if (!string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase))
    {
      return Problem(statusCode: 400, title: "Invalid file extention", detail: "cuma JPG dulu yg bisa");
    }

    // Memanggil ProductService
    var product = await _productService.GetByCode(safeCode, ct);

    if (product is null)
    {
      return Problem(statusCode: 404, title: "Product not found", detail: $"Product {safeCode} tidak ditemukan");

        // Simpan Gambar
    }
    var savedImage = await _imageStorage.SaveAsync(safeCode, file, ct);

    return StatusCode(StatusCodes.Status201Created, new
    {
        message = "Product image uploaded successfully",
        productCode = safeCode,
        fileName = savedImage.StoredFileName,
        fileSize = savedImage.SizeInBytes,
        contentType = savedImage.ContentType
    });
  }
}