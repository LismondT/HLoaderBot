using System.Globalization;
using System.Xml;



namespace HLoaderBot
{
    interface IDataReader
    {
        public int? GetInfoChatTopicId();
        public long? GetMainChatId();

        public InfoChatMessage? GetInfoChatMessage(InfoChatMessageType type);
        public Title? GetTitle(int id);

        public List<Title> GetTitlesList();
        public int? GetCount();
    }


    //Singletone
    class DataReader : IDataReader
    {
        private static DataReader? _instance;
        private static IDataReader? _reader;

        public static DataReader Instance => _instance ??= new DataReader();

        DataReader()
        {
            _reader = null;
        }

        public void SetDataReader(IDataReader ireader)
        {
            _reader ??= ireader;
        }

        public int? GetInfoChatTopicId()
        {
            if (_reader == null)
            {
                Logger.Log($"[DataReaderERROR](GetInfoChatTopicId): reader == null", ConsoleColor.Red);
                return null;
            }

            return _reader.GetInfoChatTopicId();
        }

        public long? GetMainChatId()
        {
            if (_reader == null)
            {
                Logger.Log($"[DataReaderERROR](GetMainChatId): reader == null", ConsoleColor.Red);
                return null;
            }

            return _reader.GetMainChatId();
        }

        public InfoChatMessage? GetInfoChatMessage(InfoChatMessageType type)
        {
            if (_reader == null)
            {
                Logger.Log($"[DataReaderERROR](GetInfoChatMessage): reader == null", ConsoleColor.Red);
                return null;
            }

            return _reader.GetInfoChatMessage(type);
        }

        public Title? GetTitle(int id)
        {
            if (_reader == null)
            {
                Logger.Log($"[DataReaderERROR](GetTitle): reader == null", ConsoleColor.Red);
                return null;
            }

            return _reader.GetTitle(id);
        }

        public List<Title> GetTitlesList()
        {
            if (_reader == null)
            {
                Logger.Log($"[DataReaderERROR](GetTitlesList): reader == null", ConsoleColor.Red);
                return new();
            }

            return _reader.GetTitlesList();
        }

        public int? GetCount()
        {
            if (_reader == null)
            {
                Logger.Log($"[DataReaderERROR](GetCount): reader == null", ConsoleColor.Red);
                return null;
            }

            return _reader.GetCount();
        }
    }


    #region realizations for DataReader

    class XmlDataReader : IDataReader
    {
        private static string? FILE_PATH;

        public XmlDataReader(string path)
        {
            FILE_PATH = path;
        }

        public int? GetInfoChatTopicId()
        {
            if (FILE_PATH == null)
            {
                Logger.Log("[XML_ERROR] FILE_PATH == null", ConsoleColor.Red);
                return null;
            }

            XmlDocument doc = new();
            string? topicIdStr;
            int topicId;
            doc.Load(FILE_PATH);

            XmlNodeList mainNode = doc.GetElementsByTagName("Main");
            XmlNode? titlesChatNode = mainNode.Item(0)?.ChildNodes.Item(0);

            if (titlesChatNode == null)
            {
                Logger.Log("[XML_ERROR](GetInfoChatTopicId) titles chat node not found", ConsoleColor.Red);
                return null;
            }

            topicIdStr = titlesChatNode.Attributes?.GetNamedItem("topicId")?.Value;

            if (topicIdStr == null)
            {
                Logger.Log("[XML_ERROR](GetInfoChatTopicId) titles chat node has not attr(\"topicId\")", ConsoleColor.Red);
                return null;
            }

            topicId = int.Parse(topicIdStr);

            return topicId;
        }
        public long? GetMainChatId()
        {
            if (FILE_PATH == null)
            {
                Logger.Log("[XML_ERROR] FILE_PATH == null", ConsoleColor.Red);
                return null;
            }

            XmlDocument doc = new();
            string? chatIdStr;
            long chatId;
            doc.Load(FILE_PATH);

            XmlNode? mainNode = doc.GetElementsByTagName("Main").Item(0);

            if (mainNode == null)
            {
                Logger.Log("[XML_ERROR](GetMainChatId) <Main> not found", ConsoleColor.Red);
                return null;
            }

            chatIdStr = mainNode.Attributes["chatId"].Value;

            if (chatIdStr == null)
            {
                Logger.Log("[XML_ERROR](GetMainChatId) <Main> attr(\"chatId\") not found", ConsoleColor.Red);
                return null;
            }

            chatId = long.Parse(chatIdStr);
            return chatId;
        }

