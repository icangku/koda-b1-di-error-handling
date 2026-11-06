using CrudApi.Middlewares;
using CrudApi.Repositories;
using CrudApi.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(); 
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(
	option =>
	{
		option.CustomizeProblemDetails = context =>
		{
			context.ProblemDetails.Instance ??= context.HttpContext.Request.Path.Value;

			context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
		};
	}
);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

//Repository
builder.Services.AddScoped<IProductRepository, ProductRepository>();
// Service
builder.Services.AddScoped<IProductService, ProductService>();

//Repository
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
// Service
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IProductImageStorage, LocalProductImageStorage>();


var app = builder.Build();

app.UseExceptionHandler();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
		app.MapScalarApiReference(options =>
{
    options
        .WithTitle("CRUD API")
        .WithTheme(ScalarTheme.Moon)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
	});
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
