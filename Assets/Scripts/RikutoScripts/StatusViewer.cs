using Oculus.Interaction.Locomotion;
using Unity.Netcode;
using UnityEngine;

public class StatusViewer : NetworkBehaviour
{
    [SerializeField] Transform playerRootTransform;
    [SerializeField] PlayerHealth playerHealth;
    [SerializeField] Syncronize syncro;
    [SerializeField] Camera mainCamera;
    public override void OnNetworkSpawn()
    {
        mainCamera ??= Camera.main;
    }
    private void OnGUI()
    {
        //サーバーは表示しない
        if (!IsClient) return;
        //オーナーでないプレイヤーの情報を表示するためのコード。オーナーでないプレイヤーのカメラが有効な場合は、そのカメラの位置にプレイヤーのIDとジャンプ回数を表示する。
        if (!IsOwner)
        {
            Vector3 positon = mainCamera.WorldToScreenPoint(playerRootTransform.position);
            if (positon.z < 0) return; // カメラの前にいる場合のみ表示
            GUI.Label(new Rect(positon.x, Screen.height - positon.y - 120, 100, 20), $"{OwnerClientId}");
            GUI.Label(new Rect(positon.x, Screen.height - positon.y - 100, 100, 20), $"jump: {syncro.JumpCount}");
            GUI.Label(new Rect(positon.x, Screen.height - positon.y - 80, 100, 20), $"HP: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}");
            return;
        }
        //オーナーのプレイヤーの情報を表示するためのコード。オーナーのカメラが有効な場合は、そのカメラの位置に「You」とジャンプ回数とHPを表示する。
        if (mainCamera != null && mainCamera.enabled)
        {
            Vector3 positon = mainCamera.WorldToScreenPoint(playerRootTransform.position);
            if (positon.z < 0) return; // カメラの前にいる場合のみ表示
            GUI.Label(new Rect(positon.x, Screen.height - positon.y - 60, 100, 20), "You");
            GUI.Label(new Rect(positon.x, Screen.height - positon.y - 30, 100, 20), $"jump: {syncro.JumpCount}");
            GUI.Label(new Rect(positon.x, Screen.height - positon.y, 100, 20), $"HP: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}");
        }


    }
}
