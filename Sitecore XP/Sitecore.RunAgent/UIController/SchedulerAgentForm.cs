
namespace Sitecore.RunAgent.UIController
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using Sitecore;
    using Sitecore.Configuration;
    using Sitecore.Jobs;
    using Sitecore.Web.UI.HtmlControls;
    using Sitecore.Web.UI.Pages;
    using Sitecore.Web.UI.Sheer;
    using Sitecore.Xml;

    public class SchedulerAgentForm : WizardForm
    {
        public Combobox SelectedAgent;

        public Combobox Priority;

        public Literal Result;

        public Literal Status;

        protected string JobHandle
        {
            get
            {
                return StringUtil.GetString(ServerProperties["JobHandle"]);
            }
            set
            {
                ServerProperties["JobHandle"] = value;
            }
        }

        protected String[] GetAgentNames()
        {
            var agentNames = new List<string>();

            foreach (XmlNode node in Factory.GetConfigNodes("scheduling/agent[@name!='']"))
            {
                if (node.Attributes != null)
                {
                    var name = node.Attributes["name", String.Empty].Value;

                    if (agentNames.Contains(name))
                    {
                        throw new Exception("Duplicate agent " + name + " in config file");
                    }

                    agentNames.Add(name);
                }
            }

            return agentNames.ToArray();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (this.SelectedAgent.Controls.Count < 1)
            {
                foreach (var entry in this.GetAgentNames().Select(agent => new ListItem
                {
                    Header = agent,
                    Value = agent
                }))
                {
                    this.SelectedAgent.Controls.Add(entry);
                }

                foreach (var entry in Enum.GetNames(typeof(System.Threading.ThreadPriority)).Select(name => new ListItem
                {
                    Header = name,
                    Value = name
                }))
                {
                    this.Priority.Controls.Add(entry);

                    this.Priority.Value = System.Threading.ThreadPriority.BelowNormal.ToString();
                }
            }
        }

        protected override void ActivePageChanged(string page, string oldPage)
        {
            base.ActivePageChanged(page, oldPage);

            if (page == "Running")
            {
                var agent = Context.ClientPage.ClientRequest.Form["SelectedAgent"];

                NextButton.Disabled = true;
                BackButton.Disabled = true;
                CancelButton.Disabled = true;

                var node = Factory.GetConfigNode("scheduling/agent[@name='" + agent + "']");

                var toRun = Factory.CreateObject(node, true);

                var method = StringUtil.GetString(new[] 
                { 
                    XmlUtil.GetAttribute("method", node), "Execute"
                });

                var contextSite = Context.Site.Name;

                if (Factory.GetSite("scheduler") != null)
                {
                    contextSite = "scheduler";
                }

                var jobOptions = new JobOptions(agent, "Manually Triggered", contextSite, toRun, method, new object[] { })
                {
                    ContextUser = Context.User,
                    AfterLife = TimeSpan.FromMinutes(5),
                    WriteToLog = true,
                };

                var priorityValue = Enum.Parse(typeof(System.Threading.ThreadPriority), this.Priority.Value, true);

                var priorityProperty = jobOptions.GetType().GetProperty("Priority");

                priorityProperty.SetValue(jobOptions, priorityValue, null);

                var job = JobManager.Start(jobOptions);

                this.JobHandle = job.Handle.ToString();

                SheerResponse.Timer("CheckStatus", 100);
            }
        }

        public void CheckStatus()
        {

            var handle = Handle.Parse(this.JobHandle);

            var job = JobManager.GetJob(handle);

            if (job.Status.State != JobState.Running)
            {
                this.Result.Text = job.Status.Failed ? "Job failed." : "Job succeeded.";

                if (job.Status.Messages.Count > 0)
                {
                    foreach (var msg in job.Status.Messages)
                    {
                        this.Result.Text += "\n" + msg;
                    }
                }

                Active = "LastPage";
            }
            else
            {
                if (job.Status.Messages.Count > 0)
                {
                    this.Status.Text = "Last status message: " + job.Status.Messages[job.Status.Messages.Count - 1];
                }
                else
                {
                    this.Status.Text = "No status messages available.";
                }

                SheerResponse.Timer("CheckStatus", 100);
            }
        }
    }
}