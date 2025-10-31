
using System.Reflection;
using Top.TelegramFramework.Core.Blocks;

namespace Top.TelegramFramework.Core
{
    public class Scenario
    {
        public string ScenarioId { get; }
        private readonly Dictionary<string, Type> _blocks = new();
        public Type? InitialBlockType { get; private set; }

        public Scenario(string id)
        {
            ScenarioId = id;
        }

        public void RegisterBlock(string id, Type type)
        {
            if (!typeof(HandlerBlock).IsAssignableFrom(type))
                throw new ArgumentException($"Type {type.Name} must inherit HandlerBlock");
            _blocks[id] = type;
        }

        public void RegisterInitialBlock<T>() where T : HandlerBlock
        {
            RegisterInitialBlockType(typeof(T));
        }

        public void RegisterInitialBlockType(Type t)
        {
            if (!typeof(HandlerBlock).IsAssignableFrom(t))
                throw new ArgumentException("Initial block type must inherit HandlerBlock", nameof(t));

            var attr = t.GetCustomAttribute<BlockAttribute>();
            if (attr == null)
                throw new InvalidOperationException($"Initial block type {t.FullName} must have [Block(\"id\")] attribute");

            RegisterBlock(attr.BlockId, t);
            InitialBlockType = t;
        }

        // Регистрировать все блоки с атрибутом [Block] из той же сборки
        public void RegisterBlocksFromAssembly(Type initialBlockType)
        {
            var assembly = initialBlockType.Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (!typeof(HandlerBlock).IsAssignableFrom(type) || type.IsAbstract) continue;
                var attr = type.GetCustomAttribute<BlockAttribute>();
                if (attr != null) RegisterBlock(attr.BlockId, type);
            }
        }

        // Регистрировать только блоки с атрибутом [Block] из того же namespace (и под-namespace) что и initialBlockType
        public void RegisterBlocksFromNamespace(Type initialBlockType)
        {
            var assembly = initialBlockType.Assembly;
            var ns = initialBlockType.Namespace ?? string.Empty;
            foreach (var type in assembly.GetTypes())
            {
                if (!typeof(HandlerBlock).IsAssignableFrom(type) || type.IsAbstract) continue;
                var tns = type.Namespace ?? string.Empty;
                // учитываем под-namespace'ы (StartsWith)
                if (!tns.StartsWith(ns, StringComparison.Ordinal)) continue;
                var attr = type.GetCustomAttribute<BlockAttribute>();
                if (attr != null) RegisterBlock(attr.BlockId, type);
            }
        }

        public Type? GetBlockType(string blockId)
        {
            _blocks.TryGetValue(blockId, out var t);
            return t;
        }

        // Возвращает все зарегистрированные типы блоков (для дальнейшей регистрации в DI)
        public IEnumerable<Type> GetRegisteredBlockTypes() => _blocks.Values.Distinct();
    }
}
