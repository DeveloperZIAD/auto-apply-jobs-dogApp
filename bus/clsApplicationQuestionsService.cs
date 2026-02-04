using System;
using System.Collections.Generic;
using System.Linq; // عشان نستخدم Select بسهولة
using DataLogic; // Reference لمشروع الـ db

namespace BusinessLogic
{
    public class ApplicationQuestionDto
    {
        public Guid Id { get; set; }
        public Guid JobId { get; set; }
        public string QuestionText { get; set; }
        public string AnswerText { get; set; }
        public string QuestionType { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public static class ApplicationQuestionsService
    {
        // 1. جلب الكل وتحويلهم لـ DTO
        public static List<ApplicationQuestionDto> GetAll()
        {
            // بننادي الـ db ونستلم List من ApplicationQuestionInfo
            var rawData = ApplicationQuestionsDataLogic.GetAll();

            var dtoList = new List<ApplicationQuestionDto>();
            foreach (var item in rawData)
            {
                dtoList.Add(new ApplicationQuestionDto
                {
                    Id = item.Id,
                    JobId = item.JobId,
                    QuestionText = item.QuestionText,
                    AnswerText = item.AnswerText,
                    QuestionType = item.QuestionType,
                    CreatedAt = item.CreatedAt
                });
            }
            return dtoList;
        }

        // 2. الإضافة باستخدام الـ DTO
        public static int Insert(ApplicationQuestionDto dto)
        {
            // تحويل الـ DTO لـ Info اللي بيفهمه الـ DataLogic
            var info = new ApplicationQuestionInfo
            {
                JobId = dto.JobId,
                QuestionText = dto.QuestionText,
                AnswerText = dto.AnswerText,
                QuestionType = dto.QuestionType,
                CreatedAt = dto.CreatedAt == default ? DateTime.Now : dto.CreatedAt
            };

            return ApplicationQuestionsDataLogic.Insert(info);
        }

        // 3. التعديل باستخدام الـ DTO
        public static int Update(ApplicationQuestionDto dto)
        {
            var info = new ApplicationQuestionInfo
            {
                Id = dto.Id,
                JobId = dto.JobId,
                QuestionText = dto.QuestionText,
                AnswerText = dto.AnswerText,
                QuestionType = dto.QuestionType,
                CreatedAt = dto.CreatedAt
            };

            return ApplicationQuestionsDataLogic.Update(info);
        }
        public static string FindBestAnswer(string onScreenQuestion)
        {
            if (string.IsNullOrWhiteSpace(onScreenQuestion)) return null;

            var savedQuestions = GetAll(); // جلب الأسئلة من DB
            string normalizedOnScreen = onScreenQuestion.ToLower().Trim();

            // الطبقة 1: التطابق التام (Exact Match) - دقة 100%
            var exactMatch = savedQuestions.FirstOrDefault(q =>
                normalizedOnScreen.Equals(q.QuestionText.ToLower().Trim()));
            if (exactMatch != null && !string.IsNullOrEmpty(exactMatch.AnswerText))
                return exactMatch.AnswerText;

            // الطبقة 2: نظام الأوزان والكلمات المفتاحية (Scoring System)
            // نبحث عن السؤال الذي يحتوي على أكبر قدر من الكلمات المفتاحية الموجودة في سؤال الشاشة
            var bestMatch = savedQuestions
                .Select(q => new
                {
                    Question = q,
                    Score = CalculateMatchScore(normalizedOnScreen, q.QuestionText.ToLower())
                })
                .Where(x => x.Score > 0.6) // يجب أن يتطابق بنسبة أكثر من 60%
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            if (bestMatch != null && !string.IsNullOrEmpty(bestMatch.Question.AnswerText))
                return bestMatch.Question.AnswerText;

            // الطبقة 3: القواعد الصلبة (Hard-coded Logic) للحالات الحرجة
            if (normalizedOnScreen.Contains("sponsorship") || normalizedOnScreen.Contains("visa")) return "No";
            if (normalizedOnScreen.Contains("authorized") && normalizedOnScreen.Contains("work")) return "Yes";
            if (normalizedOnScreen.Contains("citizen")) return "Yes";
            if (normalizedOnScreen.Contains("background check")) return "Yes";

            return null;
        }

        // ميثود مساعدة لحساب قوة التطابق
        private static double CalculateMatchScore(string onScreen, string dbQuestion)
        {
            if (string.IsNullOrEmpty(onScreen) || string.IsNullOrEmpty(dbQuestion)) return 0;

            double score = 0;
            // تنظيف النصوص من الرموز التي قد تعيق المقارنة مع الحفاظ على النقاط داخل الكلمات التقنية فقط
            string s1 = onScreen.ToLower().Trim();
            string s2 = dbQuestion.ToLower().Trim();

            // 1. الكلمات المفتاحية التقنية (أعلى وزن)
            var techKeywords = new[] { "azure", "c#", ".net", "sql", "angular", "react", "python", "javascript", "php", "c++", "docker", "aws" };

            foreach (var tech in techKeywords)
            {
                // استخدام Regex أو Contains ذكية للتأكد من أن الكلمة موجودة ككلمة مستقلة
                if (s1.Contains(tech) && s2.Contains(tech)) score += 2.0;
                else if (s1.Contains(tech) != s2.Contains(tech)) score -= 1.0;
            }

            // 2. تقسيم الجمل (استخدمنا Regex هنا لتقسيم الكلمات مع الحفاظ على صيغ مثل .NET أو C++)
            var words1 = s1.Split(new[] { ' ', '?', ',', '(', ')', '/' }, StringSplitOptions.RemoveEmptyEntries)
                           .Where(w => w.Length > 2).Distinct().ToList();
            var words2 = s2.Split(new[] { ' ', '?', ',', '(', ')', '/' }, StringSplitOptions.RemoveEmptyEntries)
                           .Where(w => w.Length > 2).Distinct().ToList();

            if (!words2.Any()) return 0;

            int commonWords = words2.Count(w => words1.Contains(w));
            score += (double)commonWords / words2.Count;

            // 3. بونص السياق
            string[] contexts = { "year", "salary", "sponsor", "authorize", "citizenship", "remote" };
            foreach (var ctx in contexts)
            {
                if (s1.Contains(ctx) && s2.Contains(ctx)) score += 0.5;
            }

            return score;
        }
        public static int Delete(Guid id)
        {
            return ApplicationQuestionsDataLogic.Delete(id);
        }
    }
}