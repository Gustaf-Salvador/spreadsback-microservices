using UserService.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using UserService.Common;

namespace UserService.Services;

public interface IEventStore
{
    Task StoreEventAsync(UserEvent userEvent);
    Task<List<UserEvent>> GetEventsAsync(string userId);
    Task<List<UserEvent>> GetEventsByTypeAsync(string eventType, DateTime? from = null, DateTime? to = null);
}