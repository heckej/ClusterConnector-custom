﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClusterConnector;

namespace ClusterLogic.Models.ChatbotModels
{
    public class ChatbotNewQuestionModel : BaseModel
    {
        private string _user_id = null;
        private String _question = null;
        private int _chatbot_temp_id = -1;

        public ChatbotNewQuestionModel()
        {
        }

        public ChatbotNewQuestionModel(Dictionary<string, string> dict)
        {
            user_id = dict["user_id"];
            question = dict["question"];
            chatbot_temp_id = int.Parse(dict["chatbot_temp_id"]);
        }

        public ChatbotNewQuestionModel(NewQuestionNonsenseCheck newQuestionNonsenseCheck)
        {
            user_id = newQuestionNonsenseCheck.user_id;
            question = newQuestionNonsenseCheck.question;
            chatbot_temp_id = newQuestionNonsenseCheck.question_id;
        }

        public string user_id { get => _user_id; set => _user_id = value; }
        public string question { get => _question; set => _question = value; }
        public int chatbot_temp_id { get => _chatbot_temp_id; set => _chatbot_temp_id = value; }

        public bool IsComplete()
        {
            return _user_id != null && _question != null;
        }
    }
}
