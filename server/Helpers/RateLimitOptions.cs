namespace QuattroLingo.Helpers
{
    /// <summary>Bound from the "RateLimiting" configuration section. Every limit is per minute.</summary>
    public class RateLimitOptions
    {
        public const string SectionName = "RateLimiting";

        public bool Enabled { get; set; } = true;

        /// <summary>Register and login, per client IP (brute force, account spam).</summary>
        public int AuthPerMinute { get; set; } = 20;

        /// <summary>Room lookup by code and joining a room, per client IP (join code guessing).
        /// Kept high on purpose: a whole classroom behind one NAT joins within a minute.</summary>
        public int RoomLookupPerMinute { get; set; } = 120;

        /// <summary>New SignalR connections, per client IP.</summary>
        public int HubConnectPerMinute { get; set; } = 120;

        /// <summary>All requests, per user (or per IP when anonymous).</summary>
        public int GlobalPerMinute { get; set; } = 300;

        /// <summary>JoinRoom calls one SignalR connection may make.</summary>
        public int HubJoinAttemptsPerConnection { get; set; } = 30;

        /// <summary>Addresses ("172.18.0.3") or subnets ("172.18.0.0/16") of reverse proxies (nginx, Caddy)
        /// whose X-Forwarded-For is trusted.
        /// Loopback is always trusted. Leave empty when nothing sits in front of the server.</summary>
        public string[] TrustedProxies { get; set; } = [];
    }
}
