using Advanced_Combat_Tracker;
using Codeplex.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace ACT_quickFFlogsPerf
{
    public class ACT_quickFFlogsPerf : IActPluginV1
    {
        private const string PERF_FILE_NAME = "fflogsPerf.json";

        enum EncType
        {
            DPS, HPS
        }

        // PerfKey : perf{dps,hps}
        private static readonly Dictionary<PerfKey, dynamic> cache = new Dictionary<PerfKey, dynamic>();

        // GUI
        private Label statusLabel;
        private Button button;
        private TextBox textbox;


        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            // Label
            statusLabel = pluginStatusText;
            statusLabel.Text = "Plugin started";

            // TabPage
            button = new Button();
            button.Location = new System.Drawing.Point(10, 10);
            button.Text = "Update";
            button.Click += new System.EventHandler(button_Click);
            textbox = new TextBox();
            textbox.Location = new System.Drawing.Point(10, 40);
            textbox.Multiline = true;
            textbox.ScrollBars = ScrollBars.Vertical;
            textbox.Size = new System.Drawing.Size(600, 300);
            pluginScreenSpace.Controls.Add(button);
            pluginScreenSpace.Controls.Add(textbox);

            LoadJson();

            CombatantData.ColumnDefs.Add("DPSPerf",
                new CombatantData.ColumnDef("DPSPerf", true, "VARCHAR(3)", "DPS Percentile",
                    Data => { return GetDpsPerf(Data).ToString(); },
                    Data => { return GetDpsPerf(Data).ToString(); },
                    (Left, Right) => { return GetDpsPerf(Left).CompareTo(GetDpsPerf(Right)); })
             );
            CombatantData.ColumnDefs.Add("HPSPerf",
                new CombatantData.ColumnDef("HPSPerf", true, "VARCHAR(3)", "HPS Percentile",
                    Data => { return GetHpsPerf(Data).ToString(); },
                    Data => { return GetHpsPerf(Data).ToString(); },
                    (Left, Right) => { return GetHpsPerf(Left).CompareTo(GetHpsPerf(Right)); })
             );

            ActGlobals.oFormActMain.ValidateTableSetup();

            CombatantData.ExportVariables.Add("DPSPerf",
                new CombatantData.TextExportFormatter("dpsperf", "DPSPercentile", "2 Week Historical Percentile based off current DPS.",
                (Data, Extra) => { return GetDpsPerf(Data).ToString(); })
             );
            CombatantData.ExportVariables.Add("HPSPerf",
                new CombatantData.TextExportFormatter("hpsperf", "HPSPercentile", "2 Week Historical Percentile based off current HPS.",
                (Data, Extra) => { return GetHpsPerf(Data).ToString(); })
             );

            statusLabel.Text = "Plugin Inited.";
        }

        public void DeInitPlugin()
        {
            CombatantData.ColumnDefs.Remove("DPSPerf");
            CombatantData.ColumnDefs.Remove("HPSPerf");

            ActGlobals.oFormActMain.ValidateTableSetup();

            CombatantData.ExportVariables.Remove("DPSPerf");
            CombatantData.ExportVariables.Remove("HPSPerf");

            statusLabel.Text = "Plugin exited";
        }

        private void LoadJson()
        {
            try
            {
                if (File.Exists(PERF_FILE_NAME))
                {
                    Log("load start.");

                    cache.Clear();

                    dynamic Json = DynamicJson.Parse(File.ReadAllText(PERF_FILE_NAME));

                    foreach (var zone in Json["zones"])
                    {
                        foreach (var boss in zone["boss"])
                        {
                            foreach (var name in boss["name"])
                            {
                                PerfKey key = CreateKey(zone["name"], name);
                                cache.Add(key, boss["perf"]);

                                Log(key.Zone + " <> " + key.Boss);
                            }
                        }
                    }
                    Log("load success.");
                }
                else
                {
                    Log("file not found. please Update");
                }
            }
            catch (Exception ex)
            {
                Log("load error.");
                Log(ex.StackTrace);
            }
        }

        private void button_Click(object sender, System.EventArgs e)
        {
            try
            {
                Log("update start.");

                WebRequest webrequest = WebRequest.Create("https://github.com/Qofar/ACT_quickFFlogsPerf/raw/master/fflogsPerf.json");
                using (WebResponse response = webrequest.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    File.WriteAllText(PERF_FILE_NAME, reader.ReadToEnd());
                    Log("update success.");
                }

                LoadJson();
            }
            catch (Exception ex)
            {
                Log("update error.");
                Log(ex.StackTrace);
            }
        }

        private string GetDpsPerf(CombatantData combatant)
        {
            return GetPerf(EncType.DPS, combatant, combatant.EncDPS);
        }

        private string GetHpsPerf(CombatantData combatant)
        {
            double absorbHPS = combatant.EncHPS;

            if (absorbHPS > 0)
            {
                int overhealpct = Int32.Parse(combatant.GetColumnByName("OverHealPct").Replace("%", string.Empty));

                double absorbHeald = combatant.Healed - Math.Round(Decimal.ToDouble(combatant.Healed * (overhealpct / 100)));
                absorbHPS = Math.Round(absorbHeald / combatant.Parent.Duration.TotalSeconds);
            }

            return GetPerf(EncType.HPS, combatant, absorbHPS);
        }


        private string GetPerf(EncType enc, CombatantData combatant, double encdpshps)
        {
            string job = combatant.GetColumnByName("Job");
            if (job == string.Empty) return string.Empty;

            string perfdpshps = enc == EncType.DPS ? "dps" : "hps";

            EncounterData encounter = combatant.Parent;
            string zonename = encounter.ZoneName;
            string bossname = encounter.GetStrongestEnemy("YOU");

            PerfKey key = CreateKey(zonename, bossname);
            dynamic perf = null;

            if (cache.TryGetValue(key, out perf))
            {
                double[] perfarry = (double[])perf[perfdpshps][job];

                if (encdpshps < perfarry[6]) return "10-";
                if (encdpshps < perfarry[5]) return "10+";
                if (encdpshps < perfarry[4]) return "25+";
                if (encdpshps < perfarry[3]) return "50+";
                if (encdpshps < perfarry[2]) return "75+";
                if (encdpshps < perfarry[1]) return "95+";
                if (encdpshps < perfarry[0]) return "99+";
                return "100";
            }
            return string.Empty;
        }

        private PerfKey CreateKey(string zone, string boss)
        {
            return new PerfKey(zone, boss);
        }

        private void Log(string str)
        {
            textbox.Text += $"[{DateTime.Now}] {str}\r\n";
        }
    }

    struct PerfKey : IEquatable<PerfKey>
    {
        public string Zone { get; }
        public string Boss { get; }

        public PerfKey(string z, string b)
        {
            Zone = z;
            Boss = b;
        }

        public bool Equals(PerfKey other)
        {
            return Zone == other.Zone && Boss == other.Boss;
        }

        public override bool Equals(object other)
        {
            if (other is PerfKey)
                return Equals((PerfKey)other);
            return false;
        }

        public override int GetHashCode()
        {
            return Zone.GetHashCode() ^ Boss.GetHashCode();
        }
    }
}
