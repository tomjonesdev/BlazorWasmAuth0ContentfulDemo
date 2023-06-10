using BlazorWasmAuth0ContentfulDemo.Shared;
using Contentful.Core;
using Contentful.Core.Configuration;
using Contentful.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BlazorWasmAuth0ContentfulDemo.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
	[Authorize]
	public class TodoController : ControllerBase
    {
        private readonly IContentfulClient _cdaClient;
        private readonly IContentfulManagementClient _cmaClient;
        private readonly ContentfulOptions _options;

        public TodoController(
            IHttpClientFactory httpClientFactory,
            IContentfulClient contentfulClient,
            IOptions<ContentfulOptions> options)
        {
            _options = options.Value;
            _cdaClient = contentfulClient;
            _cmaClient = new ContentfulManagementClient(
                httpClientFactory.CreateClient(),
                _options.ManagementApiKey,
                _options.SpaceId);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get(
            CancellationToken cancellationToken = default)
        {
            var entries = await _cdaClient.GetEntries<TodoItem>(cancellationToken: cancellationToken);

            return Ok(entries.Select(i => new TodoItemViewModel
            {
                Id = i.Sys.Id,
                Task = i.Task,
                Completed = i.Completed,
            }));
        }

        [HttpPost]
        public async Task<IActionResult> Post(
            [FromBody] string task,
            CancellationToken cancellationToken = default)
        {
            var newEntryId = Guid.NewGuid().ToString();
            var entry = new Entry<dynamic>
            {
                SystemProperties = new() { Id = newEntryId },
                Fields = new
                {
                    Task = new Dictionary<string, string>()
                    {
                        { "en-US", task }
                    }
                }
            };

            await _cmaClient.CreateOrUpdateEntry(entry, contentTypeId: "todoItem", cancellationToken: cancellationToken);
            await _cmaClient.PublishEntry(newEntryId, 1, cancellationToken: cancellationToken);

            var itemViewModel = new TodoItemViewModel
            {
                Id = newEntryId,
                Task = task
            };

            return Created(newEntryId, itemViewModel);
        }

        [HttpPut]
        public async Task<IActionResult> Put(
            [FromBody] TodoItemViewModel item,
            CancellationToken cancellationToken = default)
        {
            var existingEntry = await _cmaClient.GetEntry(item.Id, cancellationToken: cancellationToken);
            var existingVersion = existingEntry.SystemProperties.Version.GetValueOrDefault(0);

            var updatedEntry = new Entry<dynamic>
            {
                SystemProperties = new() { Id = item.Id },
                Fields = new
                {
                    Task = new Dictionary<string, string>()
                    {
                        { "en-US", item.Task }
                    },
                    Completed = new Dictionary<string, bool>()
                    {
                        { "en-US", !item.Completed }
                    }
                }
            };

            await _cmaClient.CreateOrUpdateEntry(updatedEntry, version: existingVersion, cancellationToken: cancellationToken);
            await _cmaClient.PublishEntry(item.Id, existingVersion + 1, cancellationToken: cancellationToken);

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
            string id,
            CancellationToken cancellationToken = default)
        {
            var existingEntry = await _cmaClient.GetEntry(id, cancellationToken: cancellationToken);
            var existingVersion = existingEntry.SystemProperties.Version.GetValueOrDefault(0);

            await _cmaClient.UnpublishEntry(id, existingVersion, cancellationToken: cancellationToken);
            await _cmaClient.DeleteEntry(id, existingVersion, cancellationToken: cancellationToken);

            return Ok();
        }
    }
}
