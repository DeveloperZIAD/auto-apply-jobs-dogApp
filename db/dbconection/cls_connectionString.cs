using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace db.dbconection
{
    public static class cls_connectionString
    {
        private static string _connectionString = string.Empty;

        public static string GetConnection()
        {
            if (!string.IsNullOrEmpty(_connectionString))
                return _connectionString;

            try
            {
                
                // بناء الإعدادات مع التأكد من وجود المراجع الصحيحة لـ SetBasePath
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory) // استخدمنا AppDomain لضمان الوصول للمسار في Worker Service
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                _connectionString = configuration.GetConnectionString("DefaultConnection");

                if (string.IsNullOrEmpty(_connectionString))
                    return "Error: Connection string 'DefaultConnection' is null.";

                return _connectionString;
            }
            catch (Exception ex)
            {
                // إرجاع رسالة الخطأ كنص ليتم استخدامه أو تسجيله
                return $"Error: {ex.Message}";
            }
        }
    }
}