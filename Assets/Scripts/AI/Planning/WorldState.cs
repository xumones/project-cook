using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.AI
{
    /// <summary>สภาพโลกที่ตัววางแผนเห็น: ข้อเท็จจริงสูงสุด 64 ข้อ ในเงื่อนไขและผล ข้อที่ไม่ได้ระบุถือว่า "ไม่สน" ในสภาพจริง ข้อที่ไม่ได้ระบุถือว่าเท็จ</summary>
    // ใช้บิตแทนรายการ เทียบหรือรวมสภาพได้ในคำสั่งเดียว ตัววางแผนจึงคัดลอกสภาพได้ไม่จำกัดโดยไม่สร้างขยะ
    public readonly struct WorldState : IEquatable<WorldState>
    {
        public const int MaxFacts = 64;

        private readonly ulong values;
        private readonly ulong known;

        private WorldState(ulong values, ulong known)
        {
            this.values = values & known;
            this.known = known;
        }

        public WorldState With(int index, bool value)
        {
            ulong bit = 1UL << index;
            return new WorldState(value ? values | bit : values & ~bit, known | bit);
        }

        /// <summary>ข้อที่ conditions ระบุตรงกับสภาพนี้ทุกข้อ</summary>
        public bool Satisfies(WorldState conditions) => ((values ^ conditions.values) & conditions.known) == 0;

        /// <summary>สภาพหลังผลของการกระทำ: ข้อที่ effects ระบุถูกเขียนทับ ข้ออื่นคงเดิม</summary>
        public WorldState Apply(WorldState effects) =>
            new WorldState((values & ~effects.known) | effects.values, known | effects.known);

        public bool Equals(WorldState other) => values == other.values && known == other.known;

        public override bool Equals(object obj) => obj is WorldState other && Equals(other);

        public override int GetHashCode() => (values, known).GetHashCode();
    }

    /// <summary>จับคู่ข้อเท็จจริงกับบิตใน WorldState ของตัวละครหนึ่งตัว สร้างจากข้อเท็จจริงทุกข้อที่เป้าหมายและการกระทำใช้</summary>
    public class FactTable
    {
        private readonly Dictionary<FactSO, int> indices = new Dictionary<FactSO, int>();

        /// <summary>บิตของข้อเท็จจริงนี้ ข้อใหม่ได้บิตถัดไป -1 = เต็ม 64 ข้อแล้ว</summary>
        public int IndexOf(FactSO fact)
        {
            if (indices.TryGetValue(fact, out int index)) return index;
            if (indices.Count >= WorldState.MaxFacts)
            {
                Debug.LogError($"[FactTable] ข้อเท็จจริงเกิน {WorldState.MaxFacts} ข้อ {fact.name} จะถูกมองข้าม");
                return -1;
            }

            index = indices.Count;
            indices[fact] = index;
            return index;
        }

        public WorldState Build(IEnumerable<FactCondition> conditions)
        {
            var state = new WorldState();
            foreach (FactCondition condition in conditions)
            {
                if (condition.fact == null) continue;

                int index = IndexOf(condition.fact);
                if (index >= 0) state = state.With(index, condition.value);
            }
            return state;
        }
    }
}
