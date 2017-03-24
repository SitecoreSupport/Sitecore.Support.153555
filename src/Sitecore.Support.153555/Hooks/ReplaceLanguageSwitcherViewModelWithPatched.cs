namespace Sitecore.Support.Hooks
{
  using Sitecore.Configuration;
  using Sitecore.Diagnostics;
  using Sitecore.Events.Hooks;
  using Sitecore.SecurityModel;
  using System;

  public class ReplaceLanguageSwitcherViewModelWithPatched : IHook
  {
    public void Initialize()
    {
      using (new SecurityDisabler())
      {
        var databaseName = "core";
        var itemPath = "/sitecore/client/Applications/ECM/Assets/Models/LanguageSwitcherModel";
        var fieldName = "Model Type";

        // protects from refactoring-related mistakes
        var type = typeof(EmailCampaign.Controls.LanguageSwitcher.LanguageSwitcherViewModel);

        // full name of the class is enough
        var fieldValue = type.FullName;

        var database = Factory.GetDatabase(databaseName);
        var item = database.GetItem(itemPath);

        if (string.Equals(item[fieldName], fieldValue, StringComparison.Ordinal))
        {
          // already installed
          return;
        }

        Log.Info($"Installing {fieldValue}", this);
        item.Editing.BeginEdit();
        item[fieldName] = fieldValue;
        item.Editing.EndEdit();
      }
    }
  }
}