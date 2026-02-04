using BusinessLogic;
using JobBot.Scraper.Iface;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobBot.Scraper.Platforms
{
    public class BaytScraper : IJobScraper
    {
        public string PlatformName => "Bayt";
        private readonly IPlatformAuthService _authService;

        public BaytScraper(IPlatformAuthService authService)
        {
            _authService = authService;
        }

        public async Task<List<JobPostingDto>> ScrapeJobsAsync(string keyword, int maxJobs)
        {
            var jobs = new List<JobPostingDto>();
            using var playwright = await Playwright.CreateAsync();
            await using var context = await _authService.GetContextAsync(playwright);
            var page = context.Pages.First();

            // 1. القفز المباشر لنتائج البحث في مصر
            string formattedKeyword = keyword.Replace(" ", "-").ToLower();
            string searchUrl = $"https://www.bayt.com/en/egypt/jobs/{formattedKeyword}-jobs/";

            Console.WriteLine($"🚀 Bayt: Jumping directly to: {searchUrl}");

            try
            {
                await page.GotoAsync(searchUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                var jobCardsSelector = "li[data-js-job], .has-pointer-d";
                await page.WaitForSelectorAsync(jobCardsSelector, new() { Timeout = 10000 });

                var cards = await page.Locator(jobCardsSelector).AllAsync();
                Console.WriteLine($"📊 Found {cards.Count} jobs. Starting safe extraction...");

                foreach (var card in cards)
                {
                    if (jobs.Count >= maxJobs) break;
                    try
                    {
                        // تفعيل العنصر لضمان قراءة المحتوى (Lazy Load)
                        await card.ScrollIntoViewIfNeededAsync();

                        // أ- استخراج العنوان والرابط
                        var titleAnchor = card.Locator("h2 a, a.u-block, a[data-js-aid='jobID']").First;
                        var rawTitle = await titleAnchor.InnerTextAsync();
                        var href = await titleAnchor.GetAttributeAsync("href");

                        if (string.IsNullOrEmpty(rawTitle) || string.IsNullOrEmpty(href)) continue;

                        // ب- استخراج اسم الشركة بسلكتورز مرنة
                        string rawCompany = "N/A";
                        var companyLocators = new[] { "b.u-block", ".t-primary", ".u-h6", ".m10t.t-small" };
                        foreach (var selector in companyLocators)
                        {
                            var el = card.Locator(selector).First;
                            if (await el.IsVisibleAsync())
                            {
                                rawCompany = await el.InnerTextAsync();
                                break;
                            }
                        }

                        // ج- تنظيف البيانات (Data Cleaning) لتجنب أخطاء قاعدة البيانات
                        // فصل اسم الشركة عن الملخص إذا وُجد
                        string cleanCompany = rawCompany.Split(new[] { "Summary:", "Responsibilities:" }, StringSplitOptions.None)[0].Trim();
                        string cleanTitle = rawTitle.Trim();

                        // التأكد من عدم تجاوز طول الأعمدة في SQL (Truncation Safety)
                        if (cleanCompany.Length > 150) cleanCompany = cleanCompany.Substring(0, 147) + "...";
                        if (cleanTitle.Length > 200) cleanTitle = cleanTitle.Substring(0, 197) + "...";

                        jobs.Add(new JobPostingDto
                        {
                            ExternalJobId = href.Split('-').Last().Replace("/", ""),
                            Platform = "Bayt",
                            JobTitle = cleanTitle,
                            CompanyName = cleanCompany,
                            SourceUrl = href.StartsWith("http") ? href : "https://www.bayt.com" + href,
                            ScrapedDate = DateTime.Now
                        });

                        Console.WriteLine($"   ✅ Saved: {cleanTitle} @ {cleanCompany}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   ⚠️ Skip item due to: {ex.Message.Substring(0, Math.Min(50, ex.Message.Length))}...");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Critical Scraper Error: {ex.Message}");
            }

            return jobs;
        }
    }
}