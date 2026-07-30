using UnityEngine;
using ProjectCook.Interaction;

namespace ProjectCook.Cooking
{
    public class PanStation : CookingStation
    {
        public override void Interact(PlayerInteractor interactor)
        {
            Debug.Log("pan interacting");
        }
    }
}
