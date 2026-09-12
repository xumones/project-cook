namespace ProjectCook.Dialogue
{
    /// <summary>แทนที่ placeholder อย่าง {quest_offer} ในข้อความบทสนทนาด้วยคำจากข้อมูลจริง ให้ JSON ใช้ซ้ำข้าม NPC ได้</summary>
    public interface IDialogueTextProvider
    {
        string ResolveText(string text);
    }
}
