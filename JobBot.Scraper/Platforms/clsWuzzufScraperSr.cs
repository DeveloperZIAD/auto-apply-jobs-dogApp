using BusinessLogic;
using JobBot.Scraper.Iface;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobBot.Scraper.Platforms
{
    public class WuzzufScraper : IJobScraper
    {
        public string PlatformName => "Wuzzuf";
        private readonly WuzzufAuthService _authService;

        public WuzzufScraper(WuzzufAuthService authService)
        {
            _authService = authService;
        }

        public async Task<List<JobPostingDto>> ScrapeJobsAsync(string keyword, int maxJobs)
        {
            var allJobs = new List<JobPostingDto>();
            using var playwright = await Playwright.CreateAsync();
            await using var context = await _authService.GetContextAsync(playwright);
            var page = context.Pages.First();

            string formattedKeyword = Uri.EscapeDataString(keyword);

            // Wuzzuf يعرض 15 وظيفة في الصفحة، لذا سنسحب أول 3 صفحات (0، 1، 2)
            for (int i = 0; i < 3; i++)
            {
                if (allJobs.Count >= maxJobs) break;

                string url = $"https://wuzzuf.net/search/jobs/?q={formattedKeyword}&start={i}";
                Console.WriteLine($"📄 Wuzzuf: Scraping page {i + 1}...");

                try
                {
                    await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

                    // انتظار تحميل الكروت (استخدام سلكتور الحاوية الرئيسي)
                    var jobCardsSelector = "[class*='JobCard_container'], .css-p44337, .css-1gsh19u";
                    await page.WaitForSelectorAsync(jobCardsSelector, new() { Timeout = 10000 });

                    // إخفاء أي شات أو نوافذ منبثقة قد تعيق النقر (اختياري)
                    await HandleWuzzufPopups(page);

                    var cards = await page.Locator(jobCardsSelector).AllAsync();
                    Console.WriteLine($"📊 Found {cards.Count} job cards on Wuzzuf page {i + 1}.");

                    foreach (var card in cards)
                    {
                        if (allJobs.Count >= maxJobs) break;

                        try
                        {
                            // استخراج العنوان والشركة والرابط
                            var titleEl = card.Locator("h2 a").First;
                            var companyEl = card.Locator("[class*='JobCard_companyName'], .css-17n233u").First;

                            string title = await titleEl.InnerTextAsync();
                            string company = await companyEl.InnerTextAsync();
                            string href = await titleEl.GetAttributeAsync("href") ?? "";

                            // تنظيف اسم الشركة
                            company = company.Replace("-", "").Trim();

                            allJobs.Add(new JobPostingDto
                            {
                                ExternalJobId = href.Split('/').Last().Split('?').First(),
                                Platform = "Wuzzuf",
                                JobTitle = title.Trim(),
                                CompanyName = company,
                                SourceUrl = href.StartsWith("http") ? href : "https://wuzzuf.net" + href,
                                ScrapedDate = DateTime.Now
                            });

                            Console.WriteLine($"   ✅ Wuzzuf Scraped: {title.Trim()}");
                        }
                        catch { continue; }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Wuzzuf Error on page {i}: {ex.Message}");
                    break;
                }

                // انتظار بسيط لمحاكاة التصفح الطبيعي
                await Task.Delay(2000);
            }

            return allJobs;
        }

        private async Task HandleWuzzufPopups(IPage page)
        {
            try
            {
                // إغلاق نافذة الـ Survey أو الـ Cookie Consent إذا ظهرت
                var dismissBtn = page.Locator("button:has-text('Dismiss'), .css-h7mdfp").First;
                if (await dismissBtn.IsVisibleAsync()) await dismissBtn.ClickAsync();
            }
            catch { }
        }
    }
}