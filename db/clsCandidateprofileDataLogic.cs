using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace DataLogic
{
    // الكلاس الداخلي لتمثيل بيانات الملف الشخصي
    public class CandidateInfo
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string BaseResumeText { get; set; }
        public string LinkedInUrl { get; set; }
        public string GitHubUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public static class CandidateProfileDataLogic
    {
        // الربط مع كلاس الاتصال المركزي
        private static readonly string ConnectionString = db.dbconection.cls_connectionString.GetConnection();

        public static List<CandidateInfo> GetAll()
        {
            var list = new List<CandidateInfo>();
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT * FROM [CandidateProfile]", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new CandidateInfo
                            {
                                Id = (Guid)reader["Id"],
                                FullName = reader["FullName"]?.ToString(),
                                Email = reader["Email"]?.ToString(),
                                Phone = reader["Phone"]?.ToString(),
                                BaseResumeText = reader["BaseResumeText"]?.ToString(),
                                LinkedInUrl = reader["LinkedInUrl"]?.ToString(),
                                GitHubUrl = reader["GitHubUrl"]?.ToString(),
                                CreatedAt = (DateTime)reader["CreatedAt"]
                            });
                        }
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in CandidateProfile.GetAll: " + ex.Message, ex);
            }
        }

        public static int Insert(CandidateInfo info)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"INSERT INTO [CandidateProfile] 
                                    ([FullName], [Email], [Phone], [BaseResumeText], [LinkedInUrl], [GitHubUrl], [CreatedAt]) 
                                    VALUES (@FullName, @Email, @Phone, @BaseResumeText, @LinkedInUrl, @GitHubUrl, @CreatedAt)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FullName", (object)info.FullName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", (object)info.Email ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Phone", (object)info.Phone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BaseResumeText", (object)info.BaseResumeText ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LinkedInUrl", (object)info.LinkedInUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@GitHubUrl", (object)info.GitHubUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedAt", info.CreatedAt == default ? DateTime.Now : info.CreatedAt);

                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in CandidateProfile.Insert: " + ex.Message, ex);
            }
        }

        public static int Update(CandidateInfo info)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"UPDATE [CandidateProfile] SET 
                                    [FullName] = @FullName, [Email] = @Email, [Phone] = @Phone, 
                                    [BaseResumeText] = @BaseResumeText, [LinkedInUrl] = @LinkedInUrl, 
                                    [GitHubUrl] = @GitHubUrl, [CreatedAt] = @CreatedAt 
                                    WHERE [Id] = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", info.Id);
                        cmd.Parameters.AddWithValue("@FullName", (object)info.FullName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", (object)info.Email ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Phone", (object)info.Phone ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BaseResumeText", (object)info.BaseResumeText ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LinkedInUrl", (object)info.LinkedInUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@GitHubUrl", (object)info.GitHubUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedAt", info.CreatedAt);

                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in CandidateProfile.Update: " + ex.Message, ex);
            }
        }

        public static int Delete(Guid id)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("DELETE FROM [CandidateProfile] WHERE [Id] = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in CandidateProfile.Delete: " + ex.Message, ex);
            }
        }
    }
}