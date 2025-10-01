using Scalar.AspNetCore;
using SensorApi;
using SensorApi.Producers;
using SensorApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MqttOptions>(builder.Configuration.GetSection("Mqtt"));
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.Configure<ProcessorOptions>(builder.Configuration.GetSection("Processor"));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddHostedService<MqttIngestWorker>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Ensure Kafka topics exist
//var bootstrap = app.Configuration["Kafka:BootstrapServers"] ?? "broker:9092";
//await TopicAdmin.EnsureTopicsAsync(
//  bootstrap,
//  new[] { "sensor.raw", "sensor.view", "sensor.alarm" },
//  partitions: 3, replication: 1);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
  app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { ok = true, ts = DateTimeOffset.UtcNow }))
  .WithSummary("Health Check")
  .WithDescription("This endpoint returns a ok sign and timestamp")
  .WithTags("Health");

app.Run();