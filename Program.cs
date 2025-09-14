using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using QuoteOffer.Configuration;
using QuoteOffer.Services;
using QuoteOffer.Services.Interfaces;
using QuoteOffer.Services.Implementation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure MongoDB
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB"));

builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = builder.Configuration.GetSection("MongoDB").Get<MongoDbSettings>();
    return new MongoClient(settings!.ConnectionString);
});

builder.Services.AddSingleton<IMongoDatabase>(serviceProvider =>
{
    var client = serviceProvider.GetRequiredService<IMongoClient>();
    var settings = builder.Configuration.GetSection("MongoDB").Get<MongoDbSettings>();
    return client.GetDatabase(settings!.DatabaseName);
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "LIS Offers Service API", 
        Version = "v1",
        Description = "API pour la gestion des offres de devis LIS"
    });
});

// Register services
builder.Services.AddScoped<IQuoteOfferService, MongoQuoteOfferService>();
builder.Services.AddHttpClient<IRequestQuoteIntegrationService, RequestQuoteIntegrationService>();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LIS Offers Service API V1");
    });
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
