using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HLoaderBot
{
    interface IDataWriter
    {
        public bool WriteToData(Title title);
        public bool AddTagsToInfoChat(InfoChatMessageType type, List<string> tags);
        public bool AddTagsToInfoChat(Title title);
        public bool DeleteAllTags();
        public void InitData(long chatId, int infoChatTopicId);
    }

    class DataWriter : IDataWriter
    {
        private static IDataWriter? _writer;
        private static DataWriter? _instance;

        public static DataWriter Instance => _instance ?? (_instance = new DataWriter());
        
        public void SetWriter(IDataWriter writer)
        {
            _writer = writer;
        }

        public bool WriteToData(Title title)
        {
            if (_writer == null) return false;
            return _writer.WriteToData(title);
        }

        public bool AddTagsToInfoChat(InfoChatMessageType type, List<string> tags)
        {
            if (_writer == null) return false;
            return _writer.AddTagsToInfoChat(type, tags);
        }

        public bool AddTagsToInfoChat(Title title)
        {
            if (_writer == null) return false;
            return _writer.AddTagsToInfoChat(title);
        }

        public bool DeleteAllTags()
        {
            if (_writer == null) return false;
            return _writer.DeleteAllTags();
        }

        public void InitData(long chatId, int infoChatTopicId)
        {
            if (_writer == null) return;
            _writer.InitData(chatId, infoChatTopicId);
        }
    }

    class XmlDataWriter : IDataWriter
    {
        private readonly string? _filename;
        private readonly string? _filepath;
        private string? FILE_PATH => _filepath + _filename;

        Dictionary<InfoChatMessageType, string> _nodeInfoChatMsg = new()
        {
            { InfoChatMessageType.AllTags, "TagsMessage" },
            { InfoChatMessageType.AllGroups, "GroupsMessage" },
            { InfoChatMessageType.AllArtists, "ArtistsMessage" },
            { InfoChatMessageType.AllCharacters, "CharactersMessage" },
            { InfoChatMessageType.AllParodies, "ParodiesMessage" },
        };

        public XmlDataWriter(string path, string filename)
        {
            _filepath = path;
            _filename = filename ?? string.Empty;
        }

        public bool WriteToData(Title title)
        {
            if (FILE_PATH == null) return false;

            XmlDocument doc = new XmlDocument();
            doc.Load(FILE_PATH);
            
            XmlNode? titlesNode = doc.SelectSingleNode("/Main/Titles");
            

            if (titlesNode == null)
            {
                Logger.Log("[XML_ERROR](XmlDataWriter.WriteToData) <Titles> not exist", ConsoleColor.Red);
                return false;
            }


            XmlNode titleNode = doc.CreateElement("Title");

            XmlAttribute idAttr = doc.CreateAttribute("id");
            XmlAttribute topicIdAttr = doc.CreateAttribute("topicId");
            XmlAttribute webIdAttr = doc.CreateAttribute("webId");

            XmlNode nameNode = doc.CreateElement("Name");
            XmlNode tagsNode = doc.CreateElement("Tags");
            XmlNode parodiesNode = doc.CreateElement("Parodies");
            XmlNode groupsNode = doc.CreateElement("Groups");
            XmlNode charactersNode = doc.CreateElement("Characters");
            XmlNode artistsNode = doc.CreateElement("Artists");
            XmlNode languagesNode = doc.CreateElement("Languages");
            XmlNode categoriesNode = doc.CreateElement("Categories");

            XmlNode pagesNode = doc.CreateElement("Pages");
            XmlAttribute countAttr = doc.CreateAttribute("count");
            XmlAttribute viewPageAttr = doc.CreateAttribute("viewPage");


            idAttr.Value = title.DataId.ToString();
            topicIdAttr.Value = title.TopicId.ToString();
            webIdAttr.Value = title.WebId.ToString();

            nameNode.InnerText = title.Name.ToString();
            tagsNode.InnerText = title.Tags != null ? string.Join(" ", title.Tags) : "";
            parodiesNode.InnerText = title.Parodies != null ? string.Join(" ", title.Parodies) : "";
            groupsNode.InnerText = title.Groups != null ? string.Join(" ", title.Groups) : "";
            charactersNode.InnerText = title.Characters != null ? string.Join(" ", title.Characters) : "";
            artistsNode.InnerText = title.Artists != null ? string.Join(" ", title.Artists) : "";
            languagesNode.InnerText = title.Languages != null ? string.Join(" ", title.Languages) : "";
            categoriesNode.InnerText = title.Categories != null ? string.Join(" ", title.Categories) : "";
            
            countAttr.Value = title.PagesCount.ToString();
            viewPageAttr.Value = title.ViewPage.ToString();


            titleNode.Attributes?.Append(idAttr);
            titleNode.Attributes?.Append(topicIdAttr);
            titleNode.Attributes?.Append(webIdAttr);

            titleNode.AppendChild(nameNode);
            titleNode.AppendChild(tagsNode);
            titleNode.AppendChild(parodiesNode);
            titleNode.AppendChild(groupsNode);
            titleNode.AppendChild(charactersNode);
            titleNode.AppendChild(artistsNode);
            titleNode.AppendChild(languagesNode);
            titleNode.AppendChild(categoriesNode);

            pagesNode.Attributes?.Append(countAttr);
            pagesNode.Attributes?.Append(viewPageAttr);

            foreach(Page page in title.Pages)
            {
                XmlNode pageNode = doc.CreateElement("Page");
                XmlAttribute fileIdAttr = doc.CreateAttribute("fileId");
                XmlAttribute messageIdAttr = doc.CreateAttribute("messageId");
                XmlAttribute numberAttr = doc.CreateAttribute("number");

                fileIdAttr.Value = page.fileId.ToString();
                messageIdAttr.Value = page.messageId.ToString();
                numberAttr.Value = page.number.ToString();

                pageNode.Attributes?.Append(fileIdAttr);
                pageNode.Attributes?.Append(messageIdAttr);
                pageNode.Attributes?.Append(numberAttr);

                pagesNode.AppendChild(pageNode);
            }

            titleNode.AppendChild(pagesNode);
            titlesNode.AppendChild(titleNode);


            doc.Save(FILE_PATH);
            return true;
        }

        public bool AddTagsToInfoChat(InfoChatMessageType type, List<string> tags)
        {
            if (tags == null)
            {
                return false;
            }

            XmlDocument doc = new XmlDocument();
            InfoChatMessage? chatMessage = DataReader.getInstance().GetInfoChatMessage(type);
            
            doc.Load(FILE_PATH);

            XmlNode messageNode = doc.SelectSingleNode($"/Main/InfoChat/{_nodeInfoChatMsg[type]}");

            for (int i = 0; i < tags.Count; i++)
            {
                var tag = tags[i];
                
                if (chatMessage.Value.Items.ContainsKey(tag))
                {
                    int count = chatMessage.Value.Items[tag];
                    chatMessage.Value.Items[tag] = count + 1;
                }
                else
                {
                    chatMessage.Value.Items[tag] = 1;
                }
            }

            messageNode.InnerText = "";

            foreach (KeyValuePair<string, int> entry in chatMessage.Value.Items)
            {
                string tagName = entry.Key;
                string tagCount = entry.Value.ToString();

                XmlNode tagNode = doc.CreateElement("Item");
                XmlAttribute countAttr = doc.CreateAttribute("count");

                countAttr.Value = tagCount;

                tagNode.Attributes.Append(countAttr);
                tagNode.InnerText = tagName;

                messageNode.AppendChild(tagNode);
            }

            doc.Save(FILE_PATH);

            return true;
        }

        public bool AddTagsToInfoChat(Title title)
        {
            List<string> tags = title.Tags;
            List<string> groups = title.Groups;
            List<string> characters = title.Characters;
            List<string> artists = title.Artists;
            List<string> parodies = title.Parodies;

            AddTagsToInfoChat(InfoChatMessageType.AllTags, tags);
            AddTagsToInfoChat(InfoChatMessageType.AllGroups, groups);
            AddTagsToInfoChat(InfoChatMessageType.AllCharacters, characters);
            AddTagsToInfoChat(InfoChatMessageType.AllArtists, artists);
            AddTagsToInfoChat(InfoChatMessageType.AllParodies, parodies);

            return true;
        }

        public bool DeleteAllTags()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(FILE_PATH);

            foreach(KeyValuePair<InfoChatMessageType, string> entry in _nodeInfoChatMsg)
            {
                XmlNode? messageNode = doc.SelectSingleNode($"/Main/InfoChat/{entry.Value}");

                if (messageNode == null)
                {
                    return false;
                }

                messageNode.InnerText = "";
            }

            doc.Save(FILE_PATH);
            return true;
        }

        public void InitData(long chatId, int infoChatTopicId)
        {
            XmlDocument doc = new();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "utf-8", null);

            doc.AppendChild(dec);

            XmlNode mainNode = doc.CreateElement("Main");
            XmlNode infoChatNode = doc.CreateElement("InfoChat");
            XmlNode tagsMessageNode = doc.CreateElement("TagsMessage");
            XmlNode groupsMessageNode = doc.CreateElement("GroupsMessage");
            XmlNode artistsMessageNode = doc.CreateElement("ArtistsMessage");
            XmlNode charactersMessageNode = doc.CreateElement("CharactersMessage");
            XmlNode parodiesMessageNode = doc.CreateElement("ParodiesMessage");
            XmlNode titlesNode = doc.CreateElement("Titles");


            XmlAttribute chatIdAttr = doc.CreateAttribute("chatId");
            XmlAttribute infoChatTopicAttr = doc.CreateAttribute("topicId");
            XmlAttribute defaultMessageIdAttr = doc.CreateAttribute("messageId");


            chatIdAttr.Value = chatId.ToString();
            infoChatTopicAttr.Value = infoChatTopicId.ToString();
            defaultMessageIdAttr.Value = "0";

            mainNode.Attributes?.Append(chatIdAttr);
            infoChatNode.Attributes?.Append(infoChatTopicAttr);
            tagsMessageNode.Attributes?.Append(defaultMessageIdAttr);
            groupsMessageNode.Attributes?.Append(defaultMessageIdAttr);
            artistsMessageNode.Attributes?.Append(defaultMessageIdAttr);
            charactersMessageNode.Attributes?.Append(defaultMessageIdAttr);
            parodiesMessageNode.Attributes?.Append(defaultMessageIdAttr);


            infoChatNode.AppendChild(tagsMessageNode);
            infoChatNode.AppendChild(groupsMessageNode);
            infoChatNode.AppendChild(artistsMessageNode);
            infoChatNode.AppendChild(charactersMessageNode);
            infoChatNode.AppendChild(parodiesMessageNode);

            mainNode.AppendChild(infoChatNode);
            mainNode.AppendChild(titlesNode);
            doc.AppendChild(mainNode);

            Directory.CreateDirectory(_filepath);
            doc.Save(FILE_PATH);
        }
    }
}
