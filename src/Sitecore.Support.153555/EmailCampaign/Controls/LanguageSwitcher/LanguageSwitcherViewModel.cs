#region Original
namespace Sitecore.Support.EmailCampaign.Controls.LanguageSwitcher
{
  using Diagnostics;
  using global::EmailCampaign.Controls;
  using Modules.EmailCampaign;
  using Modules.EmailCampaign.Messages;
  using Modules.EmailCampaign.Messages.Interfaces;
  using Mvc;
  using Mvc.Common;
  using Mvc.Presentation;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Web;
  using System.Web.Mvc;
  using Web;
  using Web.UI.Controls.Common.UserControls;

  public class LanguageSwitcherViewModel : Sitecore.EmailCampaign.Controls.LanguageSwitcher.LanguageSwitcherViewModel
  {
    /// <summary>
    /// Get the control id value.
    /// </summary>
    public new string ControlId
    {
      get { return this.UserControl.ControlId; }
    }

    /// <summary>
    /// Get the HTML attributes value.
    /// </summary>
    public new HtmlString HtmlAttributes
    {
      get { return this.UserControl.HtmlAttributes; }
    }

    #endregion
    #region PatchChanges
    /// <summary>
    /// Get the diaplay name value.
    /// </summary>
    public new String DisplayName
    {
      // in case of any null values
      get { return CurrentLanguage?.DisplayName ?? ""; }
    }
    #endregion
    #region Original
    /// <summary>
    /// Get or set the current language value.
    /// </summary>
    public new LanguageInfo CurrentLanguage { get; private set; }

    /// <summary>
    /// Get or set the current language tool tip value.
    /// </summary>
    public new string CurrentLanguageToolTip { get; private set; }

    /// <summary>
    /// Get or set HTML helper object.
    /// </summary>
    public new HtmlHelper<RenderingModel> Html { get; set; }

    /// <summary>
    /// Get or set the message languages values.
    /// </summary>
    public new List<LanguageInfo> MessageLanguages { get; set; }

    /// <summary>
    /// Get or set the formatted languages values.
    /// </summary>
    public new List<LanguageInfo> FormattedLanguages { get; set; }


    /// <summary>
    /// Get or set the rendering value.
    /// </summary>
    public override Rendering Rendering
    {
      get
      {
        Rendering rendering = null;
        try
        {
          rendering = base.Rendering;
        }
        catch (InvalidOperationException e)
        {
        }

        return rendering;
      }
      set { base.Rendering = value; }
    }

    private UserControl UserControl { get; set; }

    private readonly ILanguageRepository languageRepository;

    private readonly Factory factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageSwitcherViewModel"/> class 
    /// </summary>
    public LanguageSwitcherViewModel()
      : this(Factory.Instance, new LanguageRepository())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageSwitcherViewModel"/> class 
    /// </summary>
    /// <param name="factory">The factory.</param>
    /// <param name="languageRepository">the language repository object.</param>
    public LanguageSwitcherViewModel([NotNull] Factory factory, [NotNull] ILanguageRepository languageRepository)
    {
      Assert.ArgumentNotNull(factory, "factory");
      Assert.ArgumentNotNull(languageRepository, "languageRepository");

      this.factory = factory;
      this.languageRepository = languageRepository;

    }

    /// <summary>
    /// Override of the initialize to set the current model add set initial values for current control , get the the current language and render the language list.
    /// </summary>
    /// <param name="rendering">The current rendering object.</param>
    public override void Initialize(Rendering rendering)
    {
      this.Rendering = rendering;

      //Get current view context and initialize new HTMLHelper
      ViewContext current = ContextService.Get().GetCurrent<ViewContext>();
      this.Html = new HtmlHelper<RenderingModel>(current, new ViewDataContainer(current.ViewData));

      // Update current language switch user control.
      UpdateCurrentUserControl();

      //Get the current language.
      GetCurrentLanguage();

      // Render language list with formatted languages list.
      RenderLanguageList();
    }

