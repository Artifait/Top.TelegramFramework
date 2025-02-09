
using Top.TelegramFramework.Core.Blocks;

namespace Top.TelegramFramework.Core
{
    public class Scenario
    {
        private readonly Dictionary<string, HandlerBlock> _blocks = new Dictionary<string, HandlerBlock>();

        public HandlerBlock InitialBlock { get; private set; }

        public string ScenarioId { get; }

        public Scenario(string scenarioId)
        {
            ScenarioId = scenarioId;
        }

        public void RegisterInitialBlock(HandlerBlock block)
        {
            _blocks[block.BlockId] = block;
            InitialBlock = block;
        }

        public void RegisterBlock(HandlerBlock block)
        {
            if (_blocks.ContainsKey(block.BlockId))
                throw new ArgumentException($"Блок с ID {block.BlockId} уже зарегистрирован");

            _blocks[block.BlockId] = block;
        }

        /// <summary>
        /// Возвращает клон блока по идентификатору.
        /// </summary>
        public HandlerBlock GetBlock(string id)
        {
            if (!_blocks.TryGetValue(id, out var block))
            {
                throw new KeyNotFoundException($"Блок с ID {id} не найден");
            }

            // Возвращаем клон, а не оригинал.
            return block.Clone();
        }
    }
}
