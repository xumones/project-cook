using UnityEngine;

namespace ProjectCook.Cooking
{
    /// <summary>แสดงผลความสุกของวัตถุดิบลงทุก Renderer ในตัว (Material Property Block, Color & Shader Progress Blending)</summary>
    [DisallowMultipleComponent]
    public class IngredientVisuals : MonoBehaviour
    {
        // ทุกชิ้นของวัตถุดิบเดียวกันใช้ material และค่าเดียวกันเสมอ จึงใช้ block เดียวส่งให้ทุกตัว
        private Renderer[] renderers;
        private MaterialPropertyBlock propertyBlock;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int CookedMapId = Shader.PropertyToID("_CookedMap");
        private static readonly int SideAProgressId = Shader.PropertyToID("_SideAProgress");
        private static readonly int SideBProgressId = Shader.PropertyToID("_SideBProgress");
        private static readonly int BurntColorId = Shader.PropertyToID("_BurntColor");
        private static readonly int TintIntensityId = Shader.PropertyToID("_TintIntensity");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private int customColorPropertyId;

        private Color lastAppliedColor;
        private float lastAppliedSideAProgress = -1f;
        private float lastAppliedSideBProgress = -1f;
        private float lastAppliedSmoothness = -1f;
        private bool isColorInitialized = false;

        private Material cachedCookingMat;
        private Texture cachedRawTex;
        private Texture cachedCookedTex;
        private float cachedBaseSmoothness = -1f;
        private bool isMaterialCached = false;

        public void Init()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            propertyBlock = new MaterialPropertyBlock();
        }

        private bool HasRenderers => renderers != null && renderers.Length > 0;

