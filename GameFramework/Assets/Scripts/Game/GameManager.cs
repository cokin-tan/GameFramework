using UnityEngine;

public class GameManager : Singleton<GameManager> 
{
    public void Fuck()
    {
        Logger.LogError("fuck", this.gameObject);
    }
}
