using Newtonsoft.Json;

namespace MainClient.Scheduler
{
    /// <summary>Durable Beijing-day/hour counters used by the UI and restart recovery.</summary>
    internal sealed class TrafficMetricsStore
    {
        private sealed class Counter
        {
            public long Request { get; set; }
            public long Start { get; set; }
            public long Dsp { get; set; }
            public long Click { get; set; }

            public void Add(TrafficTaskStateKind state, int count)
            {
                switch (state)
                {
                    case TrafficTaskStateKind.Request: Request += count; break;
                    case TrafficTaskStateKind.Start: Start += count; break;
                    case TrafficTaskStateKind.DSP: Dsp += count; break;
                    case TrafficTaskStateKind.Clickthrough: Click += count; break;
                }
            }

            public TrafficTaskUiSnapshot Snapshot(int taskId) => new(
                taskId, Request, Start, Dsp, Click, 0, 0, 0, 0,
                Dsp == 0 ? 0 : Click / (double)Dsp);
        }

        private sealed class HourBucket
        {
            public Counter Host { get; set; } = new();
            public Dictionary<int, Counter> Tasks { get; set; } = new();
        }

        private readonly object _sync = new();
        private readonly object _saveSync = new();
        private readonly string _filePath;
        private Dictionary<string, HourBucket> _hours = new();
        private string _activeDay;
        private bool _dirty;
        private long _version;

        public TrafficMetricsStore(string filePath, string currentDay)
        {
            _filePath = filePath;
            _activeDay = currentDay;
            LoadCurrentDay(currentDay);
        }

        public void Add(string hourKey, int taskId, TrafficTaskStateKind state, int count)
        {
            if (count == 0) return;
            lock (_sync)
            {
                var dayKey = hourKey[..8];
                if (!string.Equals(_activeDay, dayKey, StringComparison.Ordinal))
                {
                    _hours.Clear();
                    _activeDay = dayKey;
                }
                if (!_hours.TryGetValue(hourKey, out var hour))
                    _hours[hourKey] = hour = new HourBucket();
                hour.Host.Add(state, count);
                if (!hour.Tasks.TryGetValue(taskId, out var task))
                    hour.Tasks[taskId] = task = new Counter();
                task.Add(state, count);
                _dirty = true;
                _version++;
            }
        }

        public TrafficTaskUiSnapshot GetHour(string hourKey, int taskId = 0)
        {
            lock (_sync)
            {
                if (!_hours.TryGetValue(hourKey, out var hour))
                    return default;
                if (taskId == 0) return hour.Host.Snapshot(0);
                return hour.Tasks.TryGetValue(taskId, out var task)
                    ? task.Snapshot(taskId) : default;
            }
        }

        public TrafficTaskUiSnapshot GetDay(string dayKey)
        {
            lock (_sync)
            {
                var total = new Counter();
                foreach (var pair in _hours)
                {
                    if (!pair.Key.StartsWith(dayKey, StringComparison.Ordinal)) continue;
                    total.Request += pair.Value.Host.Request;
                    total.Start += pair.Value.Host.Start;
                    total.Dsp += pair.Value.Host.Dsp;
                    total.Click += pair.Value.Host.Click;
                }
                return total.Snapshot(0);
            }
        }

        public void Save()
        {
            lock (_saveSync)
            {
                string json;
                long savedVersion;
                lock (_sync)
                {
                    if (!_dirty) return;
                    json = JsonConvert.SerializeObject(_hours, Formatting.Indented);
                    savedVersion = _version;
                }

                var directory = Path.GetDirectoryName(_filePath)!;
                Directory.CreateDirectory(directory);
                var temporaryPath = _filePath + ".tmp";
                File.WriteAllText(temporaryPath, json);
                File.Move(temporaryPath, _filePath, true);

                lock (_sync)
                {
                    if (_version == savedVersion)
                        _dirty = false;
                }
            }
        }

        private void LoadCurrentDay(string currentDay)
        {
            if (!File.Exists(_filePath)) return;
            try
            {
                var loaded = JsonConvert.DeserializeObject<Dictionary<string, HourBucket>>(
                    File.ReadAllText(_filePath));
                if (loaded == null) return;
                _hours = loaded
                    .Where(pair => pair.Key.StartsWith(currentDay, StringComparison.Ordinal))
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
            }
            catch (JsonException)
            {
                // A damaged statistics file must not prevent MainClient from starting.
                _hours = new Dictionary<string, HourBucket>();
            }
        }
    }
}
