// Services/AnalyticsService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FocusMate.Models;

namespace FocusMate.Services
{
    public class AnalyticsService
    {
        private readonly SessionService _sessionService;
        // REMOVED: SettingsService dependency (not needed for current analytics)
        // private readonly SettingsService _settingsService;

        // Caching to prevent repeated expensive calculations
        private readonly Dictionary<DateTimeOffset, DailySummary> _dailyCache = new();
        private readonly Dictionary<(DateTimeOffset, DateTimeOffset), WeeklySummary> _weeklyCache = new();
        private CategoryBreakdown _categoryBreakdownCache;
        private StreakInfo _streakInfoCache;
        private DateTimeOffset _lastCacheUpdate = DateTimeOffset.MinValue;

        public AnalyticsService(SessionService sessionService)
        {
            _sessionService = sessionService;
            // REMOVED: SettingsService parameter
            // _settingsService = settingsService;
        }

        public async Task<DailySummary> GetDailySummaryAsync(DateTimeOffset date)
        {
            // Use cache if available and recent
            if (_dailyCache.TryGetValue(date.Date, out var cached) &&
                _lastCacheUpdate > DateTimeOffset.UtcNow.AddMinutes(-5))
            {
                return cached;
            }

            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            var sessions = await _sessionService.GetSessionsAsync(startOfDay, endOfDay);
            var focusSessions = sessions.Where(s => s.Mode == TimerMode.Focus);

            var summary = new DailySummary
            {
                Date = date.DateTime,
                TotalFocusTime = TimeSpan.FromMinutes(focusSessions.Sum(s => s.DurationMinutes)),
                SessionCount = focusSessions.Count(),
                CompletedSessions = focusSessions.Count(s => !s.WasInterrupted)
            };

            _dailyCache[date.Date] = summary;
            _lastCacheUpdate = DateTimeOffset.UtcNow;

            return summary;
        }

        public async Task<WeeklySummary> GetWeeklySummaryAsync(DateTimeOffset startDate)
        {
            var endDate = startDate.AddDays(7);
            var cacheKey = (startDate.Date, endDate.Date);

            // Use cache if available and recent
            if (_weeklyCache.TryGetValue(cacheKey, out var cached) &&
                _lastCacheUpdate > DateTimeOffset.UtcNow.AddMinutes(-5))
            {
                return cached;
            }

            var sessions = await _sessionService.GetSessionsAsync(startDate, endDate);
            var focusSessions = sessions.Where(s => s.Mode == TimerMode.Focus);

            var summary = new WeeklySummary
            {
                StartDate = startDate.DateTime,
                EndDate = endDate.DateTime,
                TotalFocusTime = TimeSpan.FromMinutes(focusSessions.Sum(s => s.DurationMinutes)),
                AverageDailyFocusTime = TimeSpan.FromMinutes(focusSessions.Sum(s => s.DurationMinutes) / 7),
                SessionCount = focusSessions.Count()
            };

            _weeklyCache[cacheKey] = summary;
            _lastCacheUpdate = DateTimeOffset.UtcNow;

            return summary;
        }

        public async Task<CategoryBreakdown> GetCategoryBreakdownAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            // Use cache if available and recent
            if (_categoryBreakdownCache != null &&
                _lastCacheUpdate > DateTimeOffset.UtcNow.AddMinutes(-5))
            {
                return _categoryBreakdownCache;
            }

            var sessions = await _sessionService.GetSessionsAsync(startDate, endDate);
            var focusSessions = sessions.Where(s => s.Mode == TimerMode.Focus);

            _categoryBreakdownCache = new CategoryBreakdown
            {
                TotalTime = TimeSpan.FromMinutes(focusSessions.Sum(s => s.DurationMinutes)),
                Categories = focusSessions
                    .GroupBy(s => s.Category)
                    .Select(g => new CategoryTime
                    {
                        Category = g.Key,
                        Time = TimeSpan.FromMinutes(g.Sum(s => s.DurationMinutes)),
                        SessionCount = g.Count()
                    })
                    .OrderByDescending(c => c.Time)
                    .ToList()
            };

            _lastCacheUpdate = DateTimeOffset.UtcNow;
            return _categoryBreakdownCache;
        }

        public async Task<StreakInfo> GetCurrentStreakAsync()
        {
            // Use cache if available and recent
            if (_streakInfoCache != null &&
                _lastCacheUpdate > DateTimeOffset.UtcNow.AddMinutes(-5))
            {
                return _streakInfoCache;
            }

            var sessions = await _sessionService.GetSessionsAsync();
            var completedSessions = sessions
                .Where(s => s.Mode == TimerMode.Focus && !s.WasInterrupted)
                .Select(s => s.StartUtc.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            if (completedSessions.Count == 0)
            {
                _streakInfoCache = new StreakInfo { CurrentStreak = 0, LongestStreak = 0 };
                return _streakInfoCache;
            }

            // Calculate current streak
            var currentStreak = 1;
            for (int i = 1; i < completedSessions.Count; i++)
            {
                var diff = (completedSessions[i - 1] - completedSessions[i]).Days;
                if (diff == 1)
                {
                    currentStreak++;
                }
                else if (diff > 1)
                {
                    break;
                }
            }

            // Calculate longest streak
            var longestStreak = 1;
            var tempStreak = 1;
            for (int i = 1; i < completedSessions.Count; i++)
            {
                var diff = (completedSessions[i - 1] - completedSessions[i]).Days;
                if (diff == 1)
                {
                    tempStreak++;
                    longestStreak = Math.Max(longestStreak, tempStreak);
                }
                else if (diff > 1)
                {
                    tempStreak = 1;
                }
            }

            _streakInfoCache = new StreakInfo
            {
                CurrentStreak = currentStreak,
                LongestStreak = longestStreak
            };

            _lastCacheUpdate = DateTimeOffset.UtcNow;
            return _streakInfoCache;
        }

        public void InvalidateCache()
        {
            _dailyCache.Clear();
            _weeklyCache.Clear();
            _categoryBreakdownCache = null;
            _streakInfoCache = null;
            _lastCacheUpdate = DateTimeOffset.MinValue;
        }
    }

    public class DailySummary
    {
        public DateTime Date { get; set; }
        public TimeSpan TotalFocusTime { get; set; }
        public int SessionCount { get; set; }
        public int CompletedSessions { get; set; }
    }

    public class WeeklySummary
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan TotalFocusTime { get; set; }
        public TimeSpan AverageDailyFocusTime { get; set; }
        public int SessionCount { get; set; }
    }

    public class CategoryBreakdown
    {
        public TimeSpan TotalTime { get; set; }
        public List<CategoryTime> Categories { get; set; } = new List<CategoryTime>();
    }

    public class CategoryTime
    {
        public string Category { get; set; } = string.Empty;
        public TimeSpan Time { get; set; }
        public int SessionCount { get; set; }
    }

    public class StreakInfo
    {
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
    }
}