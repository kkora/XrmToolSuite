using System;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.PrivilegeGapAnalyzer
{
    public partial class PrivilegeGapAnalyzerControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private ToolSettings _settings;

        // Update these for your repo â€” powers "Report a bug" / help links in XrmToolBox
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "your-github-username";
        public string HelpUrl => "https://github.com/your-github-username/XrmToolSuite";

        public PrivilegeGapAnalyzerControl()
        {
            InitializeComponent();
        }

        private void PrivilegeGapAnalyzerControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<ToolSettings>();
            LogInfo("Privilege Gap Analyzer loaded");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            SaveSettings(_settings);
            base.ClosingPlugin(info);
        }

        public override void UpdateConnection(
            IOrganizationService newService,
            ConnectionDetail detail,
            string actionName,
            object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            MetadataCache.Clear(); // metadata may differ between environments
            SetStatusMessage($"Connected to {detail?.ConnectionName}");
        }

        private void tsbClose_Click(object sender, EventArgs e) => CloseTool();

        // ExecuteMethod ensures a connection exists (prompts to connect if not)
        private void tsbLoadSample_Click(object sender, EventArgs e) => ExecuteMethod(LoadSampleData);

        private void LoadSampleData()
        {
            RunAsync(
                "Retrieving accounts...",
                worker =>
                {
                    var query = new QueryExpression("account")
                    {
                        ColumnSet = new ColumnSet("name", "createdon"),
                        TopCount = 100
                    };
                    return Service.RetrieveMultiple(query).Entities;
                },
                results =>
                {
                    lvResults.BeginUpdate();
                    lvResults.Items.Clear();
                    foreach (var entity in results)
                    {
                        lvResults.Items.Add(new ListViewItem(new[]
                        {
                            entity.GetAttributeValue<string>("name") ?? "(no name)",
                            entity.GetAttributeValue<DateTime?>("createdon")?.ToLocalTime().ToString("g") ?? "",
                            entity.Id.ToString()
                        }));
                    }
                    lvResults.EndUpdate();
                    SetStatusMessage($"Retrieved {results.Count} account(s)");
                });
        }
    }

    /// <summary>Diagnoses missing Dataverse privileges (why a user/team/role cannot perform an operation) and recommends role changes.</summary>
    public class ToolSettings
    {
        public string LastOption { get; set; }
    }
}
