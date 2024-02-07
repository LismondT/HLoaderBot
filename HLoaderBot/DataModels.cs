namespace HLoaderBot
{
    enum InfoChatMessageType
    {
        AllTags,
        AllGroups,
        AllArtists,
        AllParodies,
        AllCharacters
    }

    struct InfoChatMessage
    {
        public long messageId;
        public Dictionary<string, int> Items;
    }

    struct Title
    {
        public int DataId;
        public int WebId;
        public int TopicId;
        public int PagesCount;
        public List<Page> Pages;
        public int ViewPage;

        public string Name;
        public List<string> Tags;
        public List<string> Groups;
        public List<string> Characters;
        public List<string> Artists;
        public List<string> Parodies;
        public List<string> Languages;
        public List<string> Categories;
    }

    struct Page
    {
        public int number;
        public string fileId;
        public int messageId;
    }
}
