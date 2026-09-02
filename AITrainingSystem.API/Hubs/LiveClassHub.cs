using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using AITrainingSystem.Persistence.Context;
using AITrainingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AITrainingSystem.API.Hubs
{
    [Authorize]
    public class LiveClassHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LiveClassHub> _logger;
        private readonly StackExchange.Redis.IDatabase? _redisDatabase;

        // Thread-safe memory collections for local development when Redis is offline/disabled
        private static readonly ConcurrentDictionary<Guid, string> MemoryUserConnections = new();
        private static readonly object MemoryUserConnectionsLock = new();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, ParticipantSession>> MemoryRoomParticipants = new();
        private static readonly object MemoryRoomParticipantsLock = new();
        private static readonly ConcurrentDictionary<string, string> MemoryConnectionToUser = new();

        // Rate limiting tracking dictionary
        private static readonly ConcurrentDictionary<string, (int MessageCount, DateTime LastReset)> Throttles = new();

        public LiveClassHub(
            ApplicationDbContext context,
            ILogger<LiveClassHub> logger,
            StackExchange.Redis.IConnectionMultiplexer? redisMultiplexer = null)
        {
            _context = context;
            _logger = logger;
            if (redisMultiplexer != null)
            {
                _redisDatabase = redisMultiplexer.GetDatabase();
            }
        }

        public async Task JoinSession(string classId, string userName)
        {
            EnforceRateLimit("JoinSession", 3);

            // Parameter Validation
            if (!Guid.TryParse(classId, out var liveClassId))
            {
                throw new HubException("Invalid live class ID format.");
            }

            if (string.IsNullOrWhiteSpace(userName) || userName.Length > 100)
            {
                throw new HubException("Invalid userName parameter.");
            }

            var connectionId = Context.ConnectionId;
            var userIdString = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                throw new HubException("Unauthorized: Invalid user identifier.");
            }

            // Zero-tolerant Database Enrollment & Role Authorization (Fail-Secure)
            ParticipantSession session;
            try
            {
                var callerUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (callerUser == null)
                {
                    throw new HubException("Unauthorized: User account not found.");
                }

                var liveClass = await _context.LiveClasses.FirstOrDefaultAsync(lc => lc.Id == liveClassId);
                if (liveClass == null)
                {
                    throw new HubException("Live class session not found.");
                }

                bool isAuthorized = false;
                string role = "Student";

                if (callerUser.Role == "Admin")
                {
                    isAuthorized = true;
                    role = "Admin";
                }
                else if (callerUser.Role == "Trainer" || callerUser.IsApprovedTrainer)
                {
                    if (liveClass.TrainerId == userId)
                    {
                        isAuthorized = true;
                        role = "Trainer";
                    }
                }
                else
                {
                    if (liveClass.CourseId.HasValue)
                    {
                        // Direct validation against Enrollments database table
                        isAuthorized = await _context.Enrollments.AnyAsync(e => e.CourseId == liveClass.CourseId.Value && e.UserId == userId);
                    }
                }

                if (!isAuthorized)
                {
                    throw new HubException("Access Denied: You are not authorized or enrolled for this live class session.");
                }

                session = new ParticipantSession
                {
                    ConnectionId = connectionId,
                    UserId = userId,
                    UserName = userName,
                    Role = role
                };
            }
            catch (Exception ex) when (!(ex is HubException))
            {
                _logger.LogError(ex, "Fail-Secure block triggered: database verification failed for user {UserId} and class {ClassId}", userId, classId);
                throw new HubException("Authorization check failed. Entry Denied.");
            }

            // Atomic Newest-Wins Duplicate Connection Management
            await EnforceSingleSession(userId, classId, connectionId);

            // Register connection metadata
            await RegisterConnectionMetadata(connectionId, userId, classId, userName);

            // Register participant session state
            await AddParticipantSession(classId, session);

            // Fetch room snapshot before joining groups so the caller gets current members (excluding itself)
            var activeRoomParticipants = await GetRoomParticipants(classId);

            // Add caller to SignalR Hub Group
            await Groups.AddToGroupAsync(connectionId, classId);

            // Send participants snapshot to the client to trigger clean WebRTC session builds
            await Clients.Caller.SendAsync("RoomSnapshot", activeRoomParticipants);

            // Notify others in the class room
            await Clients.OthersInGroup(classId).SendAsync("UserJoined", connectionId, userIdString, userName);

            // Persistent Live Class Attendance entry Log
            try
            {
                var attendanceObj = new LiveClassAttendance
                {
                    Id = Guid.NewGuid(),
                    LiveClassId = liveClassId,
                    UserId = userId,
                    UserName = userName,
                    JoinTime = DateTime.UtcNow
                };

                _context.LiveClassAttendances.Add(attendanceObj);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not save attendance log for user {UserId} and live class {LiveClassId}", userId, liveClassId);
            }
        }

        public async Task LeaveSession(string classId)
        {
            EnforceRateLimit("LeaveSession", 3);

            if (!Guid.TryParse(classId, out var liveClassId))
            {
                throw new HubException("Invalid live class ID.");
            }

            var connectionId = Context.ConnectionId;
            var userIdString = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                throw new HubException("Unauthorized.");
            }

            // Remove participant configurations
            await RemoveParticipantSession(classId, userId, connectionId);

            // Clean SignalR Groups
            await Groups.RemoveFromGroupAsync(connectionId, classId);
            await Clients.OthersInGroup(classId).SendAsync("UserLeft", connectionId, userIdString);

            // Clean cached connection metadata
            await RemoveConnectionMetadata(connectionId);

            // Close Attendance Entry log
            await TerminateAttendanceSession(liveClassId, userId);
        }

        public async Task SendSignal(string classId, string targetConnectionId, string signalData)
        {
            EnforceRateLimit("SendSignal", 50);

            // Validate signaling payload length (comfortably fit WebRTC details without being vulnerable to overflow)
            if (string.IsNullOrWhiteSpace(signalData) || signalData.Length > 65536)
            {
                throw new HubException("Invalid signaling packet size.");
            }

            // Spoofing Guard: Verify that both the sender and recipient are registered members in the room
            var activeParticipants = await GetRoomParticipants(classId);
            bool isSenderInRoom = activeParticipants.Any(p => p.ConnectionId == Context.ConnectionId);
            bool isTargetInRoom = activeParticipants.Any(p => p.ConnectionId == targetConnectionId);

            if (!isSenderInRoom || !isTargetInRoom)
            {
                _logger.LogWarning("Spoofed signal blocked in room {ClassId} from {SenderConnection} to {TargetConnection}", classId, Context.ConnectionId, targetConnectionId);
                throw new HubException("Operation denied: Target connection routing mismatched.");
            }

            await Clients.Client(targetConnectionId).SendAsync("ReceiveSignal", Context.ConnectionId, signalData);
        }

        public async Task SendChatMessage(string classId, string message)
        {
            EnforceRateLimit("SendChatMessage", 6);

            if (string.IsNullOrWhiteSpace(message) || message.Length > 1000)
            {
                throw new HubException("Chat message must be between 1 and 1000 characters.");
            }

            var userParticipants = await GetRoomParticipants(classId);
            if (!userParticipants.Any(p => p.ConnectionId == Context.ConnectionId))
            {
                throw new HubException("Access Denied: You are not in this live session.");
            }

            var userName = Context.User?.Identity?.Name ?? "Participant";
            await Clients.Group(classId).SendAsync("ReceiveChatMessage", Context.UserIdentifier, userName, message, DateTime.UtcNow);
        }

        public async Task RaiseHand(string classId)
        {
            EnforceRateLimit("RaiseHand", 3);

            var userParticipants = await GetRoomParticipants(classId);
            if (!userParticipants.Any(p => p.ConnectionId == Context.ConnectionId))
            {
                throw new HubException("Access Denied.");
            }

            await Clients.Group(classId).SendAsync("UserRaisedHand", Context.UserIdentifier);
        }

        public async Task LowerHand(string classId)
        {
            EnforceRateLimit("LowerHand", 3);

            var userParticipants = await GetRoomParticipants(classId);
            if (!userParticipants.Any(p => p.ConnectionId == Context.ConnectionId))
            {
                throw new HubException("Access Denied.");
            }

            await Clients.Group(classId).SendAsync("UserLoweredHand", Context.UserIdentifier);
        }

        public async Task EndClassSessionForEveryone(string classId)
        {
            EnforceRateLimit("EndClassSessionForEveryone", 2);

            if (!Guid.TryParse(classId, out var liveClassId))
            {
                throw new HubException("Invalid live class ID.");
            }

            var userIdString = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                throw new HubException("Unauthorized.");
            }

            // Fetch Trainer Details
            var callerUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (callerUser == null)
            {
                throw new HubException("Unauthorized.");
            }

            var liveClass = await _context.LiveClasses.FirstOrDefaultAsync(lc => lc.Id == liveClassId);
            if (liveClass == null)
            {
                throw new HubException("Live class session not found.");
            }

            bool isTrainer = callerUser.Role == "Admin" || (callerUser.Role == "Trainer" && liveClass.TrainerId == userId);
            if (!isTrainer)
            {
                throw new HubException("Access Denied: Instructor credentials are required to close sessions.");
            }

            // Retrieve all participants to clear states before broadcasting eject command
            var sessionsToClose = await GetRoomParticipants(classId);

            // Broadcast closing event
            await Clients.Group(classId).SendAsync("ClassSessionEnded", "The instructor has forced closed the live class.");

            foreach (var p in sessionsToClose)
            {
                await RemoveParticipantSession(classId, p.UserId, p.ConnectionId);
                await RemoveConnectionMetadata(p.ConnectionId);
            }

            if (_redisDatabase != null)
            {
                await _redisDatabase.KeyDeleteAsync($"AITraining:LiveClassRoom:{classId}");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            // Retrieve meta variables mapping this socket ID
            var connectionMeta = await GetConnectionMetadata(connectionId);
            if (!string.IsNullOrEmpty(connectionMeta))
            {
                var parts = connectionMeta.Split(':');
                if (parts.Length >= 3)
                {
                    var userIdStr = parts[0];
                    var classId = parts[1];

                    if (Guid.TryParse(userIdStr, out var userId) && Guid.TryParse(classId, out var liveClassId))
                    {
                        // Clean participant records
                        await RemoveParticipantSession(classId, userId, connectionId);

                        // Broadcast exit message to remaining members
                        await Groups.RemoveFromGroupAsync(connectionId, classId);
                        await Clients.OthersInGroup(classId).SendAsync("UserLeft", connectionId, userIdStr);

                        // Eject attendance records
                        await TerminateAttendanceSession(liveClassId, userId);
                    }
                }
            }

            await RemoveConnectionMetadata(connectionId);
            CleanConnectionThrottle(connectionId);

            await base.OnDisconnectedAsync(exception);
        }

        // --- Core Internal & Scaling Helpers ---

        private async Task EnforceSingleSession(Guid userId, string classId, string newConnectionId)
        {
            var userKey = $"AITraining:LiveClassUser:{userId}";
            string? oldSession = null;

            if (_redisDatabase != null)
            {
                var newValue = $"{newConnectionId}:{classId}";
                // Atomic SET + GET to guarantee race safety across clusters
                var oldVal = await _redisDatabase.StringGetSetAsync(userKey, newValue);
                if (oldVal.HasValue)
                {
                    oldSession = oldVal.ToString();
                }
                await _redisDatabase.KeyExpireAsync(userKey, TimeSpan.FromHours(4));
            }
            else
            {
                lock (MemoryUserConnectionsLock)
                {
                    MemoryUserConnections.TryGetValue(userId, out oldSession);
                    MemoryUserConnections[userId] = $"{newConnectionId}:{classId}";
                }
            }

            if (!string.IsNullOrEmpty(oldSession))
            {
                var parts = oldSession.Split(':');
                if (parts.Length >= 2)
                {
                    var oldConnectId = parts[0];
                    var oldClassId = parts[1];

                    if (oldConnectId != newConnectionId)
                    {
                        _logger.LogInformation("Atomic Eviction: User {UserId} connected under {NewConnId}. Evicting old connection {OldConnId}.", userId, newConnectionId, oldConnectId);
                        await Clients.Client(oldConnectId).SendAsync("DisconnectDuplicate", "You have joined this class session from another window.");
                        await RemoveParticipantSession(oldClassId, userId, oldConnectId);
                    }
                }
            }
        }

        private async Task AddParticipantSession(string classId, ParticipantSession session)
        {
            var json = JsonSerializer.Serialize(session);

            if (_redisDatabase != null)
            {
                var roomKey = $"AITraining:LiveClassRoom:{classId}";
                await _redisDatabase.HashSetAsync(roomKey, session.UserId.ToString(), json);
                await _redisDatabase.KeyExpireAsync(roomKey, TimeSpan.FromHours(4));
            }
            else
            {
                lock (MemoryRoomParticipantsLock)
                {
                    var dict = MemoryRoomParticipants.GetOrAdd(classId, _ => new ConcurrentDictionary<Guid, ParticipantSession>());
                    dict[session.UserId] = session;
                }
            }
        }

        private async Task RemoveParticipantSession(string classId, Guid userId, string connectionId)
        {
            if (_redisDatabase != null)
            {
                var roomKey = $"AITraining:LiveClassRoom:{classId}";
                var sessionVal = await _redisDatabase.HashGetAsync(roomKey, userId.ToString());
                if (sessionVal.HasValue)
                {
                    var session = JsonSerializer.Deserialize<ParticipantSession>(sessionVal.ToString());
                    if (session != null && session.ConnectionId == connectionId)
                    {
                        await _redisDatabase.HashDeleteAsync(roomKey, userId.ToString());
                    }
                }

                var userKey = $"AITraining:LiveClassUser:{userId}";
                var userVal = await _redisDatabase.StringGetAsync(userKey);
                if (userVal.HasValue)
                {
                    var parts = userVal.ToString().Split(':');
                    if (parts.Length > 0 && parts[0] == connectionId)
                    {
                        await _redisDatabase.KeyDeleteAsync(userKey);
                    }
                }
            }
            else
            {
                lock (MemoryRoomParticipantsLock)
                {
                    if (MemoryRoomParticipants.TryGetValue(classId, out var dict))
                    {
                        if (dict.TryGetValue(userId, out var session) && session.ConnectionId == connectionId)
                        {
                            dict.TryRemove(userId, out _);
                        }
                    }
                }
                lock (MemoryUserConnectionsLock)
                {
                    if (MemoryUserConnections.TryGetValue(userId, out var userVal))
                    {
                        var parts = userVal.Split(':');
                        if (parts.Length > 0 && parts[0] == connectionId)
                        {
                            MemoryUserConnections.TryRemove(userId, out _);
                        }
                    }
                }
            }
        }

        private async Task<List<ParticipantSession>> GetRoomParticipants(string classId)
        {
            var list = new List<ParticipantSession>();

            if (_redisDatabase != null)
            {
                var roomKey = $"AITraining:LiveClassRoom:{classId}";
                var entries = await _redisDatabase.HashGetAllAsync(roomKey);
                foreach (var entry in entries)
                {
                    if (entry.Value.HasValue)
                    {
                        var session = JsonSerializer.Deserialize<ParticipantSession>(entry.Value.ToString());
                        if (session != null)
                        {
                            list.Add(session);
                        }
                    }
                }
            }
            else
            {
                lock (MemoryRoomParticipantsLock)
                {
                    if (MemoryRoomParticipants.TryGetValue(classId, out var dict))
                    {
                        list.AddRange(dict.Values);
                    }
                }
            }

            return list;
        }

        private async Task RegisterConnectionMetadata(string connectionId, Guid userId, string classId, string userName)
        {
            var value = $"{userId}:{classId}:{userName}";
            if (_redisDatabase != null)
            {
                var connKey = $"AITraining:LiveClassConnection:{connectionId}";
                await _redisDatabase.StringSetAsync(connKey, value, TimeSpan.FromHours(4));
            }
            else
            {
                MemoryConnectionToUser[connectionId] = value;
            }
        }

        private async Task<string?> GetConnectionMetadata(string connectionId)
        {
            if (_redisDatabase != null)
            {
                var connKey = $"AITraining:LiveClassConnection:{connectionId}";
                var val = await _redisDatabase.StringGetAsync(connKey);
                return val.HasValue ? val.ToString() : null;
            }
            else
            {
                MemoryConnectionToUser.TryGetValue(connectionId, out var value);
                return value;
            }
        }

        private async Task RemoveConnectionMetadata(string connectionId)
        {
            if (_redisDatabase != null)
            {
                await _redisDatabase.KeyDeleteAsync($"AITraining:LiveClassConnection:{connectionId}");
            }
            else
            {
                MemoryConnectionToUser.TryRemove(connectionId, out _);
            }
        }

        private void EnforceRateLimit(string action, int limitPerTenSeconds)
        {
            var connectionId = Context.ConnectionId;
            var key = $"{connectionId}:{action}";
            var now = DateTime.UtcNow;

            Throttles.AddOrUpdate(key,
                (1, now),
                (k, old) =>
                {
                    if ((now - old.LastReset).TotalSeconds >= 10)
                    {
                        return (1, now);
                    }
                    if (old.MessageCount >= limitPerTenSeconds)
                    {
                        throw new HubException($"Rate limit exceeded for action: {action}. Please slow down.");
                    }
                    return (old.MessageCount + 1, old.LastReset);
                });
        }

        private void CleanConnectionThrottle(string connectionId)
        {
            var keysToRemove = Throttles.Keys.Where(k => k.StartsWith(connectionId + ":")).ToList();
            foreach (var key in keysToRemove)
            {
                Throttles.TryRemove(key, out _);
            }
        }

        private async Task TerminateAttendanceSession(Guid liveClassId, Guid userId)
        {
            try
            {
                var openAttendance = await _context.LiveClassAttendances
                    .Where(a => a.LiveClassId == liveClassId && a.UserId == userId && a.LeaveTime == null)
                    .OrderByDescending(a => a.JoinTime)
                    .FirstOrDefaultAsync();

                if (openAttendance != null)
                {
                    openAttendance.LeaveTime = DateTime.UtcNow;
                    openAttendance.DurationInMinutes = (openAttendance.LeaveTime.Value - openAttendance.JoinTime).TotalMinutes;
                    _context.LiveClassAttendances.Update(openAttendance);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not terminate attendance session for user {UserId} and live class {LiveClassId}", userId, liveClassId);
            }
        }
    }

    public class ParticipantSession
    {
        public string ConnectionId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = "Student";
    }
}
