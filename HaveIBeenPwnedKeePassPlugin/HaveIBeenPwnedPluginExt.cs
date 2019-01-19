using System;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using KeePass.Plugins;
using KeePass.UI;
using KeePassLib;
using KeePassLib.Utility;

namespace HaveIBeenPwnedPlugin
{
    public sealed class HaveIBeenPwnedPluginExt : Plugin
    {
        private IPluginHost _pluginHost = null;
        private bool _checkPasswordOnEntryTouched = true;
        private const string Option_CheckPasswordOnEntryTouched = "HaveIBeenPwnedPlugin_Option_CheckPasswordOnEntryTouched";
        private const string BreachedTag = "pwned";
        private const string BreachCountCustomDataName = "pwned-count";
        private readonly HIBP _hibp;

        public HaveIBeenPwnedPluginExt() => _hibp = new HIBP();

        public override string UpdateUrl => "https://raw.githubusercontent.com/kapsiR/HaveIBeenPwnedKeePassPlugin/master/KeePass.version";

        /// <summary>
        /// Initialization of the plugin
        /// </summary>
        /// <param name="pluginHost">Plugin host interface. Access the KeePass main window, the currently opened database, etc.</param>
        /// <returns><c>true</c> if plugin should be loaded within KeePass, otherwise <c>false</c></returns>
        public override bool Initialize(IPluginHost pluginHost)
        {
            if (pluginHost == null)
            {
                return false;
            }

            _pluginHost = pluginHost;

            try
            {
                // Supported protocols for HIBP are >= TLS 1.2
                // https://haveibeenpwned.com/API/v2#HTTPS
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }
            catch (NotSupportedException)
            {
                MessageService.ShowWarning($"Can't load {Assembly.GetAssembly(typeof(HaveIBeenPwnedPluginExt)).GetName()}",
                                           $"{nameof(SecurityProtocolType)} '{nameof(SecurityProtocolType.Tls12)}' is not supported!");

                return false;
            }

            _checkPasswordOnEntryTouched = _pluginHost.CustomConfig.GetBool(Option_CheckPasswordOnEntryTouched, true);
            PwEntry.EntryTouched += PwEntry_TouchedAsync;

            return true;
        }

        private async void PwEntry_TouchedAsync(object o, ObjectTouchedEventArgs e)
        {
            if (!_checkPasswordOnEntryTouched)
            {
                return;
            }

            if (e.Modified && e.Object is PwEntry pwEntry)
            {
                await CheckHaveIBeenPwnedForEntry(pwEntry);
            }
        }

        private async Task CheckHaveIBeenPwnedForEntry(PwEntry pwEntry)
        {
            if (pwEntry == null)
            {
                throw new ArgumentNullException(nameof(pwEntry));
            }

            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var (isBreached, breachCount) = await _hibp.CheckRangeAsync(ComputeSha1HexPartsFromEntry(pwEntry, sha1));

                if (isBreached)
                {
                    UpdateEntryWithBreachInformation(pwEntry, breachCount);
                    MessageService.ShowWarning($"Oh no. This password is breached and has been seen {breachCount} times before!",
                                               $"Source: https://haveibeenpwned.com");
                }
                else
                {
                    UpdateEntryRemoveBreachInformation(pwEntry);
                }

                UpdateUI_EntryList();
            }
        }

        private void UpdateEntryRemoveBreachInformation(PwEntry pwEntry)
        {
            pwEntry.Tags.Remove(BreachedTag);
            pwEntry.CustomData.Remove(BreachCountCustomDataName);
        }

        private void UpdateEntryWithBreachInformation(PwEntry pwEntry, int breachCount)
        {
            pwEntry.AddTag(BreachedTag);

            if (breachCount > 0)
            {
                pwEntry.CustomData.Set(BreachCountCustomDataName, breachCount.ToString());
            }
        }

        public override void Terminate()
        {
            _pluginHost.CustomConfig.SetBool(Option_CheckPasswordOnEntryTouched, _checkPasswordOnEntryTouched);
            PwEntry.EntryTouched -= PwEntry_TouchedAsync;

            _hibp.Dispose();
        }

