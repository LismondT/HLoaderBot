using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLoaderBot
{
    class MessageBuilder
    {
        public static string InfoMessageParse(Title title)
        {
            string infoMessage = "";

            string tagsStr = title.Tags != null ? string.Join(" ", title.Tags) : "";
            string groupsStr = title.Groups != null ? string.Join(" ", title.Groups) : "";
            string charactersStr = title.Characters != null ? string.Join(" ", title.Characters) : "";
            string artistsStr = title.Artists != null ? string.Join(" ", title.Artists) : "";
            string parodiesStr = title.Parodies != null ? string.Join(" ", title.Parodies) : "";
            string categoriesStr = title.Categories != null ? string.Join(" ", title.Categories) : "";
            string languagesStr = title.Languages != null ? string.Join(" ", title.Languages) : "";

            infoMessage += $"Name: {title.Name}\n";
            infoMessage += $"Pages: {title.PagesCount} | Id: {title.DataId}\n";
            infoMessage += tagsStr == "" ? "" : $"Tags: {tagsStr}\n";
            infoMessage += groupsStr == "" ? "" : $"Groups: {groupsStr}\n";
            infoMessage += charactersStr == "" ? "" : $"Characters: {charactersStr}\n";
            infoMessage += artistsStr == "" ? "" : $"Artists: {artistsStr}\n";
            infoMessage += parodiesStr == "" ? "" : $"Parodies: {parodiesStr}\n";
            infoMessage += categoriesStr == "" ? "" : $"Categories: {categoriesStr}\n";
            infoMessage += languagesStr == "" ? "" : $"Languages: {languagesStr}\n";

            return infoMessage;
        }

        public static string TagsMessageParse(InfoChatMessageType type)
        {
            InfoChatMessage? message = DataReader.Instance.GetInfoChatMessage(type);
            string tagsMessage = "";

            foreach(KeyValuePair<string, int> entry in message.Value.Items)
            {
                string tagName = entry.Key;
                int tagCount = entry.Value;
                tagsMessage += $"| {tagName} ({tagCount})\n";
            }

            return tagsMessage;
        }
    }
}
