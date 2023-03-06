using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Caching.Cosmos;
using stocktrades.Hubs;
using Microsoft.Extensions.Configuration;
using System.Drawing.Text;
using stocktrades.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR().AddAzureSignalR();
builder.Services.AddSingleton<ICosmosService>(new CosmosService(builder.Configuration["ConnectionStrings:cosmosConnectionString"]));

builder.Services.AddCosmosCache((CosmosCacheOptions cacheOptions) =>
{
    CosmosClientBuilder clientBuilder = new CosmosClientBuilder(builder.Configuration["ConnectionStrings:cosmosConnectionString"]);
    cacheOptions.ContainerName = "sessionState";
    cacheOptions.DatabaseName = "trading";
    cacheOptions.ClientBuilder = clientBuilder;
    /* Creates the container if it does not exist */
    cacheOptions.CreateIfNotExists = true;
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(3600);
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.UseSession();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<AllOrders>("/allOrders");
    endpoints.MapHub<StockHub>("/stockhub");
});

app.MapRazorPages();

app.Run();
