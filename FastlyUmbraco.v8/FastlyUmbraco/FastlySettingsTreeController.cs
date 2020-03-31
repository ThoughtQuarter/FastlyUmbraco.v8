using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using Umbraco.Core;
using Umbraco.Web.Actions;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Mvc;
using Umbraco.Web.Trees;

namespace FastlyUmbraco
{
	[Tree("fastlyUmbraco", "Fastly", TreeTitle = "Settings", TreeGroup = "fastlyGroup", SortOrder = 1)]
	[PluginController("FastlyUmbraco")]
	public class FastlySettingsTreeController : TreeController
	{
		protected override TreeNode CreateRootNode(FormDataCollection queryStrings)
		{
			var root = base.CreateRootNode(queryStrings);

			//optionally setting a routepath would allow you to load in a custom UI instead of the usual behaviour for a tree
			root.RoutePath = "fastlyUmbraco/Fastly/FastlySettings";

			// set the icon
			root.Icon = "icon-umb-developer";
			// could be set to false for a custom tree with a single node.
			root.HasChildren = false;
			//url for menu
			root.MenuUrl = null;

			return root;
		}
		protected override TreeNodeCollection GetTreeNodes(string id, FormDataCollection queryStrings)
		{
			//We don't have any child nodes & only use the root node to load a custom UI
			return new TreeNodeCollection();
		}

		protected override MenuItemCollection GetMenuForNode(string id, FormDataCollection queryStrings)
		{
			//We don't have any menu item options (such as create/delete/reload) & only use the root node to load a custom UI
			return null;
		}
	}
}