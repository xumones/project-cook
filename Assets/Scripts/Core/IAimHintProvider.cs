namespace ProjectCook.Core
{
    /// <summary>สิ่งที่ผู้เล่นทำได้กับเป้าที่เล็งอยู่ UI เป็นคนแปลงเป็นไอคอนเอง</summary>
    public enum AimHint
    {
        None,
        Talk,
        Use,
        PickUp
    }

    /// <summary>เป้าที่บอกได้ว่าตอนนี้เล็งแล้วจะทำอะไรได้ คืนค่าตามสถานะปัจจุบัน</summary>
    public interface IAimHintProvider
    {
        AimHint GetAimHint();
    }
}
