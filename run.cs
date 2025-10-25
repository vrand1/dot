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
        public List<char> Slots { get; }  // снизу вверх (0 — дно)
        public Room(int index, int depth) { Index = index; Depth = depth; Slots = new List<char>(depth); }
        public Room Clone()
        {
            var r = new Room(Index, Depth);
            r.Slots.AddRange(Slots);
            return r;
        }
        public bool IsComplete
        {
            get
            {
                if (Slots.Count != Depth) return false;
                for (int i = 0; i < Slots.Count; i++)
                    if (Slots[i] != (char)('A' + Index)) return false;
                return true;
            }
        }
        public bool CanAccept(char c)
        {
            if (Slots.Count >= Depth) return false;
            for (int i = 0; i < Slots.Count; i++)
                if (Slots[i] != c) return false;
            return true;
        }
        public int StepsToHallForTop()
        {
            int k = Slots.Count;             
            if (k == 0) return -1;
            return Depth - (k - 1);
        }
        public char PeekTop() => Slots[^1];
        public char PopTop() { var t = Slots[^1]; Slots.RemoveAt(Slots.Count - 1); return t; }
        public void PushBottom(char c) { if (Slots.Count < Depth) Slots.Add(c); }
    }

    class State
    {
        public char[] Hall;                  
        public Room[] Rooms;
        public State(char[] hall, Room[] rooms) { Hall = hall; Rooms = rooms; }
        public bool IsGoal
        {
            get
            {
                for (int i = 0; i < Hall.Length; i++) if (Hall[i] != '.') return false;
                for (int r = 0; r < 4; r++) if (!Rooms[r].IsComplete) return false;
                return true;
            }
        }
        public State Clone()
        {
            var h = new char[Hall.Length];
            Array.Copy(Hall, h, Hall.Length);
            var rs = new Room[4];
            for (int i = 0; i < 4; i++) rs[i] = Rooms[i].Clone();
            return new State(h, rs);
        }
    }

    static string Key(State s)
    {
        var parts = new char[12 + s.Rooms.Sum(r => r.Slots.Count)];
        int p = 0;
        for (int i = 0; i < 11; i++) parts[p++] = s.Hall[i];
        parts[p++] = '|';
        for (int r = 0; r < 4; r++)
            for (int i = 0; i < s.Rooms[r].Slots.Count; i++)
                parts[p++] = s.Rooms[r].Slots[i];
        return new string(parts);
    }

    static int Heuristic(State s)
    {
        int h = 0;
        var correctBottom = new int[4];
        for (int r = 0; r < 4; r++)
        {
            int ok = 0;
            for (int i = 0; i < s.Rooms[r].Slots.Count; i++)
            {
                if (s.Rooms[r].Slots[i] == (char)('A' + r)) ok++;
                else break;
            }
            correctBottom[r] = ok;
        }

        for (int i = 0; i < s.Hall.Length; i++)
        {
            char c = s.Hall[i];
            if (c == '.') continue;
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
                if (t == r && i < correctBottom[r]) continue;
                int outSteps = s.Rooms[r].Depth - i;
                int horiz = Math.Abs(RoomPos[r] - RoomPos[t]);
                int inSteps = 1;
                h += (outSteps + horiz + inSteps) * Cost[t];
            }
        }
        return h;
    }

    static IEnumerable<(State,int)> NextStates(State s)
    {
        for (int r = 0; r < 4; r++)
        {
            var room = s.Rooms[r];
            if (room.Slots.Count == 0) continue;
            bool allGood = true;
            for (int i = 0; i < room.Slots.Count; i++)
                if (room.Slots[i] != (char)('A' + r)) { allGood = false; break; }
            if (allGood) continue;

            int topDist = room.StepsToHallForTop();
            char pod = room.PeekTop();
            int startX = RoomPos[r];
            foreach (int pos in HallStops)
            {
                if (s.Hall[pos] != '.') continue;
                if (!Clear(s.Hall, startX, pos)) continue;
                var ns = s.Clone();
                ns.Rooms[r].PopTop();
                ns.Hall[pos] = pod;
                int steps = topDist + Math.Abs(startX - pos);
                int cost = steps * Cost[pod - 'A'];
                yield return (ns, cost);
            }
        }

        for (int i = 0; i < s.Hall.Length; i++)
        {
            char c = s.Hall[i];
            if (c == '.') continue;
            int t = c - 'A';
            var room = s.Rooms[t];
            if (!room.CanAccept(c)) continue;
            int door = RoomPos[t];
            if (!Clear(s.Hall, i, door)) continue;

            var ns = s.Clone();
            ns.Hall[i] = '.';
            ns.Rooms[t].PushBottom(c);
            int steps = Math.Abs(i - door) + (ns.Rooms[t].Depth - ns.Rooms[t].Slots.Count + 1);
            int cost = steps * Cost[t];
            yield return (ns, cost);
        }
    }

    static bool Clear(char[] hall, int a, int b)
    {
        int min = Math.Min(a, b), max = Math.Max(a, b);
        for (int x = min + 1; x < max; x++) if (hall[x] != '.') return false;
        return true;
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
            if (pods.Length < 4) return 0;
            for (int r = 0; r < 4; r++) rooms[r].PushBottom(pods[r]);
        }

        var start = new State("...........".ToCharArray(), rooms);

        var open = new PriorityQueue<(State S, int G), int>();
        var best = new Dictionary<string, int>(1 << 20);
        var k0 = Key(start);
        best[k0] = 0;
        open.Enqueue((start, 0), Heuristic(start));

        while (open.TryDequeue(out var item, out _))
        {
            var s = item.S;
            int g = item.G;
            var key = Key(s);
            if (best[key] != g) continue;
            if (s.IsGoal) return g;

            foreach (var (ns, moveCost) in NextStates(s))
            {
                int ng = g + moveCost;
                var nk = Key(ns);
                if (!best.TryGetValue(nk, out var og) || ng < og)
                {
                    best[nk] = ng;
                    open.Enqueue((ns, ng), ng + Heuristic(ns));
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
