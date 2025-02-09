
using Newtonsoft.Json;

namespace Top.TelegramFramework.Core.Data
{
    public class UserState
    {
        public string CurrentCompositeBlockId { get; set; }
        public Dictionary<string, object> CompositeBlockState { get; set; }
        public Dictionary<string, object> Context { get; set; }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static UserState Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<UserState>(json);
        }
    }
}
