using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace DataLogic
{
    public class JobPostingInfo
    {
        public Guid Id { get; set; }
        public string ExternalJobId { get; set; }
        public string Platform { get; set; }
        public string JobTitle { get; set; }
        public string CompanyName { get; set; }
        public string JobLocation { get; set; }
        public string JobDescription { get; set; }
        public string SourceUrl { get; set; }
        public DateTime? PostedDate { get; set; }
        public DateTime ScrapedDate { get; set; }
        public bool IsApplied { get; set; }
        public DateTime? ApplicationDate { get; set; }
    }

    public static class JobPostingsDataLogic
    {
        private static readonly string ConnectionString = db.dbconection.cls_connectionString.GetConnection();

        public static List<JobPostingInfo> GetAll()
        {
            var list = new List<JobPostingInfo>();
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                string query = "SELECT * FROM [JobPostings] ORDER BY ScrapedDate DESC";
                using (var cmd = new SqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new JobPostingInfo
                        {
                            Id = (Guid)reader["Id"],
                            ExternalJobId = reader["ExternalJobId"]?.ToString(),
                            Platform = reader["Platform"]?.ToString(),
                            JobTitle = reader["JobTitle"]?.ToString(),
                            CompanyName = reader["CompanyName"]?.ToString(),
                            JobLocation = reader["JobLocation"]?.ToString(),
                            JobDescription = reader["JobDescription"]?.ToString(),
                            SourceUrl = reader["SourceUrl"]?.ToString(),
                            PostedDate = reader["PostedDate"] as DateTime?,
                            ScrapedDate = (DateTime)reader["ScrapedDate"],
                            IsApplied = reader["IsApplied"] != DBNull.Value && Convert.ToBoolean(reader["IsApplied"]),
                            ApplicationDate = reader["ApplicationDate"] as DateTime?
                        });
                    }
                }
            }
            return list;
        }

        public static int Insert(JobPostingInfo info)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                string query = @"INSERT INTO [JobPostings] 
                    (ExternalJobId, Platform, JobTitle, CompanyName, JobLocation, JobDescription, SourceUrl, PostedDate, ScrapedDate, IsApplied) 
                    VALUES (@ExternalJobId, @Platform, @JobTitle, @CompanyName, @JobLocation, @JobDescription, @SourceUrl, @PostedDate, @ScrapedDate, @IsApplied)";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ExternalJobId", (object)info.ExternalJobId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Platform", (object)info.Platform ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@JobTitle", (object)info.JobTitle ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CompanyName", (object)info.CompanyName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@JobLocation", (object)info.JobLocation ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@JobDescription", (object)info.JobDescription ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SourceUrl", (object)info.SourceUrl ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PostedDate", (object)info.PostedDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ScrapedDate", info.ScrapedDate == default ? DateTime.Now : info.ScrapedDate);
                    cmd.Parameters.AddWithValue("@IsApplied", info.IsApplied);
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public static bool IsJobExists(string externalJobId, string platform)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                string query = "SELECT COUNT(1) FROM JobPostings WHERE ExternalJobId = @ExtId AND Platform = @Platform";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ExtId", externalJobId);
                    cmd.Parameters.AddWithValue("@Platform", platform);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        public static bool IsJobExistsAdvanced(string externalJobId, string platform, string title, string company)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    // دمج ذكاء المحتوى مع المعرف الرقمي
                    string query = @"SELECT COUNT(1) FROM JobPostings 
                             WHERE (LOWER(TRIM(JobTitle)) = LOWER(TRIM(@Title)) 
                                    AND LOWER(TRIM(CompanyName)) = LOWER(TRIM(@Company))
                                    AND Platform = @Platform)
                             OR ExternalJobId = @ExtId";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Title", (object)title ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Company", (object)company ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Platform", (object)platform ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ExtId", (object)externalJobId ?? DBNull.Value);

                        return (int)cmd.ExecuteScalar() > 0;
                    }
                }
            }
            catch { return false; }
        }

        public static int MarkAsApplied(string externalJobId, string platform)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                string query = @"UPDATE [JobPostings] 
                                SET IsApplied = 1, ApplicationDate = @AppDate 
                                WHERE ExternalJobId = @ExtId AND Platform = @Platform";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@AppDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@ExtId", externalJobId);
                    cmd.Parameters.AddWithValue("@Platform", platform);
                    return cmd.ExecuteNonQuery();
                }
            }
        }
    }
}