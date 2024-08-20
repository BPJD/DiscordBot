// See https://aka.ms/new-console-template for more information

/*     private const ulong GuildId = 878506826545655859; // 서버 ID
       private const ulong ChannelId = 878506916303749150; // 채널 ID
        private const ulong RoleIdPatch = 1175695651636514886; // Patch 역할 ID
        private const ulong RoleIdNotice = 1175695614567256095; // Notice 역할 ID
        private const ulong RoleIdTserver = 1272571651464368262; // 역할 ID
        private const ulong RoleIdSunday = 1272571687891636265; // 역할 ID
        private const ulong RoleIdCash = 1272571721353793587; // 역할 ID
테스트용 ID

*/

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static System.Collections.Specialized.BitVector32;
using System.Threading.Channels;
using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;
using JsonConverter = Newtonsoft.Json.JsonConverter;

namespace DiscordBot
{
    class Program
    {
        // 상수 및 설정 값

        private const ulong ChannelId = 1173925554144170065; // 채널 ID
        private const ulong RoleIdPatch = 1173919883667439676; // Patch 역할 ID
        private const ulong RoleIdNotice = 1174961036185587752; // Notice 역할 ID
        private const ulong RoleIdTserver = 1175027509612527637; // 역할 ID
        private const ulong RoleIdSunday = 1174008549295263814; // 역할 ID
        private const ulong RoleIdCash = 1175993825227976765; // 역할 ID

        private const string imgPatch = "https://media.discordapp.net/attachments/1072220784262664253/1177153740520833084/FileDownloader.png?ex=66b9159a&is=66b7c41a&hm=b8f70c8323e0117ba7e136425294f1a5bd3288dad63834a7fe4ae7241addbded&=&format=webp&quality=lossless&width=810&height=162";
        private const string imgNotice = "https://media.discordapp.net/attachments/1072220784262664253/1177152621560221786/images.jpg?ex=66b9148f&is=66b7c30f&hm=2e76682efdcdd8b27112cf6dd3bc33a45f00c343eb3b13de1746e399419c8354&=&format=webp";
        private const string imgTserver = "https://media.discordapp.net/attachments/1072220784262664253/1272572564044255345/gm_news_04.png?ex=66bb770e&is=66ba258e&hm=6146a79ba6234e1b35ea938bdd6ffd9673cdfdb77e82dcff49144e62186a6699&=&format=webp&quality=lossless&width=810&height=162";
        private const string imgSunday = "https://media.discordapp.net/attachments/1072220784262664253/1272576354088652974/UF0Eb6kspUwnfJ5-pcSPrCH8pgyNOgwGIG4p_qp-ogbz0QPPFQ_VUqeiVGxidwFNZYhOTdr6zcwttVqvoZ1h1g.png?ex=66bb7a96&is=66ba2916&hm=e694da35296a14cb6dc43782d8e166e67ea5d53b2debe2a64ac88f49d5f68537&=&format=webp&quality=lossless&width=810&height=608";
        private static string[] emojies = { ":wrench:", ":adhesive_bandage:", ":sun_with_face:", ":test_tube:", ":money_with_wings:", ":money_with_wings:" };

        private static readonly string[] ArmorClasses = { "전사", "마법사", "궁수", "도적", "해적" };
        private static readonly string[] ArmorParts = { "견장", "장갑", "망토", "신발" };
        private static readonly string[] WeaponList = {
            "샤이닝로드", "튜너", "브레스슈터", "소울슈터", "데스페라도", "에너지소드",
            "ESP리미터", "체인", "매직건틀렛", "부채", "한손 검", "한손 도끼", "한손 둔기",
            "단검", "케인", "완드", "스태프", "두손 검", "두손 도끼", "두손 둔기", "차크람",
            "창", "폴암", "활", "석궁", "아대", "너클", "건", "듀얼보우건", "핸드캐논",
            "건틀렛리볼버", "에인션트보우", "에센스"
        };




        private DiscordSocketClient _client;
        private Random _rand = new Random();

        public static async Task Main(string[] args)
        {

            var program = new Program();
            await program.RunBotAsync();
        }