        public InfoChatMessage? GetInfoChatMessage(InfoChatMessageType type)
        {
            XmlDocument doc = new();
            InfoChatMessage outMessage = new()
            {
                messageId = 0,
                Items = new()
            };

            doc.Load(FILE_PATH);

            string messageNodeName = type switch
            {
                InfoChatMessageType.AllTags => "TagsMessage",
                InfoChatMessageType.AllGroups => "GroupsMessage",
                InfoChatMessageType.AllArtists => "ArtistsMessage",
                InfoChatMessageType.AllCharacters => "CharactersMessage",
                InfoChatMessageType.AllParodies => "ParodiesMessage",
                _ => "",
            };

            XmlNode? mainNode = doc.GetElementsByTagName("Main")[0];

            if (mainNode == null)
            {
                Logger.Log("[XML_ERROR](GetInfoChatMessage): <Main> node is not exist", ConsoleColor.Red);
                return null;
            }

            XmlNodeList? InfoChatNodeList = mainNode.ChildNodes.Item(0)?.ChildNodes;

            if (InfoChatNodeList == null)
            {
                Logger.Log("[XML_ERROR](GetInfoChatMessage): <InfoChat> node is not exist", ConsoleColor.Red);
                return null;
            }
            if (InfoChatNodeList.Count == 0)
            {
                Logger.Log("[XML_ERROR](GetInfoChatMessage): <InfoChat> child nodes is not exist", ConsoleColor.Red);
                return null;
            }


            foreach (XmlNode node in InfoChatNodeList)
            {
                if (node.Name != messageNodeName) continue;

                string? id = node.Attributes?.GetNamedItem("messageId")?.Value;

                if (id == null)
                {
                    Logger.Log("[XML_ERROR](GetInfoChatMessage): <InfoChatMessage> has not attr(\"messageId\")", ConsoleColor.Red);
                    return null;
                };

                outMessage.messageId = int.Parse(id);

                foreach (XmlNode itemNode in node.ChildNodes)
                {
                    if (itemNode.Name != "Item") continue;

                    string name = itemNode.InnerText;
                    string? countStr = itemNode.Attributes?.GetNamedItem("count")?.Value;
                    int count;


                    if (countStr == null)
                    {
                        Logger.Log("[XML_ERROR](GetInfoChatMessage): <InfoChatMessageItem> has not attr(\"count\")", ConsoleColor.Red);
                        return null;
                    }

                    count = int.Parse(countStr);

                    outMessage.Items[name] = count;
                }
            }

            return outMessage;
        }

        public Title? GetTitle(int id)
        {
            if (GetCount() == 0) return null;
            if (FILE_PATH == null)
            {
                Logger.Log("[XML_ERROR] FILE_PATH == null", ConsoleColor.Red);
                return null;
            }

            Title title = new();
            XmlDocument doc = new();
            XmlNode? mainNode;
            XmlNodeList mainChildNodesList;

            doc.Load(FILE_PATH);

            mainNode = doc.GetElementsByTagName("Main")[0];

            if (mainNode == null)
            {
                Logger.Log("[XML_ERROR](GetTitle): <Main> is not exist", ConsoleColor.Red);
                return null;
            }

            mainChildNodesList = mainNode.ChildNodes;

            foreach (XmlNode mainChildNode in mainChildNodesList)
            {
                if (mainChildNode.Name != "Titles") continue;

                foreach (XmlNode titleNode in mainChildNode.ChildNodes)
                {
                    if (titleNode.Name != "Title") continue;

                    XmlAttributeCollection titleAttr = titleNode.Attributes;
                    string idStr = titleAttr["id"].Value;
                    int titleId = int.Parse(idStr);

                    if (titleId != id) continue;

                    string topicIdStr = titleAttr["topicId"].Value;
                    string webIdStr = titleAttr["webId"].Value;

                    string tagsStr = "";
                    string parodiesStr = "";
                    string groupsStr = "";
                    string charactersStr = "";
                    string artistsStr = "";
                    string languagesStr = "";
                    string categoriesStr = "";

                    List<Page> pages = new();
                    int viewPage = -1;
                    int pagesCount = -1;

                    foreach (XmlNode childNode in titleNode.ChildNodes)
                    {
                        string nodeName = childNode.Name;

                        if (nodeName == "Name")
                            title.Name = childNode.InnerText;

                        if (nodeName == "Tags")
                            tagsStr = childNode.InnerText;

                        if (nodeName == "Parodies")
                            parodiesStr = childNode.InnerText;

                        if (nodeName == "Groups")
                            groupsStr = childNode.InnerText;

                        if (nodeName == "Characters")
                            charactersStr = childNode.InnerText;

                        if (nodeName == "Artists")
                            artistsStr = childNode.InnerText;

                        if (nodeName == "Languages")
                            languagesStr = childNode.InnerText;

                        if (nodeName == "Categories")
                            categoriesStr = childNode.InnerText;

                        if (nodeName == "Pages")
                        {
                            XmlAttributeCollection PagesAttr = childNode.Attributes;
                            string pagesCountStr = PagesAttr["count"].Value;
                            string viewPageStr = PagesAttr["viewPage"].Value;

                            pagesCount = int.Parse(pagesCountStr);
                            viewPage = int.Parse(viewPageStr);

                            foreach (XmlNode pageNode in childNode)
                            {
                                if (pageNode.Name != "Page") continue;

                                Page page = new();
                                XmlAttributeCollection pageAttr = pageNode.Attributes;

                                string fileId = pageAttr["fileId"].Value;
                                string messageIdStr = pageAttr["messageId"].Value;
                                string number = pageAttr["number"].Value;

                                page.fileId = fileId;
                                page.messageId = int.Parse(messageIdStr);
                                page.number = int.Parse(number);

                                pages.Add(page);
                            }
                        }
                    }

                    title.DataId = id;
                    title.TopicId = int.Parse(topicIdStr);
                    title.WebId = int.Parse(webIdStr);

                    title.Tags = InfoStrToList(tagsStr);
                    title.Parodies = InfoStrToList(parodiesStr);
                    title.Groups = InfoStrToList(groupsStr);
                    title.Characters = InfoStrToList(charactersStr);
                    title.Artists = InfoStrToList(artistsStr);
                    title.Languages = InfoStrToList(languagesStr);
                    title.Categories = InfoStrToList(categoriesStr);

                    title.ViewPage = viewPage;
                    title.PagesCount = pagesCount;
                    title.Pages = pages;
                }
            }

            return title;


            static List<string> InfoStrToList(string str)
            {
                List<string> list = new();

                string[] parts = str.Split(' ');
                foreach (string part in parts)
                {
                    if (part.StartsWith('#'))
                    {
                        list.Add(part);
                    }
                }

                return list;
            }
        }