        private void SetSharedMaterial(Material material)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = material;
            }
        }

        /// <summary>ตั้งค่าการแสดงผลใหม่ทั้งชุดตามข้อมูลวัตถุดิบที่ได้รับ</summary>
        public void Configure(IngredientDataSO data, float sideACookTime, float sideBCookTime, float omniCookTime)
        {
            UpdateColorPropertyId(data);
            ApplyInitialMaterial(data);
            ResetAppliedStates();
            ApplyVisuals(data, sideACookTime, sideBCookTime, omniCookTime);
        }

        private void UpdateColorPropertyId(IngredientDataSO data)
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

        private void ApplyInitialMaterial(IngredientDataSO data)
        {
            if (HasRenderers && data != null && data.CookingMaterial != null)
            {
                SetSharedMaterial(data.CookingMaterial);
            }
        }

        private void ResetAppliedStates()
        {
            isColorInitialized = false;
            isMaterialCached = false;
            lastAppliedSideAProgress = -1f;
            lastAppliedSideBProgress = -1f;
            lastAppliedSmoothness = -1f;
        }

        private void CacheMaterialProperties(Material mat, IngredientDataSO data)
        {
            cachedCookingMat = mat;
            cachedRawTex = null;
            cachedCookedTex = null;
            cachedBaseSmoothness = data != null ? data.RawSmoothness : 0.5f;

            if (mat != null)
            {
                if (mat.HasProperty(BaseMapId))
                {
                    cachedRawTex = mat.GetTexture(BaseMapId);
                }
                else if (mat.HasProperty(MainTexId))
                {
                    cachedRawTex = mat.mainTexture;
                }

                if (mat.HasProperty(CookedMapId))
                {
                    cachedCookedTex = mat.GetTexture(CookedMapId);
                }

                if (mat.HasProperty(SmoothnessId))
                {
                    cachedBaseSmoothness = mat.GetFloat(SmoothnessId);
                }
            }
            isMaterialCached = true;
        }

        /// <summary>ส่ง Texture ที่แคชไว้เข้า MaterialPropertyBlock เพียงครั้งเดียวตอนเปลี่ยน Material</summary>
        private void ApplyCachedTexturesToBlock()
        {
            if (propertyBlock == null) return;

            if (cachedRawTex != null)
            {
                propertyBlock.SetTexture(BaseMapId, cachedRawTex);
                propertyBlock.SetTexture(MainTexId, cachedRawTex);
            }

            if (cachedCookedTex != null)
            {
                propertyBlock.SetTexture(CookedMapId, cachedCookedTex);
            }
        }

        /// <summary>สลับ Material ตามสภาวะความสุก (Raw -> Cooked -> Burnt)</summary>
        public void UpdateMaterialForState(CookingState state, IngredientDataSO data)
        {
            if (!HasRenderers || data == null) return;

            // ดิบ→สุกให้ Shader ของ CookingMaterial ไล่ Texture เอง สลับ Material เฉพาะตอนไหม้
            Material targetMat = state == CookingState.Burnt && data.BurntMaterial != null
                ? data.BurntMaterial
                : data.CookingMaterial;

            if (targetMat != null && renderers[0].sharedMaterial != targetMat)
            {
                SetSharedMaterial(targetMat);
                ResetAppliedStates();
            }
        }

        /// <summary>อัปเดตสีและ Texture ของ Material แบบ Smooth Transition ด้วย MaterialPropertyBlock</summary>
        public void ApplyVisuals(IngredientDataSO data, float sideACookTime, float sideBCookTime, float omniCookTime)
        {
            if (!HasRenderers || data == null) return;

            Color targetColor = CalculateCurrentColor(data, sideACookTime, sideBCookTime, omniCookTime);

            float normSideA = 0f;
            float normSideB = 0f;
            if (data.IsTwoSidedCooking)
            {
                normSideA = data.CookTime > 0 ? (sideACookTime / data.CookTime) : 0f;
                normSideB = data.CookTime > 0 ? (sideBCookTime / data.CookTime) : 0f;
            }
            else
            {
                float normOmni = data.CookTime > 0 ? (omniCookTime / data.CookTime) : 0f;
                normSideA = normOmni;
                normSideB = normOmni;
            }

            Material targetMat = data.CookingMaterial != null ? data.CookingMaterial : renderers[0].sharedMaterial;

            if (!isMaterialCached || cachedCookingMat != targetMat)
            {
                CacheMaterialProperties(targetMat, data);

                // Texture ไม่เปลี่ยนระหว่างทอด จึงส่งเข้า PropertyBlock เพียงครั้งเดียวตอนเปลี่ยน Material
                ApplyCachedTexturesToBlock();

                // บังคับให้รอบนี้ส่งค่าเข้า GPU เสมอ เพราะเพิ่งเปลี่ยน Material ใหม่
                isColorInitialized = false;
            }

            float baseRawSmoothness = cachedBaseSmoothness;
            float effectiveTime = data.IsTwoSidedCooking ? (sideACookTime + sideBCookTime) * 0.5f : omniCookTime;
            float currentSmoothness = baseRawSmoothness;

            if (effectiveTime <= data.CookTime)
            {
                float cookRatio = data.CookTime > 0 ? Mathf.Clamp01(effectiveTime / data.CookTime) : 1f;
                currentSmoothness = Mathf.Lerp(baseRawSmoothness, data.CookedSmoothness, cookRatio);
            }
            else
            {
                float burnRatio = data.BurnTime > 0 ? Mathf.Clamp01((effectiveTime - data.CookTime) / data.BurnTime) : 1f;
                currentSmoothness = Mathf.Lerp(data.CookedSmoothness, data.BurntSmoothness, burnRatio);
            }

            // Smart Dirty Check: ป้องกันการส่งค่าเข้า GPU ซ้ำ เมื่อค่าไม่มีการเปลี่ยนแปลงที่มีนัยสำคัญทางสายตา
            if (isColorInitialized)
            {
                float colorDiffSqr = (targetColor.r - lastAppliedColor.r) * (targetColor.r - lastAppliedColor.r) +
                                     (targetColor.g - lastAppliedColor.g) * (targetColor.g - lastAppliedColor.g) +
                                     (targetColor.b - lastAppliedColor.b) * (targetColor.b - lastAppliedColor.b) +
                                     (targetColor.a - lastAppliedColor.a) * (targetColor.a - lastAppliedColor.a);

                bool isColorDirty = colorDiffSqr >= 0.000025f;
                bool isSideADirty = Mathf.Abs(normSideA - lastAppliedSideAProgress) >= 0.005f;
                bool isSideBDirty = Mathf.Abs(normSideB - lastAppliedSideBProgress) >= 0.005f;
                bool isSmoothnessDirty = Mathf.Abs(currentSmoothness - lastAppliedSmoothness) >= 0.005f;

                if (!isColorDirty && !isSideADirty && !isSideBDirty && !isSmoothnessDirty)
                {
                    return;
                }
            }

            lastAppliedColor = targetColor;
            lastAppliedSideAProgress = normSideA;
            lastAppliedSideBProgress = normSideB;
            lastAppliedSmoothness = currentSmoothness;
            isColorInitialized = true;

            propertyBlock.SetColor(BaseColorId, targetColor);
            propertyBlock.SetColor(ColorId, targetColor);
            if (customColorPropertyId != 0 && customColorPropertyId != BaseColorId && customColorPropertyId != ColorId)
            {
                propertyBlock.SetColor(customColorPropertyId, targetColor);
            }

            propertyBlock.SetFloat(SideAProgressId, normSideA);
            propertyBlock.SetFloat(SideBProgressId, normSideB);
            propertyBlock.SetColor(BurntColorId, data.BurntColor);
            propertyBlock.SetFloat(TintIntensityId, data.TintIntensity);

            propertyBlock.SetFloat(SmoothnessId, currentSmoothness);

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].SetPropertyBlock(propertyBlock);
            }
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
