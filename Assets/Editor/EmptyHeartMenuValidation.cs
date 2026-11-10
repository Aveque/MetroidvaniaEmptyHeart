#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;

// All previews/checks operate on the authored station, never on its saved scene data.
public static class EmptyHeartMenuValidation
{
    [MenuItem("Tools/Empty Heart/Проверки/Босс/Щит, ворота и луч", true)]
    [MenuItem("Tools/Empty Heart/Проверки/Босс/Анимации и эффекты", true)]
    [MenuItem("Tools/Empty Heart/Проверки/Босс/Полный бой (новый запуск игры)", true)]
    [MenuItem("Tools/Empty Heart/Проверки/Персонаж/Чекпоинты", true)]
    [MenuItem("Tools/Empty Heart/Проверки/Персонаж/Получение Dash (новый запуск игры)", true)]
    [MenuItem("Tools/Empty Heart/Проверки/Персонаж/Плавность анимаций", true)]
    [MenuItem("Tools/Empty Heart/Проверки/Персонаж/Прыжки от стен и отскок", true)]
    [MenuItem("Tools/Empty Heart/Проверки/Персонаж/Атаки и попадания", true)]
    [MenuItem("Tools/Empty Heart/Проверки/Персонаж/Окна терминалов", true)]
    [MenuItem("Tools/Empty Heart/Проверки/Мир/Проходимость комнат", true)]
    [MenuItem("Tools/Empty Heart/Проверки/Мир/Исследование (после проверки босса)", true)]
    [MenuItem("Tools/Empty Heart/Проверки/Мир/Карта, оборудование и лифты", true)]
    [MenuItem("Tools/Empty Heart/Проверки/Мир/Крупные установки", true)]
    [MenuItem("Tools/Empty Heart/Проверки/Мир/Малый и средний декор", true)]
    [MenuItem("Tools/Empty Heart/Проверки/Мир/Переходы камеры", true)]
    [MenuItem("Tools/Empty Heart/Просмотр/Арена босса", true)]
    [MenuItem("Tools/Empty Heart/Просмотр/Чекпоинт", true)]
    [MenuItem("Tools/Empty Heart/Просмотр/Карта изученных комнат", true)]
    [MenuItem("Tools/Empty Heart/Просмотр/Полная карта", true)]
    [MenuItem("Tools/Empty Heart/Просмотр/Отсек с фрагментом оболочки", true)]
    [MenuItem("Tools/Empty Heart/Просмотр/Обсерватория", true)]
    [MenuItem("Tools/Empty Heart/Просмотр/Насосная", true)]
    [MenuItem("Tools/Empty Heart/Просмотр/Зал инкубаторов", true)]
    [MenuItem("Tools/Empty Heart/Просмотр/Мастерская", true)]
    [MenuItem("Tools/Empty Heart/Просмотр/Нижние сады", true)]
    static bool PlayingStation()=>EditorApplication.isPlaying && SceneManager.GetActiveScene().name=="StationEcho_Organic";

    [MenuItem("Tools/Empty Heart/Экспорт/Контуры комнат и переходов", true)]
    static bool EditingStation()=>!EditorApplication.isPlaying && SceneManager.GetActiveScene().name=="StationEcho_Organic";
}
#endif
