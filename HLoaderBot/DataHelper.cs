using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HLoaderBot
{
    class DataHelper
    {
        public static void TitleToTagsSync()
        {
            List<Title> titles = DataReader.getInstance().GetTitlesList();

            if (titles.Count == 0)
            {
                Logger.Log("[Warrning] no title exist", ConsoleColor.Magenta);
                return;
            }

            DataWriter.Instance.DeleteAllTags();

            foreach (Title title in titles)
            {
                DataWriter.Instance.AddTagsToInfoChat(title);
            }

        }
    }
}
