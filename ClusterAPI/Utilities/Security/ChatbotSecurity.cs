﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClusterAPI.Controllers.Security
{
    public class ChatbotSecurity : ISecurityHandler
    {
        public bool Authenticate(IEnumerable<string> enumerable)
        {
            if (enumerable == null | enumerable.First() == "chatbot")
            {
                return false;
            }
            if (enumerable.First().Equals("843iu233d3m4pxb1"))
            {
                return true;
            }

            return false;
        }
    }
}