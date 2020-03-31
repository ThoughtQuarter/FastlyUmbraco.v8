using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using Umbraco.Core.Composing;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.PackageActions;

namespace FastlyUmbraco
{
	public class AddFastlySectionToAdminPackageAction : IPackageAction
	{
		public string Alias() => "AddFastlySectionToAdminGroupPackageAction";

        public bool Execute(string packageName, XElement xmlData)
        {
            //Add Fastly section to admin group on install
            IUserGroup adminGroup = Current.Services.UserService.GetUserGroupByAlias("admin");
            adminGroup.AddAllowedSection("fastlyUmbraco");

            return true;
        }

        public bool Undo(string packageName, XElement xmlData)
        {
            //Remove Fastly section from the admin group on package removal
            IUserGroup adminGroup = Current.Services.UserService.GetUserGroupByAlias("admin");
            adminGroup.RemoveAllowedSection("fastlyUmbraco");

            return true;
        }

    }
}