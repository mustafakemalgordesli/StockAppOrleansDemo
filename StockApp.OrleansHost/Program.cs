using Microsoft.EntityFrameworkCore;
using Orleans.Configuration;
using StockApp.Contracts;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=../app.db"));


builder.UseOrleans(silo =>
 {
     silo.Configure<ClusterOptions>(options =>
     {
         options.ClusterId = "dev";
         options.ServiceId = "StockService";
     })
     .UseMongoDBClient("mongodb://localhost:27017")
     .AddMongoDBGrainStorage(name: "MongoStorage", options =>
     {
         options.DatabaseName = "OrleansDemoDB";
     })
     .ConfigureEndpoints(IPAddress.Loopback, 11111, 30000)
     .UseInMemoryReminderService()
     .UseMongoDBClustering(options =>
     {
         options.DatabaseName = "ClusterDB";
     });
     
     
     silo.UseDashboard(options => {
         options.Username = "USERNAME";
         options.Password = "PASSWORD";
         options.Host = "*";
         options.Port = 8081;
         options.HostSelf = true;
         options.CounterUpdateIntervalMs = 1000;
     });
 });

var app = builder.Build();

app.Map("/dashboard", x => x.UseOrleansDashboard());


app.Run();

