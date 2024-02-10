using CommandLine;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace HLoaderBot
{
    interface ICommand
    {
        string Name { get; }
        Task Execute(TelegramBotClient botClient, Message message, string[] args);
    }



    /*  ###########
        /start   
        ###########*/
    class StartCommand : ICommand
    {
        public string Name => "/start";
        public async Task Execute(TelegramBotClient botClient, Message message, string[] args)
        {
            string startMessage =
                "Этот бот создан для сохранения тайтлов с сайта nhnetai.net \n" +
                "Его необходимо привязать к группе, для этого нужно создать группу, включить в ней темы и дать права боту, затем прописать команду /init в группе\n" +
                "После этого будет создана тема \"Все тайтлы\", в которую будет отправляться информация обо всех добавленных тайтлах, а бот будет привязан к ней\n";

            await botClient.SendTextMessageAsync(message.Chat.Id, startMessage);
        }
    }




    /*  ###########
        /mid      
        ###########*/
    class MessageIdCommand : ICommand
    {
        public string Name => "/mid";

        public async Task Execute(TelegramBotClient botClient, Message message, string[] args)
        {
            int thisMid = message.MessageId;
            int? replyToMessageId = message.ReplyToMessage?.MessageId;
            int? topicId = message.MessageThreadId;

            string infoMessage =
                $"this message id: {thisMid} \n" +
                $"reply message id: {replyToMessageId}\n" +
                $"threaded message id: {topicId}";

            await botClient.SendTextMessageAsync(message.Chat.Id, infoMessage, topicId);
        }
    }




    /*  ###########
        /cid      
        ###########*/
    class ChatIdCommand : ICommand
    {
        public string Name => "/cid";

        public async Task Execute(TelegramBotClient botClient, Message message, string[] args)
        {
            var chatId = message.Chat.Id;
            var topicId = message.MessageThreadId;

            string infoMessage =
                $"this chat id: {chatId} \n" +
                $"topic id: {topicId}";

            await botClient.SendTextMessageAsync(message.Chat.Id, infoMessage, topicId);
        }
    }




    /*  ###########
        /add
        ###########
        * --name "<string>" <обязательно, если не указать -topicId>
        * --titleId "<int>" <обязательно>
        * --topicId "<int>" <обязательно, если не указать -name> ? Если тема не указана, то будет создана новая
        * --viewPage "<int>" (0)
        * --firstPage "<int>" (0)
        * --latestPage "<int>" <обязательно>
        * --formate "<string>" (jpg)
        * --info "<Tags: #example #example1 #example2>
        *        <Artists: #exampleArtist>
        *        <Groups: #exampleGroup>"
        * --uniqPages "<formate: 0 1 2 4 5>" (formate="jpg | png")
        * 
        * --serverNumber "<int>" (3) ? https://i3.nhentai.net/galleries/
        * --sendInterval "<int>" (3100)
        * 
     */
    class AddTitleCommand : ICommand
    {
        public string Name => "/add";
        public async Task Execute(TelegramBotClient botClient, Message message, string[] args)
        {
            Title title = new()
            {
                Name = "",
                DataId = DataReader.Instance.GetCount() ?? 0
            };
            List<Page> pages = new();

            string formate = "jpg";
            string serverUrl;

            int firstPage = 1;
            string viewPhotoUrl;

            string info = "";
            Dictionary<int, string> uniqPages = new();

            int serverNumber = 0;
            int sendInterval = 0;

            long mainChatId = DataReader.Instance.GetMainChatId().Value;
            int? infoChatTopicId = DataReader.Instance.GetInfoChatTopicId();//id чата со всеми тайтлами
            int topicId = 0; //id чата этого тайтла


            try
            {
                Parser.Default.ParseArguments<Options>(args)
                       .WithParsed<Options>(o =>
                       {
                           title.Name = o.Name ?? "";
                           title.WebId = o.TitleId;
                           title.PagesCount = o.LatestPage;
                           title.ViewPage = o.ViewPage == 0 ? 1 : o.ViewPage;
                           TitleInfoDataParse(ref title, o.Info ?? "");
                           info = MessageBuilder.InfoMessageParse(title);

                           formate = o.Formate ?? "jpg";
                           firstPage = o.FirstPage == 0 ? 1 : o.FirstPage;
                           uniqPages = UniqPagesParse(o.UniqPages ?? "");

                           serverNumber = o.ServerNumber == 0 ? 3 : o.ServerNumber;
                           sendInterval = o.SendInterval == 0 ? 3100 : o.SendInterval;
                           topicId = o.TopicId;
                       });
            }
            catch (Exception e)
            {
                Logger.Log(e.Message, ConsoleColor.Red);
            }

            if (mainChatId == null)
            {
                Logger.Log($"[CommandError](AddTitleCommand.Execute): mainChatId == null", ConsoleColor.Red);
                return;
            }

            if (infoChatTopicId == null)
            {
                Logger.Log($"[CommandError](AddTitleCommand.Execute): infoChatTopicId == null", ConsoleColor.Red);
            }

            //Проверки
            if (title.WebId == 0 || title.PagesCount == 0 || (title.Name == "" && topicId == 0))
            {
                string invalidCommandMessage = InvalidCommandMessage();
                await botClient.SendTextMessageAsync(message.Chat.Id, invalidCommandMessage);
                return;
            }


            if (topicId == 0)
            {
                ForumTopic topic = await botClient.CreateForumTopicAsync(mainChatId, title.Name);
                topicId = topic.MessageThreadId;
            }

            title.TopicId = topicId;

            serverUrl = $"https://i{serverNumber}.nhentai.net/galleries/{title.WebId}/";
            viewPhotoUrl = serverUrl + $"{title.ViewPage}.{formate}";

            InlineKeyboardButton toTitleBtn = new("Перейти к тайтлу")
            {
                Url = $"https://t.me/c/1662106396/{topicId}"
            };
            InlineKeyboardMarkup infoChatKeyboard = new(toTitleBtn);

            
            //Отправка в чат тайтла
            try
            {
                Message infoTopicMsg = await botClient.SendTextMessageAsync(mainChatId, info, topicId);
                await botClient.PinChatMessageAsync(mainChatId, infoTopicMsg.MessageId, true);
            }
            catch (Exception e)
            {
                Logger.Log(e.Message, ConsoleColor.Red);
            }
            

            for (int i = firstPage; i <= title.PagesCount; i++)
            {
                if (!uniqPages.TryGetValue(i, out string? thisFormate))
                    thisFormate = formate;

                string url = serverUrl + $"{i}.{thisFormate}";
                Message m = await botClient.SendPhotoAsync(mainChatId, new InputFileUrl(url), topicId);

                Page page = new()
                {
                    number = i,
                    messageId = m.MessageId,
                    fileId = m.Photo?[0].FileId ?? ""
                };
                pages.Add(page);

                await Task.Delay(sendInterval);
            }

            title.Pages = pages;


            //Отправка в чат с информацией
            Message viewPhotoMessage = await botClient.SendPhotoAsync(mainChatId,
                new InputFileUrl(viewPhotoUrl),
                infoChatTopicId, info,
                replyMarkup: infoChatKeyboard);


            //запись тайтла в базу данных
            if (DataWriter.Instance.WriteToData(title))
            {
                Logger.Log("[Command](/add) Title was added to db", ConsoleColor.Green);
            }

            DataHelper.TitleToTagsSync();
            ChangeTagsMessages(botClient, mainChatId);
        }


        private static async void ChangeTagsMessages(ITelegramBotClient botClient, long chatId)
        {
            string tagsMsgStr = "Tags:\n" + MessageBuilder.TagsMessageParse(InfoChatMessageType.AllTags);
            string groupsMsgStr = "Groups:\n" + MessageBuilder.TagsMessageParse(InfoChatMessageType.AllGroups);
            string artistsMsgStr = "Artists:\n" + MessageBuilder.TagsMessageParse(InfoChatMessageType.AllArtists);
            string charactersMsgStr = "Characters:\n" + MessageBuilder.TagsMessageParse(InfoChatMessageType.AllCharacters);
            string parodiesMsgStr = "Parodies:\n" + MessageBuilder.TagsMessageParse(InfoChatMessageType.AllParodies);

            int tagsMsgId = DataReader.Instance.GetInfoChatMessage(InfoChatMessageType.AllTags).Value.messageId;
            int groupsMsgId = DataReader.Instance.GetInfoChatMessage(InfoChatMessageType.AllGroups).Value.messageId;
            int artistsMsgId = DataReader.Instance.GetInfoChatMessage(InfoChatMessageType.AllArtists).Value.messageId;
            int charactersMsgId = DataReader.Instance.GetInfoChatMessage(InfoChatMessageType.AllCharacters).Value.messageId;
            int parodiesMsgId = DataReader.Instance.GetInfoChatMessage(InfoChatMessageType.AllParodies).Value.messageId;

            try { await botClient.EditMessageTextAsync(chatId, tagsMsgId, tagsMsgStr); } catch { };
            try { await botClient.EditMessageTextAsync(chatId, groupsMsgId, groupsMsgStr); } catch { };
            try { await botClient.EditMessageTextAsync(chatId, artistsMsgId, artistsMsgStr); } catch { };
            try { await botClient.EditMessageTextAsync(chatId, charactersMsgId, charactersMsgStr); } catch { };
            try { await botClient.EditMessageTextAsync(chatId, parodiesMsgId, parodiesMsgStr); } catch { };
        }


        private static Title TitleInfoDataParse(ref Title title, string message)
        {
            string pattern = @"<([^>]*)>";
            MatchCollection matches = Regex.Matches(message, pattern);

            foreach (Match match in matches.Cast<Match>())
            {
                string messageData = match.Groups[1].Value;
                string[] messageDataParts = messageData.Split(' ');
                string type = messageDataParts[0].ToLower();

                List<string> titleData = new();

                foreach (string titleDataPart in messageDataParts)
                {
                    if (titleDataPart.StartsWith('#'))
                    {
                        titleData.Add(titleDataPart.Replace("-", "_"));
                    }
                }

                switch (type)
                {
                    case "tags:": title.Tags = titleData; break;
                    case "groups:": title.Groups = titleData; break;
                    case "characters:": title.Characters = titleData; break;
                    case "artists:": title.Artists = titleData; break;
                    case "parodies:": title.Parodies = titleData; break;
                    case "categories:": title.Categories = titleData; break;
                    case "languages:": title.Languages = titleData; break;
                }
            }

            return title;
        }



        private static Dictionary<int, string> UniqPagesParse(string message)
        {
            string pattern = @"<([^>]*)>";
            MatchCollection matches = Regex.Matches(message, pattern);
            Dictionary<int, string> uniqPages = new();

            foreach (Match match in matches.Cast<Match>())
            {
                string data = match.Groups[1].Value;
                string[] dataParts = data.Split(' ');

                if (dataParts.Length <= 0) continue;

                string formate = dataParts[0];

                if (formate.EndsWith(':'))
                {
                    formate = formate[..^1];
                }

                foreach (string part in dataParts.Skip(1).ToArray())
                {
                    _ = int.TryParse(part, out int page);
                    Console.WriteLine($"@{page} | {formate}");

                    uniqPages[page] = formate;
                }
            }

            return uniqPages;
        }

        public class Options
        {
            [Option('n', "name", Required = false)]
            public string? Name { get; set; }


            [Option('i', "titleId", Required = false, HelpText = "Название тайтла")]
            public int TitleId { get; set; }


            [Option('t', "topicId", Required = false, HelpText = "Название тайтла")]
            public int TopicId { get; set; }


            [Option('v', "viewPage", Required = false, HelpText = "Название тайтла")]
            public int ViewPage { get; set; }


            [Option('f', "firstPage", Required = false, HelpText = "Название тайтла")]
            public int FirstPage { get; set; }


            [Option('l', "latestPage", Required = false, HelpText = "Название тайтла")]
            public int LatestPage { get; set; }


            [Option('F', "formate", Required = false, HelpText = "Название тайтла")]
            public string? Formate { get; set; }


            [Option('I', "info", Required = false, HelpText = "Название тайтла")]
            public string? Info { get; set; }


            [Option('u', "uniqPages", Required = false, HelpText = "Название тайтла")]
            public string? UniqPages { get; set; }


            [Option('U', "server", Required = false, HelpText = "Название тайтла")]
            public int ServerNumber { get; set; }


            [Option('s', "sendInterval", Required = false, HelpText = "Название тайтла")]
            public int SendInterval { get; set; }
        }

        private static string InvalidCommandMessage()
        {
            string message = "";
            message += "Неверно набрана команда, она обязательно должна содержать поля:\n";
            message += "\t--name \"<string>\" || --topicId <int>\n";
            message += "\t--titleId \"<int>\"\n";
            message += "\t--latestPage \"<int>\"\n";
            return message;
        }
    }




    /*  ###########
     *  /tags
     *  ###########
     *  <string> (TypeName) ? Если не указано, то отправятся все теги
     *  TypeName: tags, groups, artists, characters, parodies
     */
    class TagsCommand : ICommand
    {
        public string Name => "/tags";

        public async Task Execute(TelegramBotClient botClient, Message message, string[] args)
        {
            string type = "";
            string tagsMessage = "";

            long chatId = message.Chat.Id;

            if (args.Length >= 2)
            {
                type = args[1];
            }

            //ToDo add for one type of tags

            if (type == "")
            {
                tagsMessage += "Tags:\n";
                tagsMessage += MessageBuilder.TagsMessageParse(InfoChatMessageType.AllTags);
                tagsMessage += "Groups:\n";
                tagsMessage += MessageBuilder.TagsMessageParse(InfoChatMessageType.AllGroups);
                tagsMessage += "Artists:\n";
                tagsMessage += MessageBuilder.TagsMessageParse(InfoChatMessageType.AllArtists);
                tagsMessage += "Characters:\n";
                tagsMessage += MessageBuilder.TagsMessageParse(InfoChatMessageType.AllCharacters);
                tagsMessage += "Parodies:\n";
                tagsMessage += MessageBuilder.TagsMessageParse(InfoChatMessageType.AllParodies);
            }
            else
            {
                switch (type)
                {
                    case "tags":
                        tagsMessage += "Tags:\n";
                        break;

                    case "groups":
                        tagsMessage += "Groups:\n";
                        break;

                    case "artists":
                        tagsMessage += "Artists:\n";
                        break;

                    case "characters":
                        tagsMessage += "Characters:\n";
                        break;

                    case "parodies":
                        tagsMessage += "Parodies:\n";
                        break;

                    default:
                        tagsMessage = "/tags <string> (TypeName) ? Если не указано, то отправятся все теги\nTypeName: tags, groups, artists, characters, parodies";
                        break;
                }
                
                tagsMessage += argTypePair.ContainsKey(type) ? MessageBuilder.TagsMessageParse(argTypePair[type]) : "";
            }

            if (tagsMessage == "") return;

            await botClient.SendTextMessageAsync(chatId, tagsMessage);
        }

        private readonly Dictionary<string, InfoChatMessageType> argTypePair = new()
        {
            {"tags",       InfoChatMessageType.AllTags},
            {"groups",     InfoChatMessageType.AllGroups},
            {"artists",    InfoChatMessageType.AllArtists},
            {"characters", InfoChatMessageType.AllCharacters},
            {"parodies",   InfoChatMessageType.AllParodies}
        };
    }




    /*  ###########
     *  /init
     *  ###########
     *  
     */
    class InitCommand : ICommand
    {
        public string Name => "/init";

        public async Task Execute(TelegramBotClient botClient, Message message, string[] args)
        {
            long chatId = message.Chat.Id;
            ForumTopic topic;

            int[] tagsMsgId = new int[5];

            try
            {
                topic = await botClient.CreateForumTopicAsync(chatId, "Все тайтлы");
                for (int i = 0; i < tagsMsgId.Length; i++)
                {
                    Message m = await botClient.SendTextMessageAsync(chatId, "place for tags", topic.MessageThreadId);
                    tagsMsgId[i] = m.MessageId;
                }
            }
            catch(Exception ex)
            {
                Logger.Log(ex.Message, ConsoleColor.Red);
                await botClient.SendTextMessageAsync(chatId, "Включите в этой группе темы и дайте права боту");
                return;
            }

            DataWriter.Instance.InitData(chatId, topic.MessageThreadId, tagsMsgId);
        }
    }
}
