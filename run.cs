using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    enum AmphipodType { A, B, C, D }

    class Amphipod
    {
        public AmphipodType Type { get; }
        public int CostPerStep => (int)Math.Pow(10, (int)Type);
        public int TargetRoomIndex => (int)Type;
        public Amphipod(AmphipodType type) => Type = type;
        public override string ToString() => Type.ToString();
    }

    class Room
    {
        public int Index { get; }
        public int Depth { get; }
        public List<Amphipod> Slots { get; }

        public Room(int index, int depth, IEnumerable<Amphipod> pods = null)
        {
            Index = index;
            Depth = depth;
            Slots = new List<Amphipod>(pods ?? Enumerable.Empty<Amphipod>());
        }

        public bool IsComplete => Slots.Count == Depth && Slots.All(a => (int)a.Type == Index);
        public bool CanAccept(Amphipod a) => Slots.Count < Depth && Slots.All(p => p.Type == a.Type);
        public Amphipod Peek() => Slots.Count > 0 ? Slots.Last() : null;
        public Amphipod Pop()
        {
            if (Slots.Count == 0) return null;
            var a = Slots.Last();
            Slots.RemoveAt(Slots.Count - 1);
            return a;
        }
        public void Push(Amphipod a) { if (a != null) Slots.Add(a); }
        public bool IsEmpty => Slots.Count == 0;
        public Room Clone() => new(Index, Depth, Slots.Select(s => new Amphipod(s.Type)));
    }

    class State
    {
        public string Hallway { get; set; }
        public Room[] Rooms { get; set; }

        public State(string hallway, Room[] rooms)
        {
            Hallway = hallway;
            Rooms = rooms;
        }

        public bool IsGoal => Hallway.All(c => c == '.') && Rooms.All(r => r.IsComplete);

        public State Copy()
        {
            var newRooms = Rooms.Select(r => r.Clone()).ToArray();
            return new State(Hallway ?? "...........", newRooms);
        }

        public override int GetHashCode()
        {
            var hash = Hallway;
            foreach (var r in Rooms)
                hash += "|" + string.Join("", r.Slots.Select(a => a.ToString()));
            return hash.GetHashCode();
        }
    }

    static int Solve(List<string> lines)
    {
        if (lines == null || lines.Count < 4 || !lines.Any(x => x.Contains('#')))
            return 0;

        int depth = lines.Count >= 7 ? 4 : 2;
        var rooms = ParseRooms(lines, depth);
        if (rooms.Any(r => r == null))
            return 0;

        var start = new State("...........", rooms);
        var RoomPos = new[] { 2, 4, 6, 8 };
        var HallStops = new[] { 0, 1, 3, 5, 7, 9, 10 };

        var pq = new PriorityQueue<State, int>();
        var best = new Dictionary<int, int>();
        pq.Enqueue(start, 0);
        best[start.GetHashCode()] = 0;

        while (pq.TryDequeue(out var state, out var cost))
        {
            if (state == null || state.Hallway == null || state.Rooms == null)
                continue;

            if (state.IsGoal)
                return cost;

            foreach (var (next, stepCost) in GetNextStates(state, RoomPos, HallStops))
            {
                if (next == null) continue;
                int newCost = cost + stepCost;
                int hash = next.GetHashCode();
                if (!best.TryGetValue(hash, out int oldCost) || newCost < oldCost)
                {
                    best[hash] = newCost;
                    pq.Enqueue(next, newCost);
                }
            }
        }
        return 0;
    }

    static IEnumerable<(State, int)> GetNextStates(State s, int[] RoomPos, int[] HallStops)
    {
        if (s == null || s.Hallway == null || s.Rooms == null)
            yield break;

        var hallway = s.Hallway.ToCharArray();

        for (int i = 0; i < hallway.Length; i++)
        {
            char c = hallway[i];
            if (c == '.') continue;
            if (c < 'A' || c > 'D') continue;
            var a = new Amphipod((AmphipodType)(c - 'A'));
            int rIndex = a.TargetRoomIndex;
            if (rIndex < 0 || rIndex >= s.Rooms.Length) continue;
            var room = s.Rooms[rIndex];
            if (!room.CanAccept(a)) continue;
            if (!PathIsClear(hallway, i, RoomPos[rIndex])) continue;
            int steps = Math.Abs(i - RoomPos[rIndex]) + (room.Depth - room.Slots.Count);
            int moveCost = steps * a.CostPerStep;
            var ns = s.Copy();
            var hallArr = ns.Hallway.ToCharArray();
            if (i >= 0 && i < hallArr.Length) hallArr[i] = '.';
            ns.Hallway = new string(hallArr);
            ns.Rooms[rIndex].Push(a);
            yield return (ns, moveCost);
        }

        for (int r = 0; r < 4; r++)
        {
            if (r >= s.Rooms.Length) continue;
            var room = s.Rooms[r];
            if (room.IsEmpty) continue;
            if (room.Slots.All(a => (int)a.Type == r)) continue;
            var pod = room.Peek();
            if (pod == null) continue;
            int roomPos = RoomPos[r];
            int exitDepth = room.Depth - room.Slots.Count + 1;
            foreach (int pos in HallStops)
            {
                if (!PathIsClear(hallway, roomPos, pos)) continue;
                int steps = exitDepth + Math.Abs(roomPos - pos);
                int moveCost = steps * pod.CostPerStep;
                var ns = s.Copy();
                ns.Rooms[r].Pop();
                var hallArr = ns.Hallway.ToCharArray();
                if (pos >= 0 && pos < hallArr.Length)
                    hallArr[pos] = pod.ToString()[0];
                ns.Hallway = new string(hallArr);
                yield return (ns, moveCost);
            }
        }
    }

    static bool PathIsClear(char[] hall, int a, int b)
    {
        if (hall == null || hall.Length == 0) return false;
        int min = Math.Min(a, b);
        int max = Math.Max(a, b);
        for (int i = min + 1; i < max && i < hall.Length; i++)
            if (hall[i] != '.') return false;
        return true;
    }

    static Room[] ParseRooms(List<string> lines, int depth)
    {
        var rooms = new Room[4];
        for (int i = 0; i < 4; i++)
            rooms[i] = new Room(i, depth);

        var roomLines = lines
            .Where(x => !string.IsNullOrWhiteSpace(x) && x.Contains('#') && x.Any(char.IsLetter))
            .ToList();

        if (roomLines.Count == 0) return rooms;

        roomLines.Reverse();

        foreach (var line in roomLines)
        {
            var pods = line.Where(ch => "ABCD".Contains(ch)).ToArray();
            for (int i = 0; i < pods.Length && i < 4; i++)
                rooms[i].Push(new Amphipod((AmphipodType)(pods[i] - 'A')));
        }
        return rooms;
    }

    static void Main()
    {
        try
        {
            var lines = new List<string>();
            string line;
            while ((line = Console.ReadLine()) != null)
                lines.Add(line);

            if (lines.Count == 0)
            {
                Console.WriteLine(0);
                return;
            }

            int result = Solve(lines);
            Console.WriteLine(result);
        }
        catch
        {
            Console.WriteLine(0);
        }
    }
}
