// Services/SessionService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FocusMate.Models;
using FocusMate.Services;

namespace FocusMate.Services
{
    public class SessionService
    {
        private readonly StorageService _storageService;
        private readonly TimerService _timerService;
        private Session? _currentSession;

        public SessionService(StorageService storageService, TimerService timerService)
        {
            _storageService = storageService;
            _timerService = timerService;
        }

        public async Task InitializeAsync()
        {
            await LoadSessionsAsync();
        }

        public Session StartSession(string label, string category)
        {
            _currentSession = new Session
            {
                Id = Guid.NewGuid(),
                StartUtc = DateTimeOffset.UtcNow,
                Label = label,
                Category = category,
                Mode = _timerService.CurrentMode,
                WasInterrupted = false
            };
            return _currentSession;
        }

        public async Task<Session?> CompleteCurrentSessionAsync()
        {
            if (_currentSession == null) return null;

            _currentSession.EndUtc = DateTimeOffset.UtcNow;
            _currentSession.WasInterrupted = false;
            await SaveSessionAsync(_currentSession);

            var completed = _currentSession;
            _currentSession = null;
            return completed;
        }

        public async Task<Session?> InterruptCurrentSessionAsync()
        {
            if (_currentSession == null) return null;

            _currentSession.EndUtc = DateTimeOffset.UtcNow;
            _currentSession.WasInterrupted = true;
            await SaveSessionAsync(_currentSession);

            var interrupted = _currentSession;
            _currentSession = null;
            return interrupted;
        }

        public Session? GetCurrentSession() => _currentSession;

        public async Task SaveSessionAsync(Session session)
        {
            var sessions = await LoadAllSessionsAsync();
            sessions.Add(session);
            await _storageService.SaveSessionsAsync(sessions.ToArray());
        }

        public async Task DeleteSessionAsync(Guid sessionId)
        {
            var sessions = await LoadAllSessionsAsync();
            var session = sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session != null)
            {
                sessions.Remove(session);
                await _storageService.SaveSessionsAsync(sessions.ToArray());
            }
        }

        public async Task<IEnumerable<Session>> GetSessionsAsync(DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null)
        {
            var sessions = await LoadAllSessionsAsync();
            return FilterSessions(sessions, fromDate, toDate);
        }

        public async Task<IEnumerable<Session>> GetSessionsByCategoryAsync(string category)
        {
            var sessions = await LoadAllSessionsAsync();
            return sessions
                .Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.StartUtc);
        }

        public async Task<TimeSpan> GetTotalFocusTimeAsync(DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null)
        {
            var sessions = await GetSessionsAsync(fromDate, toDate);
            return TimeSpan.FromMinutes(sessions.Sum(s => s.DurationMinutes));
        }

        public async Task<int> GetSessionCountAsync(DateTimeOffset? fromDate = null, DateTimeOffset? toDate = null)
        {
            var sessions = await GetSessionsAsync(fromDate, toDate);
            return sessions.Count();
        }

        private async Task<List<Session>> LoadAllSessionsAsync()
        {
            var sessions = await _storageService.LoadSessionsAsync();
            return sessions?.ToList() ?? new List<Session>();
        }

        private IEnumerable<Session> FilterSessions(IEnumerable<Session> sessions, DateTimeOffset? fromDate, DateTimeOffset? toDate)
        {
            var filtered = sessions.AsEnumerable();

            if (fromDate.HasValue)
            {
                filtered = filtered.Where(s => s.StartUtc >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                filtered = filtered.Where(s => s.StartUtc <= toDate.Value);
            }

            return filtered.OrderByDescending(s => s.StartUtc);
        }
    }
}