using BusinessLogic;
using JobBot.Scraper.Iface;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobBot.Scraper.Platforms
{
    public class ArcDevScraper : IJobScraper
    {
        public string PlatformName => "ArcDev";
        private readonly IPlatformAuthService _authService;

        public ArcDevScraper(IPlatformAuthService authService)
        {
            _authService = authService;
        }

        public async Task<List<JobPostingDto>> ScrapeJobsAsync(string keyword, int maxJobs)
        {
            var jobs = new List<JobPostingDto>();
            using var playwright = await Playwright.CreateAsync();
            await using var context = await _authService.GetContextAsync(playwright);
            var page = context.Pages.First();

            // الذهاب للصفحة التي فتحت معك في الصورة
            await page.GotoAsync("https://arc.dev/dashboard/d/freelance-jobs/browse", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // سلكتور الكروت في صفحة الفريلانس (بناءً على الصورة)
            var jobCardsSelector = "div[class*='JobItem_container'], .job-card, [class*='JobCard_container']";

            try
            {
                await page.WaitForSelectorAsync(jobCardsSelector, new() { Timeout = 10000 });
                var cards = await page.Locator(jobCardsSelector).AllAsync();
                Console.WriteLine($"📊 Found {cards.Count} freelance jobs.");

                foreach (var card in cards)
                {
                    if (jobs.Count >= maxJobs) break;
                    try
                    {
                        // استخراج العنوان (مثل Senior Native iOS Engineer)
                        var titleEl = card.Locator("h3, [class*='title'], h2").First;
                        // استخراج اسم الشركة (مثل Arc Exclusive)
                        var companyEl = card.Locator("[class*='company'], [class*='CompanyName']").First;

                        string title = await titleEl.InnerTextAsync();
                        string company = await companyEl.IsVisibleAsync() ? await companyEl.InnerTextAsync() : "ArcDev Client";

                        jobs.Add(new JobPostingDto
                        {
                            ExternalJobId = Guid.NewGuid().ToString().Substring(0, 8),
                            Platform = "ArcDev",
                            JobTitle = title.Trim(),
                            CompanyName = company.Trim(),
                            SourceUrl = page.Url, // في صفحة الفريلانس أحياناً لا يوجد رابط مباشر لكل كارت
                            ScrapedDate = DateTime.Now
                        });
                        Console.WriteLine($"   ✅ Scraped: {title}");
                    }
                    catch { continue; }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Could not find cards: {ex.Message}");
            }
            return jobs;
        }
    }
}