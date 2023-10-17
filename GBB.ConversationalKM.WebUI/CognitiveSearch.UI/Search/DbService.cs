﻿using CognitiveSearch.UI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace CognitiveSearch.UI
{
    public class DbService
    {
        private IConfiguration _configuration { get; set; }

        private string sqlServer { get; set; }
        private string sqlDatabase { get; set; }
        private string sqlUser { get; set; }
        private string sqlPassword { get; set; }

        private string connectionString { get { return $"Data Source={sqlServer};Initial Catalog={sqlDatabase};Persist Security Info=False;User ID={sqlUser};Password={sqlPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"; } }

        public DbService(IConfiguration configuration)
        {
            try
            {
                _configuration = configuration;

                sqlServer = _configuration.GetSection("SqlServer")?.Value;
                sqlDatabase = _configuration.GetSection("SqlDatabase")?.Value;
                sqlUser = _configuration.GetSection("SqlUser")?.Value;
                sqlPassword = _configuration.GetSection("SqlPassword")?.Value;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message.ToString());
            }
        }

        public AggregateInsightViewModel GetSatisfactionInsights(int numberOfTopInsights)
        {
            var viewModel = new AggregateInsightViewModel();

            // query for top percentages
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                SqlCommand cmdKey = new SqlCommand("SELECT COUNT(*) AS TotalCount, SUM(CASE WHEN Satisfied = 'yes' THEN 1 ELSE 0 END) AS Satisfied, SUM(CASE WHEN Satisfied = 'no' THEN 1 ELSE 0 END) AS Unsatisfied FROM ConversationIndexData", conn);
                var keyInsightReader = cmdKey.ExecuteReader();

                while (keyInsightReader.Read())
                {
                    var totalCount = Convert.ToDouble(keyInsightReader["TotalCount"]);
                    var satisfied = Convert.ToDouble(keyInsightReader["Satisfied"]);
                    var unsatisfied = Convert.ToDouble(keyInsightReader["Unsatisfied"]);

                    viewModel.KeyInsight1 = Math.Round(satisfied / totalCount * 100, 1) + "%";
                    viewModel.KeyInsight2 = Math.Round(unsatisfied / totalCount * 100, 1) + "%";
                }
            }

            // query for the top compliments
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                SqlCommand cmdTop = new SqlCommand("SELECT TOP (@returnCount) Complaint, 'TotalCount'=count(*) FROM ConversationIndexData WHERE complaint NOT IN ('','None','N/A','No complaint') GROUP BY Complaint ORDER BY count(*) DESC", conn);
                cmdTop.Parameters.AddWithValue("@returnCount", numberOfTopInsights);
                var topInsightReader = cmdTop.ExecuteReader();

                while (topInsightReader.Read())
                {
                    viewModel.TopInsights.Add(topInsightReader[0].ToString());
                    if (viewModel.TopInsights.Count == numberOfTopInsights) break;
                }
            }

            return viewModel;
        }

        public AggregateInsightViewModel GetTopCityInsights(int numberOfTopInsights)
        {
            var viewModel = new AggregateInsightViewModel();

            // query for top origin city
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                SqlCommand cmdKey = new SqlCommand("SELECT TOP 1 OriginCity, 'TotalCount'=count(*) FROM ConversationIndexData WHERE OriginCity NOT IN ('','None','N/A','Not mentioned','Unknown') GROUP BY OriginCity ORDER BY count(*) DESC", conn);
                var keyInsightReader = cmdKey.ExecuteReader();

                while (keyInsightReader.Read())
                {
                    try
                    {
                        viewModel.KeyInsight2 = Convert.ToString(keyInsightReader["OriginCity"]);
                    }
                    catch (Exception ex)
                    {
                        viewModel.KeyInsight1 = "No value";
                    }
                }
            }

            // query for top destination city
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                SqlCommand cmdKey = new SqlCommand("SELECT TOP 1 DestinationCity, 'TotalCount'=count(*) FROM ConversationIndexData WHERE DestinationCity NOT IN ('','None','N/A','Not mentioned','Unknown') GROUP BY DestinationCity ORDER BY count(*) DESC", conn);
                var keyInsightReader = cmdKey.ExecuteReader();

                while (keyInsightReader.Read())
                {
                    try
                    {
                        viewModel.KeyInsight1 = Convert.ToString(keyInsightReader["DestinationCity"]);
                    }
                    catch (Exception ex)
                    {
                        viewModel.KeyInsight2 = "No value";
                    }

                }
            }

            // query for the top compliments
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                SqlCommand cmdTop = new SqlCommand("SELECT TOP (@returnCount) Compliment, 'TotalCount'=count(*) FROM ConversationIndexData WHERE compliment NOT IN ('','None','N/A','No complaint') GROUP BY compliment ORDER BY count(*) DESC", conn);
                cmdTop.Parameters.AddWithValue("@returnCount", numberOfTopInsights);
                var topInsightReader = cmdTop.ExecuteReader();

                while (topInsightReader.Read())
                {
                    viewModel.TopInsights.Add(topInsightReader[0].ToString());
                    if (viewModel.TopInsights.Count == numberOfTopInsights) break;
                }
            }

            return viewModel;
        }

    }
}
