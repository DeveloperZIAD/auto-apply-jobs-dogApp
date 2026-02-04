using JobBot.Scraper;
using JobBot.Scraper.Iface;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BusinessLogic;

namespace TheApp
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEnumerable<IApplyService> _applyServices;
        private readonly JobPostingsService _jobService;
        private readonly ScraperEngine _scraperEngine;

        public Worker(ILogger<Worker> logger, IEnumerable<IApplyService> applyServices, JobPostingsService jobService)
        {
            _logger = logger;
            _applyServices = applyServices;
            _jobService = jobService; // حقن خدمة الوظائف
            _scraperEngine = new ScraperEngine();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // انتظر 5 ثوانٍ ليبدأ النظام بالاستقرار
            await Task.Delay(5000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("🚀 Full Orchestration Cycle Started at: {time}", DateTimeOffset.Now);

                try
                {
                    // --- الخطوة 1: سحب الوظائف الجديدة ---
                    _logger.LogInformation("🔍 Phase 1: Scraping Jobs...");
                    await _scraperEngine.RunAllScrapersAsync(
                        searchKeyword: "Full Stack",
                        jobsPerPlatform: 30,
                        maxRetryAttempts: 2
                    );

                    // --- الخطوة 2: التقديم التلقائي ---
                    _logger.LogInformation("🖱️ Phase 2: Auto-Applying to Pending Jobs...");
                    foreach (var service in _applyServices)
                    {
                        if (stoppingToken.IsCancellationRequested) break;

                        _logger.LogInformation("🤖 Processing Platform: {platform}", service.PlatformName);
                        await ApplyToPlatformJobs(service, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Critical Error in Worker Loop.");
                }

                _logger.LogInformation("✨ Cycle completed. Waiting for 6 hours...");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task ApplyToPlatformJobs(IApplyService service, CancellationToken stoppingToken)
        {
            try
            {
                // استخدام الميثود الموجودة فعلياً في الـ BusinessLogic الخاص بك
                var pendingJobs = _jobService.GetPendingApplications(service.PlatformName);

                if (pendingJobs == null || pendingJobs.Count == 0)
                {
                    _logger.LogInformation("✅ No pending jobs for {platform}.", service.PlatformName);
                    return;
                }

                foreach (var job in pendingJobs)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    _logger.LogInformation("📝 Applying: {title} @ {company}", job.JobTitle, job.CompanyName);

                    // تنفيذ التقديم (الذي يستخدم الـ AuthService القوي داخلياً)
                    bool isSuccess = await service.ApplyAsync(job, "your_email", "your_password");

                    if (isSuccess)
                    {
                        _logger.LogInformation("✅ Application successful for ExternalId: {id}", job.ExternalJobId);

                        // تحديث الحالة في قاعدة البيانات باستخدام الميثود الخاصة بك
                        _jobService.MarkAsApplied(job.ExternalJobId, job.Platform);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Could not complete application for {id}", job.ExternalJobId);
                    }

                    // فاصل زمني "بشري" بين كل تقديم وآخر لتجنب الحظر
                    await Task.Delay(new Random().Next(45000, 90000), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in Application Flow for {platform}", service.PlatformName);
            }
        }
    }
}