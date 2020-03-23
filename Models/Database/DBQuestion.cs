﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterConnector.Models.Database
{
    public class DBQuestion : DBData
    {
        private int question_id;
        private String question;
        private int answer_id;

        public int Question_id { get => question_id; set => question_id = value; }
        public string Question { get => question; set => question = value; }
        public int Answer_id { get => answer_id; set => answer_id = value; }
    }
}