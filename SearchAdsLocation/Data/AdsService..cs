using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SearchAdsLocation.Data
{
    public class TrieNode
    {
        public Dictionary<string, TrieNode> Children { get; } = new Dictionary<string, TrieNode>();
        public HashSet<string> Platforms { get; } = new HashSet<string>(StringComparer.Ordinal); // Список рекламных площадок
    }

    public class Trie
    {
        private readonly TrieNode _root = new TrieNode();

        public void AddLocation(List<string> location, string platformName)
        {
            var currentNode = _root;

            foreach (var ch in location)
            {
                if (!currentNode.Children.ContainsKey(ch))
                {
                    currentNode.Children[ch] = new TrieNode();
                }
                currentNode = currentNode.Children[ch];
            }

            currentNode.Platforms.Add(platformName);
        }

        public HashSet<string> SearchLocations(List<string> location)
        {
            var currentNode = _root;
            var result = new HashSet<string>(StringComparer.Ordinal);

            foreach (var ch in location)
            {
                if (!currentNode.Children.ContainsKey(ch))
                {
                    return result;
                }
                currentNode = currentNode.Children[ch];
            }

            CollectPlatforms(currentNode, result);
            return result;
        }

        private void CollectPlatforms(TrieNode node, HashSet<string> result)
        {
            if (node.Platforms.Any())
            {
                foreach (var platform in node.Platforms)
                {
                    result.Add(platform);
                }
            }

            foreach (var child in node.Children.Values)
            {
                CollectPlatforms(child, result);
            }
        }
    }

    public class AdsService
    {
        private readonly Trie _trie = new Trie();

        private List<string> NormalizeLocation(string loc)
        {
            if (string.IsNullOrWhiteSpace(loc)) return null;
            loc = loc.Trim();
            if (!loc.StartsWith("/")) loc = "/" + loc;
            if (loc.Length > 1 && loc.EndsWith("/")) loc = loc.TrimEnd('/');

            return loc.Split("/").ToList();
        }

        public async Task UploadAsync(Stream fileStream)
        {
            var reader = new StreamReader(fileStream, Encoding.UTF8);
            var text = await reader.ReadToEndAsync();

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var idx = trimmed.IndexOf(':');
                if (idx <= 0) continue;

                var name = trimmed[..idx].Trim();
                if (string.IsNullOrEmpty(name)) continue;

                var locPart = trimmed[(idx + 1)..];
                var locs = locPart.Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var rawLoc in locs)
                {
                    var norm = NormalizeLocation(rawLoc);
                    if (norm == null) continue;

                    _trie.AddLocation(norm, name);
                }
            }
        }

        public async Task<List<string>> SearchAsync(string location)
        {
            var normQuery = NormalizeLocation(location);
            if (normQuery == null) return new List<string>();

            var result = await Task.Run(() => _trie.SearchLocations(normQuery));

            return result.OrderBy(x => x).ToList();
        }
    }
}
