﻿using ClusterConnector.Manager;
using ClusterConnector.Models.Database;
using ClusterLogic.Models;
using ClusterLogic.Models.ChatbotModels;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterLogic.ChatbotHandler
{
    public class ProcessChatbotLogic
    {
        /// <summary>
        /// Takes a certain userID AND/OR questionID and checks the database for new answers on possible open questions
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="questionID"></param>
        /// <returns> Returns new answers on possible open questions for a certain user or question </returns>
        public static List<ChatbotNewAnswerModel> CheckAndGetNewAnswers(string userID = null, int questionID = -1)
        {
            // connect to database and define query
            DBManager manager = new DBManager(true);
            String query = "Select q.question_id, q.question, q.answer_id, a.answer " +
                           "from dbo.Answers a, dbo.Questions q " +
                           $"where q.question_id = {questionID} and q.answer_id = a.answer_id;";// and " +
                           //"a.approved = 'true';";

            // execute query and read answer
            DBQuestion sqlResult = null;
            using (SqlDataReader reader = manager.Read(query))
            {
                if (reader.Read())
                {
                    sqlResult = new DBQuestion();
                    sqlResult.Question_id = (int)reader["question_id"];
                    sqlResult.Question = (String)reader["question"];
                    sqlResult.Answer_id = (int)reader["answer_id"];
                    sqlResult.Answer = (String)reader["answer"];
                }
            }

            // Close the connection
            manager.Close();

            if (sqlResult == null)
            {
                return new List<ChatbotNewAnswerModel>()
                {
                    new ChatbotNewAnswerModel(userID, null, questionID, -1, null, -1, 0)
                };
            }

            // Return new output-model
            return new List<ChatbotNewAnswerModel>()
            {
                new ChatbotNewAnswerModel(userID, sqlResult.Question, sqlResult.Question_id, -1 /**no temporary chatbot id needed*/,
                                            sqlResult.Answer, sqlResult.Answer_id, 1 /**Where is the certainty found?*/)
            };
        }

        /// <summary>
        /// Get all the answered questions and wrap them into a MatchQuestionModelRequest.
        /// </summary>
        /// <param name="list">A list of ChatbotNewQuestionModels to process.</param>
        /// <returns>A MatchQuestionModelRequest containing all the answered questions from the forum.</returns>
        public static MatchQuestionModelRequest ProcessChatbotReceiveQuestion(List<ChatbotNewQuestionModel> list)
        {
            MatchQuestionModelRequest mqmr = new MatchQuestionModelRequest();

            //Example on how to turn a Query String into data from the SQL database

            List<DBQuestion> result = new List<DBQuestion>();
            DBManager manager = new DBManager(true);

            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * ");
            sb.Append("FROM Questions q ");
            sb.Append("WHERE q.answer_id IS NOT NULL; ");
            String sqlCommand = sb.ToString();

            var reader = manager.Read(sqlCommand);

            while (reader.Read()) //reader.Read() reads entries one-by-one for all matching entries to the query
            {
                //
                //reader["xxx"] where 'xxx' is the collumn name of the particular table you get as result from the query.
                //You get these values a generic 'Object' so typecasting to the proper value should be safe. eg. (int)reader["xxx"]
                //
               

                DBQuestion answer = new DBQuestion();
                answer.Question_id = (int)reader["question_id"];
                answer.Question = (String)reader["question"];
                answer.Answer_id = (int)reader["answer_id"];

                result.Add(answer);
            }
            manager.Close(); //IMPORTANT! Should happen automatically, but better safe than sorry.


            //**********************************
            mqmr.action = "MATCH_QUESTIONS".ToLower(); //Standard
            mqmr.question = list[0].question;
            mqmr.msg_id = 0; //Currently not really used
            mqmr.question_id = 0; //This id does not exist at this point

            List<NLPQuestionModelInfo> comparisonQuestions = new List<NLPQuestionModelInfo>();
            for (int i = 0; i < result.Count; i++)
            {
                comparisonQuestions.Add(new NLPQuestionModelInfo() { question = result[i].Question, question_id = result[i].Question_id });
            }
            mqmr.compare_questions = comparisonQuestions.ToArray();

            return mqmr;
        }

        /// <summary>
        /// Perform NLP Nonsense check
        /// </summary>
        /// <param name="list"></param>
        public static OffensivenessModelRequest ProcessChatbotReceiveAnswer(List<ChatbotGivenAnswerModel> list)
        {
            return new OffensivenessModelRequest(list[0]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <returns> This functions should have a response, if the response == null, then no response will be given through the websocket</returns>
        public static List<ChatbotAnswerRequestResponseModel> ProcessChatbotRequestAnswerToQuestion(List<ChatbotAnswerRequestModel> list)
        {
            ChatbotAnswerRequestModel answerRequest = list[0];

            int question_id = answerRequest.question_id;

            // connect to database and define query
            DBManager manager = new DBManager(true); //this false 
            String query = "Select a.answer_id, a.answer " +
                           "from dbo.Answers as a, dbo.Questions as q " +
                           $"where q.question_id = {question_id} and q.answer_id = a.answer_id ";
                           // "+ and a.approved == true;";

            // execute query and read answer
            DBQuestion sqlResult = null;
            using (SqlDataReader reader = manager.Read(query))
            {
                while (reader.Read())
                {
                    sqlResult = new DBQuestion();
                    sqlResult.Answer_id = (int)reader["answer_id"];
                    sqlResult.Answer = (String)reader["answer"];
                }
            }

            // Close the connection
            manager.Close();

            if (sqlResult == null){
                return null;
            }

            return new List<ChatbotAnswerRequestResponseModel>()
            {
                new ChatbotAnswerRequestResponseModel(answerRequest.user_id, sqlResult.Answer_id, question_id, true, sqlResult.Answer)
            };
        }

        /// <summary>
        ///     TODO: What should the socket response be when there are no unanswered questions?
        /// </summary>
        /// <param name="list"></param>
        /// <returns> This functions should have a response, if the response == null or empty, then no response will be given through the websocket</returns>
        public static List<ChatbotRequestUnansweredQuestionsResponseModel> ProcessChatbotRequestAnswerToQuestion(List<ChatbotRequestUnansweredQuestionsModel> list)
        {
            // What should this function do? --> see function below
            throw new NotImplementedException();
        }


        /// <summary>
        /// Function to get a number of open questions from the database
        /// </summary>
        /// <param name="nbQuestions"></param>
        /// <param name="user_id"></param>
        /// <returns> Returns a model with a list of unanswered questions </returns>
        public static ChatbotResponseUnansweredQuestionsModel RetrieveOpenQuestions(int nbQuestions, string user_id = null)
        {
            List<DBQuestion> result = new List<DBQuestion>();
            DBManager manager = new DBManager(true); //this false 

            StringBuilder sb = new StringBuilder();
            sb.Append($"SELECT TOP {nbQuestions} q.question_id, q.question ");
            sb.Append("FROM Questions q ");
            sb.Append("WHERE q.answer_id is NULL");
            String sqlCommand = sb.ToString();

            var reader = manager.Read(sqlCommand);

            while (reader.Read()) //reader.Read() reads entries one-by-one for all matching entries to the query
            {
                DBQuestion answer = new DBQuestion();
                answer.Question_id = (int)reader["question_id"];
                answer.Question = (String)reader["question"];
                result.Add(answer);
            }
            manager.Close(); //IMPORTANT! Should happen automatically, but better safe than sorry.

            List<ChatbotQuestionHasNoAnswerModel> openQuestions = new List<ChatbotQuestionHasNoAnswerModel>();
            for (int i = 0; i < result.Count; i++)
            {
                openQuestions.Add(new ChatbotQuestionHasNoAnswerModel(result[i].Question, result[i].Question_id));
            }

            return new ChatbotResponseUnansweredQuestionsModel(openQuestions.ToArray(), user_id);
        }

















    }
}
