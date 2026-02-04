using System;
using System.Collections.Generic;
using DataLogic; // الارتباط الوحيد المسموح به

namespace BusinessLogic
{
    // DTO: الكائن الذي ستستخدمه في واجهة المستخدم أو السكرابر
    public class CandidateProfileDto
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

    public static class CandidateProfileService
    {
        // 1. جلب كل الحسابات وتحويلها من Info لـ Dto
        public static List<CandidateProfileDto> GetAll()
        {
            var rawData = CandidateProfileDataLogic.GetAll();
            var dtoList = new List<CandidateProfileDto>();

            foreach (var item in rawData)
            {
                dtoList.Add(MapToDto(item));
            }
            return dtoList;
        }

        // 2. إضافة حساب جديد باستخدام الكائن
        public static int Insert(CandidateProfileDto dto)
        {
            var info = new CandidateInfo
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                BaseResumeText = dto.BaseResumeText,
                LinkedInUrl = dto.LinkedInUrl,
                GitHubUrl = dto.GitHubUrl,
                CreatedAt = dto.CreatedAt == default ? DateTime.Now : dto.CreatedAt
            };

            return CandidateProfileDataLogic.Insert(info);
        }

        // 3. تحديث حساب
        public static int Update(CandidateProfileDto dto)
        {
            var info = new CandidateInfo
            {
                Id = dto.Id,
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                BaseResumeText = dto.BaseResumeText,
                LinkedInUrl = dto.LinkedInUrl,
                GitHubUrl = dto.GitHubUrl,
                CreatedAt = dto.CreatedAt
            };

            return CandidateProfileDataLogic.Update(info);
        }

        public static int Delete(Guid id)
        {
            return CandidateProfileDataLogic.Delete(id);
        }

        // دالة مساعدة للتحويل (Mapper) لتقليل تكرار الكود
        private static CandidateProfileDto MapToDto(CandidateInfo info)
        {
            return new CandidateProfileDto
            {
                Id = info.Id,
                FullName = info.FullName,
                Email = info.Email,
                Phone = info.Phone,
                BaseResumeText = info.BaseResumeText,
                LinkedInUrl = info.LinkedInUrl,
                GitHubUrl = info.GitHubUrl,
                CreatedAt = info.CreatedAt
            };
        }
    }
}