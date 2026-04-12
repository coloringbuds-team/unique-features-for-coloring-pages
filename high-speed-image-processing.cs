using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AdvancedImageToolbox
{
    /// <summary>
    /// Provides high-performance, multi-threaded image processing and HTML manipulation utilities.
    /// Designed for automated content generation systems.
    /// </summary>
    public static class ImageProcessor
    {
        // Reusable HttpClient to prevent socket exhaustion
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(200) };

        /// <summary>
        /// Intelligently distributes and injects random images into text nodes of an HTML document.
        /// Useful for dynamically enriching articles with visual content and SEO alt tags.
        /// </summary>
        /// <param name="doc">The loaded HTML document to modify.</param>
        /// <param name="imageUrls">A pool of available image URLs.</param>
        /// <param name="keywords">Comma-separated keywords for SEO alt attributes.</param>
        /// <param name="fallbackTitle">Fallback title for alt attributes if keywords are missing.</param>
        public static void InjectRandomImages(HtmlDocument doc, IReadOnlyList<string> imageUrls, string keywords, string fallbackTitle)
        {
            if (doc == null || imageUrls == null || !imageUrls.Any()) return;

            var textNodes = doc.DocumentNode.SelectNodes("//text()")?
                .Where(node => !string.IsNullOrWhiteSpace(node.InnerText))
                .ToList();

            if (textNodes == null || !textNodes.Any()) return;

            var random = new Random();
            int insertCount = random.Next(2, 4);
            
            // Parse SEO tags
            var altTags = string.IsNullOrWhiteSpace(keywords) 
                ? new List<string> { fallbackTitle } 
                : keywords.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(k => k.Trim()).ToList();

            for (int i = 0; i < insertCount; i++)
            {
                string imageUrl = imageUrls[random.Next(imageUrls.Count)];
                string randomAlt = altTags[random.Next(altTags.Count)];
                
                var imgNode = HtmlNode.CreateNode($"<img src='{imageUrl}' alt='{randomAlt}' title='{randomAlt}' />");
                var randomTextNode = textNodes[random.Next(textNodes.Count)];

                randomTextNode.ParentNode.InsertAfter(imgNode, randomTextNode);
            }
        }

        /// <summary>
        /// Extracts a randomized, non-repeating subset of image URLs from a source collection.
        /// </summary>
        /// <param name="sourceList">The original list of image URLs.</param>
        /// <param name="minLimit">Minimum number of images to return.</param>
        /// <param name="maxLimit">Maximum number of images to return.</param>
        /// <returns>A new list containing randomly selected, distinct image URLs.</returns>
        public static List<string> SelectDistinctRandomImages(IReadOnlyList<string> sourceList, int minLimit, int maxLimit)
        {
            if (sourceList == null || !sourceList.Any()) return new List<string>();

            var random = new Random();
            int selectionCount = maxLimit > 0 ? random.Next(minLimit, maxLimit + 1) : 0;
            
            selectionCount = Math.Min(selectionCount, sourceList.Count);
            if (selectionCount <= 0) return new List<string>();

            // Optimized random selection without while-loop overhead
            return sourceList.OrderBy(x => random.Next()).Take(selectionCount).ToList();
        }

        /// <summary>
        /// Generates an image asynchronously utilizing an external AI provider API.
        /// </summary>
        /// <param name="prompt">The descriptive prompt for image generation.</param>
        /// <param name="modelName">The target AI model identifier.</param>
        /// <param name="apiKey">Authentication token for the API.</param>
        /// <returns>The URL string of the generated image.</returns>
        public static async Task<string> GenerateImageFromPromptAsync(string prompt, string modelName, string apiKey)
        {
            try
            {
                var requestUri = "ai url";
                var requestData = new
                {
                    model = modelName,
                    prompt = prompt,
                    n = 1,
                    size = "1024x1024",
                    response_format = "url" 
                };

                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
                requestMessage.Headers.Add("Authorization", $"Bearer {apiKey}");
                
                string jsonBody = JsonConvert.SerializeObject(requestData);
                requestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                var jsonObject = JObject.Parse(responseString);
                
                return jsonObject["data"]?[0]?["url"]?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                // Return an error string or handle logging properly in production
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Processes a massive batch of image tasks concurrently.
        /// Leverages multi-threading across multiple CPU cores to prevent bottlenecking.
        /// </summary>
        /// <param name="imageJobs">The collection of images to process.</param>
        /// <param name="processingAction">The specific action delegate to apply to each image.</param>
        public static void ProcessImagesInParallel(IEnumerable<ImageJob> imageJobs, Action<ImageJob> processingAction)
        {
            if (imageJobs == null) return;

            // Utilizing Parallel.ForEach for optimal multi-core CPU performance during heavy I/O or rendering ops
            Parallel.ForEach(imageJobs, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, job =>
            {
                try
                {
                    processingAction(job);
                }
                catch (Exception ex)
                {
                    // Isolate individual job failures to prevent halting the entire parallel execution
                    System.Diagnostics.Debug.WriteLine($"Failed to process image job {job.Path}: {ex.Message}");
                }
            });
        }
    }

    /// <summary>
    /// Represents a payload for a queued image processing task.
    /// </summary>
    public class ImageJob
    {
        public string Path { get; set; }
        public string CatName { get; set; }
        public string ParentCatName { get; set; }
    }
}
