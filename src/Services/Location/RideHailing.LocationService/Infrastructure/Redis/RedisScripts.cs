namespace RideHailing.LocationService.Infrastructure.Redis;

internal static class RedisScripts
{
    public const string ConnectDriver = """
        local lifecycleKey = KEYS[1]
        local stateKey = KEYS[2]
        local connectionsKey = KEYS[3]
        local heartbeatKey = KEYS[4]

        local expectedLifecycle = ARGV[1]
        local connectionId = ARGV[2]
        local heartbeatTtl = tonumber(ARGV[3])
        local offlineState = ARGV[4]
        local updatedAt = ARGV[5]

        local lifecycle = redis.call('GET', lifecycleKey)
        if lifecycle ~= expectedLifecycle then return -1 end

        local state = redis.call('HGET', stateKey, 'state')
        if not state then
            redis.call('HSET', stateKey, 'state', offlineState, 'updatedAtUtc', updatedAt)
        end

        redis.call('SADD', connectionsKey, connectionId)
        redis.call('SET', heartbeatKey, '1', 'EX', heartbeatTtl)

        return 1
        """;

    public const string DisconnectDriver = """
        local stateKey = KEYS[1]
        local connectionsKey = KEYS[2]
        local availableLocationsKey = KEYS[3]

        local driverId = ARGV[1]
        local connectionId = ARGV[2]
        local offlineState = ARGV[3]
        local availableState = ARGV[4]
        local updatedAt = ARGV[5]

        redis.call('SREM', connectionsKey, connectionId)
        local connectionCount = redis.call('SCARD', connectionsKey)

        if connectionCount == 0 then
            local state = redis.call('HGET', stateKey, 'state')
            if state == availableState then
                redis.call('HSET', stateKey, 'state', offlineState, 'updatedAtUtc', updatedAt)
                redis.call('ZREM', availableLocationsKey, driverId)
            end
        end

        return connectionCount
        """;

    public const string SetAvailable = """
        local lifecycleKey = KEYS[1]
        local stateKey = KEYS[2]
        local locationKey = KEYS[3]
        local heartbeatKey = KEYS[4]
        local connectionsKey = KEYS[5]
        local availableLocationsKey = KEYS[6]

        local driverId = ARGV[1]
        local expectedLifecycle = ARGV[2]
        local busyState = ARGV[3]
        local offlineState = ARGV[4]
        local availableState = ARGV[5]
        local updatedAt = ARGV[6]

        local lifecycle = redis.call('GET', lifecycleKey)
        if lifecycle ~= expectedLifecycle then
            return {-1, -1}
        end

        local connectionCount = redis.call('SCARD', connectionsKey)
        if connectionCount == 0 then
            return {-2, -1}
        end

        local heartbeatExists = redis.call('EXISTS', heartbeatKey)
        if heartbeatExists == 0 then
            return {-3, -1}
        end

        local latitude = redis.call('HGET', locationKey, 'latitude')
        local longitude = redis.call('HGET', locationKey, 'longitude')
        if not latitude or not longitude then
            return {-4, -1}
        end

        local state = redis.call('HGET', stateKey, 'state')
        if not state then
            state = offlineState
        end

        if state ~= busyState and state ~= offlineState then
            return {-5, tonumber(state)}
        end

        redis.call('HSET', stateKey, 'state', availableState, 'updatedAtUtc', updatedAt)
        redis.call('GEOADD', availableLocationsKey, longitude, latitude, driverId)

        return {1, tonumber(state)}
        """;

    public const string SetOffline = """
        local lifecycleKey = KEYS[1]
        local stateKey = KEYS[2]
        local availableLocationsKey = KEYS[3]

        local driverId = ARGV[1]
        local expectedLifecycle = ARGV[2]
        local offlineState = ARGV[3]
        local availableState = ARGV[4]
        local updatedAt = ARGV[5]

        local lifecycle = redis.call('GET', lifecycleKey)
        if lifecycle ~= expectedLifecycle then return -1 end

        local state = redis.call('HGET', stateKey, 'state')
        if state == offlineState then
            redis.call('ZREM', availableLocationsKey, driverId)
            return 1
        end

        if state == 'Busy' then return -2 end
        if state ~= availableState then return -3 end

        redis.call('HSET', stateKey, 'state', offlineState, 'updatedAtUtc', updatedAt)
        redis.call('ZREM', availableLocationsKey, driverId)

        return 1
        """;

    public const string SetBusy = """
        local lifecycleKey = KEYS[1]
        local stateKey = KEYS[2]
        local availableLocationsKey = KEYS[3]

        local driverId = ARGV[1]
        local expectedLifecycle = ARGV[2]
        local availableState = ARGV[3]
        local busyState = ARGV[4]
        local updatedAt = ARGV[5]

        local lifecycle = redis.call('GET', lifecycleKey)
        if lifecycle ~= expectedLifecycle then return -1 end

        local state = redis.call('HGET', stateKey, 'state')
        if state ~= availableState then return -2 end

        redis.call('HSET', stateKey, 'state', busyState, 'updatedAtUtc', updatedAt)
        redis.call('ZREM', availableLocationsKey, driverId)

        return 1
        """;

    public const string SetLifecycle = """
        local lifecycleKey = KEYS[1]
        local stateKey = KEYS[2]
        local availableLocationsKey = KEYS[3]

        local driverId = ARGV[1]
        local lifecycle = ARGV[2]
        local offlineState = ARGV[3]
        local updatedAt = ARGV[4]

        redis.call('SET', lifecycleKey, lifecycle)
        if lifecycle ~= 'Active' then
            redis.call('HSET', stateKey, 'state', offlineState, 'updatedAtUtc', updatedAt)
            redis.call('ZREM', availableLocationsKey, driverId)
        end

        return 1
        """;

    public const string UpdateLocation = """
        local lifecycleKey = KEYS[1]
        local stateKey = KEYS[2]
        local locationKey = KEYS[3]
        local heartbeatKey = KEYS[4]
        local availableLocationsKey = KEYS[5]

        local driverId = ARGV[1]
        local expectedLifecycle = ARGV[2]
        local latitude = ARGV[3]
        local longitude = ARGV[4]
        local updatedAt = ARGV[5]
        local heartbeatTtl = tonumber(ARGV[6])

        local lifecycle = redis.call('GET', lifecycleKey)
        if lifecycle ~= expectedLifecycle then return -1 end

        redis.call('HSET', locationKey, 'latitude', latitude, 'longitude', longitude, 'updatedAtUtc', updatedAt)
        redis.call('SET', heartbeatKey, '1', 'EX', heartbeatTtl)

        local state = redis.call('HGET', stateKey, 'state')
        if state == 'Available' then
            redis.call('GEOADD', availableLocationsKey, longitude, latitude, driverId)
        end

        return 1
        """;

    public const string UpdateHeartbeat = """
        local lifecycleKey = KEYS[1]
        local heartbeatKey = KEYS[2]

        local expectedLifecycle = ARGV[1]
        local heartbeatTtl = tonumber(ARGV[2])

        local lifecycle = redis.call('GET', lifecycleKey)
        if lifecycle ~= expectedLifecycle then return -1 end

        redis.call('SET', heartbeatKey, '1', 'EX', heartbeatTtl)
        return 1
        """;
}