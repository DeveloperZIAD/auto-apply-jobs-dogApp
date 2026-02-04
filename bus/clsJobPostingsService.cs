using System;
using System.Collections.Generic;
using DataLogic;

namespace BusinessLogic
{
    public class JobPostingDto
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

    public  class JobPostingsService
    {
        public static List<JobPostingDto> GetAllJobs()
        {
            var data = JobPostingsDataLogic.GetAll();
            var dtos = new List<JobPostingDto>();
            foreach (var item in data) dtos.Add(MapToDto(item));
            return dtos;
        }

        public static bool AddJob(JobPostingDto dto)
        {
            // 1. استخدام الميثود المتقدمة للفحص (Fingerprinting)
            // نمرر العنوان، المنصة، واسم الشركة للمقارنة العميقة
            if (JobPostingsDataLogic.IsJobExistsAdvanced(dto.ExternalJobId, dto.Platform, dto.JobTitle, dto.CompanyName))
            {
                return false; // الوظيفة مكررة فعلياً
            }

            // 2. فلترة إضافية: إذا كان العنوان مريباً (مجرد اسم قسم وليس وظيفة)
            if (dto.JobTitle.EndsWith("Jobs", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var info = new JobPostingInfo
            {
                ExternalJobId = dto.ExternalJobId,
                Platform = dto.Platform,
                JobTitle = dto.JobTitle,
                CompanyName = string.IsNullOrWhiteSpace(dto.CompanyName) ? "Unknown" : dto.CompanyName,
                JobLocation = dto.JobLocation,
                JobDescription = dto.JobDescription,
                SourceUrl = dto.SourceUrl,
                PostedDate = dto.PostedDate,
                ScrapedDate = dto.ScrapedDate == default ? DateTime.Now : dto.ScrapedDate,
                IsApplied = dto.IsApplied
            };

            return JobPostingsDataLogic.Insert(info) > 0;
        }

        public  bool MarkAsApplied(string externalId, string platform)
        {
            return JobPostingsDataLogic.MarkAsApplied(externalId, platform) > 0;
        }

        // ميثود مخصصة للـ Worker لجلب الوظائف التي لم يتم التقديم عليها بعد
        public  List<JobPostingDto> GetPendingApplications()
        {
            var all = GetAllJobs();
            return all.FindAll(j => !j.IsApplied && !string.IsNullOrEmpty(j.SourceUrl));
        }
        public List<JobPostingDto> GetPendingApplications(string platformName)
        {
            var all = GetAllJobs();
            // Filter by: Not Applied AND Has URL AND Matches the specific platform
            return all.FindAll(j => !j.IsApplied
                                 && !string.IsNullOrEmpty(j.SourceUrl)
                                 && j.Platform.Equals(platformName, StringComparison.OrdinalIgnoreCase));
        }

        private static JobPostingDto MapToDto(JobPostingInfo info)
        {
            return new JobPostingDto
            {
                Id = info.Id,
                ExternalJobId = info.ExternalJobId,
                Platform = info.Platform,
                JobTitle = info.JobTitle,
                CompanyName = info.CompanyName,
                JobLocation = info.JobLocation,
                JobDescription = info.JobDescription,
                SourceUrl = info.SourceUrl,
                PostedDate = info.PostedDate,
                ScrapedDate = info.ScrapedDate,
                IsApplied = info.IsApplied,
                ApplicationDate = info.ApplicationDate
            };
        }
    }
}