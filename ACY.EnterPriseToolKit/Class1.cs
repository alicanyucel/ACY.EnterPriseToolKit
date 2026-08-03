using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[AttributeUsage(AttributeTargets.Class)]
public class CacheableAttribute : Attribute { }

public static class CacheManager
{
    private static readonly Dictionary<string, object> _cache = new();

    public static async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory)
    {
        if (_cache.ContainsKey(key))
            return (T)_cache[key];

        var value = await factory();
        _cache[key] = value;
        return value;
    }

    public static void Invalidate(string key)
    {
        if (_cache.ContainsKey(key))
            _cache.Remove(key);
    }
}

// ---------------- Resilience ----------------
[AttributeUsage(AttributeTargets.Method)]
public class ResilientAttribute : Attribute
{
    public int RetryCount { get; set; } = 3;
    public int DelayMilliseconds { get; set; } = 200;
}

public static class ResilienceHandler
{
    public static async Task<T> ExecuteAsync<T>(Func<Task<T>> action, int retryCount = 3, int delay = 200)
    {
        for (int i = 0; i < retryCount; i++)
        {
            try { return await action(); }
            catch { await Task.Delay(delay); }
        }
        throw new Exception("Resilience retries exhausted");
    }
}

// ---------------- Job Monitoring ----------------
[AttributeUsage(AttributeTargets.Method)]
public class MonitoredJobAttribute : Attribute { }

public static class JobLogger
{
    public static void Log(string jobName, DateTime start, DateTime end, bool success)
    {
        Console.WriteLine($"{jobName} ran at {start}, success={success}, duration={end - start}");
    }
}

// ---------------- Audit ----------------
[AttributeUsage(AttributeTargets.Class)]
public class AuditableAttribute : Attribute { }

public static class AuditInterceptor
{
    public static void OnEntityChanged(object entity, string user)
    {
        Console.WriteLine($"Entity {entity.GetType().Name} changed by {user}");
    }
}

// ---------------- Example Usage --------
// --------
[Cacheable]
[Auditable]
public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; }
}

public class OrderService
{
    [Resilient(RetryCount = 5, DelayMilliseconds = 500)]
    public async Task<Order> GetOrderAsync(int id)
    {
        return await ResilienceHandler.ExecuteAsync(async () =>
        {
            // Simulate DB call
            await Task.Delay(100);
            return new Order { Id = id, CustomerName = "Ali" };
        }, 5, 500);
    }

    [MonitoredJob]
    public async Task ProcessOrdersJob()
    {
        var start = DateTime.Now;
        try
        {
            await Task.Delay(200); // simulate work
            JobLogger.Log(nameof(ProcessOrdersJob), start, DateTime.Now, true);
        }
        catch
        {
            JobLogger.Log(nameof(ProcessOrdersJob), start, DateTime.Now, false);
        }
    }
}
