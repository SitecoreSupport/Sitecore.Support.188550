using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Pipelines.Save;
using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Sitecore.Support.Pipelines.Save
{
    public class CheckLinks
    {
        public void Process(SaveArgs args)
        {
            if (!args.HasSheerUI)
            {
                return;
            }
            if (args.Result == "no" || args.Result == "undefined")
            {
                args.AbortPipeline();
                return;
            }
            int num = 0;
            if (args.Parameters["LinkIndex"] == null)
            {
                args.Parameters["LinkIndex"] = "0";
            }
            else
            {
                num = MainUtil.GetInt(args.Parameters["LinkIndex"], 0);
            }
            for (int i = 0; i < args.Items.Length; i++)
            {
                if (i >= num)
                {
                    num++;
                    SaveArgs.SaveItem saveItem = args.Items[i];
                    Item item = Context.ContentDatabase.Items[saveItem.ID, saveItem.Language, saveItem.Version];
                    if (item != null)
                    {
                        item.Editing.BeginEdit();
                        SaveArgs.SaveField[] fields = saveItem.Fields;
                        for (int j = 0; j < fields.Length; j++)
                        {
                            SaveArgs.SaveField saveField = fields[j];
                            Field field = item.Fields[saveField.ID];
                            if (field != null)
                            {
                                if (!string.IsNullOrEmpty(saveField.Value))
                                {
                                    field.Value = saveField.Value;
                                }
                                else
                                {
                                    field.Value = null;
                                }
                            }
                        }
                        bool allVersions = false;
                        ItemLink[] brokenLinks = item.Links.GetBrokenLinks(allVersions);
                        if (brokenLinks.Length > 0)
                        {
                            CheckLinks.ShowDialog(item, brokenLinks);
                            args.WaitForPostBack();
                            break;
                        }
                        item.Editing.CancelEdit();
                    }
                }
            }
            args.Parameters["LinkIndex"] = num.ToString();
        }

        private static void ShowDialog(Item item, ItemLink[] links)
        {
            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(Translate.Text("The item \"{0}\" contains broken links in these fields:\n\n", new object[]
            {
                item.DisplayName
            }));
            bool flag = false;
            if (links.Count<ItemLink>() > 0)
            {
                stringBuilder.Append("<table style='word-break:break-all;'>");
                stringBuilder.Append("<tbody>");
                for (int i = 0; i < links.Length; i++)
                {
                    ItemLink itemLink = links[i];
                    if (!itemLink.SourceFieldID.IsNull)
                    {
                        stringBuilder.Append("<tr>");
                        stringBuilder.Append("<td style='width:100%;vertical-align:top;padding-bottom:5px;padding-right:55px;'>");
                        if (item.Fields.Contains(itemLink.SourceFieldID))
                        {
                            stringBuilder.Append(item.Fields[itemLink.SourceFieldID].DisplayName);
                        }
                        else
                        {
                            stringBuilder.Append(Translate.Text("[Unknown field: {0}]", new object[]
                            {
                                itemLink.SourceFieldID.ToString()
                            }));
                        }
                        stringBuilder.Append("</td>");
                        stringBuilder.Append("<tr>");
                        stringBuilder.Append("<td style='width:100%;vertical-align:top;padding-bottom:5px;padding-right:55px;'>");
                        if (!string.IsNullOrEmpty(itemLink.TargetPath) && !ID.IsID(itemLink.TargetPath))
                        {
                            stringBuilder.Append(itemLink.TargetPath);
                        }
                        stringBuilder.Append("</td>");
                        stringBuilder.Append("</tr>");
                    }
                    else
                    {
                        flag = true;
                    }
                }
                stringBuilder.Append("</tbody></table>");
            }
            if (flag)
            {
                stringBuilder.Append("\n");
                stringBuilder.Append(Translate.Text("The template or branch for this item is missing."));
            }
            stringBuilder.Append("\n");
            stringBuilder.Append(Translate.Text("Do you want to save anyway?"));
            Context.ClientPage.ClientResponse.Confirm(stringBuilder.ToString());
        }
    }
}