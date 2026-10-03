namespace Core.Save
{
    public struct SaveVersionTooNewEvent
    {
        public int slotIndex;
        public int fileVersion;
        public int currentVersion;
    }
}
