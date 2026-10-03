using TMPro;
using UnityEngine;
using ProjectCook.Inventory;

namespace ProjectCook.UI
{
    /// <summary>แสดงเงินของผู้เล่น อัปเดตทุกครั้งที่ยอดเปลี่ยน</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class MoneyLabel : MonoBehaviour
    {
        [Tooltip("{0} = จำนวนเงิน")]
        [SerializeField] private string format = "MONEY ${0}";

        private TMP_Text text;

        // แคชไว้ตอน subscribe เพราะตอนหยุดเกม Instance อาจคืน null ก่อน OnDestroy
        private PlayerWallet wallet;

        private void Start()
        {
            text = GetComponent<TMP_Text>();
            wallet = PlayerWallet.Instance;
            if (wallet == null) return;

            wallet.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (wallet != null) wallet.Changed -= Refresh;
        }

        private void Refresh() => text.text = string.Format(format, wallet.Money);
    }
}
