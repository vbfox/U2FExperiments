using System;
using JetBrains.Annotations;

namespace BlackFox.U2FHid.Utils
{
    [AttributeUsage(AttributeTargets.Field)]
    class EnumDescriptionAttribute : Attribute
    {
        [NotNull]
        public string Description { get; set; }

        public EnumDescriptionAttribute([NotNull] string description)
        {
            if (description == null) throw new ArgumentNullException(nameof(description));

            Description = description;
        }
    }
}
