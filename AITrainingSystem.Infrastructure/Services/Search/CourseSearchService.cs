using AITrainingSystem.Application.Interfaces.Services;
using AITrainingSystem.Domain.Entities;
using AITrainingSystem.Persistence.Context;
using Elastic.Clients.Elasticsearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AITrainingSystem.Infrastructure.Services.Search
{
    public class CourseSearchService : ICourseSearchService
    {
        private readonly ElasticsearchClient _client;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CourseSearchService> _logger;
        private readonly string _indexName;

        public CourseSearchService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<CourseSearchService> logger)
        {
            _context = context;
            _logger = logger;

            var uriString = configuration["Elasticsearch:Url"] ?? "http://localhost:9200";
            _indexName = configuration["Elasticsearch:Index"] ?? "courses";

            try
            {
                var settings = new ElasticsearchClientSettings(new Uri(uriString))
                    .DefaultIndex(_indexName);
                _client = new ElasticsearchClient(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Elasticsearch client.");
            }
        }

        public async Task IndexCourseAsync(Course course)
        {
            if (_client == null)
            {
                _logger.LogWarning("Elasticsearch client is not initialized. Skipping index.");
                return;
            }

            try
            {
                var response = await _client.IndexAsync(course, idx => idx.Index(_indexName));
                if (!response.IsSuccess())
                {
                    _logger.LogError("Failed to index course {CourseId} in Elasticsearch: {Error}", course.Id, response.DebugInformation);
                }
                else
                {
                    _logger.LogInformation("Successfully indexed course {CourseId} in Elasticsearch", course.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while indexing course {CourseId} in Elasticsearch", course.Id);
            }
        }

        public async Task DeleteCourseAsync(Guid courseId)
        {
            if (_client == null)
            {
                _logger.LogWarning("Elasticsearch client is not initialized. Skipping deletion.");
                return;
            }

            try
            {
                var response = await _client.DeleteAsync<Course>(courseId, idx => idx.Index(_indexName));
                if (!response.IsSuccess())
                {
                    _logger.LogError("Failed to delete course {CourseId} from Elasticsearch: {Error}", courseId, response.DebugInformation);
                }
                else
                {
                    _logger.LogInformation("Successfully deleted course {CourseId} from Elasticsearch", courseId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting course {CourseId} from Elasticsearch", courseId);
            }
        }

        public async Task<IEnumerable<Course>> SearchCoursesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await _context.Courses
                    .Include(c => c.Lessons)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();
            }

            if (_client != null)
            {
                try
                {
                    var response = await _client.SearchAsync<Course>(s => s
                        .Index(_indexName)
                        .Query(q => q
                            .MultiMatch(m => m
                                .Query(query)
                                .Fields(new[] { "title^3", "description" })
                                .Fuzziness(new Elastic.Clients.Elasticsearch.Fuzziness("AUTO"))
                            )
                        )
                    );

                    if (response.IsSuccess())
                    {
                        var ids = response.Documents.Select(d => d.Id).ToList();
                        if (ids.Any())
                        {
                            // Retrieve the actual course entities from DB with lessons included
                            var courses = await _context.Courses
                                .Include(c => c.Lessons)
                                .Where(c => ids.Contains(c.Id))
                                .ToListAsync();

                            // Maintain Elasticsearch relevance ordering
                            return courses.OrderBy(c => ids.IndexOf(c.Id));
                        }
                    }
                    else
                    {
                        _logger.LogError("Elasticsearch search failed: {Error}", response.DebugInformation);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error searching in Elasticsearch. Falling back to SQL search.");
                }
            }

            // Fallback to SQL Search
            _logger.LogInformation("Performing fallback database search for query: {Query}", query);
            var normalizedQuery = query.ToLower();
            return await _context.Courses
                .Include(c => c.Lessons)
                .Where(c => c.Title.ToLower().Contains(normalizedQuery) ||
                            c.Description.ToLower().Contains(normalizedQuery))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}
