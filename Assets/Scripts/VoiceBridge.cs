using UnityEngine;
using Meta.WitAi;
using Meta.WitAi.Json;

public class VoiceBridge : MonoBehaviour
{
    [Tooltip("把挂载 DetectGestures 的物体拖进来")]
    [SerializeField] private DetectGestures detectGestures;

    // 这个函数会被 Wit.ai 的 OnResponse 事件调用
    public void HandleWitResponse(WitResponseNode response)
    {
        if (response == null) return;

        // 1. 获取所有实体 (entities)
        var entities = response["entities"];
        if (entities == null) return;

        // 🔴 调试：打印原始数据，方便你看 Wit 到底回了什么
        Debug.Log($"[VoiceBridge] 收到 Wit 数据: {response.ToString()}");

        string foundKeyword = "";

        // 2. 暴力查找：不管实体叫 spell_name 还是什么，只要有 value 就提取
        // 这样可以解决 "spell_name:spell_name" 这种名字匹配不上的问题
        foreach (string entityName in entities.ChildNodeNames)
        {
            var node = entities[entityName];
            if (node != null && node.Count > 0)
            {
                // 尝试提取 value (比如 "fireball")
                if (node[0]["value"] != null)
                {
                    foundKeyword = node[0]["value"].Value;
                    Debug.Log($"[VoiceBridge] 解析成功! 找到关键词: {foundKeyword}");
                    break; // 找到一个就停
                }
            }
        }

        // 3. 通知 DetectGestures 执行魔法
        if (!string.IsNullOrEmpty(foundKeyword) && detectGestures != null)
        {
            detectGestures.OnVoiceCommandReceived(foundKeyword);
        }
    }
}