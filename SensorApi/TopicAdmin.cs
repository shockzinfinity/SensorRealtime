using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace SensorApi;

public static class TopicAdmin
{
  public static async Task EnsureTopicsAsync(string bootstrap, IEnumerable<string> topics, int partitions = 3, short replication = 1)
  {
    using var admin = new AdminClientBuilder(new AdminClientConfig
    {
      BootstrapServers = bootstrap
    }).Build();

    var md = admin.GetMetadata(TimeSpan.FromSeconds(5));
    var exists = new HashSet<string>(md.Topics.Select(t => t.Topic));

    var creates = topics.Where(t => !exists.Contains(t))
      .Select(t => new TopicSpecification
      {
        Name = t,
        NumPartitions = partitions,
        ReplicationFactor = replication
      }).ToList();

    if (creates.Count == 0) return;

    try
    {
      await admin.CreateTopicsAsync(creates, new CreateTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(10) });
      Console.WriteLine($"[KAFKA] Created topics: {string.Join(", ", creates.Select(x => x.Name))}");
    }
    catch (CreateTopicsException e)
    {
      if (e.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists)) return;
      throw;
    }
  }
}