using System.Collections.Generic;
using UnityEngine;

namespace ProjectCook.Dialogue
{
    /// <summary>
    /// ScriptableObject สำหรับจัดเก็บชุดบทสนทนาทั้งหมด (Dialogue Tree Container)
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogueData", menuName = "ProjectCook/Dialogue/Dialogue Data")]
    public class DialogueDataSO : ScriptableObject
    {
        [Header("Dialogue Information")]
        [Tooltip("ID ประจำชุดบทสนทนานี้")]
        public string dialogID;

        [Tooltip("ชื่อผู้พูดหลักของชุดบทสนทนานี้")]
        public string defaultSpeakerName;

        [Tooltip("ID ของโหนดเริ่มต้น (ปกติคือ 'start')")]
        public string startNodeID = "start";

        [Header("Nodes Collection")]
        [Tooltip("รายการโหนดบทสนทนาทั้งหมดใน Tree นี้")]
        public List<DialogueNode> nodes = new List<DialogueNode>();

        private Dictionary<string, DialogueNode> nodeCache;

        private void OnEnable()
        {
            BuildNodeCache();
        }

        /// <summary>
        /// สร้าง Dictionary แคชชิ่งเพื่อเร่งความเร็วการค้นหาโหนดและตรวจสอบ ID ซ้ำ
        /// </summary>
        public void BuildNodeCache()
        {
            if (nodeCache == null)
            {
                nodeCache = new Dictionary<string, DialogueNode>();
            }
            else
            {
                nodeCache.Clear();
            }

            if (nodes == null) return;

            foreach (var node in nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.nodeID)) continue;

                if (!nodeCache.ContainsKey(node.nodeID))
                {
                    nodeCache.Add(node.nodeID, node);
                }
                else
                {
                    Debug.LogWarning($"[DialogueDataSO] พบ nodeID ซ้ำกัน: '{node.nodeID}' ใน DialogueDataSO: '{dialogID}'", this);
                }
            }
        }

        /// <summary>
        /// ค้นหาโหนดตาม nodeID
        /// </summary>
        public DialogueNode GetNode(string nodeID)
        {
            if (string.IsNullOrEmpty(nodeID)) return null;

            if (nodeCache == null || nodeCache.Count != nodes.Count)
            {
                BuildNodeCache();
            }

            if (nodeCache.TryGetValue(nodeID, out DialogueNode cachedNode))
            {
                return cachedNode;
            }

            return null;
        }

        /// <summary>
        /// ดึงโหนดเริ่มต้น
        /// </summary>
        public DialogueNode GetStartNode()
        {
            DialogueNode node = GetNode(startNodeID);
            if (node == null && nodes.Count > 0)
            {
                node = nodes[0]; // fallback ใช้โหนดแรกสุดถ้าหา startNodeID ไม่เจอ
            }
            return node;
        }

        /// <summary>
        /// เติมข้อมูลลงใน ScriptableObject นี้โดยอ่านจาก JSON String
        /// </summary>
        public bool PopulateFromJson(string jsonText)
        {
            if (string.IsNullOrEmpty(jsonText)) return false;

            try
            {
                DialogueDataDTO dto = JsonUtility.FromJson<DialogueDataDTO>(jsonText);
                if (dto != null)
                {
                    this.dialogID = dto.dialogID;
                    this.defaultSpeakerName = dto.defaultSpeakerName;
                    this.startNodeID = dto.startNodeID;
                    this.nodes = dto.nodes != null ? dto.nodes : new List<DialogueNode>();
                    BuildNodeCache();
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DialogueDataSO] เกิดข้อผิดพลาดในการ Parse JSON: {ex.Message}", this);
            }
            return false;
        }
    }

    /// <summary>
    /// Data Transfer Object สำหรับช่วยให้ Unity JsonUtility สามารถ Deserialize JSON ข้อความมาใส่ใน DialogueDataSO ได้ตรงๆ
    /// </summary>
    [System.Serializable]
    public class DialogueDataDTO
    {
        public string dialogID;
        public string defaultSpeakerName;
        public string startNodeID = "start";
        public List<DialogueNode> nodes = new List<DialogueNode>();
    }
}
