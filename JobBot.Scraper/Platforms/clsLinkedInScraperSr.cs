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
    public class LinkedInScraper : IJobScraper
    {
        public string PlatformName => "LinkedIn";
        private readonly IPlatformAuthService _authService;

        public LinkedInScraper(IPlatformAuthService authService)
        {
            _authService = authService;
        }

        public async Task<List<JobPostingDto>> ScrapeJobsAsync(string keyword, int maxJobs)
        {
            var jobs = new List<JobPostingDto>();
            using var playwright = await Playwright.CreateAsync();
            await using var context = await _authService.GetContextAsync(playwright);
            var page = context.Pages.First();

            try
            {
                int startAt = 0;
                while (jobs.Count < maxJobs)
                {
                    // الرابط مع فلاتر: عالمي، ريموت، آخر 3 أيام، الأحدث
                    string url = "https://www.linkedin.com/jobs/search/?" +
                                 "keywords=" + Uri.EscapeDataString(keyword) +
                                 "&location=Worldwide&f_CF=2&f_TPR=r259200&sortBy=DD&start=" + startAt;

                    Console.WriteLine($"🚀 Accessing LinkedIn: {keyword} (Found: {jobs.Count}/{maxJobs})");

                    await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });

                    // 1. انتظار القائمة الجانبية حتى تصبح مرئية
                    string sidebarSelector = ".jobs-search-results-list, .scaffold-layout__list";
                    try
                    {
                        await page.WaitForSelectorAsync(sidebarSelector, new() { State = WaitForSelectorState.Visible, Timeout = 20000 });
                    }
                    catch
                    {
                        Console.WriteLine("⚠️ Sidebar container not detected. Checking session...");
                        if (page.Url.Contains("login")) { Console.WriteLine("❌ Session Expired!"); break; }
                    }

                    // 2. ميثود التمرير الذكي القوي (Scrolling)
                    // نمرر 10 مرات، في كل مرة ننزل مسافة تسمح بتحميل الوظائف الجديدة
                    for (int s = 0; s < 10; s++)
                    {
                        await page.EvaluateAsync(@"(sel) => {
                    const el = document.querySelector(sel);
                    if (el) {
                        el.scrollBy(0, 600); // تمرير لأسفل داخل القائمة
                    } else {
                        window.scrollBy(0, 600); // تمرير للصفحة كاملة كحل احتياطي
                    }
                }", sidebarSelector);

                        await Task.Delay(1000); // انتظار التحميل (Lazy Load)
                    }

                    // 3. جلب الكروت بعد التمرير الكامل
                    var cards = await page.Locator(".jobs-search-results-list__item, [data-occludable-job-id]").AllAsync();
                    Console.WriteLine($"🔍 Found {cards.Count} jobs on this page after scrolling.");

                    if (cards.Count == 0)
                    {
                        Console.WriteLine("⚠️ No cards detected. Attempting emergency refresh...");
                        await page.ReloadAsync();
                        await Task.Delay(5000);
                        continue;
                    }

                    // 4. استخراج البيانات من الكروت
                    foreach (var card in cards)
                    {
                        if (jobs.Count >= maxJobs) break;

                        try
                        {
                            // جلب الرابط والعنوان (استخدام سلكتورات قوية)
                            var linkLoc = card.Locator("a[href*='/view/'], a[href*='/jobs/view/']").First;
                            if (await linkLoc.CountAsync() == 0) continue;

                            var link = await linkLoc.GetAttributeAsync("href");
                            var title = await linkLoc.InnerTextAsync();

                            // جلب اسم الشركة
                            var companyLoc = card.Locator(".job-card-container__primary-description, .artdeco-entity-lockup__subtitle, .job-card-container__company-name").First;
                            string company = (await companyLoc.CountAsync() > 0) ? await companyLoc.InnerTextAsync() : "N/A";

                            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(link))
                            {
                                string externalId = ExtractJobId(link);

                                // التأكد من عدم تكرار نفس الوظيفة
                                if (!jobs.Any(j => j.ExternalJobId == externalId))
                                {
                                    jobs.Add(new JobPostingDto
                                    {
                                        ExternalJobId = externalId,
                                        Platform = PlatformName,
                                        JobTitle = title.Replace("\n", " ").Replace("with verification", "").Trim(),
                                        CompanyName = company.Trim(),
                                        JobLocation = "Remote (Worldwide)",
                                        SourceUrl = link.Split('?')[0].StartsWith("http") ? link.Split('?')[0] : "https://www.linkedin.com" + link.Split('?')[0],
                                        PostedDate = DateTime.Now,
                                        ScrapedDate = DateTime.Now
                                    });
                                    Console.WriteLine($"   ✅ [{jobs.Count}/{maxJobs}] {title.Trim()}");
                                }
                            }
                        }
                        catch { continue; }
                    }

                    // إذا لم نصل للعدد المطلوب، ننتقل للـ 25 وظيفة التالية
                    if (jobs.Count < maxJobs)
                    {
                        startAt += 25;
                        Console.WriteLine("➡️ Moving to next set of results...");
                        await Task.Delay(2000);
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"❌ Critical Error: {ex.Message}"); }

            return jobs;
        }
        private string ExtractJobId(string link)
        {
            if (string.IsNullOrEmpty(link)) return "LI_" + Guid.NewGuid().ToString().Substring(0, 8);
            var match = Regex.Match(link, @"/view/(\d+)/|currentJobId=(\d+)|jobs/(\d+)/");
            if (match.Success)
            {
                for (int i = 1; i <= 3; i++) { if (match.Groups[i].Success) return match.Groups[i].Value; }
            }
            return "LI_" + Guid.NewGuid().ToString().Substring(0, 8);
        }
    }
}