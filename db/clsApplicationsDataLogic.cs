using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace DataLogic
{
    // الكلاس المسؤول عن تمثيل بيانات "عمليات التقديم" داخل هذا المشروع فقط
    public class ApplicationInfo
    {
        public Guid Id { get; set; }
        public Guid CandidateId { get; set; }
        public Guid JobId { get; set; }
        public int? MatchScore { get; set; }
        public string TailoredResumeText { get; set; }
        public string CoverLetterText { get; set; }
        public string Status { get; set; }
        public DateTime? AppliedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public static class ApplicationsDataLogic
    {
        // جلب نص الاتصال من الكلاس المركزي لضمان سهولة التعديل مستقبلاً
        private static readonly string ConnectionString = db.dbconection.cls_connectionString.GetConnection();

        public static List<ApplicationInfo> GetAll()
        {
            var list = new List<ApplicationInfo>();
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT * FROM [Applications]", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new ApplicationInfo
                            {
                                Id = (Guid)reader["Id"],
                                CandidateId = (Guid)reader["CandidateId"],
                                JobId = (Guid)reader["JobId"],
                                MatchScore = reader["MatchScore"] != DBNull.Value ? (int)reader["MatchScore"] : (int?)null,
                                TailoredResumeText = reader["TailoredResumeText"]?.ToString(),
                                CoverLetterText = reader["CoverLetterText"]?.ToString(),
                                Status = reader["Status"]?.ToString(),
                                AppliedAt = reader["AppliedAt"] != DBNull.Value ? (DateTime)reader["AppliedAt"] : (DateTime?)null,
                                CreatedAt = (DateTime)reader["CreatedAt"]
                            });
                        }
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in ApplicationsDataLogic.GetAll: " + ex.Message, ex);
            }
        }

        public static int Insert(ApplicationInfo info)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"INSERT INTO [Applications] 
                                    ([CandidateId], [JobId], [MatchScore], [TailoredResumeText], [CoverLetterText], [Status], [AppliedAt], [CreatedAt]) 
                                    VALUES (@CandidateId, @JobId, @MatchScore, @TailoredResumeText, @CoverLetterText, @Status, @AppliedAt, @CreatedAt)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CandidateId", info.CandidateId);
                        cmd.Parameters.AddWithValue("@JobId", info.JobId);
                        cmd.Parameters.AddWithValue("@MatchScore", (object)info.MatchScore ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TailoredResumeText", (object)info.TailoredResumeText ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CoverLetterText", (object)info.CoverLetterText ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Status", (object)info.Status ?? "Pending");
                        cmd.Parameters.AddWithValue("@AppliedAt", (object)info.AppliedAt ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedAt", info.CreatedAt == default ? DateTime.Now : info.CreatedAt);

                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in ApplicationsDataLogic.Insert: " + ex.Message, ex);
            }
        }

        public static int Update(ApplicationInfo info)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"UPDATE [Applications] SET 
                                    [CandidateId] = @CandidateId, [JobId] = @JobId, 
                                    [MatchScore] = @MatchScore, [TailoredResumeText] = @TailoredResumeText, 
                                    [CoverLetterText] = @CoverLetterText, [Status] = @Status, 
                                    [AppliedAt] = @AppliedAt, [CreatedAt] = @CreatedAt 
                                    WHERE [Id] = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", info.Id);
                        cmd.Parameters.AddWithValue("@CandidateId", info.CandidateId);
                        cmd.Parameters.AddWithValue("@JobId", info.JobId);
                        cmd.Parameters.AddWithValue("@MatchScore", (object)info.MatchScore ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TailoredResumeText", (object)info.TailoredResumeText ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CoverLetterText", (object)info.CoverLetterText ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Status", (object)info.Status ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@AppliedAt", (object)info.AppliedAt ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedAt", info.CreatedAt);

                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in ApplicationsDataLogic.Update: " + ex.Message, ex);
            }
        }

        public static int Delete(Guid id)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("DELETE FROM [Applications] WHERE [Id] = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in ApplicationsDataLogic.Delete: " + ex.Message, ex);
            }
        }
    }
}