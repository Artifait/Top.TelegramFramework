
namespace Top.TelegramFramework.Core.Blocks
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class BlockAttribute : Attribute
    {
        public string BlockId { get; }

        public BlockAttribute(string blockId)
        {
            BlockId = blockId;
        }
    }
}
