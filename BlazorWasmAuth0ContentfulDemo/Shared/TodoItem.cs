using Contentful.Core.Models;

namespace BlazorWasmAuth0ContentfulDemo.Shared
{
    public class TodoItem
    {
        public SystemProperties Sys { get; set; } = new();

        public string Task { get; set; } = string.Empty;

        public bool Completed { get; set; }
    }
}
