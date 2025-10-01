using SensorProcessor;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));

builder.Services.AddHostedService<ProcessorWorker>();

var host = builder.Build();

await host.RunAsync();