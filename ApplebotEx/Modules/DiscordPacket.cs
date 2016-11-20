using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApplebotEx.Modules
{
    public class DiscordPacket
    {
        public class GuildDelete
        {
            [JsonProperty("id")]
            public string ID;

            [JsonProperty("unavailable")]
            public bool IsUnavailable;
        }

        public class Message
        {
            [JsonProperty("id")]
            public string ID;

            [JsonProperty("channel_id")]
            public string ChannelID;

            [JsonProperty("author")]
            public User Author;

            [JsonProperty("content")]
            public string Content;

            [JsonProperty("timestamp")]
            public DateTime TimeStamp;

            [JsonProperty("edited_timestamp")]
            public DateTime? EditedTimeStamp;

            [JsonProperty("tts")]
            public bool IsTTS;

            [JsonProperty("mention_everyone")]
            public bool IsMentionEveryone;

            [JsonProperty("mentions")]
            public User[] Mentions;

            [JsonProperty("mention_roles")]
            public Role[] MentionRoles;

            [JsonProperty("attachments")]
            public Attachment[] Attachments;
        }

        public class Embed
        {
            [JsonProperty("title")]
            public string Title;

            [JsonProperty("type")]
            public string Type;

            [JsonProperty("description")]
            public string Description;

            [JsonProperty("url")]
            public string Url;

            [JsonProperty("thumbnail")]
            public EmbedThumbnail Thumbnail;

            [JsonProperty("provider")]
            public EmbedProvider Provider;
        }

        public class EmbedProvider
        {
            [JsonProperty("name")]
            public string Name;

            [JsonProperty("url")]
            public string Url;
        }

        public class EmbedThumbnail
        {
            [JsonProperty("url")]
            public string Url;

            [JsonProperty("proxy_url")]
            public string ProxyUrl;

            [JsonProperty("height")]
            public int Height;

            [JsonProperty("width")]
            public int Width;
        }

        public class Attachment
        {
            [JsonProperty("id")]
            public string ID;

            [JsonProperty("filename")]
            public string Filename;

            [JsonProperty("size")]
            public int Size;

            [JsonProperty("url")]
            public string Url;

            [JsonProperty("proxy_url")]
            public string ProxyUrl;

            [JsonProperty("height")]
            public int? Height;

            [JsonProperty("width")]
            public int? Width;
        }

        public class Game
        {
            [JsonProperty("name")]
            public string Name;
        }

        public class StatusUpdate
        {
            [JsonProperty("status")]
            public string Status = "online";

            [JsonProperty("since")]
            public int Since = 0;

            [JsonProperty("game")]
            public Game Game;

            [JsonProperty("afk")]
            public bool AFK = false;
        }

        public class User
        {
            [JsonProperty("id")]
            public string ID;

            [JsonProperty("username")]
            public string Username;

            [JsonProperty("discriminator")]
            public string Discriminator;

            [JsonProperty("avatar")]
            public string Avatar;

            [JsonProperty("bot")]
            public bool IsBot;

            [JsonProperty("mfa_enabled")]
            public bool IsMFAEnabled;

            [JsonProperty("verified")]
            public bool IsVerified;

            [JsonProperty("email")]
            public string Email;
        }

        public class Guild
        {
            [JsonProperty("id")]
            public string ID;

            [JsonProperty("name")]
            public string Name;

            [JsonProperty("icon")]
            public string IconHash;

            [JsonProperty("splash")]
            public string SplashHash;

            [JsonProperty("owner_id")]
            public string OwnerID;

            [JsonProperty("region")]
            public string Region;

            [JsonProperty("afk_channel_id")]
            public string AfkChannelID;

            [JsonProperty("afk_timeout")]
            public int AfkTimeout;

            [JsonProperty("embed_enabled")]
            public bool EmbedEnabled;

            [JsonProperty("embed_channel_id")]
            public string EmbedChannelID;

            [JsonProperty("verification_level")]
            public int VerificationLevel;

            [JsonProperty("voice_states")]
            public VoiceState[] VoiceStates;

            [JsonProperty("roles")]
            public Role[] Roles;

            [JsonProperty("emojis")]
            public Emoji[] Emojis;

            [JsonProperty("features")]
            public string[] Features;

            [JsonProperty("unavailable")]
            public bool IsUnavailable;
        }

        public class Emoji
        {
            [JsonProperty("id")]
            public string ID;

            [JsonProperty("name")]
            public string Name;

            [JsonProperty("roles")]
            public string RoleIDs;

            [JsonProperty("require_colons")]
            public bool RequiresColons;

            [JsonProperty("managed")]
            public bool IsManaged;
        }

        public class Role
        {
            [JsonProperty("id")]
            public string ID;

            [JsonProperty("name")]
            public string Name;

            [JsonProperty("Color")]
            public int Color;

            [JsonProperty("hoist")]
            public bool IsHoist;

            [JsonProperty("position")]
            public int Position;

            [JsonProperty("managed")]
            public bool IsManaged;

            [JsonProperty("mentionable")]
            public bool IsMentionable;
        }

        public class VoiceState
        {
            [JsonProperty("guild_id")]
            public string GuildID;

            [JsonProperty("channel_id")]
            public string ChannelID;

            [JsonProperty("user_id")]
            public string UserID;

            [JsonProperty("session_id")]
            public string SessionID;

            [JsonProperty("deaf")]
            public bool IsDeaf;

            [JsonProperty("mute")]
            public bool IsMuted;

            [JsonProperty("self_deaf")]
            public bool IsSelfDeaf;

            [JsonProperty("self_mute")]
            public bool IsSelfMuted;

            [JsonProperty("suppress")]
            public bool IsSuppressed;
        }

        public class DMChannel
        {
            [JsonProperty("id")]
            public string ID;

            [JsonProperty("is_private")]
            public bool IsPrivate;

            [JsonProperty("recipient")]
            public User Recipient;

            [JsonProperty("last_message_id")]
            public string LastMessageID;
        }

        public class GuildChannel
        {
            [JsonProperty("id")]
            public string ID;

            [JsonProperty("guild_id")]
            public string GuildID;

            [JsonProperty("name")]
            public string Name;

            [JsonProperty("type")]
            public string Type;

            [JsonProperty("position")]
            public int Position;

            [JsonProperty("is_private")]
            public bool IsPrivate;

            [JsonProperty("permission_overwrites")]
            public Overwrite[] PermissionOverwrites;

            [JsonProperty("topic")]
            public string Topic;

            [JsonProperty("last_message_id")]
            public string LastMessageID;

            [JsonProperty("bitrate")]
            public int Bitrate;

            [JsonProperty("user_limit")]
            public int UserLimit;
        }

        public class Overwrite
        {
            [JsonProperty("id")]
            public string ID;

            [JsonProperty("type")]
            public string Type;

            [JsonProperty("allow")]
            public int Allow;

            [JsonProperty("deny")]
            public int Deny;
        }


        public class UnavaliableGuild
        {
            [JsonProperty("id")]
            public string ID;

            [JsonProperty("unavailable")]
            public bool IsUnavailable;
        }

        public class Ready
        {
            [JsonProperty("v")]
            public int Version;

            [JsonProperty("user")]
            public User User;

            [JsonProperty("private_channels")]
            public DMChannel[] PrivateChannels;

            [JsonProperty("guilds")]
            public UnavaliableGuild[] Guilds;

            [JsonProperty("heartbeat_interval")]
            public int HeartbeatInterval;
        }

        [JsonRequired]
        [JsonProperty("op")]
        public int OPCode;

        [JsonRequired]
        [JsonProperty("d")]
        public object Data;

        [JsonProperty("t")]
        public string Type;

        [JsonProperty("s")]
        public int SequenceNumber;
    }
}
