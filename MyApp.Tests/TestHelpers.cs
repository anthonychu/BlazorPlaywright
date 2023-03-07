using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace MyApp.Tests
{
    internal static class TestHelpers
    {
        public static IAsyncDisposable StartTracingAsync(this IBrowserContext context, string traceName)
        {
            var traceContext = new TraceContext(context, traceName);
            traceContext.StartTracingAsync().GetAwaiter().GetResult();
            return traceContext;
        }
    }

    internal class TraceContext : IAsyncDisposable
    {
        private readonly IBrowserContext _context;
        private readonly string _traceName;
        private readonly string _tracePath = "";
        private readonly bool _traceEnabled = false;
        public TraceContext(IBrowserContext context, string traceName)
        {
            _tracePath = Environment.GetEnvironmentVariable("TRACE_PATH") ?? "";
            _traceEnabled = !string.IsNullOrEmpty(_tracePath);
            _context = context;
            _traceName = traceName;
        }

        public ValueTask DisposeAsync()
        {
            return StopTracingAsync();
        }

        public async ValueTask StartTracingAsync()
        {
            if (_traceEnabled)
            {
                await _context.Tracing.StartAsync(new()
                {
                    Screenshots = true,
                    Snapshots = true,
                    Sources = true,
                });
            }
        }

        public async ValueTask StopTracingAsync()
        {
            if (_traceEnabled)
            {
                await _context.Tracing.StopAsync(new()
                {
                    Path = Path.Combine(_tracePath, $"{_traceName}.zip"),
                });
            }
        }

    }
}
