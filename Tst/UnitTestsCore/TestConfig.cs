using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnitTestsCore
{
    public class TestConfig
    {
        public List<string> Includes { get; } = new List<string>();
        public List<string> Deletes { get; } = new List<string>();
        public List<string> Arguments { get; } = new List<string>();
        public string Description { get; set; } = "No description";
        public string Link { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append($"{nameof(Description)}: {Description}\n");
            if (!string.IsNullOrEmpty(Link))
            {
                builder.Append($"{nameof(Link)}: {Link}\n");
            }

            foreach (var include in Includes.Select((value, i) => new {i, value}))
            {
                builder.Append($"{nameof(Includes)}[{include.i}]: {include.value}\n");
            }

            foreach (var delete in Deletes.Select((value, i) => new {i, value}))
            {
                builder.Append($"{nameof(Deletes)}[{delete.i}]: {delete.value}\n");
            }

            foreach (var arg in Arguments.Select((value, i) => new {i, value}))
            {
                builder.Append($"{nameof(Arguments)}[{arg.i}]: {arg.value}\n");
            }

            return builder.ToString();
        }
    }
}