        public List<Title> GetTitlesList()
        {
            int? count = GetCount();
            List<Title> titles = new();


            if (count == null || count == 0)
            {
                Logger.Log("[Warning] no title exist", ConsoleColor.Magenta);
                return titles;
            }

            for (int i = 0; i < count; i++)
            {
                Title? title = GetTitle(i);
                if (title == null)
                {
                    Logger.Log($"[Warning] no title has id: {i}", ConsoleColor.Magenta);
                    continue;
                }
                titles.Add(title.Value);
            }

            return titles;
        }

        public int? GetCount()
        {
            if (FILE_PATH == null)
            {
                Logger.Log("[XML_ERROR] FILE_PATH == null", ConsoleColor.Red);
                return null;
            }

            int count = 0;
            XmlDocument doc = new();

            doc.Load(FILE_PATH);

            XmlNode? mainNode = doc.GetElementsByTagName("Main")[0];

            if (mainNode == null)
            {
                Logger.Log("[XML_ERROR](GetCount): <Main> is not exist", ConsoleColor.Red);
                return null;
            }

            XmlNodeList mainChildNodesList = mainNode.ChildNodes;

            foreach (XmlNode mainChildNode in mainChildNodesList)
            {
                if (mainChildNode.Name != "Titles") continue;

                foreach (XmlNode titleNode in mainChildNode.ChildNodes)
                {
                    if (titleNode.Name != "Title") continue;
                    count++;
                }
            }

            return count;
        }
    }

    #endregion


    class TokenDataParser
    {
        public TokenDataParser() { }

        public static string GetToken(string path)
        {
            string token = "";
            XmlDocument doc = new();

            try
            {
                doc.Load(path);
            }
            catch
            {
                Logger.Log($"[ERROR](path: {path}) Token was not found", ConsoleColor.Red);
                return token;
            }

            XmlNodeList nodeList = doc.GetElementsByTagName("Bot");

            if (nodeList.Count == 0)
            {
                Logger.Log($"[ERROR](path: {path}) Token was not found", ConsoleColor.Red);
                return "";
            }

            XmlNodeList? children = nodeList[0]?.ChildNodes;

            if (children == null)
            {
                Logger.Log($"[ERROR] <Token /> was not found", ConsoleColor.Red);
                return "";
            }

            foreach (XmlNode child in children)
            {
                if (child.Name == "Token")
                {
                    token = child.InnerText;
                }
            }

            if (token == "")
            {
                Logger.Log("[ERROR] Token was not readed", ConsoleColor.Red);
            }
            else
            {
                Logger.Log("[SUCCESS] Token was readed", ConsoleColor.Green, false);
            }

            return token;
        }
    }
}