using UnityEngine;
using ProjectCook.Interaction;

namespace ProjectCook.Cooking
{
    /// <summary>
    /// สถานีทำอาหารประเภทกระทะ (Pan Cooking Station)
    /// </summary>
    public class PanStation : CookingStation
    {
        [Header("Pan Control Settings")]
        [Tooltip("PanController สำหรับควบคุมการเคลื่อนที่ของกระทะ")]
        [SerializeField] private PanController panController;

        private void Awake()
        {
            if (panController == null)
            {
                panController = GetComponentInChildren<PanController>();
            }
        }

        public override void Interact(PlayerInteractor interactor)
        {
            Debug.Log("[Change to PanStation mode]");
            EnterStation(interactor);
        }

        public override void EnterStation(PlayerInteractor interactor)
        {
            base.EnterStation(interactor);
            if (panController != null)
            {
                Transform camTrans = stationCamera != null ? stationCamera.transform : (Camera.main != null ? Camera.main.transform : null);
                panController.SetReferenceTransform(camTrans);
                panController.SetControllerActive(true);
            }
        }

        public override void ExitStation()
        {
            if (panController != null)
            {
                panController.SetControllerActive(false);
            }
            base.ExitStation();
        }
    }
}
