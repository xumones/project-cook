namespace ProjectCook.Cooking
{
    /// <summary>
    /// สถานีทำอาหารประเภทกระทะ (Pan Cooking Station)
    ///
    /// การเปิด/ปิดโมดูล (PanController) ถูกจัดการโดย CookingStation ให้อัตโนมัติแล้ว
    /// (IngredientDragController แยกเป็นระบบ Global บน Player ไม่ผูกกับสถานีนี้อีกต่อไป)
    /// คลาสนี้จึงเหลือไว้เพื่อระบุชนิดสถานีและรองรับพฤติกรรมเฉพาะของกระทะในอนาคต
    /// </summary>
    public class PanStation : CookingStation
    {
    }
}
