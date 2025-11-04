using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static bool IsGateway(string n) => n.Length > 0 && char.IsUpper(n[0]);

    static (Dictionary<string,int> dist, Dictionary<string,string> prev) Bfs(
        Dictionary<string, SortedSet<string>> g, string start)
    {
        var dist = new Dictionary<string,int>();
        var prev = new Dictionary<string,string>();
        var q = new Queue<string>();
        dist[start] = 0;
        q.Enqueue(start);

        while (q.Count > 0)
        {
            var u = q.Dequeue();
            if (!g.TryGetValue(u, out var nbrs)) continue;

            foreach (var v in nbrs) 
            {
                if (dist.ContainsKey(v)) continue;
                dist[v] = dist[u] + 1;
                prev[v] = u;
                q.Enqueue(v);
            }
        }

        return (dist, prev);
    }

    static List<string> ReconstructPath(Dictionary<string,string> prev, string start, string end)
    {
        if (start == end) return new List<string> { start };
        if (!prev.ContainsKey(end)) return new List<string>();

        var path = new List<string>();
        string cur = end;
        while (cur != start)
        {
            path.Add(cur);
            if (!prev.TryGetValue(cur, out var p)) return new List<string>();
            cur = p;
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    static (bool done, string next) NextVirusStep(
        Dictionary<string, SortedSet<string>> g,
        SortedSet<string> gateways,
        string virus)
    {
        var (dist, prev) = Bfs(g, virus);
        var reach = gateways.Where(gw => dist.ContainsKey(gw))
                            .OrderBy(gw => dist[gw])
                            .ThenBy(gw => gw, StringComparer.Ordinal)
                            .ToList();

        if (reach.Count == 0)
            return (true, null); 

        var target = reach[0];
        if (dist[target] == 1)
            return (true, "FAIL"); 

        var path = ReconstructPath(prev, virus, target);
        if (path.Count < 2) return (true, "FAIL"); 
        return (false, path[1]); 
    }

    static Dictionary<string, SortedSet<string>> CloneGraph(Dictionary<string, SortedSet<string>> g)
    {
        var copy = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (var kv in g)
            copy[kv.Key] = new SortedSet<string>(kv.Value, StringComparer.Ordinal);
        return copy;
    }

    static void AddEdge(Dictionary<string, SortedSet<string>> g, string a, string b)
    {
        if (!g.ContainsKey(a)) g[a] = new SortedSet<string>(StringComparer.Ordinal);
        if (!g.ContainsKey(b)) g[b] = new SortedSet<string>(StringComparer.Ordinal);
        g[a].Add(b);
        g[b].Add(a);
    }

    static void RemoveEdge(Dictionary<string, SortedSet<string>> g, string a, string b)
    {
        if (g.TryGetValue(a, out var sa)) sa.Remove(b);
        if (g.TryGetValue(b, out var sb)) sb.Remove(a);
    }

    static IEnumerable<string> AllGatewayCuts(
        Dictionary<string, SortedSet<string>> g, SortedSet<string> gateways)
    {
        foreach (var gw in gateways)
            if (g.TryGetValue(gw, out var nbrs))
                foreach (var x in nbrs)
                    yield return $"{gw}-{x}";
    }

    static string StateKey(Dictionary<string, SortedSet<string>> g, SortedSet<string> gateways, string virus)
    {
        var parts = new List<string> { $"V:{virus}" };
        foreach (var gw in gateways)
            if (g.TryGetValue(gw, out var nbrs))
                foreach (var x in nbrs)
                    parts.Add($"{gw}-{x}");
        return string.Join("|", parts);
    }

    static List<string> Dfs(
        Dictionary<string, SortedSet<string>> g,
        SortedSet<string> gateways,
        string virus,
        HashSet<string> bad)
    {
        var key = StateKey(g, gateways, virus);
        if (bad.Contains(key)) return null;

        var adj = g.TryGetValue(virus, out var near)
            ? near.Where(IsGateway).Select(gw => $"{gw}-{virus}").ToList()
            : new List<string>();

        List<string> cuts = adj.Count > 0
            ? adj
            : AllGatewayCuts(g, gateways).ToList();

        cuts.Sort(StringComparer.Ordinal);

        foreach (var cut in cuts)
        {
            var parts = cut.Split('-');
            var gw = parts[0];
            var x  = parts[1];

            var g2 = CloneGraph(g);
            RemoveEdge(g2, gw, x);

            var (done, next) = NextVirusStep(g2, gateways, virus);
            if (done && next == null)
                return new List<string> { cut }; 
            if (done && next == "FAIL")
                continue; 

            var sub = Dfs(g2, gateways, next, bad);
            if (sub != null)
            {
                sub.Insert(0, cut);
                return sub; 
            }
        }

        bad.Add(key);
        return null;
    }

    static List<string> Solve(List<(string, string)> edges)
    {
        var g = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        var gateways = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var (a, b) in edges)
        {
            AddEdge(g, a, b);
            if (IsGateway(a)) gateways.Add(a);
            if (IsGateway(b)) gateways.Add(b);
        }

        var ans = Dfs(g, gateways, "a", new HashSet<string>());
        return ans ?? new List<string>(); 
    }

    static void Main()
    {
        var edges = new List<(string, string)>();
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.Length == 0) continue;
            var parts = line.Split('-');
            if (parts.Length == 2)
                edges.Add((parts[0], parts[1]));
        }

        var result = Solve(edges);
        foreach (var edge in result)
        {
            Console.WriteLine(edge);
        }
    }
}
