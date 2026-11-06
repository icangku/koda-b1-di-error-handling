namespace CrudApi.DTOs;

public sealed record StoredProductImage
(
  string StoredFileName,
  long SizeInBytes,
  string ContentType
);