        public override ToolStripMenuItem GetMenuItem(PluginMenuType t)
        {
            // Don't add menu items to tray
            if (t == PluginMenuType.Tray)
            {
                return null;
            }

            ToolStripMenuItem pluginRootMenuItem = new ToolStripMenuItem("Have I Been Pwned");

            ToolStripMenuItem checkCurrentPasswordMenuItem = new ToolStripMenuItem
            {
                Text = "Check current password"
            };
            checkCurrentPasswordMenuItem.Click += CheckCurrentPasswordMenuItem_ClickAsync;
            pluginRootMenuItem.DropDownItems.Add(checkCurrentPasswordMenuItem);

            ToolStripMenuItem checkAllPasswordsMenuItem = new ToolStripMenuItem
            {
                Text = "Check all passwords"
            };
            checkAllPasswordsMenuItem.Click += CheckAllPasswordsMenuItem_ClickAsync;
            pluginRootMenuItem.DropDownItems.Add(checkAllPasswordsMenuItem);

            pluginRootMenuItem.DropDownItems.Add(new ToolStripSeparator());

            ToolStripMenuItem checkBoxMenuItem_Option_CheckPasswordOnEntryTouched = new ToolStripMenuItem
            {
                Text = "Check password when entry gets modified",
                Checked = _checkPasswordOnEntryTouched
            };
            checkBoxMenuItem_Option_CheckPasswordOnEntryTouched.Click += CheckBoxMenuItem_Clicked;
            pluginRootMenuItem.DropDownItems.Add(checkBoxMenuItem_Option_CheckPasswordOnEntryTouched);

            return pluginRootMenuItem;
        }

        private async void CheckCurrentPasswordMenuItem_ClickAsync(object sender, EventArgs e)
        {
            if (IsDatabaseOpen())
            {
                PwEntry selectedEntry = _pluginHost.MainWindow.GetSelectedEntry(true, false);

                if (selectedEntry != null)
                {
                    await CheckHaveIBeenPwnedForEntry(selectedEntry);
                }
                else
                {
                    MessageService.ShowInfo("No entry selected!");
                }
            }
        }

        private async void CheckAllPasswordsMenuItem_ClickAsync(object sender, EventArgs e)
        {
            if (!IsDatabaseOpen())
            {
                return;
            }

            var entries = _pluginHost.Database.RootGroup.GetEntries(true);

            int checkedEntriesCount = 0;
            int pwnedEntriesCount = 0;

            using (SHA1Managed sha1 = new SHA1Managed())
            {
                foreach (var entry in entries)
                {
                    (string sha1Prefix, string sha1Suffix) = ComputeSha1HexPartsFromEntry(entry, sha1);
                    var (isBreached, breachCount) = await _hibp.CheckRangeAsync(sha1Prefix, sha1Suffix);

                    checkedEntriesCount++;

                    if (isBreached)
                    {
                        UpdateEntryWithBreachInformation(entry, breachCount);
                        pwnedEntriesCount++;
                    }
                    else
                    {
                        UpdateEntryRemoveBreachInformation(entry);
                    }
                }
            }

            UpdateUI_EntryList();

            MessageService.ShowInfo($"Checked entries: {checkedEntriesCount}",
                                    $"Pwned entries: {pwnedEntriesCount}");
        }

        private void UpdateUI_EntryList()
        {
            _pluginHost.MainWindow?.UpdateUI(false, null, false, null, true, null, true);
        }

        private (string sha1Prefix, string sha1Suffix) ComputeSha1HexPartsFromEntry(PwEntry entry, SHA1Managed sha1Managed)
        {
            string sha1Hex = BitConverter.ToString(sha1Managed.ComputeHash(entry.Strings.GetSafe("Password").ReadUtf8())).Replace("-", string.Empty);

            return (sha1Hex.Substring(0, 5), sha1Hex.Substring(5, sha1Hex.Length - 5));
        }

        private void CheckBoxMenuItem_Clicked(object sender, EventArgs e)
        {
            _checkPasswordOnEntryTouched = !_checkPasswordOnEntryTouched;
            UIUtil.SetChecked(sender as ToolStripMenuItem, _checkPasswordOnEntryTouched);
        }

        private bool IsDatabaseOpen()
        {
            PwDatabase database = _pluginHost.Database;

            if (database != null && database.IsOpen)
            {
                return true;
            }

            return false;
        }
    }
}
