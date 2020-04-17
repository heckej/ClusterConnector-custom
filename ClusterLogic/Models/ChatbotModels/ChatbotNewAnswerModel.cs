﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterLogic.Models.ChatbotModels
{
    //From wiki https://github.com/heckej/P-O-Entrepreneurship-Team-A-code/wiki/Server-to-Chatbot-Communication
    public class ChatbotNewAnswerModel
    {
        private int _user_id = -1;
        private String _question = null;
        private int _question_id = -1;
        private int _chatbot_temp_id = -1;
        private int _answer_id = -1;
        private String _answer = null;
        private float _certainty = -1;

        public int user_id { get => _user_id; set => _user_id = value; }
        public string question { get => _question; set => _question = value; }
        public int question_id { get => _question_id; set => _question_id = value; }
        public int chatbot_temp_id { get => _chatbot_temp_id; set => _chatbot_temp_id = value; }
        public int answer_id { get => _answer_id; set => _answer_id = value; }
        public string answer { get => _answer; set => _answer = value; }
        public float certainty { get => _certainty; set => _certainty = value; }
    }
}