    private void RenderLanguageList()
    {
      RenderingHelper helper = new RenderingHelper(Html, this.ControlId);
      helper.InsertPartialAt("/sitecore/shell/client/Applications/ECM/EmailCampaign.Controls/LanguageSwitcher/LanguageList.cshtml", this.ControlId + "DropDownButton", this);

    }
    #endregion
    #region PatchChanges
    private void GetCurrentLanguage()
    {
      var messageId = WebUtil.GetQueryString("id");
      var contentLanguage = WebUtil.GetQueryString(Sitecore.Modules.EmailCampaign.Core.Constants.SpeakContentLanguage);

      var messageItem = this.factory.GetMessageItem(messageId);
      if (messageItem != null)
      {
        var mailMessageItem = messageItem as MailMessageItem;
        var targetLanguage = mailMessageItem == null ? messageItem.InnerItem.Language : mailMessageItem.TargetLanguage;
        if (targetLanguage != null && targetLanguage.Name != contentLanguage)
        {
          contentLanguage = targetLanguage.Name;
        }
      }
      // if message item is not found, no need to do anything here, going to the EXM dashboard
      else
      {
        WebUtil.Redirect("/sitecore/client/Applications/ECM/Pages/Dashboard");
        Log.Info("Sitecore.Support.153555: No EXM message was found with ID " + messageId, this);
        return;
      }
      // end of changes

      this.MessageLanguages = languageRepository.GetLanguages(messageId, contentLanguage);

      this.CurrentLanguageToolTip = string.Empty;
      this.CurrentLanguage = this.MessageLanguages.SingleOrDefault(x => x.IsDefault);
      if (this.CurrentLanguage != null)
      {
        this.CurrentLanguageToolTip = this.CurrentLanguage.DisplayName;
        this.UserControl.Attributes.Add("data-sc-defaultLanguage", this.CurrentLanguage.IsoCode);
        this.UserControl.Attributes.Add("data-sc-defaultLanguageToolTip", this.CurrentLanguageToolTip);

        HttpCookie myCookie = new HttpCookie("messageLanguage", this.CurrentLanguage.IsoCode);
        myCookie.Expires = DateTime.Now.AddDays(1);
        HttpContext.Current.Response.Cookies.Add(myCookie);
      }


      GetFormattedLanguages();
    }

    private void GetFormattedLanguages()
    {
      var allLanguages = new Sitecore.Modules.EmailCampaign.Util().GetDb().Languages;

      this.FormattedLanguages = allLanguages.Select(l =>
      {
        // if message item in null, no languages will assigned. that's why we need the null check
        if (MessageLanguages != null)
        {
          // end of changes
          var messageLangauge = this.MessageLanguages.FirstOrDefault(messageLang => messageLang.IsoCode == l.Name);

          if (messageLangauge != null)
          {
            return new LanguageInfo()
            {
              HasVersion = messageLangauge.HasVersion,
              IsDefault = messageLangauge.IsDefault,
              IsoCode = messageLangauge.IsoCode,
              DisplayName = messageLangauge.DisplayName
            };
          }
        }

        return new LanguageInfo()
        {
          HasVersion = false,
          IsDefault = false,
          IsoCode = l.Name,
          DisplayName = l.CultureInfo.DisplayName
        };
      }).ToList();
    }
    #endregion
    #region Original

    private void UpdateCurrentUserControl()
    {
      UserControl = Html.Sitecore().Controls().GetUserControl(Sitecore.Mvc.Presentation.RenderingContext.Current.Rendering);
      UserControl.Requires.Script("ecm", "LanguageSwitcher.js");
      UserControl.Requires.Css("ecm", "LanguageSwitcher.css");
      UserControl.Class = "sc-ecm-language sc-actionpanel";
      UserControl.Attributes["data-bind"] = "visible: isVisible, isOpen: false";
    }
  }

}
#endregion