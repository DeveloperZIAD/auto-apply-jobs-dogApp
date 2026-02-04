using System;
using System.Collections.Generic;
using DataLogic; // استدعاء طبقة البيانات

namespace BusinessLogic
{
    // DTO: الكائن الذي سيتنقل بين الـ UI والـ Scraper والـ Bus
    public class ApplicationDto
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

    public static class ApplicationsService
    {
        // 1. جلب كل الطلبات وتحويلها من (Info) إلى (Dto)
        public static List<ApplicationDto> GetAll()
        {
            // نطلب البيانات كـ List جاهزة من الداتا لوجيك
            var rawData = ApplicationsDataLogic.GetAll();
            var dtoList = new List<ApplicationDto>();

            foreach (var item in rawData)
            {
                dtoList.Add(new ApplicationDto
                {
                    Id = item.Id,
                    CandidateId = item.CandidateId,
                    JobId = item.JobId,
                    MatchScore = item.MatchScore,
                    TailoredResumeText = item.TailoredResumeText,
                    CoverLetterText = item.CoverLetterText,
                    Status = item.Status,
                    AppliedAt = item.AppliedAt,
                    CreatedAt = item.CreatedAt
                });
            }
            return dtoList;
        }

        // 2. إضافة طلب جديد باستخدام الكائن بالكامل
        public static int Insert(ApplicationDto dto)
        {
            // تحويل الـ Dto القادم من الخارج إلى كائن يفهمه الـ DataLogic
            var info = new ApplicationInfo
            {
                CandidateId = dto.CandidateId,
                JobId = dto.JobId,
                MatchScore = dto.MatchScore,
                TailoredResumeText = dto.TailoredResumeText,
                CoverLetterText = dto.CoverLetterText,
                Status = dto.Status ?? "Pending",
                AppliedAt = dto.AppliedAt,
                CreatedAt = dto.CreatedAt == default ? DateTime.Now : dto.CreatedAt
            };

            return ApplicationsDataLogic.Insert(info);
        }

        // 3. تحديث طلب
        public static int Update(ApplicationDto dto)
        {
            var info = new ApplicationInfo
            {
                Id = dto.Id,
                CandidateId = dto.CandidateId,
                JobId = dto.JobId,
                MatchScore = dto.MatchScore,
                TailoredResumeText = dto.TailoredResumeText,
                CoverLetterText = dto.CoverLetterText,
                Status = dto.Status,
                AppliedAt = dto.AppliedAt,
                CreatedAt = dto.CreatedAt
            };

            return ApplicationsDataLogic.Update(info);
        }

        public static int Delete(Guid id)
        {
            return ApplicationsDataLogic.Delete(id);
        }
    }
}