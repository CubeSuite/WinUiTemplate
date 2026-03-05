using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUiTemplate.Services;

namespace WinUiTemplate.MVVM.Models
{
    public record LogEntry {
        public DateTime Timestamp { get; init; } = DateTime.Now;
        public LogLevel LogLevel { get; init; }
        public string Message { get; init; } = "";
        public string[] Tags { get; init; } = Array.Empty<string>();

        public LogEntry(LogLevel level, string message, string[]? tags) {
            LogLevel = level;
            Message = message;
            if (tags != null) Tags = tags;
        }
    }
}
