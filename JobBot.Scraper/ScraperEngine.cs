using BusinessLogic;
using JobBot.Scraper.Iface;
using JobBot.Scraper.Platforms;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace JobBot.Scraper
{
    public class ScraperEngine
    {
        private readonly IConfiguration _configuration;

        public ScraperEngine()
        {
            // بناء الإعدادات مرة واحدة عند إنشاء المحرك
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _configuration = new ConfigurationBuilder()
                .SetBasePath(assemblyPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        /// <summary>
        /// الميثود الرئيسية لتشغيل عملية السحب لكافة المنصات
        /// </summary>
        /// <param name="searchKeyword">اسم الوظيفة المطلوبة</param>
        /// <param name="jobsPerPlatform">عدد الوظائف المطلوب سحبها من كل موقع</param>
        /// <param name="maxRetryAttempts">عدد مرات إعادة المحاولة في حال الفشل</param>
        public async Task RunAllScrapersAsync(string searchKeyword = ".NET Developer", int jobsPerPlatform = 20, int maxRetryAttempts = 3)
        {
            Console.WriteLine($"🚀 Starting Scraper Engine | Keyword: {searchKeyword} | Target: {jobsPerPlatform} jobs/platform");

            // --- 1. تجهيز خدمات الـ Auth من الإعدادات ---
            var authServices = InitializeAuthServices();

            // --- 2. تعريف قائمة المنصات (Scrapers) ---
            List<IJobScraper> scrapers = new List<IJobScraper>
            {
                new LinkedInScraper(authServices.LinkedIn),
                // new IndeedScraper(authServices.Indeed), // فك الكومنت عند جاهزية الكلاس
              //  new BaytScraper(authServices.Bayt),
               // new ArcDevScraper(authServices.ArcDev),
               // new GlassdoorScraper(authServices.Glassdoor),
               // new DiceScraper(authServices.Dice),
              //  new WuzzufScraper(authServices.Wuzzuf),
                //new YCScraper(authServices.YCombinator)
            };

            // --- 3. بدء عملية السحب لكل منصة ---
            foreach (var scraper in scrapers)
            {
                int currentRetry = 0;
                bool success = false;

                while (currentRetry < maxRetryAttempts && !success)
                {
                    try
                    {
                        Console.WriteLine($"\n--- 🔍 [{scraper.PlatformName}] Attempt {currentRetry + 1}/{maxRetryAttempts} ---");

                        var foundJobs = await scraper.ScrapeJobsAsync(searchKeyword, jobsPerPlatform);

                        if (foundJobs != null && foundJobs.Count > 0)
                        {
                            ProcessAndSaveJobs(scraper.PlatformName, foundJobs);
                            success = true; // تم السحب بنجاح، اخرج من حلقة الـ Retry
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ No jobs found or session issue for {scraper.PlatformName}.");
                            currentRetry++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error in {scraper.PlatformName}: {ex.Message}");
                        currentRetry++;
                        if (currentRetry < maxRetryAttempts) await Task.Delay(2000 * currentRetry); // انتظار متزايد قبل الإعادة
                    }
                }
            }

            Console.WriteLine("\n🏁 All Scraper tasks completed!");
        }

        private void ProcessAndSaveJobs(string platformName, List<JobPostingDto> jobs)
        {
            int newJobsCount = 0;
            foreach (var job in jobs)
            {
                if (JobPostingsService.AddJob(job))
                {
                    Console.WriteLine($"   ✅ Saved: {job.JobTitle} @ {job.CompanyName}");
                    newJobsCount++;
                }
                else
                {
                    Console.WriteLine($"   ⏭️ Skipped: {job.JobTitle}");
                }
            }
            Console.WriteLine($"📊 {platformName} Summary: {jobs.Count} Scraped, {newJobsCount} New Saved.");
        }

        private dynamic InitializeAuthServices()
        {
            return new
            {
                LinkedIn = new LinkedInAuthService(_configuration["JobBotSettings:Credentials:LinkedIn:Email"], _configuration["JobBotSettings:Credentials:LinkedIn:Password"], "LinkedIn_Session"),
               // Bayt = new BaytAuthService(_configuration["JobBotSettings:Credentials:Bayt:Email"], _configuration["JobBotSettings:Credentials:Bayt:Password"], _configuration["JobBotSettings:Credentials:Bayt:ProfilePath"]),
              //  ArcDev = new ArcDevAuthService(_configuration["JobBotSettings:Credentials:ArcDev:Email"], _configuration["JobBotSettings:Credentials:ArcDev:Password"], _configuration["JobBotSettings:Credentials:ArcDev:ProfilePath"]),
             //   Glassdoor = new GlassdoorAuthService(_configuration["JobBotSettings:Credentials:Glassdoor:Email"], _configuration["JobBotSettings:Credentials:Glassdoor:Password"], _configuration["JobBotSettings:Credentials:Glassdoor:ProfilePath"]),
            //    Dice = new DiceAuthService(_configuration["JobBotSettings:Credentials:Dice:Email"], _configuration["JobBotSettings:Credentials:Dice:Password"], _configuration["JobBotSettings:Credentials:Dice:ProfilePath"]),
               // Wuzzuf = new WuzzufAuthService(_configuration["JobBotSettings:Credentials:Wuzzuf:Email"], _configuration["JobBotSettings:Credentials:Wuzzuf:Password"]),
               // YCombinator = new YCAuthService(_configuration["JobBotSettings:Credentials:GlassYCombinatordoor:Email"], _configuration["JobBotSettings:Credentials:YCombinator:Password"], _configuration["JobBotSettings:Credentials:YCombinator:ProfilePath"])
                // Indeed = new IndeedAuthService(...) // أضفها هنا عند الحاجة
            };
        }
    }
}