using UnityEngine;
using UnityEngine.SceneManagement;
public class startgame : MonoBehaviour
{
    public void ChangeScene()
    {
        SceneManager.LoadScene("GameScene");
    }
}
