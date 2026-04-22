using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UI_Board : MonoBehaviour
{
    [SerializeField] TMP_InputField inputField;
    [SerializeField] Transform parent;
    [SerializeField] GameObject prefab;

    private void Start()
    {
        //inputField.onSubmit.AddListener(_ => OnClickSubmit());//Enterを押したとき
        inputField.onEndEdit.AddListener(_ => OnClickSubmit());//Fieldから話したとき
    }
    private void OnDestroy()
    {
        //inputField.onSubmit.RemoveListener(_ => OnClickSubmit());
        inputField.onEndEdit.RemoveListener(_ => OnClickSubmit());
    }
    public void OnClickSubmit()
    {
        if (!(NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)) return;

        var sender = $"Player{NetworkManager.Singleton.LocalClientId}";
        var text = inputField.text;

        ChatSystem.Instance.SubmitMessageRpc(sender, text);

        inputField.text = "";
    }

    public void AddMessage(ChatMessage msg)
    {
        var obj = Instantiate(prefab,parent);
        obj.GetComponent<TMP_Text>().text =
            $"[{msg.Sender}]\n {msg.Text}";

        //レイアウトを手動で即時更新
        ContentSizeFitter contentSizeFitter = obj.GetComponent<ContentSizeFitter>();
        contentSizeFitter.SetLayoutHorizontal();
        contentSizeFitter.SetLayoutVertical();

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentSizeFitter.GetComponent<RectTransform>());
    }
}
