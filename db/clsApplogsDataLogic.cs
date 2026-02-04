using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace DataLogic
{
    // كلاس داخلي لتمثيل السجل (Log)
    public class LogInfo
    {
        public int LogId { get; set; }
        public string LogType { get; set; }
        public string LogMessage { get; set; }
        public DateTime LogDate { get; set; }
    }

    public static class AppLogsDataLogic
    {
        // استدعاء نص الاتصال المركزي
        private static readonly string ConnectionString = db.dbconection.cls_connectionString.GetConnection();

        public static List<LogInfo> GetAll()
        {
            var list = new List<LogInfo>();
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT * FROM [AppLogs] ORDER BY LogDate DESC", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new LogInfo
                            {
                                LogId = (int)reader["LogId"],
                                LogType = reader["LogType"]?.ToString(),
                                LogMessage = reader["LogMessage"]?.ToString(),
                                LogDate = (DateTime)reader["LogDate"]
                            });
                        }
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in AppLogsDataLogic.GetAll: " + ex.Message, ex);
            }
        }

        public static int Insert(LogInfo info)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"INSERT INTO [AppLogs] ([LogType], [LogMessage], [LogDate]) 
                                    VALUES (@LogType, @LogMessage, @LogDate)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@LogType", (object)info.LogType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LogMessage", (object)info.LogMessage ?? DBNull.Value);
                        // إذا لم يتم تحديد تاريخ، نضع تاريخ اللحظة الحالية
                        cmd.Parameters.AddWithValue("@LogDate", info.LogDate == default ? DateTime.Now : info.LogDate);

                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // ملاحظة: هنا لا نقوم بعمل Log للخطأ لأنه كلاس الـ Log نفسه! نكتفي بالـ Exception
                throw new Exception("Error in AppLogsDataLogic.Insert: " + ex.Message, ex);
            }
        }

        public static int Delete(int logId)
        {
            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("DELETE FROM [AppLogs] WHERE [LogId] = @LogId", conn))
                    {
                        cmd.Parameters.AddWithValue("@LogId", logId);
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error in AppLogsDataLogic.Delete: " + ex.Message, ex);
            }
        }
    }
}