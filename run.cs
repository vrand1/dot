using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    enum AmphipodType { A, B, C, D }

    static readonly int[] RoomPos = { 2, 4, 6, 8 };
    static readonly int[] HallStops = { 0, 1, 3, 5, 7, 9, 10 };
    static readonly int[] Cost = { 1, 10, 100, 1000 };

    class Room
    {
        public int Index { get; }
        public int Depth { get; }
        public List<char> Slots { get; }
        public Room(int index, int depth) { Index = index; Depth = depth; Slots = new List<char>(depth); }
        public Room Clone() { var r = new Room(Index, Depth); r.Slots.AddRange(Slots); return r; }
        public bool IsComplete
        {
            get
            {
                if (Slots.Count != Depth) return false;
                for (int i = 0; i < Depth; i++) if (Slots[i] != (char)('A' + Index)) return false;
                return true;
            }
        }
        public bool CanAccept(char c)
        {
            if (Slots.Count >= Depth) return false;
            for (int i = 0; i < Slots.Count; i++) if (Slots[i] != c) return false;
            return true;
        }
        public int StepsToHallForTop()
        {
            int k = Slots.Count;
            if (k == 0) return -1;
            int topIdx = k - 1;
            return Depth - topIdx;
        }
        public char PeekTop() => Slots[^1];
        public char PopTop() { var t = Slots[^1]; Slots.RemoveAt(Slots.Count - 1); return t; }
        public void PushBottom(char c) { if (Slots.Count < Depth) Slots.Add(c); }
    }

    class State
    {
        public char[] Hall;
        public Room[] Rooms;
        public int Depth;
        public State(char[] hall, Room[] rooms, int depth) { Hall = hall; Rooms = rooms; Depth = depth; }
        public State Clone()
        {
            var h = new char[Hall.Length]; Array.Copy(Hall, h, Hall.Length);
            var rs = new Room[4]; for (int i = 0; i < 4; i++) rs[i] = Rooms[i].Clone();
            return new State(h, rs, Depth);
        }
        public bool IsGoal
        {
            get
            {
                for (int i = 0; i < Hall.Length; i++) if (Hall[i] != '.') return false;
                for (int r = 0; r < 4; r++) if (!Rooms[r].IsComplete) return false;
                return true;
            }
        }
    }

    static string Key(State s)
    {
        int len = 11 + 1 + (s.Depth * 4) + 4;
        var k = new char[len];
        int p = 0;
        for (int i = 0; i < 11; i++) k[p++] = s.Hall[i];
        k[p++] = '|';
        for (int r = 0; r < 4; r++)
        {
            for (int i = 0; i < s.Depth; i++)
                k[p++] = i < s.Rooms[r].Slots.Count ? s.Rooms[r].Slots[i] : '.';
            k[p++] = '|';
        }
        return new string(k);
    }

    static int Heuristic(State s)
    {
        int h = 0;
        var ok = new int[4];
        for (int r = 0; r < 4; r++)
        {
            int cnt = 0;
            for (int i = 0; i < s.Rooms[r].Slots.Count; i++)
            {
                if (s.Rooms[r].Slots[i] == (char)('A' + r)) cnt++; else break;
            }
            ok[r] = cnt;
        }
        for (int i = 0; i < s.Hall.Length; i++)
        {
            char c = s.Hall[i]; if (c == '.') continue;
            int t = c - 'A';
            int steps = Math.Abs(i - RoomPos[t]) + 1;
            h += steps * Cost[t];
        }
        for (int r = 0; r < 4; r++)
        {
            for (int i = 0; i < s.Rooms[r].Slots.Count; i++)
            {
                char c = s.Rooms[r].Slots[i];
                int t = c - 'A';
                if (t == r && i < ok[r]) continue;
                int outSteps = s.Rooms[r].Depth - i;
                int horiz = Math.Abs(RoomPos[r] - RoomPos[t]);
                int inSteps = 1;
                h += (outSteps + horiz + inSteps) * Cost[t];
            }
        }
        return h;
    }

    static bool Clear(char[] hall, int a, int b)
    {
        int min = Math.Min(a, b), max = Math.Max(a, b);
        for (int x = min + 1; x < max; x++) if (hall[x] != '.') return false;
        return true;
    }

    static IEnumerable<(State,int)> NextStates(State s)
    {
        for (int r = 0; r < 4; r++)
        {
            var room = s.Rooms[r];
            if (room.Slots.Count == 0) continue;

            bool allRight = true;
            for (int i = 0; i < room.Slots.Count; i++) if (room.Slots[i] != (char)('A' + r)) { allRight = false; break; }
            if (allRight) continue;

            int exit = room.StepsToHallForTop();
            char pod = room.PeekTop();
            int from = RoomPos[r];
            int t = pod - 'A';
            var dest = s.Rooms[t];
            if (dest.CanAccept(pod) && Clear(s.Hall, from, RoomPos[t]))
            {
                var ns = s.Clone();
                ns.Rooms[r].PopTop();
                int down = dest.Depth - dest.Slots.Count; 
                ns.Rooms[t].PushBottom(pod);
                int steps = exit + Math.Abs(from - RoomPos[t]) + down;
                int cost = steps * Cost[t];
                yield return (ns, cost);
                continue; 
            }
            for (int j = 0; j < HallStops.Length; j++)
            {
                int hx = HallStops[j];
                if (s.Hall[hx] != '.') continue;
                if (!Clear(s.Hall, from, hx)) continue;
                var ns = s.Clone();
                ns.Rooms[r].PopTop();
                ns.Hall[hx] = pod;
                int steps = exit + Math.Abs(from - hx);
                int cost = steps * Cost[pod - 'A'];
                yield return (ns, cost);
            }
        }

        for (int i = 0; i < s.Hall.Length; i++)
        {
            char c = s.Hall[i]; if (c == '.') continue;
            int t = c - 'A';
            var room = s.Rooms[t];
            if (!room.CanAccept(c)) continue;
            int door = RoomPos[t];
            if (!Clear(s.Hall, i, door)) continue;
            var ns = s.Clone();
            ns.Hall[i] = '.';
            int down = room.Depth - room.Slots.Count;
            ns.Rooms[t].PushBottom(c);
            int steps = Math.Abs(i - door) + down;
            int cost = steps * Cost[t];
            yield return (ns, cost);
        }
    }

    static int Solve(List<string> lines)
    {
        if (lines == null || lines.Count == 0) return 0;
        var roomLines = lines.Where(l => l.Any(ch => ch is >= 'A' and <= 'D')).ToList();
        int depth = roomLines.Count;
        if (depth != 2 && depth != 4) return 0;

        var rooms = new Room[4];
        for (int r = 0; r < 4; r++) rooms[r] = new Room(r, depth);
        roomLines.Reverse();
        foreach (var row in roomLines)
        {
            var pods = row.Where(ch => ch is >= 'A' and <= 'D').ToArray();
            if (pods.Length != 4) return 0;
            for (int r = 0; r < 4; r++) rooms[r].PushBottom(pods[r]);
        }

        var start = new State("...........".ToCharArray(), rooms.Select(x => x.Clone()).ToArray(), depth);

        var open = new PriorityQueue<(State S, int G, int Seq), (int F, int Seq)>();
        var best = new Dictionary<string, int>(1 << 18);

        string k0 = Key(start);
        best[k0] = 0;
        int seq = 0;
        open.Enqueue((start, 0, seq), (Heuristic(start), seq));

        while (open.TryDequeue(out var item, out var pri))
        {
            var s = item.S;
            int g = item.G;
            var key = Key(s);
            if (!best.TryGetValue(key, out var gbest) || gbest != g) continue;
            if (s.IsGoal) return g;

            foreach (var (ns, move) in NextStates(s))
            {
                int ng = g + move;
                var nk = Key(ns);
                if (!best.TryGetValue(nk, out var old) || ng < old)
                {
                    best[nk] = ng;
                    seq++;
                    int f = ng + Heuristic(ns);
                    open.Enqueue((ns, ng, seq), (f, seq));
                }
            }
        }
        return 0;
    }

    static void Main()
    {
        try
        {
            var lines = new List<string>();
            string line;
            while ((line = Console.ReadLine()) != null) lines.Add(line);
            Console.WriteLine(Solve(lines));
        }
        catch
        {
            Console.WriteLine(0);
        }
    }
}
