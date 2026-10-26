using System.Collections.Generic;

namespace ProjectCook.AI
{
    /// <summary>การกระทำในรูปที่ตัววางแผนใช้ ผู้เรียกคิดราคาและกรองการกระทำที่ทำไม่ได้ในตอนนี้มาก่อนแล้ว</summary>
    public readonly struct PlannerAction
    {
        public readonly WorldState Preconditions;
        public readonly WorldState Effects;
        public readonly float Cost;

        public PlannerAction(WorldState preconditions, WorldState effects, float cost)
        {
            Preconditions = preconditions;
            Effects = effects;
            Cost = cost;
        }
    }

    /// <summary>หาลำดับการกระทำที่ราคารวมถูกที่สุดจากสภาพตอนนี้ไปถึงเป้าหมาย (แบบ GOAP ของ F.E.A.R.)</summary>
    // ค้นหาไปข้างหน้าแบบราคาน้อยสุดก่อน (A* ที่ heuristic = 0) แผนยาวไม่เกินไม่กี่ขั้นและการกระทำมีไม่กี่สิบ จึงเร็วพอโดยไม่ต้องเดา
    public static class GoapPlanner
    {
        public const int DefaultMaxDepth = 4;

        private struct Node
        {
            public WorldState State;
            public float Cost;
            public int Depth;
            public int Parent;
            public int Action;
        }

        /// <summary>plan ได้ลำดับ index ของ actions false = ไปไม่ถึงภายใน maxDepth ขั้น สภาพที่ถึงเป้าแล้วได้แผนว่าง</summary>
        public static bool TryPlan(WorldState start, WorldState goal, IReadOnlyList<PlannerAction> actions, List<int> plan, int maxDepth = DefaultMaxDepth)
        {
            plan.Clear();
            var nodes = new List<Node> { new Node { State = start, Parent = -1, Action = -1 } };
            var open = new List<int> { 0 };
            var bestCost = new Dictionary<WorldState, float> { [start] = 0f };

            while (open.Count > 0)
            {
                int current = PopCheapest(open, nodes);
                Node node = nodes[current];
                if (node.State.Satisfies(goal))
                {
                    Trace(nodes, current, plan);
                    return true;
                }
                if (node.Depth >= maxDepth) continue;

                for (int i = 0; i < actions.Count; i++)
                {
                    PlannerAction action = actions[i];
                    if (!node.State.Satisfies(action.Preconditions)) continue;

                    WorldState next = node.State.Apply(action.Effects);
                    float cost = node.Cost + action.Cost;
                    if (bestCost.TryGetValue(next, out float known) && known <= cost) continue;

                    bestCost[next] = cost;
                    nodes.Add(new Node { State = next, Cost = cost, Depth = node.Depth + 1, Parent = current, Action = i });
                    open.Add(nodes.Count - 1);
                }
            }
            return false;
        }

        // ราคาเท่ากันเลือกตัวที่เข้ามาก่อน ผลจึงเหมือนเดิมทุกครั้ง
        private static int PopCheapest(List<int> open, List<Node> nodes)
        {
            int best = 0;
            for (int i = 1; i < open.Count; i++)
            {
                if (nodes[open[i]].Cost < nodes[open[best]].Cost) best = i;
            }

            int index = open[best];
            open.RemoveAt(best);
            return index;
        }

        private static void Trace(List<Node> nodes, int last, List<int> plan)
        {
            for (int i = last; nodes[i].Parent >= 0; i = nodes[i].Parent)
            {
                plan.Add(nodes[i].Action);
            }
            plan.Reverse();
        }
    }
}
