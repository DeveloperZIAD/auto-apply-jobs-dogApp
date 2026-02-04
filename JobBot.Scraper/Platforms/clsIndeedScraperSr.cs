using BusinessLogic;
using JobBot.Scraper.Iface;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JobBot.Scraper.Platforms
{
    public class IndeedScraper : IJobScraper
    {
        public string PlatformName => "Indeed";
        private readonly IPlatformAuthService _authService;

        public IndeedScraper(IPlatformAuthService authService)
        {
            _authService = authService;
        }

        public async Task<List<JobPostingDto>> ScrapeJobsAsync(string keyword, int maxJobs)
        {
            var jobs = new List<JobPostingDto>();
            using var playwright = await Playwright.CreateAsync();

            // نستخدم AuthService للحصول على الجلسة
            await using var context = await _authService.GetContextAsync(playwright);
            var page = context.Pages.First();

            int startAt = 0;
            // كلمات دلالية للاستبعاد لضمان عدم سحب وظائف خبيرة جداً
            string[] excludedKeywords = { 
    // 1. استبعاد الرتب العالية (Seniority)
    "senior", "sr.", "lead", "staff", "principal", "architect", "manager", "head", "director", "expert",
    
    // 2. استبعاد لغات وتقنيات لا تعمل بها حالياً (Languages)
    "java", "python", "golang", "ruby", "php", "laravel", "ios", "swift", "android", "flutter", "salesforce",
    
    // 3. استبعاد الوظائف غير البرمجية أو التدريبية (Roles)
    "intern", "internship", "trainee", "aerospace", "mechanical", "electrical", "dba", "data scientist", "devops"
};
            while (jobs.Count < maxJobs)
            {
                // إضافة فلاتر إضافية في الرابط: ريموت + ترتيب بالتاريخ
                // في سكرابر LinkedIn، اجعل الرابط يحتوي على فلاتر إضافية
                string url = $"https://www.linkedin.com/jobs/search/?f_LF=f_AL&keywords={Uri.EscapeDataString(keyword)}%20NOT%20Senior%20NOT%20Lead&f_WT=2";
                Console.WriteLine($"🚀 Indeed: Searching page {startAt / 10 + 1} for Easy Apply jobs...");

                await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 60000 });
                await Task.Delay(3000);

                var jobCardsSelector = ".job_seen_beacon";
                try
                {
                    await page.WaitForSelectorAsync(jobCardsSelector, new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
                }
                catch
                {
                    Console.WriteLine("⚠️ No more jobs found.");
                    break;
                }

                var cards = await page.Locator(jobCardsSelector).AllAsync();

                foreach (var card in cards)
                {
                    if (jobs.Count >= maxJobs) break;

                    try
                    {
                        // 1. التحقق من وجود "Easily apply"
                        // إنديد يضع هذا النص غالباً في عنصر span داخل الكارت
                        var easyApplyIndicator = card.Locator("span.iaIcon, [data-testid='indeedApply']");
                        bool isEasyApply = await easyApplyIndicator.CountAsync() > 0;

                        if (!isEasyApply) continue; // تخطي إذا لم تكن Easy Apply

                        // 2. قراءة بيانات الوظيفة
                        var titleElement = card.Locator("h2.jobTitle a").First;
                        var title = await titleElement.InnerTextAsync();

                        // 3. فلترة الـ Senior برمجياً للتأكيد
                        if (excludedKeywords.Any(word => title.ToLower().Contains(word)))
                        {
                            continue; // تخطي الوظائف الخبيرة
                        }

                        var href = await titleElement.GetAttributeAsync("href");
                        var jobId = ExtractIndeedJobId(href);
                        var company = await card.Locator("[data-testid='company-name']").InnerTextAsync();
                        var location = await card.Locator("[data-testid='text-location']").InnerTextAsync();

                        if (!string.IsNullOrEmpty(title) && !jobs.Any(j => j.ExternalJobId == jobId))
                        {
                            jobs.Add(new JobPostingDto
                            {
                                ExternalJobId = jobId,
                                Platform = PlatformName,
                                JobTitle = title.Trim(),
                                CompanyName = company.Trim(),
                                JobLocation = location.Trim(),
                                SourceUrl = href.StartsWith("http") ? href : "https://www.indeed.com" + href,
                                PostedDate = DateTime.Now,
                                ScrapedDate = DateTime.Now
                            });
                            Console.WriteLine($"   ✅ Found Easy Apply: {title.Trim()} @ {company.Trim()}");
                        }
                    }
                    catch { continue; }
                }

                // إذا لم نجد أي وظيفة في هذه الصفحة (كلها كانت سنيور أو ليست إيزي أبلاي)
                // نتقدم للصفحة التالية
                startAt += 10;
                await Task.Delay(new Random().Next(3000, 5000));
            }

            return jobs;
        }

        private string ExtractIndeedJobId(string url)
        {
            if (string.IsNullOrEmpty(url)) return Guid.NewGuid().ToString();
            var match = Regex.Match(url, @"jk=([a-zA-Z0-9]+)");
            return match.Success ? match.Groups[1].Value : "IND_" + Guid.NewGuid().ToString().Substring(0, 8);
        }
    }
}