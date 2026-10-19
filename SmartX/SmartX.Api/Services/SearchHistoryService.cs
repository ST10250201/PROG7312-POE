
using SmartX.Api.Models;

namespace SmartX.Api.Services
{
    public class SearchHistoryService
    {
        private readonly object _lock = new();

        // Chronological history of recorded sensor searches.
        private readonly List<SearchHistoryEntry> _history = new();

        // Dictionary for fast lookup of search frequency.
        private readonly Dictionary<string, int> _searchFrequency =
            new(StringComparer.OrdinalIgnoreCase);

        private int _nextId = 1;

        public SearchHistoryEntry RecordSearch(
            string searchTerm,
            string category,
            bool activeAlertsOnly,
            int resultCount)
        {
            if (resultCount < 0)
            {
                throw new ArgumentException(
                    "Result count cannot be negative.");
            }

            string normalizedTerm = searchTerm?.Trim() ?? string.Empty;

            string normalizedCategory =
                string.IsNullOrWhiteSpace(category)
                    ? "All"
                    : category.Trim();

            lock (_lock)
            {
                var entry = new SearchHistoryEntry
                {
                    Id = _nextId++,
                    SearchTerm = normalizedTerm,
                    Category = normalizedCategory,
                    ActiveAlertsOnly = activeAlertsOnly,
                    ResultCount = resultCount,
                    Timestamp = DateTime.UtcNow
                };

                _history.Add(entry);

                // Count meaningful searches, not empty filter states.
                if (!string.IsNullOrWhiteSpace(normalizedTerm))
                {
                    if (_searchFrequency.ContainsKey(normalizedTerm))
                    {
                        _searchFrequency[normalizedTerm]++;
                    }
                    else
                    {
                        _searchFrequency[normalizedTerm] = 1;
                    }
                }

                return entry;
            }
        }

        public List<SearchHistoryEntry> GetHistory()
        {
            lock (_lock)
            {
                return _history
                    .OrderByDescending(entry => entry.Timestamp)
                    .ThenByDescending(entry => entry.Id)
                    .ToList();
            }
        }

        public Dictionary<string, int> GetSearchFrequency()
        {
            lock (_lock)
            {
                return new Dictionary<string, int>(
                    _searchFrequency,
                    StringComparer.OrdinalIgnoreCase);
            }
        }

        public int GetTotalSearches()
        {
            lock (_lock)
            {
                return _history.Count;
            }
        }
    }
}
