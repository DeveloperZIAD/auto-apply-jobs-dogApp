using BusinessLogic;


namespace JobBot.Scraper.Iface
{
    public interface IJobScraper
    {
        string PlatformName { get; }
        Task<List<JobPostingDto>> ScrapeJobsAsync(string keyword, int maxJobs);
    }
}