        public async Task RunBotAsync()
        {

            string token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Bot token is not set in environment variables.");
                return;
            }


            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds
            });

            _client.Log += LogAsync;
            _client.SlashCommandExecuted += SlashCommandHandlerAsync;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.Ready += async () =>
            {
                // Ready 이벤트 핸들러는 가능한 빠르게 처리하고, 오래 걸리는 작업은 백그라운드에서 실행
                _ = Task.Run(async () =>
                {
                    await RegisterCommandsAsync();

                    var apiService = new NexonApiService();
                    while (true)
                    {
                        await CheckAndSendNotices(apiService);
                        await Task.Delay(180000); // 3분마다 확인
                    }
                });
            };

            await Task.Delay(-1); // 블로킹을 방지하기 위해 무한 대기
        }

        private async Task RegisterCommandsAsync()
        {
            string GuildID = Environment.GetEnvironmentVariable("GUILD_ID");
            
            if (string.IsNullOrEmpty(GuildID))
            {
                Console.WriteLine("Guild ID is not set in environment variables.");
                return;
            }

            ulong _id;
            bool success = ulong.TryParse(GuildID, out _id);

            if(success)
            {
                Console.WriteLine("Guild ID Loaded successfully.");
            }
            else
            {
                Console.WriteLine("Guild ID Error!");
            }

            var guild = _client.GetGuild(_id);

            var commands = new List<SlashCommandBuilder>
            {
                new SlashCommandBuilder().WithName("방어구상자").WithDescription("앱방상/아방상에서 뽑을 방어구를 랜덤으로 골라서 추천합니다."),
                new SlashCommandBuilder().WithName("무기상자").WithDescription("앱무상/아무상에서 뽑을 무기를 랜덤으로 골라서 추천합니다.")
            };

            foreach (var command in commands)
            {
                try
                {
                    await guild.CreateApplicationCommandAsync(command.Build());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error registering command '{command.Name}': {ex.Message}");
                }
            }

            Console.WriteLine("Slash commands registered successfully.");
        }



        private async Task CheckAndSendNotices(NexonApiService apiService)
        {
            var newNotices = await apiService.GetNoticesAsync();
            var newTserver = await apiService.GetTserverAsync();
            var noticeUpdates = await apiService.GetNoticeUpdatesAsync();
            var eventUpdates = await apiService.GetSundayAsync();
            var noticeCashes = await apiService.GetCashshopAsync();

            if (newNotices.Any())
            {
                foreach (var notice in newNotices)
                {
                    await SendEmbedMessageAsync(notice, $"<@&{RoleIdNotice}>", imgNotice, 0);
                }
            }

            if (newTserver.Any())
            {
                foreach (var notice in newTserver)
                {
                    await SendEmbedMessageAsync(notice, $"<@&{RoleIdTserver}>", imgTserver, 3);
                }
            }

            if (noticeUpdates.Any())
            {
                foreach (var update in noticeUpdates)
                {
                    await SendEmbedMessageAsync(update, $"<@&{RoleIdPatch}>", imgPatch, 1);
                }
            }
            if (eventUpdates.Any())
            {
                foreach (var sunday in eventUpdates)
                {
                    await SendEmbedMessageAsync(sunday, $"<@&{RoleIdSunday}>", imgSunday, 2);
                }
            }
            if (noticeCashes.Any())
            {
                foreach (var cash in noticeCashes)
                {
                    await SendEmbedMessageAsync(cash, $"<@&{RoleIdCash}>", "", 4);
                }
            }

        }

        private async Task SlashCommandHandlerAsync(SocketSlashCommand command)
        {
            switch (command.Data.Name)
            {
                case "방어구상자":
                    await command.RespondAsync($"[ {ArmorClasses[_rand.Next(ArmorClasses.Length)]} {ArmorParts[_rand.Next(ArmorParts.Length)]} ] 당첨!");
                    break;
                case "무기상자":
                    await command.RespondAsync($"[ {WeaponList[_rand.Next(WeaponList.Length)]} ] 당첨!");
                    break;
                case "ping":
                    await command.RespondAsync("Pong!");
                    break;
                default:
                    await command.RespondAsync("Unknown command.");
                    break;
            }
        }

        public void ThumbnailURLGet(int type)
        {

        }

        private async Task SendEmbedMessageAsync(object noticeOrUpdate, string mention, string imageUrl, int type)
        {

            var channel = _client.GetChannel(ChannelId) as IMessageChannel;
            if (channel == null)
            {
                Console.WriteLine("Channel not found!");
                return;
            }

            var embedBuilder = new EmbedBuilder()
                //.WithDescription($"{mention} {message}")
                .WithTimestamp(DateTimeOffset.Now)
                .WithColor(Color.Blue)
                .WithImageUrl(imageUrl);

            if (noticeOrUpdate is Notice notice)
            {
                embedBuilder.WithTitle(notice.Title).WithUrl(notice.Url);
                embedBuilder.WithColor(Color.LightGrey);
            }
            else if (noticeOrUpdate is NoticeUpdate update)
            {
                embedBuilder.WithTitle(update.Title).WithUrl(update.Url);
                embedBuilder.WithColor(Color.Blue);
            }
            else if (noticeOrUpdate is EventNotice sunday)
            {
                embedBuilder.WithTitle(sunday.Title).WithUrl(sunday.Url);
                embedBuilder.WithDescription($"{sunday.Date}");
                embedBuilder.WithColor(Color.Red);
            }
            else if (noticeOrUpdate is CashshopNotice cash)
            {
                embedBuilder.WithTitle(cash.Title).WithUrl(cash.Url);
                embedBuilder.WithDescription($"{cash.Date}");
                embedBuilder.WithColor(Color.Gold);
            }

            await channel.SendMessageAsync($"[ {emojies[type]} | {mention} ]", embed: embedBuilder.Build());

        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
    }


    public class NoticeIdConverter : Newtonsoft.Json.JsonConverter<long?>
    {
        public override long? ReadJson(JsonReader reader, Type objectType, long? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Integer)
            {
                return Convert.ToInt64(reader.Value);
            }
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing notice_id.");
        }

        public override void WriteJson(JsonWriter writer, long? value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }
    }

    public class CashshopNotice
    {
        public string Title { get; set; }
        public string Url { get; set; }

        [JsonPropertyName("notice_id")]
        public long NoticeId { get; set; }

        public DateTime Date { get; set; }
        public DateTime DateSaleStart { get; set; }
        public DateTime DateSaleEnd { get; set; }
        public bool OngoingFlag { get; set; }
    }

    public class EventNotice
    {
        public string Title { get; set; }
        public string Url { get; set; }

        [JsonPropertyName("notice_id")]
        public long NoticeId { get; set; }

        public DateTime Date { get; set; }
        public DateTime DateEventStart { get; set; }
        public DateTime DateEventEnd { get; set; }
    }

    public class NoticeUpdate
    {
        public string Title { get; set; }
        public string Url { get; set; }

        [JsonPropertyName("notice_id")]
        public long NoticeId { get; set; }

        public DateTime Date { get; set; }
    }

    public class Notice
    {
        public string Title { get; set; }
        public string Url { get; set; }

        [JsonPropertyName("notice_id")]
        public long NoticeId { get; set; }

        public DateTime Date { get; set; }
    }

    public class NexonApiResponse
    {
        public List<Notice> Notice { get; set; }

        [JsonProperty("update_notice")]
        public List<NoticeUpdate> NoticeUpdates { get; set; }

        [JsonProperty("event_notice")]
        public List<EventNotice> NoticeEvent { get; set; }

        [JsonProperty("cashshop_notice")]
        public List<CashshopNotice> NoticeCashshop { get; set; }
    }

    public class NexonApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _filePath = "notice_ids.txt";
        private readonly HashSet<long> _previousNoticeIds;

        private const string noticeUrl = "https://open.api.nexon.com/maplestory/v1/notice";

        private const string updateUrl = "https://open.api.nexon.com/maplestory/v1/notice-update";

        private const string sundayUrl = "https://open.api.nexon.com/maplestory/v1/notice-event";

        private const string cashUrl = "https://open.api.nexon.com/maplestory/v1/notice-cashshop";

        public NexonApiService()
        {

            string apiKey = Environment.GetEnvironmentVariable("API_KEY");

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("API Key is not set in environment variables.");
                return;
            }

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("x-nxopen-api-key", apiKey);

            _previousNoticeIds = LoadPreviousNoticeIds();
        }


        public async Task<List<Notice>> GetNoticesAsync()
        {
            try
            {
                // API 호출
                var response = await _httpClient.GetAsync(noticeUrl);

                // HTTP 상태 코드 확인
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to fetch data. Status code: {response.StatusCode}");
                    return new List<Notice>();
                }

                // JSON 응답 읽기
                var jsonResponse = await response.Content.ReadAsStringAsync();

                // JSON을 JObject로 파싱
                var jObject = JObject.Parse(jsonResponse);

                // Notice 목록을 생성
                var newNotices = new List<Notice>();

                // 공지 배열을 순회
                foreach (var item in jObject["notice"])
                {
                    // NoticeId 추출
                    long noticeId = item["notice_id"]?.Value<long>() ?? 0;
                    string title = item["title"]?.Value<string>();
                    string url = item["url"]?.Value<string>();
                    DateTime date = item["date"]?.Value<DateTime>() ?? DateTime.MinValue;

                    // 필터링 조건 확인
                    if (!_previousNoticeIds.Contains(noticeId) && title.Contains("패치예정"))
                    {
                        Console.WriteLine($"Notice ID: {noticeId}, Title: {title}");

                        // Notice 객체 생성
                        var notice = new Notice
                        {
                            NoticeId = noticeId,
                            Title = title,
                            Url = url,
                            Date = date
                        };

                        // 리스트에 추가
                        newNotices.Add(notice);
                    }
                    else
                    {
                       // Console.WriteLine($"Skipped Notice: ID={noticeId}, Title={title}");
                    }
                }

                if (!newNotices.Any())
                {
                    Console.WriteLine("No new notices to process.");
                }

                // 새 공지의 ID 저장
                SaveNewNoticeIds(newNotices.Select(n => n.NoticeId));

                return newNotices;
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return new List<Notice>();
            }
        }


        private const string tServerURL = "https://maplestory.nexon.com/Testworld/Main";

        public async Task<List<Notice>> GetTserverAsync()
        {
            try
            {
                // API 호출
                var response = await _httpClient.GetAsync(noticeUrl);

                // HTTP 상태 코드 확인
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to fetch data. Status code: {response.StatusCode}");
                    return new List<Notice>();
                }

                // JSON 응답 읽기
                var jsonResponse = await response.Content.ReadAsStringAsync();

                // JSON을 JObject로 파싱
                var jObject = JObject.Parse(jsonResponse);

                // Notice 목록을 생성
                var newNotices = new List<Notice>();

                // 공지 배열을 순회
                foreach (var item in jObject["notice"])
                {
                    // NoticeId 추출
                    long noticeId = item["notice_id"]?.Value<long>() ?? 0;
                    string title = item["title"]?.Value<string>();
                    string url = tServerURL;
                    DateTime date = item["date"]?.Value<DateTime>() ?? DateTime.MinValue;

                    // 필터링 조건 확인
                    if (!_previousNoticeIds.Contains(noticeId) && title.Contains("테스트월드"))
                    {
                        Console.WriteLine($"Notice ID: {noticeId}, Title: {title}");

                        // Notice 객체 생성
                        var notice = new Notice
                        {
                            NoticeId = noticeId,
                            Title = title,
                            Url = url,
                            Date = date
                        };

                        // 리스트에 추가
                        newNotices.Add(notice);
                    }
                    else
                    {
                     //   Console.WriteLine($"Skipped Notice: ID={noticeId}, Title={title}");
                    }
                }

                if (!newNotices.Any())
                {
                    Console.WriteLine("No new notices to process.");
                }

                // 새 공지의 ID 저장
                SaveNewNoticeIds(newNotices.Select(n => n.NoticeId));

                return newNotices;
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return new List<Notice>();
            }
        }

        public async Task<List<NoticeUpdate>> GetNoticeUpdatesAsync()
        {
            try
            {
                // API 호출
                var response = await _httpClient.GetAsync(updateUrl);

                // HTTP 상태 코드 확인
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to fetch data. Status code: {response.StatusCode}");
                    return new List<NoticeUpdate>();
                }

                // JSON 응답 읽기
                var jsonResponse = await response.Content.ReadAsStringAsync();

                // JSON을 JObject로 파싱
                var jObject = JObject.Parse(jsonResponse);

                // Notice 목록을 생성
                var newNotices = new List<NoticeUpdate>();

                // 공지 배열을 순회
                foreach (var item in jObject["update_notice"])
                {
                    // NoticeId 추출
                    long noticeId = item["notice_id"]?.Value<long>() ?? 0;
                    string title = item["title"]?.Value<string>();
                    string url = item["url"]?.Value<string>();
                    DateTime date = item["date"]?.Value<DateTime>() ?? DateTime.MinValue;

                    // 필터링 조건 확인
                    if (!_previousNoticeIds.Contains(noticeId) && title.Contains("업데이트"))
                    {
                        Console.WriteLine($"Notice ID: {noticeId}, Title: {title}");

                        // Notice 객체 생성
                        var notice = new NoticeUpdate
                        {
                            NoticeId = noticeId,
                            Title = title,
                            Url = url,
                            Date = date
                        };

                        // 리스트에 추가
                        newNotices.Add(notice);
                    }
                    else
                    {
                      //  Console.WriteLine($"Skipped Notice: ID={noticeId}, Title={title}");
                    }
                }

                if (!newNotices.Any())
                {
                    Console.WriteLine("No new notices to process.");
                }

                // 새 공지의 ID 저장
                SaveNewNoticeIds(newNotices.Select(n => n.NoticeId));

                return newNotices;
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return new List<NoticeUpdate>();
            }
        }

        public async Task<List<EventNotice>> GetSundayAsync()
        {
            try
            {
                // API 호출
                var response = await _httpClient.GetAsync(sundayUrl);

                // HTTP 상태 코드 확인
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to fetch data. Status code: {response.StatusCode}");
                    return new List<EventNotice>();
                }

                // JSON 응답 읽기
                var jsonResponse = await response.Content.ReadAsStringAsync();

                // JSON을 JObject로 파싱
                var jObject = JObject.Parse(jsonResponse);

                // Notice 목록을 생성
                var newNotices = new List<EventNotice>();

                // 공지 배열을 순회
                foreach (var item in jObject["event_notice"])
                {
                    // NoticeId 추출
                    long noticeId = item["notice_id"]?.Value<long>() ?? 0;
                    string title = item["title"]?.Value<string>();
                    string url = item["url"]?.Value<string>();
                    DateTime date = item["date"]?.Value<DateTime>() ?? DateTime.MinValue;

                    // 필터링 조건 확인
                    if (!_previousNoticeIds.Contains(noticeId) && title.Contains("썬데이"))
                    {
                        Console.WriteLine($"Notice ID: {noticeId}, Title: {title}");

                        // Notice 객체 생성
                        var notice = new EventNotice
                        {
                            NoticeId = noticeId,
                            Title = title,
                            Url = url,
                            Date = date
                        };

                        // 리스트에 추가
                        newNotices.Add(notice);
                    }
                    else
                    {
                       // Console.WriteLine($"Skipped Notice: ID={noticeId}, Title={title}");
                    }
                }

                if (!newNotices.Any())
                {
                    Console.WriteLine("No new notices to process.");
                }

                // 새 공지의 ID 저장
                SaveNewNoticeIds(newNotices.Select(n => n.NoticeId));

                return newNotices;
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return new List<EventNotice>();
            }
        }

        public async Task<List<CashshopNotice>> GetCashshopAsync()
        {
            try
            {
                // API 호출
                var response = await _httpClient.GetAsync(cashUrl);

                // HTTP 상태 코드 확인
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to fetch data. Status code: {response.StatusCode}");
                    return new List<CashshopNotice>();
                }

                // JSON 응답 읽기
                var jsonResponse = await response.Content.ReadAsStringAsync();

                // JSON을 JObject로 파싱
                var jObject = JObject.Parse(jsonResponse);

                // Notice 목록을 생성
                var newNotices = new List<CashshopNotice>();

                // 공지 배열을 순회
                foreach (var item in jObject["cashshop_notice"])
                {
                    // NoticeId 추출
                    long noticeId = item["notice_id"]?.Value<long>() ?? 0;
                    string title = item["title"]?.Value<string>();
                    string url = item["url"]?.Value<string>();
                    DateTime date = item["date"]?.Value<DateTime>() ?? DateTime.MinValue;

                    // 필터링 조건 확인
                    if (!_previousNoticeIds.Contains(noticeId) && title.Contains(""))
                    {
                        Console.WriteLine($"Notice ID: {noticeId}, Title: {title}");

                        // Notice 객체 생성
                        var notice = new CashshopNotice
                        {
                            NoticeId = noticeId,
                            Title = title,
                            Url = url,
                            Date = date
                        };

                        // 리스트에 추가
                        newNotices.Add(notice);
                    }
                    else
                    {
                      //  Console.WriteLine($"Skipped Notice: ID={noticeId}, Title={title}");
                    }
                }

                if (!newNotices.Any())
                {
                    Console.WriteLine("No new notices to process.");
                }

                // 새 공지의 ID 저장
                SaveNewNoticeIds(newNotices.Select(n => n.NoticeId));

                return newNotices;
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return new List<CashshopNotice>();
            }
        }

        private HashSet<long> LoadPreviousNoticeIds()
        {
            var noticeIds = new HashSet<long>();
            if (File.Exists(_filePath))
            {
                foreach (var line in File.ReadAllLines(_filePath))
                {
                    if (int.TryParse(line, out int noticeId))
                    {
                        noticeIds.Add(noticeId);
                    }
                }
            }
            return noticeIds;
        }

        private void SaveNewNoticeIds(IEnumerable<long> noticeIds)
        {
            using (var writer = new StreamWriter(_filePath, true))
            {
                foreach (var id in noticeIds.Where(id => _previousNoticeIds.Add(id)))
                {
                    writer.WriteLine(id);
                }
            }
        }
    }



}