using BusinessLogic;
using JobBot.Scraper.Iface;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobBot.Scraper.Platforms
{
    public class DiceScraper : IJobScraper
    {
        public string PlatformName => "Dice";
        private readonly IPlatformAuthService _authService;

        public DiceScraper(IPlatformAuthService authService) => _authService = authService;

        public async Task<List<JobPostingDto>> ScrapeJobsAsync(string keyword, int maxJobs)
        {
            var allJobs = new List<JobPostingDto>();
            int currentPage = 1;

            using var playwright = await Playwright.CreateAsync();
            await using var context = await _authService.GetContextAsync(playwright);
            var page = context.Pages.LastOrDefault() ?? await context.NewPageAsync();

            try
            {
                // استمرار السحب طالما لم نصل للعدد المطلوب
                while (allJobs.Count < maxJobs)
                {
                    string searchUrl = $"https://www.dice.com/jobs?q={Uri.EscapeDataString(keyword)}&location=Remote&page={currentPage}&pageSize=20";
                    Console.WriteLine($"🔍 Scraping Page {currentPage} | Collected so far: {allJobs.Count}/{maxJobs}");

                    await page.GotoAsync(searchUrl, new() { WaitUntil = WaitUntilState.Load });

                    // انتظار ظهور الحاوية
                    try
                    {
                        await page.WaitForSelectorAsync("[data-testid='job-card']", new() { Timeout = 10000 });
                    }
                    catch
                    {
                        Console.WriteLine("🛑 No more jobs found or page failed to load.");
                        break; // نخرج من اللوب إذا لم نجد كروت وظائف
                    }

                    await page.EvaluateAsync("window.scrollBy(0, 800)");
                    await Task.Delay(2000);

                    var jobCards = await page.Locator("[data-testid='job-card']").AllAsync();
                    if (jobCards.Count == 0) break;

                    foreach (var card in jobCards)
                    {
                        if (allJobs.Count >= maxJobs) break;

                        try
                        {
                            var titleLink = card.Locator("[data-testid='job-search-job-detail-link']");
                            var companyLink = card.Locator("a[href*='/company-profile/'] p");
                            string? jobId = await card.GetAttributeAsync("data-job-guid");

                            string title = await titleLink.InnerTextAsync();
                            string company = await companyLink.CountAsync() > 0 ? await companyLink.InnerTextAsync() : "Unknown";
                            string? url = await titleLink.GetAttributeAsync("href");

                            if (!string.IsNullOrEmpty(title))
                            {
                                allJobs.Add(new JobPostingDto
                                {
                                    ExternalJobId = jobId ?? Guid.NewGuid().ToString(),
                                    JobTitle = title.Trim(),
                                    CompanyName = company.Trim(),
                                    SourceUrl = url,
                                    Platform = "Dice",
                                    ScrapedDate = DateTime.Now
                                });
                                Console.WriteLine($"   ✅ Found: {title.Trim()}");
                            }
                        }
                        catch { continue; }
                    }

                    // الانتقال للصفحة التالية
                    currentPage++;

                    // إضافة وقت راحة بسيط لتجنب الحظر (Anti-Bot Friendly)
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during pagination: {ex.Message}");
            }

            Console.WriteLine($"🏁 Final Summary: {allJobs.Count} jobs collected from {currentPage - 1} pages.");
            return allJobs;
        }
    }
}