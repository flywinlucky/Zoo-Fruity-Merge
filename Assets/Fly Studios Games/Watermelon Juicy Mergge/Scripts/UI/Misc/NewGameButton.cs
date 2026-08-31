using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "New game" entry for the settings window. It lives on a prefab, so it cannot hold a
/// scene reference to the GameManager and resolves it through the singleton at click time.
/// </summary>
[RequireComponent(typeof(Button))]
public class NewGameButton : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(StartNewGame);
    }

    private void StartNewGame()
    {
        var gameManager = WatermelonGameClone.GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogWarning("[NewGameButton] No GameManager in the scene.");
            return;
        }

        gameManager.NewGame();
    }
}
