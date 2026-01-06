public class GameController : Singleton<GameController>
{
    public bool IsEditorMode = false;
    public void SetEditorMode(bool isEditor)
    {
        IsEditorMode = isEditor;
    }
}
