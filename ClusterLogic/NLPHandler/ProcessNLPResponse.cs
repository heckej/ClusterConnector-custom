﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClusterLogic.Models;
using ClusterConnector.Manager;
using ClusterConnector.Manager;
using ClusterConnector.Models.Database;
using System.Data.SqlClient;

namespace ClusterLogic.NLPHandler
{
    public class ProcessNLPResponse
    {
        /// <summary>
        /// The threshold to find question matches.
        /// </summary>
        private static double MatchThreshold { get; } = 0.75;

        /// <summary>
        /// Process a NLPMatchQuestionResponse and turn it into a MatchQuestionLogicResponse containing an answer,
        /// if any.
        /// </summary>
        /// <param name="matchQuestionModels">The NLP model to process.</param>
        /// <returns></returns>
        public static MatchQuestionLogicResponse ProcessNLPMatchQuestionsResponse(List<MatchQuestionModelResponse> matchQuestionModels)
        {
            // Create a "no match" response
            MatchQuestionLogicResponse nullResponse = new MatchQuestionLogicResponse();

            // Check to see whether there is at least a valid answer given
            if (matchQuestionModels == null || 
                matchQuestionModels.Count < 1 || 
                matchQuestionModels[0] == null ||
                ! matchQuestionModels[0].IsComplete())
            {
                return nullResponse;
            }

            MatchQuestionModelResponse matchQuestionModel = matchQuestionModels[0];

            MatchQuestionModelInfo bestInfo = null;
            double bestMatch = 0.0;

            // Find the best match above the threshold
            foreach (MatchQuestionModelInfo info in matchQuestionModel.possible_matches)
            {
                if (info.prob > MatchThreshold && info.prob > bestMatch)
                {
                    bestInfo = info;
                    bestMatch = bestInfo.prob;
                }
            }

            // Get the answer of the best match, if any
            if (bestInfo != null)
            {
                DBManager manager = new DBManager(false); //this false 

                // Initialize the result
                MatchQuestionLogicResponse result = null;

                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT answer ");
                sb.Append("FROM Answers a, Questions q ");
                sb.Append($"WHERE q.question_id = {bestInfo.question_id} AND q.answer_id = a.answer_id; ");
                String sql = sb.ToString();

                using (SqlDataReader reader = manager.Read(sql))
                {
                    // This query should only return 0 or 1 result
                    if (reader.Read())
                    {
                        result = new MatchQuestionLogicResponse(matchQuestionModel.question_id, matchQuestionModel.msg_id, bestInfo.question_id, true, reader.GetString(0));
                    }
                }

                // Close the connection
                manager.Close();

                if (result != null)
                {
                    return result;
                }
            }

            // No valid match was found, so "no answer" is returned.
            return nullResponse;
        }

        public static Object ProcessNLPOffensivenessResponse(List<OffensivenessModelResponse> offensivenessModels)
        {

            return null;
        }

        public static Object ProcessNLPNonsenseResponse(List<NonsenseModelResponse> nonsenseModelResponses)
        {
            NonsenseModelResponse nonsenseModelResponse = nonsenseModelResponses[0];
            if (nonsenseModelResponse.nonsense) //if is nonsense
            {
                return null;
            }
            else
            {
                return new OffensivenessModelRequest(nonsenseModelResponse); //This gives the Server the correct response to make it known what to do next. As a simple example
            }
        }
    }
}