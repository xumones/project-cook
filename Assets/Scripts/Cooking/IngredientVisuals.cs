using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>
    /// สคริปต์ย่อยจัดการการแสดงผลทางสายตาของวัตถุดิบ (Material Property Block, Color & Shader Progress Blending)
    /// </summary>
    [DisallowMultipleComponent]
    public class IngredientVisuals : MonoBehaviour
    {
        private Renderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int CookedMapId = Shader.PropertyToID("_CookedMap");
        private static readonly int SideAProgressId = Shader.PropertyToID("_SideAProgress");
        private static readonly int SideBProgressId = Shader.PropertyToID("_SideBProgress");
        private static readonly int BurntColorId = Shader.PropertyToID("_BurntColor");
        private static readonly int TintIntensityId = Shader.PropertyToID("_TintIntensity");
        private int customColorPropertyId;

        private Color lastAppliedColor;
        private float lastAppliedSideAProgress = -1f;
        private float lastAppliedSideBProgress = -1f;
        private bool isColorInitialized = false;

        public void Init(Renderer renderer)
        {
            meshRenderer = renderer;
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<Renderer>();
                if (meshRenderer == null)
                {
                    meshRenderer = GetComponentInChildren<Renderer>();
                }
            }

            propertyBlock = new MaterialPropertyBlock();
        }

        public void UpdateColorPropertyId(IngredientDataSO data)
        {
            if (data != null && !string.IsNullOrEmpty(data.ColorPropertyName))
            {
                customColorPropertyId = Shader.PropertyToID(data.ColorPropertyName);
            }
            else
            {
                customColorPropertyId = ColorId;
            }
        }

        public void ApplyInitialMaterial(IngredientDataSO data)
        {
            if (meshRenderer != null && data != null && data.RawMaterial != null)
            {
                meshRenderer.sharedMaterial = data.RawMaterial;
            }
        }

        public void ResetAppliedStates()
        {
            isColorInitialized = false;
            lastAppliedSideAProgress = -1f;
            lastAppliedSideBProgress = -1f;
        }

        /// <summary>
        /// สลับ Material ตามสภาวะความสุก (Raw -> Cooked -> Burnt)
        /// </summary>
        public void UpdateMaterialForState(CookingState state, IngredientDataSO data)
        {
            if (meshRenderer == null || data == null) return;

            Material targetMat = null;

            if (data.IsTwoSidedCooking)
            {
                if (state == CookingState.Burnt && data.BurntMaterial != null)
                {
                    targetMat = data.BurntMaterial;
                }
                else if (data.RawMaterial != null)
                {
                    targetMat = data.RawMaterial;
                }
            }
            else
            {
                if (state == CookingState.Cooked && data.CookedMaterial != null)
                {
                    targetMat = data.CookedMaterial;
                }
                else if (state == CookingState.Burnt && data.BurntMaterial != null)
                {
                    targetMat = data.BurntMaterial;
                }
                else if (state == CookingState.Raw && data.RawMaterial != null)
                {
                    targetMat = data.RawMaterial;
                }
            }

            if (targetMat != null && meshRenderer.sharedMaterial != targetMat)
            {
                meshRenderer.sharedMaterial = targetMat;
                ResetAppliedStates();
            }
        }

        /// <summary>
        /// อัปเดตสีและ Texture ของ Material แบบ Smooth Transition ด้วย MaterialPropertyBlock
        /// </summary>
        public void ApplyVisuals(IngredientDataSO data, float sideACookTime, float sideBCookTime, float omniCookTime)
        {
            if (meshRenderer == null || data == null) return;

            Color targetColor = CalculateCurrentColor(data, sideACookTime, sideBCookTime, omniCookTime);

            float normSideA = 0f;
            float normSideB = 0f;
            if (data.IsTwoSidedCooking)
            {
                normSideA = data.CookTime > 0 ? (sideACookTime / data.CookTime) : 0f;
                normSideB = data.CookTime > 0 ? (sideBCookTime / data.CookTime) : 0f;
            }

            // Smart Dirty Check: ป้องกันการส่งค่าเข้า GPU ซ้ำ เมื่อค่าไม่มีการเปลี่ยนแปลงที่มีนัยสำคัญทางสายตา
            if (isColorInitialized)
            {
                float colorDiffSqr = (targetColor.r - lastAppliedColor.r) * (targetColor.r - lastAppliedColor.r) +
                                     (targetColor.g - lastAppliedColor.g) * (targetColor.g - lastAppliedColor.g) +
                                     (targetColor.b - lastAppliedColor.b) * (targetColor.b - lastAppliedColor.b) +
                                     (targetColor.a - lastAppliedColor.a) * (targetColor.a - lastAppliedColor.a);

                bool isColorDirty = colorDiffSqr >= 0.000025f;
                bool isSideADirty = data.IsTwoSidedCooking && Mathf.Abs(normSideA - lastAppliedSideAProgress) >= 0.005f;
                bool isSideBDirty = data.IsTwoSidedCooking && Mathf.Abs(normSideB - lastAppliedSideBProgress) >= 0.005f;

                if (!isColorDirty && !isSideADirty && !isSideBDirty)
                {
                    return;
                }
            }

            lastAppliedColor = targetColor;
            lastAppliedSideAProgress = normSideA;
            lastAppliedSideBProgress = normSideB;
            isColorInitialized = true;

            if (data.IsTwoSidedCooking)
            {
                propertyBlock.SetColor(BaseColorId, Color.white);
                propertyBlock.SetColor(ColorId, Color.white);

                if (data.CookedMaterial != null && data.CookedMaterial.mainTexture != null)
                {
                    propertyBlock.SetTexture(CookedMapId, data.CookedMaterial.mainTexture);
                }

                propertyBlock.SetFloat(SideAProgressId, normSideA);
                propertyBlock.SetFloat(SideBProgressId, normSideB);
                propertyBlock.SetColor(BurntColorId, data.BurntColor);
                propertyBlock.SetFloat(TintIntensityId, data.TintIntensity);
            }
            else
            {
                propertyBlock.SetColor(BaseColorId, targetColor);
                propertyBlock.SetColor(ColorId, targetColor);
                if (customColorPropertyId != 0 && customColorPropertyId != BaseColorId && customColorPropertyId != ColorId)
                {
                    propertyBlock.SetColor(customColorPropertyId, targetColor);
                }
            }

            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private Color CalculateCurrentColor(IngredientDataSO data, float sideACookTime, float sideBCookTime, float omniCookTime)
        {
            float effectiveTime = data.IsTwoSidedCooking ? (sideACookTime + sideBCookTime) * 0.5f : omniCookTime;

            if (effectiveTime <= data.CookTime)
            {
                return Color.white;
            }

            float burnProgress = data.BurnTime > 0 ? Mathf.Clamp01((effectiveTime - data.CookTime) / data.BurnTime) : 1f;
            float intensity = data != null ? Mathf.Clamp01(data.TintIntensity) : 0.5f;
            Color targetBurntColor = Color.Lerp(Color.white, data.BurntColor, burnProgress);

            Color finalColor = Color.Lerp(Color.white, targetBurntColor, intensity);
            finalColor.a = 1f;
            return finalColor;
        }
    }
}
