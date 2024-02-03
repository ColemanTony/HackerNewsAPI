using HackerNewsAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace HackerNewsAPI.Controllers
{
    [ApiController]
    [Route("BestStories")]
    public class NewsController : Controller
    {
        private IMemoryCache cache;
        private readonly ILogger<NewsController> logger;
        private static HttpClient client = new HttpClient();

        private readonly string bestStoriesUrl;
        private readonly string getStoryUrl;

        public NewsController(IMemoryCache cache, ILogger<NewsController> logger, IConfiguration config)
        {
            this.cache = cache;
            this.logger = logger;
            bestStoriesUrl = config["HackerNews:BestStoriesUrl"] ?? string.Empty;
            getStoryUrl = config["HackerNews:GetStoryUrl"] ?? string.Empty;
        }

        [HttpGet(Name = "GetTopNews")]
        [ProducesResponseType(type: typeof(IEnumerable<NewsStory>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<NewsStory>>> GetTopNews(int n)
        {
            if (n < 1 || n > 1000)
                return BadRequest("Validation Error");

            try
            {
                var response = await client.GetAsync(bestStoriesUrl);
                if (response.IsSuccessStatusCode)
                {
                    var stories = new List<NewsStory>();

                    var result = response.Content.ReadAsStringAsync().Result;

                    var topNIds = JsonConvert.DeserializeObject<List<int>>(result).Take(n);

                    if (topNIds != null)
                    {
                        var tasks = topNIds.Select(GetNewsStoryAsync);
                        stories = (await Task.WhenAll(tasks)).ToList();
                    }
                    return Ok(stories.OrderByDescending(s => s.Score));
                }
                else
                {
                    logger.LogError(response.ReasonPhrase);
                    return BadRequest(response.ReasonPhrase);
                }
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return BadRequest(ex.Message);
            }
        }

        private async Task<NewsStory> GetNewsStoryAsync(int storyId)
        {
            return await cache.GetOrCreateAsync(storyId,
                async cacheEntry =>
                {
                    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                    NewsStory? story = null;

                    var response = await client.GetAsync(string.Format(getStoryUrl, storyId));
                    if (response.IsSuccessStatusCode)
                    {
                        var result = response.Content.ReadAsStringAsync().Result;
                        story = JsonConvert.DeserializeObject<NewsStory>(result);
                    }

                    return story ?? new NewsStory();
                });
        }

    }
}
