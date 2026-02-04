using BusinessLogic;
using JobBot.Scraper.Iface;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobBot.Scraper.Platforms
{
    public class GlassdoorScraper : IJobScraper
    {
        public string PlatformName => "Glassdoor";
        private readonly GlassdoorAuthService _authService;

        public GlassdoorScraper(GlassdoorAuthService authService)
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

            for (int pageNum = 1; pageNum <= 5; pageNum++) // زدنا عدد الصفحات المتاحة
            {
                string pageUrl = $"https://www.glassdoor.com/Job/jobs.htm?sc.keyword={formattedKeyword}&locT=N&locId=69&p={pageNum}";
                Console.WriteLine($"📄 Glassdoor: Scraping Page {pageNum} for [{keyword}]...");

                try
                {
                    // استخدام DomContentLoaded لتجنب تعليق الصفحة عند الصور أو الإعلانات الثقيلة
                    await page.GotoAsync(pageUrl, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 60000 // زيادة المهلة تحسباً لبطء الموقع
                    });

                    // انتظار عشوائي لمحاكاة السلوك البشري
                    await page.WaitForTimeoutAsync(new Random().Next(3000, 6000));

                    // إزالة النوافذ المنبثقة
                    await HandlePopups(page);

                    var jobCardsSelector = "li[data-test='jobListing'], [class*='JobCard_jobCardWrapper']";

                    // التأكد من وجود كروت قبل البدء
                    try
                    {
                        await page.WaitForSelectorAsync(jobCardsSelector, new() { Timeout = 10000 });
                    }
                    catch
                    {
                        Console.WriteLine($"⚠️ No job cards visible on page {pageNum}. Possible end of results.");
                        break;
                    }

                    var cards = await page.Locator(jobCardsSelector).AllAsync();
                    Console.WriteLine($"📊 Found {cards.Count} jobs on page {pageNum}.");

                    foreach (var card in cards)
                    {
                        if (allJobs.Count >= maxJobs) break;

                        try
                        {
                            // استخراج البيانات بدقة
                            var titleEl = card.Locator("[data-test='job-title']").First;
                            var companyEl = card.Locator("[class*='EmployerProfile_employerName'], [data-test='employer-short-name']").First;
                            var linkEl = card.Locator("a[data-test='job-link']").First;

                            string title = await titleEl.InnerTextAsync();
                            string company = await companyEl.IsVisibleAsync() ? await companyEl.InnerTextAsync() : "Unknown";
                            string href = await linkEl.GetAttributeAsync("href") ?? "";

                            // فلتر ذكي: نقبل الوظيفة إذا كان العنوان يحتوي على الكلمة المفتاحية أو كلمات برمجية
                            if (title.ToLower().Contains(keyword.ToLower()) || title.ToLower().Contains("software") || title.ToLower().Contains("developer"))
                            {
                                // تنظيف اسم الشركة من التقييم (مثلاً: Google 4.5 -> Google)
                                company = System.Text.RegularExpressions.Regex.Replace(company, @"\d+\.\d+", "").Trim();

                                allJobs.Add(new JobPostingDto
                                {
                                    ExternalJobId = href.Contains("jl=") ? href.Split("jl=")[1].Split("&")[0] : Guid.NewGuid().ToString().Substring(0, 8),
                                    Platform = "Glassdoor",
                                    JobTitle = title.Trim(),
                                    CompanyName = company,
                                    SourceUrl = href.StartsWith("http") ? href : "https://www.glassdoor.com" + href,
                                    ScrapedDate = DateTime.Now
                                });

                                Console.WriteLine($"   ✅ Scraped: {title.Trim()}");
                            }
                        }
                        catch { continue; } // إذا فشل استخراج كرت واحد لا نتوقف عن البقية
                    }
                }
                catch (Exception ex)
                {
                    // في حال حدوث Timeout في صفحة، لا نغلق البرنامج بل ننتقل للي بعدها أو ننهي السحب بما لدينا
                    Console.WriteLine($"⚠️ Issue on page {pageNum}: {ex.Message}. Saving collected jobs...");
                    break;
                }

                // سكرول عشوائي قبل الانتقال للصفحة التالية
                await page.EvaluateAsync($"window.scrollBy(0, {new Random().Next(300, 700)})");
                await Task.Delay(new Random().Next(2000, 4000));
            }

            return allJobs;
        }        // ميثود مساعدة لتنظيف الشاشة من النوافذ المنبثقة
        private async Task HandlePopups(IPage page)
        {
            try
            {
                var closeBtn = page.Locator("button[class*='Close'], [aria-label='Close'], .modal_closeIcon").First;
                if (await closeBtn.IsVisibleAsync())
                {
                    await closeBtn.ClickAsync();
                    Console.WriteLine("🧼 Pop-up cleared.");
                }
            }
            catch { }
        }
    }
}