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

        [Tooltip("IngredientDragController สำหรับควบคุมการลากเคลื่อนย้ายวัตถุดิบอาหาร")]
        [SerializeField] private IngredientDragController dragController;

        private void Awake()
        {
            if (panController == null)
            {
                panController = GetComponentInChildren<PanController>();
            }

            if (dragController == null)
            {
                dragController = GetComponentInChildren<IngredientDragController>();
                if (dragController == null)
                {
                    dragController = Object.FindFirstObjectByType<IngredientDragController>();
                }
            }
        }

        public override void Interact(PlayerInteractor interactor)
        {
            EnterStation(interactor);
        }

        public override void EnterStation(PlayerInteractor interactor)
        {
            base.EnterStation(interactor);
            Camera cam = stationCamera != null ? stationCamera.GetComponent<Camera>() : Camera.main;

            if (panController != null)
            {
                Transform camTrans = cam != null ? cam.transform : null;
                panController.SetReferenceTransform(camTrans);
                panController.SetControllerActive(true);
            }

            if (dragController != null)
            {
                dragController.SetTargetCamera(cam);
                dragController.SetControllerActive(true);
            }
        }

        public override void ExitStation()
        {
            if (panController != null)
            {
                panController.SetControllerActive(false);
            }

            if (dragController != null)
            {
                dragController.SetControllerActive(false);
            }

            base.ExitStation();
        }
    }
}
