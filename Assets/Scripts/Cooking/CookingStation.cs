using UnityEngine;
using ProjectCook.Interaction;

namespace ProjectCook.Cooking
{
    /// Abstract Base Class สำหรับสถานีทำอาหารทุกประเภทในเกม (กระทะ, หม้อ, เตาอบ ฯลฯ)
    public abstract class CookingStation : MonoBehaviour, IInteractable
    {
        public abstract void Interact(PlayerInteractor interactor);
    }
}
