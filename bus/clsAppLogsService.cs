using System;
using System.Collections.Generic;
using DataLogic; // الارتباط الوحيد المسموح به

namespace BusinessLogic
{
    // DTO: الكائن البسيط لنقل البيانات
    public class AppLogDto
    {
        public int LogId { get; set; }
        public string LogType { get; set; }
        public string LogMessage { get; set; }
        public DateTime LogDate { get; set; }
    }

    public static class AppLogsService
    {
        // 1. جلب السجلات وتحويلها من Info لـ Dto
        public static List<AppLogDto> GetAll()
        {
            // نطلب القائمة الجاهزة من طبقة الداتا
            var rawLogs = AppLogsDataLogic.GetAll();
            var dtoList = new List<AppLogDto>();

            foreach (var log in rawLogs)
            {
                dtoList.Add(new AppLogDto
                {
                    LogId = log.LogId,
                    LogType = log.LogType,
                    LogMessage = log.LogMessage,
                    LogDate = log.LogDate
                });
            }
            return dtoList;
        }

        // 2. إضافة سجل جديد (مهم جداً للسكرابر لتسجيل الأخطاء)
        public static int Log(string type, string message)
        {
            var info = new LogInfo
            {
                LogType = type,
                LogMessage = message,
                LogDate = DateTime.Now
            };

            return AppLogsDataLogic.Insert(info);
        }

        // 3. مسح السجلات
        public static int Delete(int logId)
        {
            return AppLogsDataLogic.Delete(logId);
        }
    }
}