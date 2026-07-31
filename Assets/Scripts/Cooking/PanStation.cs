using UnityEngine;
using ProjectCook.Interaction;

namespace ProjectCook.Cooking
{
    /// <summary>
    /// สถานีทำอาหารประเภทกระทะ (Pan Cooking Station)
    /// </summary>
    public class PanStation : CookingStation
    {
        public override void Interact(PlayerInteractor interactor)
        {
            Debug.Log("[Change to PanStation mode]");
            EnterStation(interactor);
        }
    }
}
