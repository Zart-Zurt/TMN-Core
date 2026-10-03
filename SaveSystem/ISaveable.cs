namespace Core.Save
{
    public interface ISaveable
    {
        string SaveKey { get; }
        void OnSave(SaveManager saveManager);
        void OnLoad(SaveManager saveManager);
    }
}
