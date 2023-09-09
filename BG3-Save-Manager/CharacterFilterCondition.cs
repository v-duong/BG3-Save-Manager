using System;

namespace BG3_Save_Manager
{
    public class FilterCondition
    {
        public bool IsEnabled { get; set; }
        public string ItemName { get; set; }

        public FilterCondition(string itemName, bool isEnabled)
        {
            IsEnabled = isEnabled;
            ItemName = itemName ?? throw new ArgumentNullException(nameof(itemName));
        }
    }
    public class CharacterFilterCondition : FilterCondition {
          public string UniqueID { get; set; }

            public CharacterFilterCondition(string itemName, bool isEnabled, string uniqueID) : base(itemName, isEnabled)
            {
                UniqueID = uniqueID ?? throw new ArgumentNullException(nameof(uniqueID));
            }
       }
}
