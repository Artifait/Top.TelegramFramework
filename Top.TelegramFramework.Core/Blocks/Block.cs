// File: Top.TelegramFramework.Core/Blocks/Block.cs
using System.Text.Json;

namespace Top.TelegramFramework.Core.Blocks
{
    public abstract class Block<TState> : HandlerBlock where TState : new()
    {
        public TState State { get; private set; } = new TState();

        public override void ApplyState(string? stateJson)
        {
            if (string.IsNullOrEmpty(stateJson))
            {
                State = new TState();
                return;
            }
            try
            {
                State = JsonSerializer.Deserialize<TState>(stateJson) ?? new TState();
            }
            catch
            {
                State = new TState();
            }
        }

        public override string? CaptureState()
        {
            try
            {
                return JsonSerializer.Serialize(State);
            }
            catch
            {
                return null;
            }
        }
    }
}
