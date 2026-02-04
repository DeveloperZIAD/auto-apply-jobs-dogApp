using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace DataLogic
{
    // الكلاس المسؤول عن البيانات داخل هذا المشروع فقط
    public class ApplicationQuestionInfo
    {
        public Guid Id { get; set; }
        public Guid JobId { get; set; }
        public string QuestionText { get; set; }
        public string AnswerText { get; set; }
        public string QuestionType { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public static class ApplicationQuestionsDataLogic
    {
        // جلب نص الاتصال من الكلاس المساعد لديك
        private static readonly string ConnectionString = db.dbconection.cls_connectionString.GetConnection();

        public static List<ApplicationQuestionInfo> GetAll()
        {
            var list = new List<ApplicationQuestionInfo>();
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT * FROM [ApplicationQuestions]", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new ApplicationQuestionInfo
                            {
                                Id = reader["Id"] != DBNull.Value ? (Guid)reader["Id"] : Guid.Empty,
                                JobId = reader["JobId"] != DBNull.Value ? (Guid)reader["JobId"] : Guid.Empty,
                                QuestionText = reader["QuestionText"]?.ToString(),
                                AnswerText = reader["AnswerText"]?.ToString(),
                                QuestionType = reader["QuestionType"]?.ToString(),
                                CreatedAt = reader["CreatedAt"] != DBNull.Value ? (DateTime)reader["CreatedAt"] : DateTime.Now
                            });
                        }
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in DataLogic.GetAll: " + ex.Message, ex);
            }
        }

        public static int Insert(ApplicationQuestionInfo info)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"INSERT INTO [ApplicationQuestions] 
                                    ([JobId], [QuestionText], [AnswerText], [QuestionType], [CreatedAt]) 
                                    VALUES (@JobId, @QuestionText, @AnswerText, @QuestionType, @CreatedAt)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@JobId", (object)info.JobId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@QuestionText", (object)info.QuestionText ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@AnswerText", (object)info.AnswerText ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@QuestionType", (object)info.QuestionType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedAt", info.CreatedAt == default ? DateTime.Now : info.CreatedAt);

                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in DataLogic.Insert: " + ex.Message, ex);
            }
        }

        public static int Update(ApplicationQuestionInfo info)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"UPDATE [ApplicationQuestions] 
                                    SET [JobId] = @JobId, [QuestionText] = @QuestionText, 
                                        [AnswerText] = @AnswerText, [QuestionType] = @QuestionType, 
                                        [CreatedAt] = @CreatedAt 
                                    WHERE [Id] = @Id";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", info.Id);
                        cmd.Parameters.AddWithValue("@JobId", (object)info.JobId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@QuestionText", (object)info.QuestionText ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@AnswerText", (object)info.AnswerText ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@QuestionType", (object)info.QuestionType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedAt", info.CreatedAt);

                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in DataLogic.Update: " + ex.Message, ex);
            }
        }

        public static int Delete(Guid id)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("DELETE FROM [ApplicationQuestions] WHERE [Id] = @Id", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in DataLogic.Delete: " + ex.Message, ex);
            }
        }
    }
}