using System.Collections.Generic;
using UnityEngine;
using ProjectCook.Items;

namespace ProjectCook.Inventory
{
    /// <summary>ข้อมูลประจำแบบของกระเป๋า: มีช่องย่อยกี่ช่อง ขนาดเท่าไหร่ และเรียงกันอย่างไร</summary>
    [CreateAssetMenu(fileName = "BagDefinition_", menuName = "ProjectCook/Items/Bag Definition")]
    public class BagDefinitionSO : ItemDefinitionSO
    {
        [Header("Bag")]
        [SerializeField] private CompartmentLayout[] compartments =
        {
            new CompartmentLayout { size = new Vector2Int(4, 1) }
        };

        public IReadOnlyList<CompartmentLayout> Compartments => compartments;
    }
}
