using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Haven.Application.Common.Contracts.Notifications;

public class DomainEventEnvelope
{
    [JsonPropertyName("eventType")]
    public string EventType { get; set; }
    [JsonPropertyName("occuredAt")]
    public DateTime OccuredAt { get; set; }
    [JsonPropertyName("message")]
    public string Message { get; set; }
    [JsonPropertyName("data")]
    public JsonNode? Data { get; set; }
}