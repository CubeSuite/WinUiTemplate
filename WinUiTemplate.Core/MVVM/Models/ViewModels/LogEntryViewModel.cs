using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.Core.MVVM.Models.ViewModels
{
    public partial class LogEntryViewModel : ObservableObject
    {
        public string Timestamp { get; }
        public string TimestampPretty { get; }
        public string Level { get; }
        public string Message { get; }
        public string[] Tags { get; }

        public LogEntryViewModel(LogEntry entry) {
            Timestamp = entry.Timestamp.ToString("yyyyMMddTHHmmss");
            TimestampPretty = entry.Timestamp.ToString("dd-MMM hh:mm:ss tt");
            Level = entry.LogLevel.ToString();
            Message = entry.Message;
            Tags = entry.Tags;
        }
    }
}
