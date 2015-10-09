using System.Linq;

namespace BlackFox.U2FHid.Utils
{
    static class EnumDescription
    {
        public static string Get<T>(T value)
            where T : struct
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute =
                field.GetCustomAttributes(
                    typeof(EnumDescriptionAttribute),
                    false).OfType<EnumDescriptionAttribute>().FirstOrDefault();

            return attribute?.Description ?? value.ToString();
        }
    }
}