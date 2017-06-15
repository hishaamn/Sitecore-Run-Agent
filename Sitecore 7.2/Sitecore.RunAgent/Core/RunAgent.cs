
namespace Sitecore.RunAgent.Core
{
    using Sitecore.Diagnostics;
    using Sitecore.Shell.Framework.Commands;
    using Sitecore.Text;
    using Sitecore.Web.UI.Sheer;

    public class RunAgent : Command
    {
        public override void Execute(CommandContext context)
        {
            Context.ClientPage.Start(this, "Process");
        }

        protected void Process(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            if (!args.IsPostBack)
            {
                var url = new UrlString(UIUtil.GetUri("control:RunAgent")).ToString();

                SheerResponse.ShowModalDialog(url, true);

                args.WaitForPostBack();
            }
        }
    }
}
