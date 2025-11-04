using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static bool IsGateway(string n) => n.Length > 0 && char.IsUpper(n[0]);

    static (Dictionary<string, int> dist, Dictionary<string, string> prev)
        Bfs(Dictionary<string, SortedSet<string>> g, string start)
    {
        var dist = new Dictionary<string, int>();
        var prev = new Dictionary<string, string>();
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

    static List<string> ReconstructPath(Dictionary<string, string> prev, string start, string end)
    {
        var path = new List<string>();
        if (start == end)
        {
            path.Add(start);
            return path;
        }
        if (!prev.ContainsKey(end)) return path; // пусто, недостижимо

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

    static IEnumerable<string> AllGatewayCuts(Dictionary<string, SortedSet<string>> g, SortedSet<string> gateways)
    {
        foreach (var gw in gateways)
            if (g.TryGetValue(gw, out var nbrs))
                foreach (var x in nbrs)
                    yield return $"{gw}-{x}";
    }

    static List<string> Solve(List<(string, string)> edges)
    {
        var graph = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        var gateways = new SortedSet<string>(StringComparer.Ordinal);
        var result = new List<string>();

        foreach (var (a, b) in edges)
        {
            AddEdge(graph, a, b);
            if (IsGateway(a)) gateways.Add(a);
            if (IsGateway(b)) gateways.Add(b);
        }

        string virus = "a";

        while (true)
        {
            var adjCuts = graph.TryGetValue(virus, out var near)
                ? near.Where(IsGateway).Select(gw => $"{gw}-{virus}").OrderBy(s => s, StringComparer.Ordinal).ToList()
                : new List<string>();

            string cut = null;

            if (adjCuts.Count > 0)
            {
                cut = adjCuts[0];
            }
            else
            {
                var (dist, _) = Bfs(graph, virus);
                var reachableGates = gateways.Where(g => dist.ContainsKey(g))
                                             .OrderBy(g => dist[g])
                                             .ThenBy(g => g, StringComparer.Ordinal)
                                             .ToList();

                if (reachableGates.Count == 0)
                    break; 

                var target = reachableGates[0];
                int dTarget = dist[target];

                var candidates = graph.TryGetValue(target, out var tNbrs)
                    ? tNbrs.Where(x => dist.TryGetValue(x, out var dx) && dx == dTarget - 1)
                           .Select(x => $"{target}-{x}")
                           .OrderBy(s => s, StringComparer.Ordinal)
                           .ToList()
                    : new List<string>();
                
                if (candidates.Count == 0)
                    candidates = AllGatewayCuts(graph, gateways)
                                 .OrderBy(s => s, StringComparer.Ordinal).ToList();

                if (candidates.Count == 0) break; 

                cut = candidates[0];
            }

            // Применяем разрез 
            var parts = cut.Split('-');
            var gnode = parts[0];
            var unode = parts[1];
            RemoveEdge(graph, gnode, unode);
            result.Add(cut);
            
            var (dist2, prev2) = Bfs(graph, virus);
            var reachableAfter = gateways.Where(g => dist2.ContainsKey(g))
                                         .OrderBy(g => dist2[g])
                                         .ThenBy(g => g, StringComparer.Ordinal)
                                         .ToList();

            if (reachableAfter.Count == 0)
                break; 
            
            var target2 = reachableAfter[0];
            var path = ReconstructPath(prev2, virus, target2);
            if (path.Count >= 2)
                virus = path[1]; 
            else
                break; 
        }

        return result;
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
