using Newtonsoft.Json.Serialization;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HLoaderBot
{

    class HLoaderBot
    {
        private static Dictionary<string, ICommand>? commands;
        private static TelegramBotClient? bot;


        public static void Main(string[] args)
        {
            string TOKEN = TokenDataParser.GetToken("./data/init.xml");

            if (TOKEN == "") return;

            using CancellationTokenSource cts = new();
            bot = new TelegramBotClient(TOKEN);

            XmlDataReader reader = new("./data/titles.xml");
            XmlDataWriter writer = new("./data/", "titles.xml");
            DataReader.Instance.SetDataReader(reader);
            DataWriter.Instance.SetWriter(writer);

            commands = new Dictionary<string, ICommand>();
            AddCommands();

            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            bot.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );


            Logger.Log($"[===================]-> [Bot ({bot.GetMeAsync().Result.FirstName}) start recieved] <-[===================] <@{{date}}>", ConsoleColor.Green);
            Console.ReadLine();
            cts.Cancel();
        }


        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (commands == null)
            {
                Logger.Log("[ERROR](HLoaderBot: HandleUpadateAsync) var commands == null", ConsoleColor.Red);
                return;
            }

            if (bot == null)
            {
                Logger.Log("[ERROR](HLoaderBot: HandleUpadateAsync) var bot == null", ConsoleColor.Red);
                return;
            }

            if (update.Message is not { } message)
                return;

            if (message.Text is not { } messageText)
                return;


            long chatId = message.Chat.Id;
            long? userId = message.From?.Id;
            string? userName = message.From?.Username;

            TimeSpan times = DateTime.UtcNow - update.Message.Date;
            string[] args = ParseArgs(message.Text);
            string commandName;
            string messageLog = ParseReceivedMessage(messageText);

            if (args.Length <= 0) return;
            commandName = args[0].ToLower();

            if (times.TotalMinutes > 1)
            {
                Logger.Log($"[Skipped](chat: {chatId}\n\t| userId: {userId}\n\t| userName: {userName}): {messageLog}", ConsoleColor.Gray);
                return;
            }

            Logger.Log($"[Message](chat: {chatId}\n\t| userId: {userId}\n\t| userName: {userName}): {messageLog}", ConsoleColor.Yellow);


            if (commands.TryGetValue(commandName, out ICommand? command))
            {
                await command.Execute(bot, message, args);
                Logger.Log($"[Command]({commandName}) Complete", ConsoleColor.Green);
            }

        }


        private static Task HandlePollingErrorAsync
            (ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"[Telegram API Error]:\n ({apiRequestException.ErrorCode})\n {apiRequestException.Message}",
                _ => exception.ToString()
            };

            Logger.Log(ErrorMessage, ConsoleColor.Red);
            return Task.CompletedTask;
        }

        private static string[] ParseArgs(string message)
        {
            string[] parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            List<string> args = new();

            bool inQuotes = false;
            string buffer = "";

            foreach (string part in parts)
            {
                if (part.StartsWith("\""))
                    inQuotes = true;

                buffer += inQuotes ? part + ' ' : "";

                if (part.EndsWith("\""))
                    inQuotes = false;

                if (!inQuotes)
                {
                    if (buffer != "")
                    {
                        buffer = buffer[..^1];
                        args.Add(buffer.Trim('\"'));
                        buffer = "";
                    }
                    else
                    {
                        args.Add(part);
                    }
                }
            }

            return args.ToArray();
        }

        private static string ParseReceivedMessage(string message)
        {
            string[] parts = message.Replace("\t", "").Split('\n');
            string messageLog = "";

            if (parts.Length == 1)
            {
                return '\'' + parts[0] + '\'';
            }

            foreach (string part in parts)
            {
                messageLog += "\n\t|" + part;
            }

            return messageLog;
        }

        private static void AddCommands()
        {
            if (commands == null)
            {
                Logger.Log("[ERROR] var commands == null", ConsoleColor.Red);
                return;
            }

            AddCommand(new StartCommand());
            AddCommand(new MessageIdCommand());
            AddCommand(new ChatIdCommand());
            AddCommand(new AddTitleCommand());
            AddCommand(new TagsCommand());
            AddCommand(new InitCommand());
        }
        private static void AddCommand(ICommand command)
        {
            if (commands == null)
            {
                Logger.Log("[ERROR] var commands == null", ConsoleColor.Red);
                return;
            }

            commands.Add(command.Name.ToLower(), command);
        }
    }
}