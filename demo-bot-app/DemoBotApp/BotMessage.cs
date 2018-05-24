using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DemoBotApp
{
    public class BotMessage
    {
        public string Watermark
        {
            get;
            set;
        }

        public string Text
        {
            get;
            set;
        }

        public string ReplyToId
        {
            get;
            set;
        }
    }
}