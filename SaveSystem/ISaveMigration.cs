namespace Core.Save
{
    public interface ISaveMigration
    {
        int FromVersion { get; }
        int ToVersion { get; }
        void Migrate(SaveSlotData data);
    }
}
