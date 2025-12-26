using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureDesigner
{
    public interface IIdMapping
    {
        int GetCompactId(string fullId);
        string GetFullId(int compactId);
        void Map(global::System.String fullId);
        IEnumerable<int> GetCompactIds();
        IEnumerable<string> GetFullIds();
    }

    public class IdMapping : IIdMapping
    {
        readonly Dictionary<int, string> _compactToFull = new();
        readonly Dictionary<string, int> _fullToCompact = new(StringComparer.InvariantCultureIgnoreCase);
        int _index = 0;

        public void Map(string fullId)
        {            
            _compactToFull[_index] = fullId;
            _fullToCompact[fullId] = _index;
            _index++;
        }

        public string GetFullId(int compactId)
            => _compactToFull.TryGetValue(compactId, out var fullId) ? fullId : null;

        public int GetCompactId(string fullId)
            => _fullToCompact.TryGetValue(fullId, out var compactId) ? compactId : -1;

        public IEnumerable<int> GetCompactIds()
            => _compactToFull.Keys;

        public IEnumerable<string> GetFullIds()
            => _compactToFull.Values;
    }
}
