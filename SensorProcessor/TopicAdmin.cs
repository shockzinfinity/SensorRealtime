using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace SensorProcessor;

public static class TopicAdmin
{
  public static async Task EnsureTopicAsync(ILogger logger, string bootstrap, IEnumerable<string> topics, int partitions = 3, short replicationo = 1)
  {
    using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrap }).Build();

    for (int attempt = 0; attempt < 8; attempt++)
    {
      try
      {
        var metadata = admin.GetMetadata(TimeSpan.FromSeconds(5));
        var exist = new HashSet<string>(metadata.Topics.Select(t => t.Topic));

        var create = topics.Where(t => !exist.Contains(t))
          .Select(t => new TopicSpecification
          {
            Name = t,
            NumPartitions = partitions,
            ReplicationFactor = replicationo,
          })
          .ToList();

        if (create.Count == 0)
        {
          logger.LogInformation("[KAFKA] topics already exist: {Topics}", string.Join(",", topics));
          return;
        }

        await admin.CreateTopicsAsync(create, new CreateTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(10) });
        logger.LogInformation("[KAFKA] created topics: {Topics}", string.Join(",", create.Select(x => x.Name)));
        return;
      }
      catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
      {
        logger.LogInformation("[KAFKA] topics already exist (race)");
        return;
      }
      catch (Exception ex)
      {
        logger.LogWarning(ex, "[KAFKA] EnsureTopics attempt {Attempt} failed. Retrying...", attempt);
        await Task.Delay(1000);
      }
    }

    throw new Exception("[KAFKA] EnsureTopics failed after retries.");
  }
}