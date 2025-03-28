using UnityEngine;

public class GameRuleManager : MonoBehaviour
{
    [SerializeField] private bool isInfinityGame = false;
    public bool IsInfinityGame {
        get { return isInfinityGame; }
    }
    public AudioSource ambientSoundConfiguration;
    public Camera playerCamera;

    // Когда выключаем игру, все данные сохраняются в файл настроек
    // Когда включается игра, загружаем все данные игры из файла настроек
}
