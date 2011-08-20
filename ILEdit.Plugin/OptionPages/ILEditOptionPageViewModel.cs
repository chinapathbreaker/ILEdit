using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ILEdit.OptionPages
{
    internal class ILEditOptionPageViewModel
    {
        #region Properties

        /// <summary>
        /// Maximum number of elements to show in the list of selecte
        /// </summary>
        public int MaxRecentMembersCount { get; set; }

        #endregion

        /// <summary>
        /// Initializes the value of the properties from the current settings
        /// </summary>
        public void Load() 
        {
            var node = GlobalContainer.InjectionSettings;
            MaxRecentMembersCount = int.Parse(node.Attribute("MaxRecentMembersCount").Value);
        }

        /// <summary>
        /// Saves the options in the given node
        /// </summary>
        /// <param name="root"></param>
        public void Save(XElement root)
        {
            //Injection node
            var injection = root.Element("Injection");

            //MaxRecentMembersCount
            injection.SetAttributeValue("MaxRecentMembersCount", MaxRecentMembersCount);
            var recentMembers = injection.Element("RecentMembers").Elements().ToArray();
            if (recentMembers.Length > MaxRecentMembersCount)
                for (int i = recentMembers.Length; i > MaxRecentMembersCount; i--)
                    recentMembers[i - 1].Remove();
        }

    }
}
