using System;
using UnityEngine;
using ProjectCook.Core;

namespace ProjectCook.Inventory
{
    /// <summary>เงินของผู้เล่น</summary>
    public class PlayerWallet : SceneSingleton<PlayerWallet>, ISaveSection
    {
        [Serializable]
        private class SaveData
        {
            public int money;
        }

        [Min(0)]
        [SerializeField] private int startingMoney = 25;

        public int Money { get; private set; }

        public event Action Changed;

        protected override void Awake()
        {
            base.Awake();
            Money = startingMoney;
        }

        private void Start() => SaveService.Instance?.Register(this);

        protected override void OnDestroy()
        {
            SaveService.Instance?.Unregister(this);
            base.OnDestroy();
        }

        public bool CanAfford(int amount) => Money >= amount;

        public void Earn(int amount)
        {
            if (amount <= 0) return;

            Money += amount;
            Changed?.Invoke();
        }

        /// <summary>หักเงิน ถ้าเงินไม่พอคืน false และไม่หัก</summary>
        public bool TrySpend(int amount)
        {
            if (!CanAfford(amount)) return false;

            Money -= amount;
            Changed?.Invoke();
            return true;
        }

        public string SectionKey => "wallet";

        public string Capture() => JsonUtility.ToJson(new SaveData { money = Money });

        public void Restore(string data)
        {
            Money = JsonUtility.FromJson<SaveData>(data).money;
            Changed?.Invoke();
        }
    }
}
